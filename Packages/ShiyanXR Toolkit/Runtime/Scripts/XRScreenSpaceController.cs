using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Serialization;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace com.unity.shiyantech.xr
{
    /// <summary>
    /// Interprets screen presses and gestures by using actions from the  Input System and converting them
    /// into XR Interaction states, such as Select. It applies the current press position on the screen to
    /// move the transform of the GameObject.
    /// </summary>
    /// <remarks>
    /// This behavior requires that the Input System is enabled in the <b>Active Input Handling</b>
    /// setting in <b>Edit &gt; Project Settings &gt; Player</b> for input values to be read.
    /// Each input action must also be enabled to read the current value of the action. Referenced
    /// input actions in an Input Action Asset are not enabled by default.
    /// </remarks>
    /// <seealso cref="XRBaseController"/>
    /// <seealso cref="TouchscreenGestureInputController"/>
    [AddComponentMenu("XR/XR Screen Space Controller", 11)]
    public partial class XRScreenSpaceController : XRBaseController
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
        bool m_HasCheckedDisabledInputReferenceActions;
        UIInputModule m_UIInputModule;
        UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor m_Interactor;

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void Start()
        {
            if (m_ScreenPointToRayHelper == null)
            {
                Debug.LogWarning($"Could not find associated {nameof(ScreenPointToRayHelper)} in scene." +
                    $"This {nameof(XRScreenSpaceController)} will be disabled.", this);
                enabled = false;
            }
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();
            m_Interactor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
            EnableAllDirectActions();
            InitializeTouchscreenGestureController();
        }

        /// <inheritdoc />
        protected override void OnDisable()
        {
            base.OnDisable();
            DisableAllDirectActions();
            DestroyTouchscreenGestureController();
            m_UIInputModule = null;
        }

        /// <inheritdoc />
        protected override void UpdateTrackingInput(XRControllerState controllerState)
        {
            base.UpdateTrackingInput(controllerState);
            if (controllerState == null || IsPointerOverScreenSpaceCanvas())
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

            if (TryGetScreenPosition(out var screenPos))
            {
                // 屏幕像素 → 世界坐标。这里要的只是方向，传多远的距离都不影响结果
                // （ScreenToWorldPoint 出来的点会被归一化成方向），所以直接借用交互器的射线长度，
                // 让下面那条调试绿线和射线实际能打到多远保持一致
                float rayLength = m_Interactor != null ? m_Interactor.maxRaycastDistance : Mathf.Abs(m_ScreenPointToRayHelper.zOffset);
                var screenWorldPoint = m_ScreenPointToRayHelper.ScreenToWorldPoint(
                    new Vector3(screenPos.x, screenPos.y, 0f),
                    rayLength);

                // 射线起点用 helper 当帧的视点：Constant 是 transform.position + forward * zOffset，
                // 左右眼 / 眉心则是对应眼位（ScreenToWorldPoint 内的 RefreshMatrices 已经算好）
                var viewPosition = m_ScreenPointToRayHelper.viewPosition;
                var ray = new Ray(viewPosition, (screenWorldPoint - viewPosition).normalized);

                // 在 Scene 视图里画出这条射线，方便调试（Debug.DrawRay 只在 Editor / Development Build 生效）
                Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.green);

                controllerState.position = transform.parent != null ? transform.parent.InverseTransformPoint(ray.origin) : ray.origin;
                controllerState.rotation = Quaternion.LookRotation(ray.direction);
                controllerState.inputTrackingState = InputTrackingState.Position | InputTrackingState.Rotation;
                controllerState.isTracked = true;
            }
            else
            {
                controllerState.inputTrackingState = InputTrackingState.None;
                controllerState.isTracked = false;
            }
        }

        /// <inheritdoc />
        protected override void UpdateInput(XRControllerState controllerState)
        {
            base.UpdateInput(controllerState);
            //Debug.Log($"IsMouseSelectPressed: {IsMouseSelectPressed()}");
            if (controllerState == null || IsPointerOverScreenSpaceCanvas())
                return;

            // Warn the user if using referenced actions and they are disabled
            if (!m_HasCheckedDisabledInputReferenceActions &&
                (m_TwistDeltaRotationAction.action != null || m_DragCurrentPositionAction.action != null || m_TapStartPositionAction.action != null))
            {
                if (IsDisabledReferenceAction(m_TwistDeltaRotationAction) ||
                    IsDisabledReferenceAction(m_DragCurrentPositionAction) ||
                    IsDisabledReferenceAction(m_TapStartPositionAction))
                {
                    Debug.LogWarning("'Enable Input Actions' is enabled, but the Tap, Drag, Pinch, and/or Twist Action is disabled." +
                        " The controller input will not be handled correctly until the Input Actions are enabled." +
                        " Input Actions in an Input Action Asset must be explicitly enabled to read the current value of the action." +
                        " The Input Action Manager behavior can be added to a GameObject in a Scene and used to enable all Input Actions in a referenced Input Action Asset.",
                        this);
                }

                m_HasCheckedDisabledInputReferenceActions = true;
            }

            controllerState.ResetFrameDependentStates();

            // 交互全部收敛到鼠标：左键按下即 select
            if (IsMouseSelectPressed())
            {
                controllerState.selectInteractionState.SetFrameState(true, 1f);
            }
            else
            {
                controllerState.selectInteractionState.SetFrameState(false, 0f);
            }

            // UI 按下读的是 uiPressInteractionState（XRBaseControllerInteractor.isUISelectActive → XRRayInteractor.model.select），
            // 它不跟随 select，必须显式同步，否则 UI 只有 hover、点不下去
            controllerState.uiPressInteractionState.SetFrameState(
                controllerState.selectInteractionState.active,
                controllerState.selectInteractionState.value);

            if (m_UseRotationThreshold && TryGetAbsoluteValue(m_TwistDeltaRotationAction.action, out var absRotationValue) && absRotationValue >= m_RotationThreshold)
            {
                scaleDelta = 0f;
            }
            else
            {
                scaleDelta = m_PinchGapDeltaAction.action != null ? m_PinchGapDeltaAction.action.ReadValue<float>() / Screen.dpi : 0f;
            }
        }

        /// <summary>
        /// 当前屏幕点（像素）：直接读鼠标位置（本设备用鼠标操作）。
        /// 按当前分辨率归一化后映射到 helper 的屏幕像素（Editor 的 Game 视图分辨率与设备屏幕不一定相同）。
        /// </summary>
        bool TryGetScreenPosition(out Vector2 screenPosition)
        {
            var mouseAction = m_MousePositionAction.action;
            if (mouseAction != null && Screen.width > 0 && Screen.height > 0)
            {
                Vector2 mousePosition = mouseAction.ReadValue<Vector2>();
                //Debug.Log($"mousePosition: {mousePosition}");
                screenPosition = new Vector2(
                    mousePosition.x / Screen.width * m_ScreenPointToRayHelper.screenWidth,
                    mousePosition.y / Screen.height * m_ScreenPointToRayHelper.screenHeight);
                return true;
            }

            screenPosition = default;
            return false;
        }

        /// <summary>
        /// 鼠标左键是否按下。优先用 action 读；
        /// 本设备 dongle 的“原地点击”不进 Input System（见对话记录），Android 侧由 MouseButtonBridge 提供，
        /// 不依赖旧输入系统。
        /// </summary>
        bool IsMouseSelectPressed()
        {
            var selectAction = m_MouseSelectAction.action;
            if (selectAction != null && (selectAction.IsPressed() || selectAction.WasPressedThisFrame()))
                return true;

            return MouseButtonBridge.IsLeftPressed() || MouseButtonBridge.ConsumeLeftClick();
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
