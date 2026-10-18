using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using com.unity.shiyantech.xr;

namespace com.shiyanxr.editor
{
    /// <summary>
    /// Game View 眼视图预览：以 XR Origin (SY)/Camera Offset/Main Camera 为准，
    /// 按 Project Settings &gt; 视延XR项目配置 里的屏幕物理尺寸 + 面板上的 View / 会聚面距离 / 瞳距，
    /// 给 Main Camera 写离轴 V/P。
    ///
    /// Left / Right：该眼的离轴矩阵；Center：眼偏移取 0 的左右对称矩阵。
    /// 这三个都是 mono 矩阵（worldToCameraMatrix / projectionMatrix），编辑态与运行态都被遵守，
    /// 不依赖 XR，也不新建相机。
    ///
    /// Both：一帧要出两张不同的投影，单台相机做不到，所以走 URP 的
    /// RenderPipeline.SubmitRenderRequest 把同一台相机渲两遍到左右两张 RT，拼成双宽贴图后，
    /// 由 ShiyanXRSbsPresentFeature（URP RendererFeature）在管线内贴进相机颜色目标 —— 不需要第二台相机。
    ///
    /// 两者不能漂移：Scene View 里画出的会聚面与投影用的是同一套几何。
    ///
    /// 另外在 Scene View 里画出会聚面（零视差面）与左右眼视锥（近平面 / 远平面 / 4 条侧边），
    /// 几何都与投影共用同一处定义，便于对照它们在场景中的实际位置与大小。
    ///
    /// 本文件只在编辑器程序集里（构建不含它），真机构建不受影响。
    /// </summary>
    [InitializeOnLoad]
    static class ShiyanXREyeStereoPreview
    {
        const float k_LeftEyeSign = -1f;
        const float k_RightEyeSign = 1f;
        const float k_CenterEyeSign = 0f;
        const string k_ComposeCommandName = "ShiyanXR SBS Compose";
        // Scene View 每帧都要重画这些角点，复用缓冲，避免每帧分配
        static readonly Vector3[] s_PlaneCorners = new Vector3[4];
        // 视锥的近 / 远平面角点：两只眼依次绘制，各一块就够
        static readonly Vector3[] s_NearCorners = new Vector3[4];
        static readonly Vector3[] s_FarCorners = new Vector3[4];

        static RenderTexture s_LeftEyeTexture;
        static RenderTexture s_RightEyeTexture;
        // SBS 合成用：左右眼各拷进它的左/右半，再整屏贴给相机目标（避开 Blit 不认 SetViewport 的问题）
        static RenderTexture s_SbsTexture;
        static bool s_PresenterChecked;

        // 上一次驱动时的面板状态：mono 模式下 Game View 不会自己刷新，状态变了才请求重绘
        static int s_LastView = int.MinValue;
        static float s_LastDistance = float.NaN;
        static float s_LastIpd = float.NaN;

        static ShiyanXREyeStereoPreview()
        {
            EditorApplication.update += OnEditorUpdate;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            SceneView.duringSceneGui += OnSceneGui;
            AssemblyReloadEvents.beforeAssemblyReload += ReleasePreviewTextures;
        }

        static void OnEditorUpdate()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                // 贴图要销毁了，得先把桥断开，否则 Feature 会去贴一张已释放的 RT
                ShiyanXRSbsPresentFeature.Source = null;
                ShiyanXRSbsPresentFeature.TargetCamera = null;
                ReleasePreviewTextures();
                return;
            }

            int view = ShiyanXREyePanel.s_View;
            if (view == ShiyanXREyePanel.ViewBoth)
            {
                // 相机自身先按左眼渲染（随后会被 SBS 呈现整屏覆盖），两张 RT 由我们渲染
                ApplyCurrentEye(camera);
                RenderEyeTextures(camera);

                // 先接好桥再接安装：安装要改 renderer 资产，万一抛异常也不至于把预览整个搭进去
                ShiyanXRSbsPresentFeature.TargetCamera = camera;
                ShiyanXRSbsPresentFeature.Source = s_SbsTexture;

                // 呈现 Feature 必须挂在 renderer 资产上才会入队，这一步确保装上
                if (!s_PresenterChecked)
                {
                    s_PresenterChecked = true;
                    ShiyanXRSbsFeatureInstaller.EnsureInstalled();
                }

                // 像素不再由相机自身的渲染给出，得持续驱动 Game View 重绘
                EditorApplication.QueuePlayerLoopUpdate();
            }
            else
            {
                ShiyanXRSbsPresentFeature.Source = null;
                ShiyanXRSbsPresentFeature.TargetCamera = null;
                s_PresenterChecked = false;
                ReleasePreviewTextures();
                ApplyCurrentEye(camera);
                // 从 Both 切回来时像素来源变了（不再由 Feature 提供），强制请求一次重绘
                s_LastView = int.MinValue;
                RepaintOnStateChange(view);
            }
        }

        static void RepaintOnStateChange(int view)
        {
            if (s_LastView == view
                && s_LastDistance.Equals(ShiyanXREyePanel.s_ConvergencePlaneDistance)
                && s_LastIpd.Equals(ShiyanXREyePanel.s_InterocularDistance))
            {
                return;
            }

            s_LastView = view;
            s_LastDistance = ShiyanXREyePanel.s_ConvergencePlaneDistance;
            s_LastIpd = ShiyanXREyePanel.s_InterocularDistance;
            EditorApplication.QueuePlayerLoopUpdate();
        }

        static float EyeSignOf(int view)
        {
            switch (view)
            {
                case ShiyanXREyePanel.ViewLeft: return k_LeftEyeSign;
                case ShiyanXREyePanel.ViewRight: return k_RightEyeSign;
                default: return k_CenterEyeSign;
            }
        }

        /// <summary>给相机写当前 View 对应的眼位矩阵。Both 下相机自身按左眼渲染，随后被 SBS 呈现整屏覆盖。</summary>
        static void ApplyCurrentEye(Camera camera)
        {
            int view = ShiyanXREyePanel.s_View;
            ApplyEye(camera, view == ShiyanXREyePanel.ViewBoth ? k_LeftEyeSign : EyeSignOf(view));
        }

        /// <summary>
        /// 相机开始渲染前再写一次眼位矩阵。管线在 BeginCameraRendering 之后才初始化相机数据
        /// （UniversalRenderPipeline：BeginCameraRendering → InitializeCameraData → RenderSingleCamera），
        /// 所以这里写的矩阵这一帧就会被用上。
        ///
        /// 为什么不能只靠 EditorApplication.update：那是编辑器 tick，进入 Play 的头几帧可能还没轮到它，
        /// 或 Unity 因游戏视图分辨率变化重置了相机的投影矩阵 —— 那几帧就会拿相机自带的投影渲染
        /// （本工程 Main Camera 的 fov 是 12.8°，与眼位投影差异明显），看起来就是先一个不对的大小、随后才跳到正常。
        /// </summary>
        static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            // 只认主相机；眼图自身渲染期间必须跳过 —— 那两次渲染各自写了自己的眼位矩阵，
            // 在这里覆盖会把右眼渲染成左眼（Suppressed 正是那段渲染的守卫）
            if (camera != Camera.main) return;
            if (ShiyanXRSbsPresentFeature.Suppressed) return;

            ApplyCurrentEye(camera);
        }

        /// <summary>会聚面到相机的距离：面板值取绝对值，并夹住下限——它要做除数。</summary>
        static float ConvergencePlaneDistance() =>
            Mathf.Max(Mathf.Abs(ShiyanXREyePanel.s_ConvergencePlaneDistance), 1e-4f);

        /// <summary>会聚面中心（世界坐标）：相机正前方 distance 处。画布工具也用它定位，避免两处各算一遍。</summary>
        internal static Vector3 ConvergencePlaneCenter(Camera camera) =>
            camera.transform.position + camera.transform.forward * ConvergencePlaneDistance();

        /// <summary>会聚面的半宽 / 半高（米），取项目设置里的屏幕物理尺寸。</summary>
        static void ConvergencePlaneHalfSize(out float halfWidth, out float halfHeight)
        {
            var settings = ShiyanXRProjectSettings.Instance;
            halfWidth = settings.ScreenPhysicalWidth * 0.5f;
            halfHeight = settings.ScreenPhysicalHeight * 0.5f;
        }

        /// <summary>某个眼位的世界位置：沿相机右轴偏移 sign * IPD / 2。</summary>
        static Vector3 EyePosition(Transform cameraTransform, float eyeSign)
        {
            float offset = eyeSign * ShiyanXREyePanel.s_InterocularDistance * 0.5f;
            return cameraTransform.position + cameraTransform.right * offset;
        }

        /// <summary>
        /// 某个眼位在“距眼睛 atDistance”处的视锥四条边界（相机空间 x/y）。
        /// 写投影矩阵（ApplyEye）与 Scene View 里画视锥都用这一处，画出来的就是实际渲染的视锥。
        /// </summary>
        static void EyeFrustumEdges(float eyeSign, float atDistance, out float left, out float right, out float top, out float bottom)
        {
            ConvergencePlaneHalfSize(out float halfWidth, out float halfHeight);
            float offset = eyeSign * ShiyanXREyePanel.s_InterocularDistance * 0.5f;
            float scale = atDistance / ConvergencePlaneDistance();

            left = (-halfWidth - offset) * scale;
            right = (halfWidth - offset) * scale;
            top = halfHeight * scale;
            bottom = -halfHeight * scale;
        }

        /// <summary>某个眼位在“距眼睛 atDistance”处的视锥四角（世界坐标），顺序：左下、右下、右上、左上。</summary>
        static void EyeFrustumCorners(Camera camera, float eyeSign, float atDistance, Vector3[] corners)
        {
            EyeFrustumEdges(eyeSign, atDistance, out float left, out float right, out float top, out float bottom);

            Transform cameraTransform = camera.transform;
            Vector3 center = EyePosition(cameraTransform, eyeSign) + cameraTransform.forward * atDistance;
            Vector3 rightAxis = cameraTransform.right;
            Vector3 upAxis = cameraTransform.up;

            corners[0] = center + rightAxis * left + upAxis * bottom;  // 左下
            corners[1] = center + rightAxis * right + upAxis * bottom; // 右下
            corners[2] = center + rightAxis * right + upAxis * top;    // 右上
            corners[3] = center + rightAxis * left + upAxis * top;     // 左上
        }

        /// <summary>
        /// 给 Main Camera 写某个眼位（-1 左眼 / 0 眉心 / +1 右眼）的离轴 V/P。
        /// 会聚面在相机正前方 ConvergencePlaneDistance 处、尺寸取项目设置里的屏幕物理尺寸；
        /// 眼位沿相机右轴偏移 sign * IPD / 2，视锥按 near / 眼到会聚面距离 缩放，
        /// 与 ShiyanDevicesSimulator 的离轴投影是同一套公式。
        /// </summary>
        static void ApplyEye(Camera camera, float eyeSign)
        {
            Transform cameraTransform = camera.transform;

            // 复用包内既有的视图矩阵构造（与 SyUIEventCamera 写 worldToCameraMatrix 用的是同一个）
            camera.worldToCameraMatrix =
                ScreenPointToRayHelper.CreateViewMatrix(EyePosition(cameraTransform, eyeSign), cameraTransform.forward, cameraTransform.up);

            // 视锥边界与 Scene View 里画的视锥共用同一处定义（EyeFrustumEdges），避免两处漂移
            float near = camera.nearClipPlane;
            EyeFrustumEdges(eyeSign, near, out float left, out float right, out float top, out float bottom);
            camera.projectionMatrix = Matrix4x4.Frustum(left, right, bottom, top, near, camera.farClipPlane);
        }

        /// <summary>Scene View 里的参考图形：会聚面（零视差面）与左右眼视锥，各自按面板上的开关绘制。</summary>
        static void OnSceneGui(SceneView sceneView)
        {
            var camera = Camera.main;
            if (camera == null) return;

            if (ShiyanXREyePanel.s_ConvergencePlaneAlpha > 0f)
                DrawConvergencePlane(camera);

            if (ShiyanXREyePanel.s_DisplayCameras)
                DrawEyeFrustums(camera);
        }

        /// <summary>
        /// 会聚面（零视差面）：相机正前方 distance 处，尺寸取项目设置的屏幕物理尺寸，与 ApplyEye 用的是同一套几何。
        ///
        /// 注意面心取相机的 position 而不是眼位：ApplyEye 的视锥左右边界是 (-halfWidth - offset)
        /// 与 (halfWidth - offset)，offset 正好抵消——所以这个面永远以两眼中点为中心，它就是零视差面。
        /// 颜色与透明度都取面板上的 Convergence Plane Color / Alpha。这个面只是编辑器里的参考图形，不参与渲染。
        ///
        /// 绘制时开启深度测试：Scene View 的 Handles 默认不测深度（zTest = Always），整个面会盖在场景之上，
        /// 把位于它前方的物体遮住 —— 从正面看就会把出屏看成入屏。
        /// </summary>
        static void DrawConvergencePlane(Camera camera)
        {
            ConvergencePlaneHalfSize(out float halfWidth, out float halfHeight);

            Transform cameraTransform = camera.transform;
            Vector3 center = ConvergencePlaneCenter(camera);
            Vector3 right = cameraTransform.right * halfWidth;
            Vector3 up = cameraTransform.up * halfHeight;

            s_PlaneCorners[0] = center - right - up; // 左下
            s_PlaneCorners[1] = center + right - up; // 右下
            s_PlaneCorners[2] = center + right + up; // 右上
            s_PlaneCorners[3] = center - right + up; // 左上

            var planeColor = ShiyanXREyePanel.s_ConvergencePlaneColor;
            var color = new Color(planeColor.r, planeColor.g, planeColor.b, ShiyanXREyePanel.s_ConvergencePlaneAlpha);

            // 让平面像普通物体一样被它前方的几何遮挡；用完恢复，不干扰其它 Scene View 绘制（如上面那套视锥）
            var previousZTest = Handles.zTest;
            Handles.zTest = CompareFunction.LessEqual;
            Handles.DrawSolidRectangleWithOutline(s_PlaneCorners, color, color);
            Handles.zTest = previousZTest;
        }

        /// <summary>
        /// 左右眼视锥：近平面、远平面，以及连接两者的 4 条侧边。
        /// 几何与 ApplyEye 写进投影矩阵的那套完全一致（共用 EyeFrustumEdges），所以画出来就是实际渲染的眼位视锥。
        /// 颜色取面板上的 Left / Right Camera Color。
        /// </summary>
        static void DrawEyeFrustums(Camera camera)
        {
            DrawEyeFrustum(camera, k_LeftEyeSign, ShiyanXREyePanel.s_LeftCameraColor);
            DrawEyeFrustum(camera, k_RightEyeSign, ShiyanXREyePanel.s_RightCameraColor);
        }

        static void DrawEyeFrustum(Camera camera, float eyeSign, Color color)
        {
            float near = camera.nearClipPlane;
            float far = camera.farClipPlane;

            EyeFrustumCorners(camera, eyeSign, near, s_NearCorners);
            EyeFrustumCorners(camera, eyeSign, far, s_FarCorners);

            Handles.color = color;
            DrawQuad(s_NearCorners);
            DrawQuad(s_FarCorners);

            // 4 条侧边
            for (int i = 0; i < 4; i++)
                Handles.DrawLine(s_NearCorners[i], s_FarCorners[i]);
        }

        static void DrawQuad(Vector3[] corners)
        {
            for (int i = 0; i < 4; i++)
                Handles.DrawLine(corners[i], corners[(i + 1) % 4]);
        }

        static void RenderEyeTextures(Camera camera)
        {
            // SBS 里每眼占半屏，所以按半宽渲染
            int halfWidth = Mathf.Max(2, camera.pixelWidth / 2);
            int height = Mathf.Max(2, camera.pixelHeight);

            EnsureEyeTexture(ref s_LeftEyeTexture, halfWidth, height, "ShiyanXR SBS Left");
            EnsureEyeTexture(ref s_RightEyeTexture, halfWidth, height, "ShiyanXR SBS Right");
            EnsureEyeTexture(ref s_SbsTexture, halfWidth * 2, height, "ShiyanXR SBS Combined", 0);

            // 这两次渲染用的是同一台 Main Camera，放任 Feature 一起呈现的话，
            // 会在渲染 RT 的中途往 Game View 上画
            ShiyanXRSbsPresentFeature.Suppressed = true;
            try
            {
                RenderEye(camera, s_LeftEyeTexture, k_LeftEyeSign);
                RenderEye(camera, s_RightEyeTexture, k_RightEyeSign);
            }
            finally
            {
                // 用 finally：中途抛异常也不能让守卫卡住，否则呈现会被永久跳过
                ShiyanXRSbsPresentFeature.Suppressed = false;
            }

            ComposeSbs(halfWidth, height);
        }

        /// <summary>
        /// 把左右眼并排拷进双宽贴图。Blit 不认 SetViewport，两半分开贴会互相整屏覆盖，所以用像素拷贝拼。
        /// </summary>
        static void ComposeSbs(int halfWidth, int height)
        {
            var cmd = new CommandBuffer { name = k_ComposeCommandName };
            cmd.CopyTexture(s_LeftEyeTexture, 0, 0, 0, 0, halfWidth, height, s_SbsTexture, 0, 0, 0, 0);
            cmd.CopyTexture(s_RightEyeTexture, 0, 0, 0, 0, halfWidth, height, s_SbsTexture, 0, 0, halfWidth, 0);
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();
        }

        static void RenderEye(Camera camera, RenderTexture destination, float eyeSign)
        {
            ApplyEye(camera, eyeSign);

            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = destination };
            RenderPipeline.SubmitRenderRequest(camera, request);
        }

        static void EnsureEyeTexture(ref RenderTexture texture, int width, int height, string name, int depth = 24)
        {
            if (texture != null && texture.width == width && texture.height == height) return;

            ReleaseTexture(ref texture);
            texture = new RenderTexture(width, height, depth) { name = name };
            texture.Create();
        }

        static void ReleasePreviewTextures()
        {
            ReleaseTexture(ref s_LeftEyeTexture);
            ReleaseTexture(ref s_RightEyeTexture);
            ReleaseTexture(ref s_SbsTexture);
        }

        static void ReleaseTexture(ref RenderTexture texture)
        {
            if (texture == null) return;

            texture.Release();
            Object.DestroyImmediate(texture);
            texture = null;
        }
    }
}
