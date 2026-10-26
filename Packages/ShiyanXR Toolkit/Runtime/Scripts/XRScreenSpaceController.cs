using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Serialization;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace com.unity.shiyantech.xr
{
    /// <summary>
    /// 屏幕射线用哪套投影。
    /// </summary>
    public enum ScreenRaySource
    {
        /// <summary>
        /// 编辑器的非播放模式用 Main Camera（它会与 Game View 里实际渲染的画面一致），
        /// 其余情况在没有 XR 设备时用 Main Camera、有 XR 设备时用 ScreenPointToRayHelper。
        /// </summary>
        Auto,
        /// <summary>始终用 Main Camera 当前的 V/P（普通的 2D / 键鼠场景，或编辑器预览）。</summary>
        RenderCamera,
        /// <summary>始终用 ScreenPointToRayHelper 的屏幕模型（与 XR Runtime / 眉心 / 左右眼一致）。</summary>
        RayHelper,
    }

    /// <summary>
    /// Interprets screen presses and gestures by using actions from the Input System and applying
    /// the resulting ray pose/select state directly to the XRRayInteractor on the same GameObject.
    /// This is the XRI 3.x replacement for the deprecated XRBaseController-based screen space controller.
    /// </summary>
    /// <remarks>
    /// This behavior requires that the Input System is enabled in the <b>Active Input Handling</b>
    /// setting in <b>Edit &gt; Project Settings &gt; Player</b> for input values to be read.
    /// Each input action must also be enabled to read the current value of the action. Referenced
    /// input actions in an Input Action Asset are not enabled by default.
    ///
    /// 键鼠模式：鼠标位置先归一化成视口坐标，再用“画面实际渲染时用的那套 V/P”生成射线，
    /// 最后把交互器的世界位姿设成这条射线。<see cref="raySource"/> 决定用 Main Camera 还是
    /// <see cref="ScreenPointToRayHelper"/>；两套投影必须和当前画面一致，射线才会正好落在鼠标下面。
    /// </remarks>
    /// <seealso cref="TouchscreenGestureInputController"/>
    [AddComponentMenu("XR/XR Screen Space Controller", 11)]
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_ScreenSpaceRayPoseDriver)]
    public partial class XRScreenSpaceController : MonoBehaviour, IXRInputButtonReader
    {
        [Header("Touchscreen Gesture Actions")]
        [SerializeField]
        [Tooltip("When enabled, a Touchscreen Gesture Input Controller will be added to the Input System device list to detect touch gestures.")]
        bool m_EnableTouchscreenGestureInputController = true;
        /// <summary>
        /// When enabled, a <see cref="TouchscreenGestureInputController"/> will be added to the Input System device list to detect touch gestures.
        /// This input controller drives the gesture values for the input actions for the screen space controller.
        /// </summary>
        public bool enableTouchscreenGestureInputController
        {
            get => m_EnableTouchscreenGestureInputController;
            set => m_EnableTouchscreenGestureInputController = value;
        }

        [SerializeField]
        [Tooltip("The action to use for the screen tap position. (Vector 2 Control).")]
        InputActionProperty m_TapStartPositionAction = new InputActionProperty(new InputAction("Tap Start Position", expectedControlType: "Vector2"));
        /// <summary>
        /// The Input System action to use for reading screen Tap Position for this GameObject. Must be a <see cref="Vector2Control"/> Control.
        /// </summary>
        public InputActionProperty tapStartPositionAction
        {
            get => m_TapStartPositionAction;
            set => SetInputActionProperty(ref m_TapStartPositionAction, value);
        }

        [SerializeField]
        [Tooltip("The action to use for the current screen drag position. (Vector 2 Control).")]
        InputActionProperty m_DragCurrentPositionAction = new InputActionProperty(new InputAction("Drag Current Position", expectedControlType: "Vector2"));
        /// <summary>
        /// The Input System action to use for reading the screen Drag Position for this GameObject. Must be a <see cref="Vector2Control"/> Control.
        /// </summary>
        /// <seealso cref="dragDeltaAction"/>
        public InputActionProperty dragCurrentPositionAction
        {
            get => m_DragCurrentPositionAction;
            set => SetInputActionProperty(ref m_DragCurrentPositionAction, value);
        }

        [SerializeField]
        [Tooltip("The action to use for the delta of the screen drag. (Vector 2 Control).")]
        InputActionProperty m_DragDeltaAction = new InputActionProperty(new InputAction("Drag Delta", expectedControlType: "Vector2"));
        /// <summary>
        /// The Input System action used to read the delta Drag values for this GameObject. Must be a <see cref="Vector2Control"/> Control.
        /// </summary>
        /// <seealso cref="dragCurrentPositionAction"/>
        public InputActionProperty dragDeltaAction
        {
            get => m_DragDeltaAction;
            set => SetInputActionProperty(ref m_DragDeltaAction, value);
        }

        [SerializeField, FormerlySerializedAs("m_PinchStartPosition")]
        [Tooltip("The action to use for the screen pinch gesture start position. (Vector 2 Control).")]
        InputActionProperty m_PinchStartPositionAction = new InputActionProperty(new InputAction("Pinch Start Position", expectedControlType: "Vector2"));
        /// <summary>
        /// The Input System action to use for reading the Pinch Start Position for this GameObject. Must be a <see cref="Vector2Control"/> Control.
        /// </summary>
        /// <seealso cref="pinchGapDeltaAction"/>
        public InputActionProperty pinchStartPositionAction
        {
            get => m_PinchStartPositionAction;
            set => SetInputActionProperty(ref m_PinchStartPositionAction, value);
        }

        [SerializeField]
        [Tooltip("The action to use for the gap of the screen pinch gesture. (Axis Control).")]
        InputActionProperty m_PinchGapAction = new InputActionProperty(new InputAction(expectedControlType: "Axis"));
        /// <summary>
        /// The Input System action used to read the Pinch values for this GameObject. Must be an <see cref="AxisControl"/> Control.
        /// </summary>
        /// <seealso cref="pinchGapDeltaAction"/>
        public InputActionProperty pinchGapAction
        {
            get => m_PinchGapAction;
            set => SetInputActionProperty(ref m_PinchGapAction, value);
        }

        [SerializeField]
        [Tooltip("The action to use for the delta of the screen pinch gesture. (Axis Control).")]
        InputActionProperty m_PinchGapDeltaAction = new InputActionProperty(new InputAction("Pinch Gap Delta", expectedControlType: "Axis"));
        /// <summary>
        /// The Input System action used to read the delta Pinch values for this GameObject. Must be an <see cref="AxisControl"/> Control.
        /// </summary>
        /// <seealso cref="pinchStartPositionAction"/>
        public InputActionProperty pinchGapDeltaAction
        {
            get => m_PinchGapDeltaAction;
            set => SetInputActionProperty(ref m_PinchGapDeltaAction, value);
        }

        [SerializeField, FormerlySerializedAs("m_TwistStartPosition")]
        [Tooltip("The action to use for the screen twist gesture start position. (Vector 2 Control).")]
        InputActionProperty m_TwistStartPositionAction = new InputActionProperty(new InputAction("Twist Start Position", expectedControlType: "Vector2"));
        /// <summary>
        /// The Input System action to use for reading the Twist Start Position for this GameObject. Must be a <see cref="Vector2Control"/> Control.
        /// </summary>
        /// <seealso cref="twistDeltaRotationAction"/>
        public InputActionProperty twistStartPositionAction
        {
            get => m_TwistStartPositionAction;
            set => SetInputActionProperty(ref m_TwistStartPositionAction, value);
        }

        [SerializeField, FormerlySerializedAs("m_TwistRotationDeltaAction")]
        [Tooltip("The action to use for the delta of the screen twist gesture. (Axis Control).")]
        InputActionProperty m_TwistDeltaRotationAction = new InputActionProperty(new InputAction("Twist Delta Rotation", expectedControlType: "Axis"));
        /// <summary>
        /// The Input System action used to read the delta Twist values for this GameObject. Must be an <see cref="AxisControl"/> Control.
        /// </summary>
        /// <seealso cref="twistStartPositionAction"/>
        public InputActionProperty twistDeltaRotationAction
        {
            get => m_TwistDeltaRotationAction;
            set => SetInputActionProperty(ref m_TwistDeltaRotationAction, value);
        }

        [SerializeField, FormerlySerializedAs("m_ScreenTouchCount")]
        [Tooltip("The number of concurrent touches on the screen. (Integer Control).")]
        InputActionProperty m_ScreenTouchCountAction = new InputActionProperty(new InputAction("Screen Touch Count", expectedControlType: "Integer"));
        /// <summary>
        /// The number of concurrent touches on the screen. Must be an <see cref="IntegerControl"/> Control.
        /// </summary>
        public InputActionProperty screenTouchCountAction
        {
            get => m_ScreenTouchCountAction;
            set => SetInputActionProperty(ref m_ScreenTouchCountAction, value);
        }

        [SerializeField]
        [Tooltip("The action to use for the mouse position in screen pixels. (Vector 2 Control).")]
        InputActionProperty m_MousePositionAction = new InputActionProperty(new InputAction("Mouse Position", expectedControlType: "Vector2", binding: "<Mouse>/position"));
        /// <summary>
        /// The Input System action used to read the mouse position for this GameObject. Must be a <see cref="Vector2Control"/> Control.
        /// The default action is bound to <c>&lt;Mouse&gt;/position</c>; a reference to an action in an Input Action Asset can be assigned instead.
        /// </summary>
        public InputActionProperty mousePositionAction
        {
            get => m_MousePositionAction;
            set => SetInputActionProperty(ref m_MousePositionAction, value);
        }

        [SerializeField]
        [Tooltip("The action to use for the mouse select (press) state. (Button Control).")]
        InputActionProperty m_MouseSelectAction = new InputActionProperty(new InputAction("Mouse Select", InputActionType.Button, "<Mouse>/leftButton", expectedControlType: "Button"));
        /// <summary>
        /// The Input System action used to read the mouse select (press) state for this GameObject. Must be a <see cref="ButtonControl"/> Control.
        /// The default action is bound to <c>&lt;Mouse&gt;/leftButton</c>; a reference to an action in an Input Action Asset can be assigned instead.
        /// </summary>
        public InputActionProperty mouseSelectAction
        {
            get => m_MouseSelectAction;
            set => SetInputActionProperty(ref m_MouseSelectAction, value);
        }

        [SerializeField]
        [Tooltip("The ray helper associated with the screen, and through which screen presses/touches will be interpreted.")]
        ScreenPointToRayHelper m_ScreenPointToRayHelper;

        [SerializeField]
        [Tooltip("鼠标射线使用哪套投影。Auto：编辑器非播放模式用 Main Camera（与 Game View 实际渲染完全一致），播放 / 真机有 XR 设备时用 ScreenPointToRayHelper，没有 XR 设备时用 Main Camera。")]
        ScreenRaySource m_RaySource = ScreenRaySource.Auto;
        /// <summary>
        /// 鼠标射线使用哪套投影。需要和当前画面上实际使用的投影一致，射线才会正好落在鼠标下面。
        /// </summary>
        public ScreenRaySource raySource
        {
            get => m_RaySource;
            set => m_RaySource = value;
        }

        [SerializeField]
        [Tooltip("Tells the XR Screen Space Controller to ignore interactions when hitting a screen space canvas.")]
        bool m_BlockInteractionsWithScreenSpaceUI = true;
        /// <summary>
        /// Tells the XR Screen Space Controller to ignore interactions when hitting a screen space canvas.
        /// </summary>
        /// <seealso cref="Canvas.renderMode"/>
        public bool blockInteractionsWithScreenSpaceUI
        {
            get => m_BlockInteractionsWithScreenSpaceUI;
            set => m_BlockInteractionsWithScreenSpaceUI = value;
        }

        [SerializeField]
        [Tooltip("Enables a rotation threshold that blocks pinch scale gestures when surpassed..")]
        bool m_UseRotationThreshold = true;
        /// <summary>
        /// Enables a rotation threshold that blocks pinch scale gestures when surpassed.
        /// </summary>
        /// <seealso cref="rotationThreshold"/>
        public bool useRotationThreshold
        {
            get => m_UseRotationThreshold;
            set => m_UseRotationThreshold = value;
        }

        [SerializeField]
        [Tooltip("The threshold at which a gestures will be interpreted only as rotation and not a pinch scale gesture.")]
        float m_RotationThreshold = 0.02f;

        /// <summary>
        /// The threshold at which a gestures will be interpreted only as rotation and not a pinch scale gesture.
        /// </summary>
        /// <seealso cref="useRotationThreshold"/>
        public float rotationThreshold
        {
            get => m_RotationThreshold;
            set => m_RotationThreshold = value;
        }

        /// <summary>
        /// This value is the change in scale based on input from the <see cref="pinchGapDeltaAction"/> pinch gap delta action
        /// with <see cref="Screen.dpi"/> applied as a factor of the value read in. The delta refers to the change from the previous frame.
        /// </summary>
        /// <remarks>
        /// This value may come back as zero if the input action is not assigned or cannot be read.
        /// </remarks>
        public float scaleDelta { get; private set; }

#if TOUCHSCREEN_GESTURE_INPUT_CONTROLLER_AVAILABLE
        TouchscreenGestureInputController m_GestureInputController;
#endif
        bool m_HasCheckedDisabledTrackingInputReferenceActions;
        UIInputModule m_UIInputModule;
        XRRayInteractor m_Interactor;

        bool m_SelectPressed;
        bool m_SelectPressedThisFrame;
        bool m_SelectReleasedThisFrame;

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void Start()
        {
            // 两套投影至少要有一样：编辑器 / 普通 PC 可以直接用 Main Camera，未必需要 helper
            if (m_ScreenPointToRayHelper == null && Camera.main == null)
            {
                Debug.LogWarning($"Could not find associated {nameof(ScreenPointToRayHelper)} or a Main Camera in scene. " +
                    $"This {nameof(XRScreenSpaceController)} will be disabled.", this);
                enabled = false;
                return;
            }

            // 再配置一次：确保 XRRayInteractor 的 OnEnable 没有把 select/uiPress 的输入源覆盖掉。
            if (m_Interactor == null)
                m_Interactor = GetComponent<XRRayInteractor>();
            if (m_Interactor != null)
            {
                ConfigureButtonReader(m_Interactor.selectInput);
                ConfigureButtonReader(m_Interactor.uiPressInput);
            }
        }

        void OnEnable()
        {
            m_Interactor = GetComponent<XRRayInteractor>();
            if (m_Interactor != null)
            {
                ConfigureButtonReader(m_Interactor.selectInput);
                ConfigureButtonReader(m_Interactor.uiPressInput);
            }

            EnableAllDirectActions();
            InitializeTouchscreenGestureController();
        }

        void OnDisable()
        {
            DisableAllDirectActions();
            DestroyTouchscreenGestureController();
            m_UIInputModule = null;
        }

        void ConfigureButtonReader(XRInputButtonReader reader)
        {
            if (reader == null)
                return;

            reader.inputSourceMode = XRInputButtonReader.InputSourceMode.ObjectReference;
            reader.SetObjectReference(this);
        }

        void Update()
        {
            UpdateSelectState();
            UpdatePose();
            UpdateScaleDelta();
        }

        void UpdateSelectState()
        {
            // 本设备 dongle 的“原地点击”不进 Input System，所以同时读 MouseButtonBridge。
            bool pressed = MouseButtonBridge.IsLeftPressed() || MouseButtonBridge.ConsumeLeftClick();

            if (!IsPointerOverScreenSpaceCanvas())
            {
                var selectAction = m_MouseSelectAction.action;
                if (selectAction != null &&
                    (selectAction.IsPressed() || selectAction.WasPressedThisFrame()))
                {
                    pressed = true;
                }

                // 与鼠标位置一样，Input Action 没启用时退回系统鼠标左键，保证键鼠调试能点
                if (Mouse.current != null &&
                    (Mouse.current.leftButton.isPressed || Mouse.current.leftButton.wasPressedThisFrame))
                {
                    pressed = true;
                }
            }
            else
            {
                pressed = false;
            }

            m_SelectPressedThisFrame = pressed && !m_SelectPressed;
            m_SelectReleasedThisFrame = !pressed && m_SelectPressed;
            m_SelectPressed = pressed;
        }

        void UpdatePose()
        {
            if (IsPointerOverScreenSpaceCanvas())
                return;

            // Warn the user if using referenced actions and they are disabled
            if (!m_HasCheckedDisabledTrackingInputReferenceActions &&
                (m_DragCurrentPositionAction.action != null || m_TapStartPositionAction.action != null || m_TwistStartPositionAction.action != null))
            {
                if (IsDisabledReferenceAction(m_DragCurrentPositionAction) ||
                    IsDisabledReferenceAction(m_TapStartPositionAction) ||
                    IsDisabledReferenceAction(m_TwistStartPositionAction))
                {
                    Debug.LogWarning("'Enable Input Tracking' is enabled, but the Tap, Drag, Pinch, and/or Twist Action is disabled." +
                        " The pose of the controller will not be updated correctly until the Input Actions are enabled." +
                        " Input Actions in an Input Action Asset must be explicitly enabled to read the current value of the action." +
                        " The Input Action Manager behavior can be added to a GameObject in a Scene and used to enable all Input Actions in a referenced Input Action Asset.",
                        this);
                }

                m_HasCheckedDisabledTrackingInputReferenceActions = true;
            }

            if (!TryGetViewportPoint(out var viewportPoint))
                return;

            // 关键：射线必须用“画面实际渲染时用的那套 V/P”。
            // 编辑器非播放模式用 Main Camera（ShiyanXREyeStereoPreview 会改写它），
            // 真机 / XR 用 helper 的屏幕模型；两者选错，射线就会和光标差一截。
            if (!TryGetAimRay(viewportPoint, out var ray))
                return;

            float rayLength = m_Interactor != null
                ? m_Interactor.maxRaycastDistance
                : (m_ScreenPointToRayHelper != null ? Mathf.Abs(m_ScreenPointToRayHelper.zOffset) : 1f);

            // 在 Scene 视图里画出这条射线，方便调试（Debug.DrawRay 只在 Editor / Development Build 生效）
            Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.green);

            // 直接用世界位姿：父节点有旋转 / 缩放时，localRotation 不一定能让 forward 对准 ray.direction
            transform.SetPositionAndRotation(ray.origin, Quaternion.LookRotation(ray.direction));
        }

        void UpdateScaleDelta()
        {
            if (m_UseRotationThreshold && TryGetAbsoluteValue(m_TwistDeltaRotationAction.action, out var absRotationValue) && absRotationValue >= m_RotationThreshold)
            {
                scaleDelta = 0f;
            }
            else
            {
                scaleDelta = m_PinchGapDeltaAction.action != null ? m_PinchGapDeltaAction.action.ReadValue<float>() / Screen.dpi : 0f;
            }
        }

        public bool ReadIsPerformed() => m_SelectPressed;

        public bool ReadWasPerformedThisFrame() => m_SelectPressedThisFrame;

        public bool ReadWasCompletedThisFrame() => m_SelectReleasedThisFrame;

        public float ReadValue() => m_SelectPressed ? 1f : 0f;

        public bool TryReadValue(out float value)
        {
            value = m_SelectPressed ? 1f : 0f;
            return true;
        }

        /// <summary>
        /// 鼠标位置 → 归一化视口坐标（0..1，左下原点）。
        /// 归一化到视口，不再经过 helper 的固定像素尺寸：Game View / 设备屏幕分辨率、宽高比不同，
        /// 归一化后仍然是画面里的同一点，鼠标和射线不会因为分辨率换算而错位。
        /// </summary>
        bool TryGetViewportPoint(out Vector2 viewportPoint)
        {
            if (Screen.width <= 0 || Screen.height <= 0 || !TryReadMousePosition(out var mousePosition))
            {
                viewportPoint = default;
                return false;
            }

            viewportPoint = new Vector2(mousePosition.x / Screen.width, mousePosition.y / Screen.height);
            return true;
        }

        /// <summary>
        /// 读系统鼠标位置：优先用面板上配置的 Mouse Position action；它没启用 / 没绑定时
        /// 直接退回 <see cref="Mouse"/>，保证键鼠调试时总能拿到光标位置。
        /// </summary>
        bool TryReadMousePosition(out Vector2 mousePosition)
        {
            var mouseAction = m_MousePositionAction.action;
            if (mouseAction != null && mouseAction.enabled)
            {
                mousePosition = mouseAction.ReadValue<Vector2>();
                return true;
            }

            if (Mouse.current != null)
            {
                mousePosition = Mouse.current.position.ReadValue();
                return true;
            }

            mousePosition = default;
            return false;
        }

        /// <summary>
        /// Auto 的选法：编辑器的非播放模式必须用 Main Camera ——
        /// ShiyanXREyeStereoPreview 会改写 Main Camera 的 V/P，只有它和 Game View 里看到的画面一致；
        /// 真机 / XR Play 用 helper 的屏幕模型；没有 XR 设备时（普通 PC / 桌面）用 Main Camera 的普通透视投影。
        /// </summary>
        ScreenRaySource ResolveRaySource()
        {
            if (m_RaySource != ScreenRaySource.Auto)
                return m_RaySource;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                return ScreenRaySource.RenderCamera;
#endif

            return UnityEngine.XR.XRSettings.isDeviceActive
                ? ScreenRaySource.RayHelper
                : ScreenRaySource.RenderCamera;
        }

        /// <summary>按当前选择的投影，把视口点变成世界射线；首选源不可用时自动退到另一套。</summary>
        bool TryGetAimRay(Vector2 viewportPoint, out Ray ray)
        {
            var viewport = new Vector3(viewportPoint.x, viewportPoint.y, 0f);

            if (ResolveRaySource() == ScreenRaySource.RenderCamera)
            {
                var camera = Camera.main;
                if (camera != null)
                {
                    ray = camera.ViewportPointToRay(viewport);
                    return true;
                }
            }

            if (m_ScreenPointToRayHelper != null)
            {
                ray = m_ScreenPointToRayHelper.ViewportPointToRay(viewportPoint);
                return true;
            }

            var fallbackCamera = Camera.main;
            if (fallbackCamera != null)
            {
                ray = fallbackCamera.ViewportPointToRay(viewport);
                return true;
            }

            ray = default;
            return false;
        }

        static bool TryGetAbsoluteValue(InputAction action, out float value)
        {
            if (action != null &&
                action.phase.IsInProgress())
            {
                value = Mathf.Abs(action.ReadValue<float>());
                return true;
            }

            value = 0f;
            return false;
        }

        bool FindUIInputModule()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem != null && eventSystem.currentInputModule != null)
            {
                m_UIInputModule = eventSystem.currentInputModule as UIInputModule;
            }
            return m_UIInputModule != null;
        }

        bool IsPointerOverScreenSpaceCanvas()
        {
            if (m_BlockInteractionsWithScreenSpaceUI)
            {
                if (m_UIInputModule != null || FindUIInputModule())
                {
                    var uiObject = m_UIInputModule.GetCurrentGameObject(-1);
                    if (uiObject == null)
                        return false;

                    var canvas = uiObject.GetComponentInParent<Canvas>();
                    var renderMode = canvas.renderMode;
                    return renderMode == RenderMode.ScreenSpaceOverlay || renderMode == RenderMode.ScreenSpaceCamera;
                }
            }

            return false;
        }

        void InitializeTouchscreenGestureController()
        {
#if TOUCHSCREEN_GESTURE_INPUT_CONTROLLER_AVAILABLE
            if (!m_EnableTouchscreenGestureInputController)
                return;

            if (m_GestureInputController == null)
            {
                m_GestureInputController = InputSystem.InputSystem.AddDevice<TouchscreenGestureInputController>();
                if (m_GestureInputController == null)
                {
                    Debug.LogError($"Failed to create {nameof(TouchscreenGestureInputController)}.", this);
                }
            }
#endif
        }

        void DestroyTouchscreenGestureController()
        {
#if TOUCHSCREEN_GESTURE_INPUT_CONTROLLER_AVAILABLE
            if (m_GestureInputController != null && m_GestureInputController.added)
            {
                InputSystem.InputSystem.RemoveDevice(m_GestureInputController);
            }
#endif
        }

        void EnableAllDirectActions()
        {
            m_TapStartPositionAction.EnableDirectAction();
            m_DragCurrentPositionAction.EnableDirectAction();
            m_DragDeltaAction.EnableDirectAction();
            m_PinchStartPositionAction.EnableDirectAction();
            m_PinchGapAction.EnableDirectAction();
            m_PinchGapDeltaAction.EnableDirectAction();
            m_TwistStartPositionAction.EnableDirectAction();
            m_TwistDeltaRotationAction.EnableDirectAction();
            m_ScreenTouchCountAction.EnableDirectAction();
            m_MousePositionAction.EnableDirectAction();
            m_MouseSelectAction.EnableDirectAction();
        }

        void DisableAllDirectActions()
        {
            m_TapStartPositionAction.DisableDirectAction();
            m_DragCurrentPositionAction.DisableDirectAction();
            m_DragDeltaAction.DisableDirectAction();
            m_PinchStartPositionAction.DisableDirectAction();
            m_PinchGapAction.DisableDirectAction();
            m_PinchGapDeltaAction.DisableDirectAction();
            m_TwistStartPositionAction.DisableDirectAction();
            m_TwistDeltaRotationAction.DisableDirectAction();
            m_ScreenTouchCountAction.DisableDirectAction();
            m_MousePositionAction.DisableDirectAction();
            m_MouseSelectAction.DisableDirectAction();
        }

        void SetInputActionProperty(ref InputActionProperty property, InputActionProperty value)
        {
            if (Application.isPlaying)
                property.DisableDirectAction();

            property = value;

            if (Application.isPlaying && isActiveAndEnabled)
                property.EnableDirectAction();
        }

        static bool IsDisabledReferenceAction(InputActionProperty property) =>
            property.reference != null && property.reference.action != null && !property.reference.action.enabled;
    }
}
