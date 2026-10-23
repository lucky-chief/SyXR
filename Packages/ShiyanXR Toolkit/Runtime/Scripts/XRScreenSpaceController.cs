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
    /// Interprets screen presses and gestures by using actions from the Input System and applying
    /// the resulting ray pose/select state directly to the XRRayInteractor on the same GameObject.
    /// This is the XRI 3.x replacement for the deprecated XRBaseController-based screen space controller.
    /// </summary>
    /// <remarks>
    /// This behavior requires that the Input System is enabled in the <b>Active Input Handling</b>
    /// setting in <b>Edit &gt; Project Settings &gt; Player</b> for input values to be read.
    /// Each input action must also be enabled to read the current value of the action. Referenced
    /// input actions in an Input Action Asset are not enabled by default.
    /// </remarks>
    /// <seealso cref="TouchscreenGestureInputController"/>
    [AddComponentMenu("XR/XR Screen Space Controller", 11)]
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_ScreenSpaceRayPoseDriver)]
    public partial class XRScreenSpaceController : MonoBehaviour, IXRInputButtonReader, IXRInputValueReader<Vector2>
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
        [Tooltip("The action to use for the mouse scroll wheel. (Vector 2 Control).")]
        InputActionProperty m_MouseScrollAction = new InputActionProperty(CreateMouseScrollAction());
        /// <summary>
        /// The Input System action used to read the mouse scroll wheel for this GameObject. Must be a <see cref="Vector2Control"/> Control.
        /// The default action is bound to both <c>&lt;Mouse&gt;/scroll</c> and <c>&lt;RawMouse&gt;/wheel</c>;
        /// a reference to an action in an Input Action Asset can be assigned instead.
        /// </summary>
        public InputActionProperty mouseScrollAction
        {
            get => m_MouseScrollAction;
            set => SetInputActionProperty(ref m_MouseScrollAction, value);
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
        [Tooltip("触摸拖拽阈值（毫米）。手指按下时的轻微抖动不应被判定为拖拽，" +
                 "否则列表里的按钮只会触发滚动、点不动。按屏幕物理尺寸换算成像素，仅在移动平台生效。")]
        float m_TouchDragThresholdMillimeters = 6f;
        /// <summary>
        /// 触摸拖拽阈值（毫米），按屏幕物理尺寸换算后写入 <c>XRUIInputModule.trackedDeviceDragThresholdMultiplier</c>。
        /// </summary>
        public float touchDragThresholdMillimeters
        {
            get => m_TouchDragThresholdMillimeters;
            set
            {
                m_TouchDragThresholdMillimeters = value;
                ApplyTouchDragThreshold();
            }
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
        Vector2 m_ScrollValue;
        Vector2 m_LastTouchScreenPosition;
        Vector2 m_MousePositionAtTouchRelease;
        bool m_HasLastTouchScreenPosition;
        bool m_HasRecordedReleaseMousePosition;
        bool m_AppliedTouchDragThreshold;

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
                return;
            }

            // 再配置一次：确保 XRRayInteractor 的 OnEnable 没有把 select/uiPress 的输入源覆盖掉。
            if (m_Interactor == null)
                m_Interactor = GetComponent<XRRayInteractor>();
            if (m_Interactor != null)
            {
                ConfigureButtonReader(m_Interactor.selectInput);
                ConfigureButtonReader(m_Interactor.uiPressInput);
                ConfigureScrollReader(m_Interactor.uiScrollInput);
            }

            ApplyTouchDragThreshold();
        }

        void OnEnable()
        {
            m_Interactor = GetComponent<XRRayInteractor>();
            if (m_Interactor != null)
            {
                ConfigureButtonReader(m_Interactor.selectInput);
                ConfigureButtonReader(m_Interactor.uiPressInput);
                ConfigureScrollReader(m_Interactor.uiScrollInput);
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

        void ConfigureScrollReader(XRInputValueReader<Vector2> reader)
        {
            if (reader == null)
                return;

            reader.inputSourceMode = XRInputValueReader.InputSourceMode.ObjectReference;
            reader.SetObjectReference(this);
        }

        void Update()
        {
            if (!m_AppliedTouchDragThreshold)
                ApplyTouchDragThreshold();

            UpdateSelectState();
            UpdatePose();
            UpdateScroll();
            UpdateScaleDelta();
        }

        void UpdateScroll()
        {
            var scrollAction = m_MouseScrollAction.action;
            m_ScrollValue = scrollAction != null ? scrollAction.ReadValue<Vector2>() : Vector2.zero;
        }

        void UpdateSelectState()
        {
            bool pressed = false;

            // 触摸屏：直接读 Unity Input System 的 primaryTouch，保证持续 select。
            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
                pressed = true;

            if (!IsPointerOverScreenSpaceCanvas())
            {
                var selectAction = m_MouseSelectAction.action;
                if (selectAction != null &&
                    (selectAction.IsPressed() || selectAction.WasPressedThisFrame()))
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

            if (!TryGetScreenPosition(out var screenPos))
                return;

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

            if (transform.parent != null)
                transform.localPosition = transform.parent.InverseTransformPoint(ray.origin);
            else
                transform.position = ray.origin;
            transform.localRotation = Quaternion.LookRotation(ray.direction);
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

        Vector2 IXRInputValueReader<Vector2>.ReadValue() => m_ScrollValue;

        bool IXRInputValueReader<Vector2>.TryReadValue(out Vector2 value)
        {
            value = m_ScrollValue;
            return true;
        }

        /// <summary>
        /// 当前屏幕点（像素）：触摸屏优先用 primaryTouch，其次是鼠标位置。
        /// 按当前分辨率归一化后映射到 helper 的屏幕像素（Editor 的 Game 视图分辨率与设备屏幕不一定相同）。
        /// </summary>
        bool TryGetScreenPosition(out Vector2 screenPosition)
        {
            // 触摸屏：按下时用触摸位置；抬手后继续保持在最后触摸位置，
            // 直到鼠标（如果有）真的移动为止。
            // 抬手那一帧如果立刻切回鼠标位置（Android 上通常是 (0,0)），
            // 射线会从按钮上跳走，UIInputModule 在 Released 帧的
            // pointerCurrentRaycast 不再指向按钮，GetEventHandler<IPointerClickHandler>
            // 得到的对象和 pointerPress 不一致，click 就被取消了。
            var touchscreen = Touchscreen.current;
            if (touchscreen != null && Screen.width > 0 && Screen.height > 0)
            {
                var mouseActionForTouch = m_MousePositionAction.action;
                Vector2 mousePosition = mouseActionForTouch != null ? mouseActionForTouch.ReadValue<Vector2>() : Vector2.zero;

                if (touchscreen.primaryTouch.press.isPressed)
                {
                    m_LastTouchScreenPosition = touchscreen.primaryTouch.position.ReadValue();
                    m_HasLastTouchScreenPosition = true;
                    m_HasRecordedReleaseMousePosition = false;
                }
                else if (m_HasLastTouchScreenPosition)
                {
                    // 抬手后记录当时的鼠标位置，鼠标一动就把控制权还给鼠标，
                    // 这样既不会抢鼠标的 hover，也不会让 click 丢掉抬手指的位置。
                    if (!m_HasRecordedReleaseMousePosition)
                    {
                        m_MousePositionAtTouchRelease = mousePosition;
                        m_HasRecordedReleaseMousePosition = true;
                    }
                    else if (mousePosition != m_MousePositionAtTouchRelease)
                    {
                        m_HasLastTouchScreenPosition = false;
                    }
                }

                if (m_HasLastTouchScreenPosition)
                {
                    screenPosition = new Vector2(
                        m_LastTouchScreenPosition.x / Screen.width * m_ScreenPointToRayHelper.screenWidth,
                        m_LastTouchScreenPosition.y / Screen.height * m_ScreenPointToRayHelper.screenHeight);
                    return true;
                }
            }

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

        /// <summary>
        /// 触摸屏上手指轻微抖动就会超过 uGUI 默认的拖拽阈值（10px × 2，在本工程的屏幕像素密度下不到 2mm），
        /// 于是“按下-抬起”被当成一次拖拽：ScrollRect 能滚，但列表里的按钮永远收不到 click。
        /// 这里按屏幕物理尺寸把阈值换算成毫米，只放大 XR 追踪指针的拖拽阈值，
        /// <see cref="EventSystem.pixelDragThreshold"/> 本身不动，鼠标 / 2D 触摸的行为保持不变。
        /// </summary>
        void ApplyTouchDragThreshold()
        {
            if (!Application.isMobilePlatform || m_ScreenPointToRayHelper == null)
                return;

            var eventSystem = EventSystem.current;
            if (eventSystem == null || eventSystem.pixelDragThreshold <= 0)
                return;

            if (!FindUIInputModule() || m_UIInputModule == null)
                return;

            float screenWorldWidth = m_ScreenPointToRayHelper.screenSizeInWorldUnits.x;
            if (screenWorldWidth <= 0f || Screen.width <= 0)
                return;

            // 本工程的屏幕像素空间就是相机看到的整块屏幕，宽度对应 Screen.width
            float pixelsPerMeter = Screen.width / screenWorldWidth;
            float thresholdInPixels = m_TouchDragThresholdMillimeters * 0.001f * pixelsPerMeter;
            int baseThreshold = eventSystem.pixelDragThreshold;
            if (thresholdInPixels <= baseThreshold)
                return;

            m_UIInputModule.trackedDeviceDragThresholdMultiplier = thresholdInPixels / baseThreshold;
            m_AppliedTouchDragThreshold = true;
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
            m_MouseScrollAction.EnableDirectAction();
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
            m_MouseScrollAction.DisableDirectAction();
        }

        /// <summary>
        /// 默认滚轮 action：同时绑定普通鼠标和本设备 dongle 暴露的 RawMouse，
        /// 避免在 Android 上只绑定 &lt;Mouse&gt;/scroll 时收不到滚轮。
        /// </summary>
        static InputAction CreateMouseScrollAction()
        {
            var action = new InputAction("Mouse Scroll", InputActionType.PassThrough, expectedControlType: "Vector2");
            action.AddBinding("<Mouse>/scroll");
            action.AddBinding("<RawMouse>/wheel");
            return action;
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
