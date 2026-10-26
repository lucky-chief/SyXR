using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    ///
    /// 设置按场景分别保存：每个场景一个状态对象，键是场景资源的 GUID，
    /// 记录写在 ProjectSettings/ShiyanXREyePanelSettings.json，跨编辑器重启保留。
    /// </summary>
    sealed class ShiyanXREyePanel : PopupWindowContent
    {
        // 未保存的场景没有资源 GUID，用 handle 拼一个只在本次编辑器会话内有效的键
        const string k_UnsavedSceneKeyPrefix = "unsaved:";
        // 编辑器启动阶段还没有活动场景时的键；不参与持久化
        const string k_NoSceneKey = "no-scene";
        const string k_UndoName = "Change ShiyanXR Eye Preview Settings";
        // 拖滑条时每帧都在改值，攒半秒再落盘，避免每个 GUI 事件都写一次文件
        const double k_SaveDelaySeconds = 0.5;

        // 每个场景一个状态对象：键是场景 GUID，切换场景时按需创建 / 取回
        static readonly Dictionary<string, ShiyanXREyePanelState> s_StatesBySceneKey =
            new Dictionary<string, ShiyanXREyePanelState>();
        static ShiyanXREyePanelState s_State;
        static string s_StateSceneKey;
        static SerializedObject s_SerializedState;
        // 当前打开的面板：Undo/Redo 后要立即重绘它，否则控件上还显示旧值
        static ShiyanXREyePanel s_OpenPanel;

        // 当前活动场景的键。每次取用时比对 handle 与 path，场景切换、未命名场景刚被保存都能发现；
        // 比依赖 activeSceneChangedInEditMode 之类的场景事件可靠（编辑器启动时并不保证会触发）。
        static string s_ActiveSceneKey;
        static int s_ActiveSceneHandle = int.MinValue;
        static string s_ActiveScenePath;

        // 延迟落盘：改值只更新内存里的记录，文件写入合并成一次
        static bool s_SavePending;
        static double s_SaveDueTime;

        /// <summary>
        /// 当前场景的状态载体，由 ShiyanXREyeStereoPreview 读走，用于 Scene View 里的参考图形。
        /// 每个场景一份：切换场景时按 GUID 取回内存里已有的对象，或从磁盘记录新建一个。
        /// </summary>
        internal static ShiyanXREyePanelState State
        {
            get
            {
                string sceneKey = ActiveSceneKey();
                if (s_State != null && s_StateSceneKey == sceneKey)
                    return s_State;

                if (!s_StatesBySceneKey.TryGetValue(sceneKey, out s_State) || s_State == null)
                {
                    s_State = CreateState(sceneKey);
                    s_StatesBySceneKey[sceneKey] = s_State;
                }

                s_StateSceneKey = sceneKey;

                // 换了载体，SerializedObject 也得换，否则面板还在改上一个场景的对象
                s_SerializedState?.Dispose();
                s_SerializedState = null;

                return s_State;
            }
        }

        static ShiyanXREyePanel()
        {
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.quitting += FlushSave;
        }

        /// <summary>新建一个场景的状态对象：先取默认值，再用该场景存下来的快照覆盖。</summary>
        static ShiyanXREyePanelState CreateState(string sceneKey)
        {
            var state = ScriptableObject.CreateInstance<ShiyanXREyePanelState>();
            // 不能用 HideAndDontSave：它包含 NotEditable，SerializedObject 会把面板控件全部画成只读。
            // DontSave 只禁止落盘 / 卸载，不影响编辑与 Undo；持久化由 ShiyanXREyePanelSettings 负责。
            state.hideFlags = HideFlags.DontSave;

            if (IsPersistableSceneKey(sceneKey))
            {
                string json = ShiyanXREyePanelSettings.GetSettingsJson(sceneKey);
                if (!string.IsNullOrEmpty(json))
                    JsonUtility.FromJsonOverwrite(json, state);
            }

            return state;
        }

        /// <summary>
        /// 当前活动场景的键。场景切换、未命名场景刚保存成文件，都会让 handle / path 变化，
        /// 这里比一下就能发现，顺便把状态切过去。
        /// </summary>
#pragma warning disable 0618 // Unity 6.4 起 scene.handle 的 int 隐式转换被标记过时；为兼容 package.json 声明的 2022.3 仍用 int
        static string ActiveSceneKey()
        {
            Scene scene = SceneManager.GetActiveScene();

            // 场景没变就用缓存的键；例外是“有路径却只拿到非持久化键”——
            // 编辑器启动早期 AssetDatabase 可能还没就绪，等它好了要再算一次。
            if (s_ActiveSceneKey != null
                && scene.handle == s_ActiveSceneHandle
                && scene.path == s_ActiveScenePath
                && (IsPersistableSceneKey(s_ActiveSceneKey) || string.IsNullOrEmpty(scene.path)))
                return s_ActiveSceneKey;

            string previousKey = s_ActiveSceneKey;
            int previousHandle = s_ActiveSceneHandle;
            string previousPath = s_ActiveScenePath;

            s_ActiveSceneHandle = scene.handle;
            s_ActiveScenePath = scene.path;
            s_ActiveSceneKey = SceneKey(scene);

            if (previousKey != s_ActiveSceneKey)
                OnActiveSceneKeyChanged(previousKey, previousHandle, previousPath);

            return s_ActiveSceneKey;
        }
#pragma warning restore 0618

        /// <summary>
        /// 场景的持久化键：优先用资源 GUID，场景改名 / 移动目录后设置仍在；
        /// 未保存的场景没有 GUID，用 handle 兜底——本次会话内各用各的，但不写进磁盘。
        /// </summary>
        static string SceneKey(Scene scene)
        {
            // 编辑器启动阶段可能还没有活动场景，别把它当成某个真实场景
            if (!scene.IsValid())
                return k_NoSceneKey;

            string path = scene.path;
            if (!string.IsNullOrEmpty(path))
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (!string.IsNullOrEmpty(guid))
                    return guid;
            }

            return k_UnsavedSceneKeyPrefix + scene.handle;
        }

        static bool IsUnsavedSceneKey(string sceneKey) =>
            sceneKey.StartsWith(k_UnsavedSceneKeyPrefix, System.StringComparison.Ordinal);

        /// <summary>只有能对应到场景资源（GUID）的键才写进工程设置；未保存 / 无场景的键只留在内存里。</summary>
        static bool IsPersistableSceneKey(string sceneKey) =>
            !string.IsNullOrEmpty(sceneKey) && sceneKey != k_NoSceneKey && !IsUnsavedSceneKey(sceneKey);

        /// <summary>
        /// 键变了。最要紧的一种情况是未命名场景刚被保存成 .unity 文件：还是同一个场景（handle 不变），
        /// 只是从没有路径变成了有路径——把会话里调好的值迁到新的场景 GUID 下，否则它会退回默认值。
        /// 注意不能只看“旧键是 unsaved”：从一个 Untitled 场景切到另一个已保存场景时，不能把设置带过去。
        /// </summary>
        static void OnActiveSceneKeyChanged(string previousKey, int previousHandle, string previousPath)
        {
            bool sceneJustSaved = previousKey != null
                                  && previousHandle == s_ActiveSceneHandle
                                  && string.IsNullOrEmpty(previousPath)
                                  && !string.IsNullOrEmpty(s_ActiveScenePath)
                                  && IsUnsavedSceneKey(previousKey)
                                  && IsPersistableSceneKey(s_ActiveSceneKey);

            if (sceneJustSaved && s_StatesBySceneKey.TryGetValue(previousKey, out var state))
            {
                s_StatesBySceneKey.Remove(previousKey);
                s_StatesBySceneKey[s_ActiveSceneKey] = state;

                if (s_StateSceneKey == previousKey)
                    s_StateSceneKey = s_ActiveSceneKey;

                PersistState(s_ActiveSceneKey, state);
            }

            // 新场景用的是另一套颜色 / 距离，立刻让 Scene View 重画
            if (previousKey != null)
                RepaintAll();
        }

        /// <summary>把当前场景的值写进持久化记录（先只改内存，文件写入延迟合并）。</summary>
        static void PersistState()
        {
            if (s_State == null || string.IsNullOrEmpty(s_StateSceneKey)) return;

            PersistState(s_StateSceneKey, s_State);
        }

        static void PersistState(string sceneKey, ShiyanXREyePanelState state)
        {
            if (!IsPersistableSceneKey(sceneKey)) return;

            string json = JsonUtility.ToJson(state);
            if (ShiyanXREyePanelSettings.SetSettingsJson(sceneKey, json))
                ScheduleSave();
        }

        /// <summary>改值很频繁（拖滑条、取色器），攒 <see cref="k_SaveDelaySeconds"/> 秒再写一次文件。</summary>
        static void ScheduleSave()
        {
            s_SavePending = true;
            s_SaveDueTime = EditorApplication.timeSinceStartup + k_SaveDelaySeconds;

            EditorApplication.update -= FlushSaveWhenDue;
            EditorApplication.update += FlushSaveWhenDue;
        }

        static void FlushSaveWhenDue()
        {
            if (EditorApplication.timeSinceStartup < s_SaveDueTime)
                return;

            FlushSave();
        }

        static void FlushSave()
        {
            EditorApplication.update -= FlushSaveWhenDue;

            if (!s_SavePending) return;

            s_SavePending = false;
            ShiyanXREyePanelSettings.SaveNow();
        }

        /// <summary>
        /// Undo/Redo 直接改的是各场景的 state 对象：把调过的都回写一遍，
        /// 这样无论撤销的是当前场景还是之前那个场景的修改，磁盘上都跟着更新。
        /// </summary>
        static void OnUndoRedoPerformed()
        {
            foreach (var pair in s_StatesBySceneKey)
            {
                // 磁盘记录里没有的场景说明从没改过，不用回写
                if (ShiyanXREyePanelSettings.HasSettings(pair.Key))
                    PersistState(pair.Key, pair.Value);
            }

            RepaintAll();
        }

        /// <summary>进 Play 前把手头攒着的一次写入落盘，避免进播放 / 崩溃时丢掉最后一笔。</summary>
        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                FlushSave();
        }

        /// <summary>
        /// 域重载会把静态字段清零，而进 Play 就会发生一次域重载——所以先把挂起的改动落盘，
        /// 再销毁各场景的状态对象；下次取用时从磁盘记录里恢复。
        /// </summary>
        static void OnBeforeAssemblyReload()
        {
            FlushSave();
            DestroyStates();
        }

        static void DestroyStates()
        {
            foreach (var state in s_StatesBySceneKey.Values)
            {
                if (state != null)
                    UnityEngine.Object.DestroyImmediate(state);
            }

            s_StatesBySceneKey.Clear();

            s_State = null;
            s_StateSceneKey = null;
            s_ActiveSceneKey = null;
            s_ActiveSceneHandle = int.MinValue;
            s_ActiveScenePath = null;
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

            // 面板关掉时通常已经改完，直接落盘，不用等延迟合并
            FlushSave();
        }

        // 场景名 + 七行控件：距离 / 瞳距 / 显示相机 / 左右相机颜色 / 会聚面颜色与透明度；末尾再跟一块只读的尺寸信息
        public override Vector2 GetWindowSize() => new Vector2(400f, 270f);

        public override void OnGUI(Rect rect)
        {
            // 统一的标签宽度，让各控件左边缘对齐
            EditorGUIUtility.labelWidth = 170f;

            DrawSceneHeader();

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

            DrawConvergencePlaneInfo();

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
                // 值已经写进当前场景的 state，接着同步到该场景的持久化记录
                PersistState();
                RepaintAll();
            }
        }

        /// <summary>
        /// 标出当前场景：设置是按场景分别保存的，标题里写清楚，免得以为改了会带到别的场景。
        /// </summary>
        static void DrawSceneHeader()
        {
            Scene scene = SceneManager.GetActiveScene();
            bool saved = !string.IsNullOrEmpty(scene.path);

            EditorGUILayout.LabelField(
                new GUIContent("Scene", "这些设置按场景分别保存，切换场景会加载对应场景自己的设置。"),
                new GUIContent(
                    saved ? scene.name : scene.name + " (unsaved)",
                    saved ? scene.path : "场景还没保存成文件，这套设置只在本次编辑器会话内有效。"));
        }

        /// <summary>
        /// 只读信息：XROrigin 缩放 k、0 视差面的有效世界尺寸，以及画面里物体相对真实尺寸的缩放 1/k。
        /// 与 ShiyanXREyeStereoPreview 共用同一个定义，改 XROrigin 的 scale 后面板会跟着变。
        /// </summary>
        static void DrawConvergencePlaneInfo()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                EditorGUILayout.HelpBox("场景里没有 Main Camera，无法读取 XROrigin 缩放。", MessageType.None);
                return;
            }

            float scale = ShiyanXREyeStereoPreview.XROriginScale(camera);
            ShiyanXREyeStereoPreview.EffectiveConvergencePlaneSize(camera, out float width, out float height);

            EditorGUILayout.HelpBox(
                $"XROrigin Scale: {scale:0.###}\n" +
                $"0-Parallax Plane: {width:0.####} × {height:0.####} m\n" +
                $"Content appears: 1 / scale = {1f / scale:0.###}×",
                MessageType.None);
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
}
