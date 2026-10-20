using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;

namespace com.shiyanxr.editor
{
    /// <summary>
    /// Scene View 工具栏里的视延XR入口：一个图标按钮，点击弹出设置面板。
    ///
    /// 放在 Scene View 工具栏而不是 Tools 菜单，也不是 Game View 工具栏：
    /// GameView 没有官方扩展点（工具栏由私有 DoToolbarGUI 绘制，Overlays 的 ISupportsOverlays 只有 SceneView 实现），
    /// 而 SceneView 支持 Overlays，工具栏条目用 ToolbarOverlay + [EditorToolbarElement] 即可——
    /// 与 Unity 自带的 TransformTools / ViewOptions / GridAndSnap 工具栏条目同一条路径。
    /// 结构参照 AR Foundation 的 XREnvironmentToolbarOverlay。
    /// </summary>
    [Overlay(typeof(SceneView), ShiyanXREyeToolbarOverlay.k_Id, "视延XR Eye View", true)]
    sealed class ShiyanXREyeToolbarOverlay : ToolbarOverlay
    {
        public const string k_Id = "ShiyanXR.EyeToolbar";

        ShiyanXREyeToolbarOverlay() : base(new[] { ShiyanXREyeButton.k_Id }) { }
    }

    /// <summary>工具栏入口按钮：只有图标，点击弹出 <see cref="ShiyanXREyePanel"/>。</summary>
    [EditorToolbarElement(ShiyanXREyeButton.k_Id, typeof(SceneView))]
    sealed class ShiyanXREyeButton : EditorToolbarButton
    {
        public const string k_Id = ShiyanXREyeToolbarOverlay.k_Id + "/EyeView";

        public ShiyanXREyeButton()
        {
            // 占位图标：先借 Unity 内置图标，之后替换成插件自己的图标资源。
            // 取不到就退回文字，避免入口变成一个看不见东西的空按钮。
            var builtinIcon = EditorGUIUtility.IconContent("Camera Icon").image as Texture2D;
            if (builtinIcon != null)
                icon = builtinIcon;
            else
                text = "Eye";

            tooltip = "视延XR：会聚面与眼视参考图形设置";
            clicked += () => ShiyanXREyePanel.Show(worldBound);
        }
    }

    /// <summary>
    /// 弹出的设置面板：会聚面距离 / 瞳距 / 显示相机与左右相机颜色 / 会聚面颜色与透明度。
    /// 会聚面距离是眼睛到会聚面（裸眼 3D 屏幕）的观看距离：屏幕固定在 Main Camera 的位置，
    /// 改变距离时移动的是左右眼位置。用 PopupWindow 承载——面板控件在常规编辑器 GUI 里绘制
    /// （不在 overlay 的事件管线里），这也是 Input System、Perforce 等包在工具栏里弹面板的既有做法。
    /// </summary>
    sealed class ShiyanXREyePanel : PopupWindowContent
    {
        // 面板状态：会聚面距离（眼睛到屏幕的观看距离）/ 瞳距 / DisplayCameras 与三个颜色、透明度，
        // 由 ShiyanXREyeStereoPreview 读走，用于 Scene View 里画的参考图形（左右眼视锥 / 会聚面）。
        // 这些字段必须活过域重载，否则进入 Play 时会退回默认值 —— 存续机制见下面的静态构造函数。
        internal static float s_ConvergencePlaneDistance = 1f;
        internal static float s_InterocularDistance = 0.065f;
        internal static bool s_DisplayCameras = true;
        // 默认取红 / 蓝：与 ShiyanDevicesSimulator 的 leftCameraColor / rightCameraColor 同一视觉约定
        internal static Color s_LeftCameraColor = Color.red;
        internal static Color s_RightCameraColor = Color.blue;
        // 默认取绿色：与 ShiyanDevicesSimulator 的 screenColor（屏幕 / 焦平面）同一视觉约定
        internal static Color s_ConvergencePlaneColor = Color.cyan;
        internal static float s_ConvergencePlaneAlpha = 0.3f;

        const string k_SessionPrefix = "ShiyanXR.EyePanel.";

        /// <summary>
        /// 域重载会把静态字段清零，而进 Play 就会发生一次域重载——不恢复的话面板会退回默认值。
        /// 所以状态改存 SessionState：它跨域重载存活，编辑器关闭才清。
        /// </summary>
        static ShiyanXREyePanel()
        {
            s_ConvergencePlaneDistance = SessionState.GetFloat(k_SessionPrefix + "ConvergencePlaneDistance", s_ConvergencePlaneDistance);
            s_InterocularDistance = SessionState.GetFloat(k_SessionPrefix + "InterocularDistance", s_InterocularDistance);
            s_DisplayCameras = SessionState.GetBool(k_SessionPrefix + "DisplayCameras", s_DisplayCameras);
            s_ConvergencePlaneAlpha = SessionState.GetFloat(k_SessionPrefix + "ConvergencePlaneAlpha", s_ConvergencePlaneAlpha);
            s_LeftCameraColor = LoadColor("LeftCameraColor", s_LeftCameraColor);
            s_RightCameraColor = LoadColor("RightCameraColor", s_RightCameraColor);
            s_ConvergencePlaneColor = LoadColor("ConvergencePlaneColor", s_ConvergencePlaneColor);
        }

        static void SaveState()
        {
            SessionState.SetFloat(k_SessionPrefix + "ConvergencePlaneDistance", s_ConvergencePlaneDistance);
            SessionState.SetFloat(k_SessionPrefix + "InterocularDistance", s_InterocularDistance);
            SessionState.SetBool(k_SessionPrefix + "DisplayCameras", s_DisplayCameras);
            SessionState.SetFloat(k_SessionPrefix + "ConvergencePlaneAlpha", s_ConvergencePlaneAlpha);
            SaveColor("LeftCameraColor", s_LeftCameraColor);
            SaveColor("RightCameraColor", s_RightCameraColor);
            SaveColor("ConvergencePlaneColor", s_ConvergencePlaneColor);
        }

        // SessionState 没有颜色类型，借 HTML 串（含 alpha）存取
        static Color LoadColor(string key, Color fallback) =>
            ColorUtility.TryParseHtmlString(SessionState.GetString(k_SessionPrefix + key, string.Empty), out var color)
                ? color
                : fallback;

        static void SaveColor(string key, Color color) =>
            SessionState.SetString(k_SessionPrefix + key, "#" + ColorUtility.ToHtmlStringRGBA(color));

        public static void Show(Rect activatorRect)
        {
            PopupWindow.Show(activatorRect, new ShiyanXREyePanel());
        }

        // 七行控件：距离 / 瞳距 / 显示相机 / 左右相机颜色 / 会聚面颜色与透明度
        public override Vector2 GetWindowSize() => new Vector2(400f, 190f);

        public override void OnGUI(Rect rect)
        {
            // 统一的标签宽度，让七行控件左边缘对齐
            EditorGUIUtility.labelWidth = 170f;

            // 这些值都用来画 Scene View 里的那个会聚面，改动后得让它立刻重绘，
            // 否则拖滑条时看不到变化（要等 Scene View 自己因鼠标事件重画）
            EditorGUI.BeginChangeCheck();

            s_ConvergencePlaneDistance = EditorGUILayout.FloatField(
                new GUIContent("Convergence Plane Distance", "眼睛到会聚面（屏幕）的观看距离。会聚面固定在 Main Camera 的位置，改变它时移动的是左右眼位置。"),
                s_ConvergencePlaneDistance);
            s_InterocularDistance = EditorGUILayout.FloatField("Interocular Distance", s_InterocularDistance);
            s_DisplayCameras = EditorGUILayout.Toggle("Display Cameras", s_DisplayCameras);

            // 只取 RGB：这两个是画视锥线用的纯色，不另给透明度
            s_LeftCameraColor = EditorGUILayout.ColorField(
                new GUIContent("Left Camera Color"), s_LeftCameraColor,
                showEyedropper: true, showAlpha: false, hdr: false);
            s_RightCameraColor = EditorGUILayout.ColorField(
                new GUIContent("Right Camera Color"), s_RightCameraColor,
                showEyedropper: true, showAlpha: false, hdr: false);

            // 只取 RGB：透明度由下面那条独立滑条管，取色器里再给一个 alpha 会互相打架
            s_ConvergencePlaneColor = EditorGUILayout.ColorField(
                new GUIContent("Convergence Plane Color"), s_ConvergencePlaneColor,
                showEyedropper: true, showAlpha: false, hdr: false);

            // EditorGUILayout.Slider 自带右侧数值输入框，不再另加 FloatField（那会变成两个输入框）
            s_ConvergencePlaneAlpha = EditorGUILayout.Slider("Convergence Plane Alpha", s_ConvergencePlaneAlpha, 0f, 1f);

            if (EditorGUI.EndChangeCheck())
            {
                SaveState();
                SceneView.RepaintAll();
                // Main Camera 的 V/P 也依赖这些参数，编辑态下额外推一次 Game View 重绘
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }
    }
}
