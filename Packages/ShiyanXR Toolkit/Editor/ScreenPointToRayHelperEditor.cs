using UnityEditor;
using UnityEngine;

namespace com.shiyanxr.editor
{
    /// <summary>
    /// 在 Scene 视图可视化 ScreenPointToRayHelper 的 projectionMatrix：
    /// 由投影矩阵反投影出视锥体的近平面、远平面与四条侧棱。
    /// 矩阵直接向组件要（按当前 provider 现算），保证与发射线用的是同一对。
    /// </summary>
    [CustomEditor(typeof(ScreenPointToRayHelper))]
    public class ScreenPointToRayHelperEditor : Editor
    {
        static readonly Color k_NearPlaneColor = new Color(1f, 0.85f, 0.2f);
        static readonly Color k_FarPlaneColor = new Color(1f, 0.5f, 0.1f);

        static GUIStyle s_LabelStyle;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var helper = (ScreenPointToRayHelper)target;
            // 现算当前 provider 下的 V / P：改 Inspector 参数后立刻生效
            helper.RefreshMatrices();
            var projection = helper.currentProjectionMatrix;

            // 近平面尺寸由投影矩阵反投影得到，不再依赖 Constant 那套固定公式
            var inverseProjection = projection.inverse;
            var nearBottomLeft = inverseProjection.MultiplyPoint(new Vector3(-1f, -1f, -1f));
            var nearTopRight = inverseProjection.MultiplyPoint(new Vector3(1f, 1f, -1f));
            float nearWidth = nearTopRight.x - nearBottomLeft.x;
            float nearHeight = nearTopRight.y - nearBottomLeft.y;

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Scene 视图（选中本对象时绘制）：黄色为近平面，橙色为远平面，" +
                "四角连线来自 projectionMatrix 的角点反投影。",
                MessageType.Info);
        }

        void OnSceneGUI()
        {
            var helper = target as ScreenPointToRayHelper;
            if (helper == null || helper.transform == null)
                return;

            // 投影矩阵 * 视图矩阵的逆矩阵负责把裁剪空间角点还原到世界空间
            helper.RefreshMatrices();
            var inverseViewProjection = (helper.currentProjectionMatrix * helper.currentViewMatrix).inverse;
            var nearCorners = UnprojectQuad(inverseViewProjection, -1f);
            var farCorners = UnprojectQuad(inverseViewProjection, 1f);

            Handles.color = k_NearPlaneColor;
            DrawQuad(nearCorners);
            Handles.color = k_FarPlaneColor;
            DrawQuad(farCorners);

            // 四条侧棱
            for (int i = 0; i < 4; i++)
            {
                Handles.DrawLine(nearCorners[i], farCorners[i]);
            }

                var labelStyle = LabelStyle();
                Handles.Label(QuadCenter(nearCorners), $"near {helper.nearClip:G4}", labelStyle);
                Handles.Label(QuadCenter(farCorners), $"far {helper.farClip:G4}", labelStyle);
        }

        /// <summary>
        /// 裁剪空间四角（ndcZ = -1 近平面，1 远平面）反投影到世界空间。
        /// MultiplyPoint 会自动做透视除法。
        /// </summary>
        static Vector3[] UnprojectQuad(Matrix4x4 inverseViewProjection, float ndcZ)
        {
            return new[]
            {
                inverseViewProjection.MultiplyPoint(new Vector3(-1f, -1f, ndcZ)),
                inverseViewProjection.MultiplyPoint(new Vector3(1f, -1f, ndcZ)),
                inverseViewProjection.MultiplyPoint(new Vector3(1f, 1f, ndcZ)),
                inverseViewProjection.MultiplyPoint(new Vector3(-1f, 1f, ndcZ)),
            };
        }

        static void DrawQuad(Vector3[] corners)
        {
            for (int i = 0; i < 4; i++)
            {
                Handles.DrawLine(corners[i], corners[(i + 1) % 4]);
            }
        }

        static Vector3 QuadCenter(Vector3[] corners)
        {
            return (corners[0] + corners[1] + corners[2] + corners[3]) * 0.25f;
        }

        static GUIStyle LabelStyle()
        {
            if (s_LabelStyle == null)
            {
                s_LabelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                };
                s_LabelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
            }

            return s_LabelStyle;
        }
    }
}
