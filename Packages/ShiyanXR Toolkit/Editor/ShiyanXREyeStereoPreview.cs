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
    /// 眼视图本身（Left / Right / Center / Both）由 Unity XR 模式提供，这里不再给相机写 V/P 矩阵。
    ///
    /// 本文件只在编辑器程序集里（构建不含它）。
    /// </summary>
    [InitializeOnLoad]
    static class ShiyanXREyeStereoPreview
    {
        const float k_LeftEyeSign = -1f;
        const float k_RightEyeSign = 1f;
        // Scene View 每帧都要重画这些角点，复用缓冲，避免每帧分配
        static readonly Vector3[] s_PlaneCorners = new Vector3[4];
        // 视锥的近 / 远平面角点：两只眼依次绘制，各一块就够
        static readonly Vector3[] s_NearCorners = new Vector3[4];
        static readonly Vector3[] s_FarCorners = new Vector3[4];

        static ShiyanXREyeStereoPreview()
        {
            SceneView.duringSceneGui += OnSceneGui;
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
        /// 会聚面与视锥共用这一处几何，画出来的就是会聚面对应的视锥。
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
        /// 会聚面（零视差面）：相机正前方 distance 处，尺寸取项目设置的屏幕物理尺寸。
        ///
        /// 注意面心取相机的 position 而不是眼位：视锥左右边界是 (-halfWidth - offset)
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
        /// 几何与上面那个会聚面共用同一套定义（EyeFrustumEdges），所以画出来就是该会聚面对应的眼位视锥。
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
    }
}
