using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
using UnityEngine.Scripting;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Input;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace Shiyan.OpenXR.RawInput
{
    /// <summary>
    /// Optional adapter for the runtime's standard /user/gamepad paths
    /// (/interaction_profiles/microsoft/xbox_controller).
    /// Enable this feature if your project wants the runtime's XInput gamepad state
    /// through OpenXR instead of Unity's native Gamepad device.
    /// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(
        UiName = "Shiyan Raw Gamepad",
        Company = "Shiyan",
        Desc = "Exposes the runtime's /user/gamepad state through OpenXR actions.",
        FeatureId = featureId,
        Version = "0.1.0",
        Category = FeatureCategory.Interaction)]
#endif
    public class RawGamepadProfile : OpenXRInteractionFeature
    {
        public const string featureId = "com.shiyan.openxr.rawgamepad";
        public const string profile = "/interaction_profiles/microsoft/xbox_controller";
        const string kDeviceName = "Shiyan Raw Gamepad OpenXR";

        [Preserve, InputControlLayout(displayName = kDeviceName)]
        public class RawGamepad : OpenXRDevice
        {
            [Preserve, InputControl(name = "leftstick", layout = "Vector2", displayName = "Left Stick")]
            public Vector2Control leftStick { get; private set; }

            [Preserve, InputControl(name = "rightstick", layout = "Vector2", displayName = "Right Stick")]
            public Vector2Control rightStick { get; private set; }

            [Preserve, InputControl(name = "lefttrigger", layout = "Axis", displayName = "Left Trigger")]
            public AxisControl leftTrigger { get; private set; }

            [Preserve, InputControl(name = "righttrigger", layout = "Axis", displayName = "Right Trigger")]
            public AxisControl rightTrigger { get; private set; }

            [Preserve, InputControl(name = "buttona", layout = "Button", displayName = "A")]
            public ButtonControl buttonA { get; private set; }

            [Preserve, InputControl(name = "buttonb", layout = "Button", displayName = "B")]
            public ButtonControl buttonB { get; private set; }

            [Preserve, InputControl(name = "buttonx", layout = "Button", displayName = "X")]
            public ButtonControl buttonX { get; private set; }

            [Preserve, InputControl(name = "buttony", layout = "Button", displayName = "Y")]
            public ButtonControl buttonY { get; private set; }

            [Preserve, InputControl(name = "menu", layout = "Button", displayName = "Menu")]
            public ButtonControl menu { get; private set; }

            [Preserve, InputControl(name = "view", layout = "Button", displayName = "View")]
            public ButtonControl view { get; private set; }

            [Preserve, InputControl(name = "leftshoulder", layout = "Button", displayName = "Left Shoulder")]
            public ButtonControl leftShoulder { get; private set; }

            [Preserve, InputControl(name = "rightshoulder", layout = "Button", displayName = "Right Shoulder")]
            public ButtonControl rightShoulder { get; private set; }

            [Preserve, InputControl(name = "dpadup", layout = "Button", displayName = "D-Pad Up")]
            public ButtonControl dpadUp { get; private set; }

            [Preserve, InputControl(name = "dpaddown", layout = "Button", displayName = "D-Pad Down")]
            public ButtonControl dpadDown { get; private set; }

            [Preserve, InputControl(name = "dpadleft", layout = "Button", displayName = "D-Pad Left")]
            public ButtonControl dpadLeft { get; private set; }

            [Preserve, InputControl(name = "dpadright", layout = "Button", displayName = "D-Pad Right")]
            public ButtonControl dpadRight { get; private set; }

            [Preserve, InputControl(name = "leftstickclick", layout = "Button", displayName = "Left Stick Click")]
            public ButtonControl leftStickClick { get; private set; }

            [Preserve, InputControl(name = "rightstickclick", layout = "Button", displayName = "Right Stick Click")]
            public ButtonControl rightStickClick { get; private set; }

            protected override void FinishSetup()
            {
                base.FinishSetup();
                Debug.Log("[ShiyanRawInput] RawGamepad FinishSetup (device created)");
                leftStick = GetChildControl<Vector2Control>("leftstick");
                rightStick = GetChildControl<Vector2Control>("rightstick");
                leftTrigger = GetChildControl<AxisControl>("lefttrigger");
                rightTrigger = GetChildControl<AxisControl>("righttrigger");
                buttonA = GetChildControl<ButtonControl>("buttona");
                buttonB = GetChildControl<ButtonControl>("buttonb");
                buttonX = GetChildControl<ButtonControl>("buttonx");
                buttonY = GetChildControl<ButtonControl>("buttony");
                menu = GetChildControl<ButtonControl>("menu");
                view = GetChildControl<ButtonControl>("view");
                leftShoulder = GetChildControl<ButtonControl>("leftshoulder");
                rightShoulder = GetChildControl<ButtonControl>("rightshoulder");
                dpadUp = GetChildControl<ButtonControl>("dpadup");
                dpadDown = GetChildControl<ButtonControl>("dpaddown");
                dpadLeft = GetChildControl<ButtonControl>("dpadleft");
                dpadRight = GetChildControl<ButtonControl>("dpadright");
                leftStickClick = GetChildControl<ButtonControl>("leftstickclick");
                rightStickClick = GetChildControl<ButtonControl>("rightstickclick");
            }
        }

        protected override InteractionProfileType GetInteractionProfileType()
            => InteractionProfileType.Device;

        protected override string GetDeviceLayoutName() => nameof(RawGamepad);

        protected override void RegisterDeviceLayout()
        {
            Debug.Log("[ShiyanRawInput] RawGamepad RegisterDeviceLayout");
            InputSystem.RegisterLayout(typeof(RawGamepad),
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct(kDeviceName));
        }

        protected override void UnregisterDeviceLayout()
        {
            InputSystem.RemoveLayout(nameof(RawGamepad));
        }

        protected override void RegisterActionMapsWithRuntime()
        {
            Debug.Log("[ShiyanRawInput] RawGamepad RegisterActionMapsWithRuntime");
            AddActionMap(new ActionMapConfig
            {
                name = "shiyanrawgamepad",
                localizedName = kDeviceName,
                desiredInteractionProfile = profile,
                manufacturer = "Shiyan",
                serialNumber = "",
                deviceInfos = new List<DeviceConfig>
                {
                    new DeviceConfig
                    {
                        characteristics = InputDeviceCharacteristics.Controller,
                        userPath = "/user/gamepad"
                    }
                },
                actions = new List<ActionConfig>
                {
                    Axis2D("leftstick", "Left Stick", "/input/thumbstick_left"),
                    Axis2D("rightstick", "Right Stick", "/input/thumbstick_right"),
                    Axis1D("lefttrigger", "Left Trigger", "/input/trigger_left/value"),
                    Axis1D("righttrigger", "Right Trigger", "/input/trigger_right/value"),
                    Binary("buttona", "A", "/input/a/click"),
                    Binary("buttonb", "B", "/input/b/click"),
                    Binary("buttonx", "X", "/input/x/click"),
                    Binary("buttony", "Y", "/input/y/click"),
                    Binary("menu", "Menu", "/input/menu/click"),
                    Binary("view", "View", "/input/view/click"),
                    Binary("leftshoulder", "Left Shoulder", "/input/shoulder_left/click"),
                    Binary("rightshoulder", "Right Shoulder", "/input/shoulder_right/click"),
                    Binary("dpadup", "D-Pad Up", "/input/dpad_up/click"),
                    Binary("dpaddown", "D-Pad Down", "/input/dpad_down/click"),
                    Binary("dpadleft", "D-Pad Left", "/input/dpad_left/click"),
                    Binary("dpadright", "D-Pad Right", "/input/dpad_right/click"),
                    Binary("leftstickclick", "Left Stick Click", "/input/thumbstick_left/click"),
                    Binary("rightstickclick", "Right Stick Click", "/input/thumbstick_right/click"),
                }
            });
        }

        static ActionConfig Binary(string name, string localizedName, string path) => new ActionConfig
        {
            name = name,
            localizedName = localizedName,
            type = ActionType.Binary,
            bindings = new List<ActionBinding>
            {
                new ActionBinding { interactionPath = path, interactionProfileName = profile }
            }
        };

        static ActionConfig Axis1D(string name, string localizedName, string path) => new ActionConfig
        {
            name = name,
            localizedName = localizedName,
            type = ActionType.Axis1D,
            bindings = new List<ActionBinding>
            {
                new ActionBinding { interactionPath = path, interactionProfileName = profile }
            }
        };

        static ActionConfig Axis2D(string name, string localizedName, string path) => new ActionConfig
        {
            name = name,
            localizedName = localizedName,
            type = ActionType.Axis2D,
            bindings = new List<ActionBinding>
            {
                new ActionBinding { interactionPath = path, interactionProfileName = profile }
            }
        };
    }
}
