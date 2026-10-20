using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using com.unity.shiyantech.xr;

namespace com.shiyanxr.editor
{
    /// <summary>
    /// Scene View 里的光学参考图形：会聚面（零视差面）与左右眼视锥。
    ///
    /// 几何都由 Project Settings &gt; 视延XR项目配置 里的屏幕物理尺寸，加上面板上的
    /// 会聚面距离 / 瞳距 / 颜色决定。会聚面的中心与尺寸也供 ShiyanXRMenuTools 摆放画布使用，
    /// 两处共用同一套定义，避免各算一遍而漂移。
    ///
    /// 会聚面就是裸眼 3D 屏幕本身，锚在 Main Camera 的 Transform 位置上，不随会聚面距离移动；
    /// 会聚面距离是“眼睛到屏幕”的观看距离，改变它时移动的是左右眼的位置。
    ///
    /// 编辑器的非播放模式下还会改写 Main Camera 的 V/P：视点放在左右眼中点（眉心），
    /// 投影用与左右眼同源的离轴矩阵，视锥在会聚面处正好覆盖屏幕。播放模式与构建里不生效。
    ///
    /// 左右眼视图（Left / Right / Both）仍由 Unity XR 模式提供；这里负责眉心预览与参考图形的绘制。
    ///
    /// 本文件只在编辑器程序集里（构建不含它）。
    /// </summary>
    [InitializeOnLoad]
    static class ShiyanXREyeStereoPreview
    {
        const float k_LeftEyeSign = -1f;
        const float k_CenterEyeSign = 0f;
        const float k_RightEyeSign = 1f;
        // Scene View 每帧都要重画这些角点，复用缓冲，避免每帧分配
        static readonly Vector3[] s_PlaneCorners = new Vector3[4];
        // 视锥的近 / 远平面角点：两只眼依次绘制，各一块就够
        static readonly Vector3[] s_NearCorners = new Vector3[4];
        static readonly Vector3[] s_FarCorners = new Vector3[4];
        // 被改写过 V/P 的 Main Camera：进 Play 或域重载前要把矩阵还原，避免影响实际运行
        static Camera s_OverriddenCamera;

        static ShiyanXREyeStereoPreview()
        {
            SceneView.duringSceneGui += OnSceneGui;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += RestoreCameraMatrices;
        }

        /// <summary>眼睛到会聚面（屏幕）的距离：面板值取绝对值，并夹住下限——它要做除数。</summary>
        static float ConvergencePlaneDistance() =>
            Mathf.Max(Mathf.Abs(ShiyanXREyePanel.State.convergencePlaneDistance), 1e-4f);

        /// <summary>
        /// 会聚面中心（世界坐标）：固定在相机 Transform 的位置，也就是裸眼 3D 屏幕所在处，不随观看距离移动。
        /// 画布工具也用它定位，避免两处各算一遍。
        /// </summary>
        internal static Vector3 ConvergencePlaneCenter(Camera camera) =>
            camera.transform.position;

        /// <summary>会聚面的半宽 / 半高（米），取项目设置里的屏幕物理尺寸。</summary>
        static void ConvergencePlaneHalfSize(out float halfWidth, out float halfHeight)
        {
            var settings = ShiyanXRProjectSettings.Instance;
            halfWidth = settings.ScreenPhysicalWidth * 0.5f;
            halfHeight = settings.ScreenPhysicalHeight * 0.5f;
        }

        /// <summary>
        /// 某个眼位的世界位置：从屏幕中心（相机位置）沿视线反方向后退观看距离，再沿相机右轴偏移 sign * IPD / 2。
        /// 屏幕固定不动，改变观看距离时移动的是眼睛。
        /// </summary>
        static Vector3 EyePosition(Transform cameraTransform, float eyeSign)
        {
            float offset = eyeSign * ShiyanXREyePanel.State.interocularDistance * 0.5f;
            return cameraTransform.position
                   - cameraTransform.forward * ConvergencePlaneDistance()
                   + cameraTransform.right * offset;
        }

        /// <summary>
        /// 某个眼位在“距眼睛 atDistance”处的视锥四条边界（相机空间 x/y）。
        /// 会聚面与视锥共用这一处几何：atDistance 取观看距离时，边界正好落在屏幕（会聚面）上。
        /// </summary>
        static void EyeFrustumEdges(float eyeSign, float atDistance, out float left, out float right, out float top, out float bottom)
        {
            ConvergencePlaneHalfSize(out float halfWidth, out float halfHeight);
            float offset = eyeSign * ShiyanXREyePanel.State.interocularDistance * 0.5f;
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

        /// <summary>Scene View 里的参考图形：会聚面（零视差面）与左右眼视锥，各自按面板上的开关绘制。</summary>
        static void OnSceneGui(SceneView sceneView)
        {
            var camera = Camera.main;
            if (camera == null) return;

            if (ShiyanXREyePanel.State.convergencePlaneAlpha > 0f)
                DrawConvergencePlane(camera);

            if (ShiyanXREyePanel.State.displayCameras)
                DrawEyeFrustums(camera);
        }

        /// <summary>
        /// 编辑器非播放模式下改写 Main Camera：视点放在左右眼中点（眉心），
        /// 投影用与左右眼同源的离轴矩阵，视锥在会聚面处正好覆盖屏幕。
        /// 播放模式 / 构建里不生效，相机仍按 Unity 自己的 Transform 与 FOV 计算矩阵。
        /// </summary>
        static void OnEditorUpdate()
        {
            // 进入 Play 的切换阶段就还原，避免改写过的矩阵被带进实际运行
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                RestoreCameraMatrices();
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                RestoreCameraMatrices();
                return;
            }

            // 场景里换了主相机：先把旧相机还原，避免它一直带着改写过的矩阵
            if (s_OverriddenCamera != null && s_OverriddenCamera != camera)
                RestoreCameraMatrices();

            ApplyCenterEye(camera);
            s_OverriddenCamera = camera;
        }

        /// <summary>
        /// 给 Main Camera 写眉心（左右眼中点）的离轴 V/P：与左右眼共用 EyePosition / EyeFrustumEdges。
        /// 只改矩阵、不动 Transform —— Transform 仍是屏幕（会聚面）的锚点。
        /// </summary>
        static void ApplyCenterEye(Camera camera)
        {
            Transform cameraTransform = camera.transform;

            // sign = 0 时 EyePosition 正好是左右眼中点，也就是“观看距离处、屏幕正前方”的视点
            camera.worldToCameraMatrix = ScreenPointToRayHelper.CreateViewMatrix(
                EyePosition(cameraTransform, k_CenterEyeSign),
                cameraTransform.forward,
                cameraTransform.up);

            // 与左右眼视锥共用边界定义：atDistance 取 near，视锥在会聚面处正好覆盖屏幕
            float near = camera.nearClipPlane;
            EyeFrustumEdges(k_CenterEyeSign, near, out float left, out float right, out float top, out float bottom);
            camera.projectionMatrix = Matrix4x4.Frustum(left, right, bottom, top, near, camera.farClipPlane);
        }

        /// <summary>把改写过的 V/P 还给 Unity：清掉覆盖后相机会重新按 Transform / FOV 计算。</summary>
        static void RestoreCameraMatrices()
        {
            var camera = s_OverriddenCamera;
            s_OverriddenCamera = null;
            if (camera == null) return;

            camera.ResetWorldToCameraMatrix();
            camera.ResetProjectionMatrix();
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                RestoreCameraMatrices();
        }

        /// <summary>
        /// 会聚面（零视差面）：固定在相机位置，也就是裸眼 3D 屏幕所在处，尺寸取项目设置的屏幕物理尺寸。
        ///
        /// 注意面心取相机的 position 而不是眼位：视锥左右边界是 (-halfWidth - offset)
        /// 与 (halfWidth - offset)，offset 正好抵消——所以这个面永远以两眼中点为中心，它就是零视差面。
        /// 改变观看距离时这个面不动，动的是左右眼位置。
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

            var planeColor = ShiyanXREyePanel.State.convergencePlaneColor;
            var color = new Color(planeColor.r, planeColor.g, planeColor.b, ShiyanXREyePanel.State.convergencePlaneAlpha);

            // 让平面像普通物体一样被它前方的几何遮挡；用完恢复，不干扰其它 Scene View 绘制（如上面那套视锥）
            var previousZTest = Handles.zTest;
            Handles.zTest = CompareFunction.LessEqual;
            Handles.DrawSolidRectangleWithOutline(s_PlaneCorners, color, color);
            Handles.zTest = previousZTest;
        }

        /// <summary>
        /// 左右眼视锥：近平面、远平面，以及连接两者的 4 条侧边。
        /// 几何与上面那个会聚面共用同一套定义（EyeFrustumEdges），所以画出来就是该会聚面对应的眼位视锥。
        /// 颜色取面板上的 Left / Right Camera Color。
        /// </summary>
        static void DrawEyeFrustums(Camera camera)
        {
            DrawEyeFrustum(camera, k_LeftEyeSign, ShiyanXREyePanel.State.leftCameraColor);
            DrawEyeFrustum(camera, k_RightEyeSign, ShiyanXREyePanel.State.rightCameraColor);
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
    }
}
