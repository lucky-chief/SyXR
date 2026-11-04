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
    /// Exposes raw mouse button/position/delta/wheel states through OpenXR actions
    /// under /user/mouse. The runtime claims /interaction_profiles/shiyan/raw_mouse.
    /// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(
        UiName = "Shiyan Raw Mouse",
        Company = "Shiyan",
        Desc = "Exposes raw mouse state through OpenXR actions (/user/mouse).",
        FeatureId = featureId,
        Version = "0.1.0",
        Category = FeatureCategory.Interaction)]
#endif
    public class RawMouseProfile : OpenXRInteractionFeature
    {
        public const string featureId = "com.shiyan.openxr.rawmouse";
        public const string profile = "/interaction_profiles/shiyan/raw_mouse";
        const string kDeviceName = "Shiyan Raw Mouse OpenXR";

        [Preserve, InputControlLayout(displayName = kDeviceName)]
        public class RawMouse : OpenXRDevice
        {
            [Preserve, InputControl(name = "left", layout = "Button", displayName = "Left")]
            public ButtonControl leftButton { get; private set; }

            [Preserve, InputControl(name = "right", layout = "Button", displayName = "Right")]
            public ButtonControl rightButton { get; private set; }

            [Preserve, InputControl(name = "middle", layout = "Button", displayName = "Middle")]
            public ButtonControl middleButton { get; private set; }

            [Preserve, InputControl(name = "back", layout = "Button", displayName = "Back")]
            public ButtonControl backButton { get; private set; }

            [Preserve, InputControl(name = "forward", layout = "Button", displayName = "Forward")]
            public ButtonControl forwardButton { get; private set; }

            [Preserve, InputControl(name = "position", layout = "Vector2", displayName = "Position (normalized)")]
            public Vector2Control position { get; private set; }

            [Preserve, InputControl(name = "positionx", layout = "Axis", displayName = "Position X (normalized)")]
            public AxisControl positionX { get; private set; }

            [Preserve, InputControl(name = "positiony", layout = "Axis", displayName = "Position Y (normalized)")]
            public AxisControl positionY { get; private set; }

            [Preserve, InputControl(name = "positionpx", layout = "Vector2", displayName = "Position (pixels)")]
            public Vector2Control positionPx { get; private set; }

            [Preserve, InputControl(name = "positionpxx", layout = "Axis", displayName = "Position X (pixels)")]
            public AxisControl positionPxX { get; private set; }

            [Preserve, InputControl(name = "positionpxy", layout = "Axis", displayName = "Position Y (pixels)")]
            public AxisControl positionPxY { get; private set; }

            [Preserve, InputControl(name = "delta", layout = "Vector2", displayName = "Delta")]
            public Vector2Control delta { get; private set; }

            [Preserve, InputControl(name = "deltax", layout = "Axis", displayName = "Delta X")]
            public AxisControl deltaX { get; private set; }

            [Preserve, InputControl(name = "deltay", layout = "Axis", displayName = "Delta Y")]
            public AxisControl deltaY { get; private set; }

            [Preserve, InputControl(name = "wheel", layout = "Vector2", displayName = "Wheel")]
            public Vector2Control wheel { get; private set; }

            [Preserve, InputControl(name = "wheelx", layout = "Axis", displayName = "Wheel X")]
            public AxisControl wheelX { get; private set; }

            [Preserve, InputControl(name = "wheely", layout = "Axis", displayName = "Wheel Y")]
            public AxisControl wheelY { get; private set; }

            protected override void FinishSetup()
            {
                base.FinishSetup();
                Debug.Log("[ShiyanRawInput] RawMouse FinishSetup (device created)");
                leftButton = GetChildControl<ButtonControl>("left");
                rightButton = GetChildControl<ButtonControl>("right");
                middleButton = GetChildControl<ButtonControl>("middle");
                backButton = GetChildControl<ButtonControl>("back");
                forwardButton = GetChildControl<ButtonControl>("forward");
                position = GetChildControl<Vector2Control>("position");
                positionX = GetChildControl<AxisControl>("positionx");
                positionY = GetChildControl<AxisControl>("positiony");
                positionPx = GetChildControl<Vector2Control>("positionpx");
                positionPxX = GetChildControl<AxisControl>("positionpxx");
                positionPxY = GetChildControl<AxisControl>("positionpxy");
                delta = GetChildControl<Vector2Control>("delta");
                deltaX = GetChildControl<AxisControl>("deltax");
                deltaY = GetChildControl<AxisControl>("deltay");
                wheel = GetChildControl<Vector2Control>("wheel");
                wheelX = GetChildControl<AxisControl>("wheelx");
                wheelY = GetChildControl<AxisControl>("wheely");
            }
        }

        protected override InteractionProfileType GetInteractionProfileType()
            => InteractionProfileType.Device;

        protected override string GetDeviceLayoutName() => nameof(RawMouse);

        protected override void RegisterDeviceLayout()
        {
            Debug.Log("[ShiyanRawInput] RawMouse RegisterDeviceLayout");
            InputSystem.RegisterLayout(typeof(RawMouse),
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct(kDeviceName));
        }

        protected override void UnregisterDeviceLayout()
        {
            InputSystem.RemoveLayout(nameof(RawMouse));
        }

        protected override void RegisterActionMapsWithRuntime()
        {
            Debug.Log("[ShiyanRawInput] RawMouse RegisterActionMapsWithRuntime");
            AddActionMap(new ActionMapConfig
            {
                name = "shiyanrawmouse",
                localizedName = kDeviceName,
                desiredInteractionProfile = profile,
                manufacturer = "Shiyan",
                serialNumber = "",
                deviceInfos = new List<DeviceConfig>
                {
                    new DeviceConfig
                    {
                        characteristics = InputDeviceCharacteristics.TrackedDevice,
                        userPath = "/user/mouse"
                    }
                },
                actions = new List<ActionConfig>
                {
                    Binary("left", "Left", "/input/left/click"),
                    Binary("right", "Right", "/input/right/click"),
                    Binary("middle", "Middle", "/input/middle/click"),
                    Binary("back", "Back", "/input/back/click"),
                    Binary("forward", "Forward", "/input/forward/click"),

                    Axis2D("position", "Position (normalized)", "/input/position"),
                    Axis1D("positionx", "Position X (normalized)", "/input/position/x"),
                    Axis1D("positiony", "Position Y (normalized)", "/input/position/y"),

                    Axis2D("positionpx", "Position (pixels)", "/input/position_px"),
                    Axis1D("positionpxx", "Position X (pixels)", "/input/position_px/x"),
                    Axis1D("positionpxy", "Position Y (pixels)", "/input/position_px/y"),

                    Axis2D("delta", "Delta", "/input/delta"),
                    Axis1D("deltax", "Delta X", "/input/delta/x"),
                    Axis1D("deltay", "Delta Y", "/input/delta/y"),

                    Axis2D("wheel", "Wheel", "/input/wheel"),
                    Axis1D("wheelx", "Wheel X", "/input/wheel/x"),
                    Axis1D("wheely", "Wheel Y", "/input/wheel/y"),
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
