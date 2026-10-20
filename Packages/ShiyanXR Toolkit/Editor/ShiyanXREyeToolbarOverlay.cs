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
    ///
    /// 面板值通过 <see cref="ShiyanXREyePanelState"/> + SerializedObject 读写：只有挂在 UnityEngine.Object 上，
    /// Unity 才能把改动记进 Undo 栈，Ctrl+Z / Ctrl+Y 可以撤销 / 重做面板上的修改。
    /// </summary>
    sealed class ShiyanXREyePanel : PopupWindowContent
    {
        const string k_SessionPrefix = "ShiyanXR.EyePanel.";
        const string k_UndoName = "Change ShiyanXR Eye Preview Settings";

        static ShiyanXREyePanelState s_State;
        static SerializedObject s_SerializedState;
        // 当前打开的面板：Undo/Redo 后要立即重绘它，否则控件上还显示旧值
        static ShiyanXREyePanel s_OpenPanel;

        /// <summary>
        /// 面板状态的载体，由 ShiyanXREyeStereoPreview 读走，用于 Scene View 里的参考图形。
        /// 不落盘（HideFlags.DontSave），域重载后由 <see cref="LoadState"/> 从 SessionState 恢复。
        /// </summary>
        internal static ShiyanXREyePanelState State
        {
            get
            {
                if (s_State != null) return s_State;

                s_State = ScriptableObject.CreateInstance<ShiyanXREyePanelState>();
                // 不能用 HideAndDontSave：它包含 NotEditable，SerializedObject 会把面板控件全部画成只读。
                // DontSave 只禁止落盘 / 卸载，不影响编辑与 Undo。
                s_State.hideFlags = HideFlags.DontSave;
                LoadState(s_State);
                return s_State;
            }
        }

        static ShiyanXREyePanel()
        {
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            AssemblyReloadEvents.beforeAssemblyReload += DestroyState;
        }

        /// <summary>
        /// 域重载会把静态字段清零，而进 Play 就会发生一次域重载——不恢复的话面板会退回默认值。
        /// 所以状态存 SessionState：它跨域重载存活，编辑器关闭才清。
        /// </summary>
        static void LoadState(ShiyanXREyePanelState state)
        {
            state.convergencePlaneDistance = SessionState.GetFloat(k_SessionPrefix + "ConvergencePlaneDistance", state.convergencePlaneDistance);
            state.interocularDistance = SessionState.GetFloat(k_SessionPrefix + "InterocularDistance", state.interocularDistance);
            state.displayCameras = SessionState.GetBool(k_SessionPrefix + "DisplayCameras", state.displayCameras);
            state.convergencePlaneAlpha = SessionState.GetFloat(k_SessionPrefix + "ConvergencePlaneAlpha", state.convergencePlaneAlpha);
            state.leftCameraColor = LoadColor("LeftCameraColor", state.leftCameraColor);
            state.rightCameraColor = LoadColor("RightCameraColor", state.rightCameraColor);
            state.convergencePlaneColor = LoadColor("ConvergencePlaneColor", state.convergencePlaneColor);
        }

        static void SaveState()
        {
            var state = State;
            SessionState.SetFloat(k_SessionPrefix + "ConvergencePlaneDistance", state.convergencePlaneDistance);
            SessionState.SetFloat(k_SessionPrefix + "InterocularDistance", state.interocularDistance);
            SessionState.SetBool(k_SessionPrefix + "DisplayCameras", state.displayCameras);
            SessionState.SetFloat(k_SessionPrefix + "ConvergencePlaneAlpha", state.convergencePlaneAlpha);
            SaveColor("LeftCameraColor", state.leftCameraColor);
            SaveColor("RightCameraColor", state.rightCameraColor);
            SaveColor("ConvergencePlaneColor", state.convergencePlaneColor);
        }

        // SessionState 没有颜色类型，借 HTML 串（含 alpha）存取
        static Color LoadColor(string key, Color fallback) =>
            ColorUtility.TryParseHtmlString(SessionState.GetString(k_SessionPrefix + key, string.Empty), out var color)
                ? color
                : fallback;

        static void SaveColor(string key, Color color) =>
            SessionState.SetString(k_SessionPrefix + key, "#" + ColorUtility.ToHtmlStringRGBA(color));

        /// <summary>Undo/Redo 直接改的是 state 对象：把结果同步回 SessionState，并刷新 Scene View / Game View 与面板。</summary>
        static void OnUndoRedoPerformed()
        {
            // state 还没创建过（或刚被域重载清掉）时，SessionState 里已经是最后一次保存的值，无需回写
            if (s_State == null) return;

            SaveState();
            RepaintAll();
        }

        static void DestroyState()
        {
            if (s_State != null)
                UnityEngine.Object.DestroyImmediate(s_State);

            s_State = null;
            s_SerializedState?.Dispose();
            s_SerializedState = null;
            s_OpenPanel = null;
        }

        static void RepaintAll()
        {
            SceneView.RepaintAll();
            // Main Camera 的 V/P 也依赖这些参数，编辑态下额外推一次 Game View 重绘
            EditorApplication.QueuePlayerLoopUpdate();

            var window = s_OpenPanel != null ? s_OpenPanel.editorWindow : null;
            if (window != null)
                window.Repaint();
        }

        public static void Show(Rect activatorRect)
        {
            PopupWindow.Show(activatorRect, new ShiyanXREyePanel());
        }

        public override void OnOpen() => s_OpenPanel = this;

        public override void OnClose()
        {
            if (s_OpenPanel == this)
                s_OpenPanel = null;
        }

        // 七行控件：距离 / 瞳距 / 显示相机 / 左右相机颜色 / 会聚面颜色与透明度
        public override Vector2 GetWindowSize() => new Vector2(400f, 190f);

        public override void OnGUI(Rect rect)
        {
            // 统一的标签宽度，让七行控件左边缘对齐
            EditorGUIUtility.labelWidth = 170f;

            var state = State;
            if (s_SerializedState == null || s_SerializedState.targetObject != state)
                s_SerializedState = new SerializedObject(state);

            var serializedState = s_SerializedState;
            serializedState.Update();

            EditorGUILayout.PropertyField(
                serializedState.FindProperty(nameof(ShiyanXREyePanelState.convergencePlaneDistance)),
                new GUIContent("Convergence Plane Distance", "眼睛到会聚面（屏幕）的观看距离。会聚面固定在 Main Camera 的位置，改变它时移动的是左右眼位置。"));
            EditorGUILayout.PropertyField(
                serializedState.FindProperty(nameof(ShiyanXREyePanelState.interocularDistance)),
                new GUIContent("Interocular Distance"));
            EditorGUILayout.PropertyField(
                serializedState.FindProperty(nameof(ShiyanXREyePanelState.displayCameras)),
                new GUIContent("Display Cameras"));

            // 只取 RGB：这两个是画视锥线用的纯色，不另给透明度
            DrawColor(serializedState.FindProperty(nameof(ShiyanXREyePanelState.leftCameraColor)), "Left Camera Color");
            DrawColor(serializedState.FindProperty(nameof(ShiyanXREyePanelState.rightCameraColor)), "Right Camera Color");

            // 只取 RGB：透明度由下面那条独立滑条管，取色器里再给一个 alpha 会互相打架
            DrawColor(serializedState.FindProperty(nameof(ShiyanXREyePanelState.convergencePlaneColor)), "Convergence Plane Color");

            // EditorGUILayout.Slider 自带右侧数值输入框，不再另加 FloatField（那会变成两个输入框）
            var alpha = serializedState.FindProperty(nameof(ShiyanXREyePanelState.convergencePlaneAlpha));
            float alphaValue = EditorGUILayout.Slider(new GUIContent("Convergence Plane Alpha"), alpha.floatValue, 0f, 1f);
            if (!Mathf.Approximately(alphaValue, alpha.floatValue))
                alpha.floatValue = alphaValue;

            // 控件只改了 SerializedObject 的本地副本；这里先确认真的有修改，
            // 再用完整快照记录改动前的状态，最后一次性写回 state。
            // 这样 Ctrl+Z / Ctrl+Y 就能撤销 / 重做面板上的修改。
            if (!serializedState.hasModifiedProperties)
                return;

            // 非资源对象（HideFlags.DontSave）上 ApplyModifiedProperties 的 diff 式 Undo 不一定可靠，
            // 改为 RegisterCompleteObjectUndo 记录完整状态，再 Apply...WithoutUndo 写回。
            Undo.RegisterCompleteObjectUndo(state, k_UndoName);
            if (serializedState.ApplyModifiedPropertiesWithoutUndo())
            {
                SaveState();
                RepaintAll();
            }
        }

        static void DrawColor(SerializedProperty property, string label)
        {
            Color value = EditorGUILayout.ColorField(
                new GUIContent(label), property.colorValue,
                showEyedropper: true, showAlpha: false, hdr: false);

            // 只在真正变化时写回，避免每帧都标记修改、产生空的 Undo 记录
            if (value != property.colorValue)
                property.colorValue = value;
        }
    }

    /// <summary>
    /// 工具栏面板的状态载体。必须是 UnityEngine.Object，Unity 的 Undo 才能记录 / 撤销这些值；
    /// 对象不落盘（HideFlags.DontSave），跨域重载的持久化由 SessionState 负责。
    /// </summary>
    internal sealed class ShiyanXREyePanelState : ScriptableObject
    {
        // public 字段会被 Unity 序列化，SerializedProperty 才能找到并记录 Undo
        public float convergencePlaneDistance = 1f;
        public float interocularDistance = 0.065f;
        public bool displayCameras = true;
        // 默认取红 / 蓝：与 ShiyanDevicesSimulator 的 leftCameraColor / rightCameraColor 同一视觉约定
        public Color leftCameraColor = Color.red;
        public Color rightCameraColor = Color.blue;
        // 默认取青色：与 ShiyanDevicesSimulator 的 screenColor（屏幕 / 焦平面）同一视觉约定
        public Color convergencePlaneColor = Color.cyan;
        public float convergencePlaneAlpha = 0.3f;
    }
}
