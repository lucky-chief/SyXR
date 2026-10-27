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
    /// Exposes runtime state that applications need for precise interaction, through
    /// OpenXR actions under /user/head (the runtime does not filter interaction profiles
    /// for /user/head, so a stock profile can be used for the suggested bindings):
    ///
    ///   /user/head/input/interaction_mode/value               Float   0 = vr_sim, 1 = natural_mouse
    ///   /user/head/input/interaction_mode/natural_mouse/click Boolean
    ///   /user/head/input/presentation_size                    Vector2f (width, height) in pixels
    ///   /user/head/input/presentation_size/x                  Float
    ///   /user/head/input/presentation_size/y                  Float
    ///
    /// interaction_mode follows G / the runtime UI immediately.
    /// presentation_size is the SBS presentation window client size and follows window
    /// resize / Alt+Enter / display-mode changes; it stays inactive until the window exists.
    /// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(
        UiName = "Shiyan Runtime Info",
        Company = "Shiyan",
        Desc = "Exposes the runtime interaction mode and SBS presentation window size through OpenXR actions (/user/head).",
        FeatureId = featureId,
        Version = "0.1.0",
        Category = FeatureCategory.Interaction)]
#endif
    public class RuntimeInfoProfile : OpenXRInteractionFeature
    {
        public const string featureId = "com.shiyan.openxr.runtimeinfo";
        // /user/head 的自定义 path 不受 interaction profile 过滤，随便一个运行时认识的
        // profile 即可（见 docs/Windows适配.md §6.2.1 / §6.2.2）。
        public const string profile = "/interaction_profiles/khr/simple_controller";
        const string kDeviceName = "Shiyan Runtime Info OpenXR";

        [Preserve, InputControlLayout(displayName = kDeviceName)]
        public class RuntimeInfo : OpenXRDevice
        {
            [Preserve, InputControl(name = "interactionmode", layout = "Axis",
                displayName = "Interaction Mode (0=vr_sim, 1=natural_mouse)")]
            public AxisControl interactionMode { get; private set; }

            [Preserve, InputControl(name = "naturalmouse", layout = "Button",
                displayName = "Natural Mouse")]
            public ButtonControl naturalMouse { get; private set; }

            [Preserve, InputControl(name = "presentationsize", layout = "Vector2",
                displayName = "Presentation Size (pixels)")]
            public Vector2Control presentationSize { get; private set; }

            [Preserve, InputControl(name = "presentationwidth", layout = "Axis",
                displayName = "Presentation Width (pixels)")]
            public AxisControl presentationWidth { get; private set; }

            [Preserve, InputControl(name = "presentationheight", layout = "Axis",
                displayName = "Presentation Height (pixels)")]
            public AxisControl presentationHeight { get; private set; }

            protected override void FinishSetup()
            {
                base.FinishSetup();
                Debug.Log("[ShiyanRuntimeInfo] RuntimeInfo FinishSetup (device created)");
                interactionMode = GetChildControl<AxisControl>("interactionmode");
                naturalMouse = GetChildControl<ButtonControl>("naturalmouse");
                presentationSize = GetChildControl<Vector2Control>("presentationsize");
                presentationWidth = GetChildControl<AxisControl>("presentationwidth");
                presentationHeight = GetChildControl<AxisControl>("presentationheight");
            }
        }

        protected override InteractionProfileType GetInteractionProfileType()
            => InteractionProfileType.Device;

        protected override string GetDeviceLayoutName() => nameof(RuntimeInfo);

        protected override void RegisterDeviceLayout()
        {
            Debug.Log("[ShiyanRuntimeInfo] RuntimeInfo RegisterDeviceLayout");
            InputSystem.RegisterLayout(typeof(RuntimeInfo),
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct(kDeviceName));
        }

        protected override void UnregisterDeviceLayout()
        {
            InputSystem.RemoveLayout(nameof(RuntimeInfo));
        }

        protected override void RegisterActionMapsWithRuntime()
        {
            Debug.Log("[ShiyanRuntimeInfo] RuntimeInfo RegisterActionMapsWithRuntime");
            AddActionMap(new ActionMapConfig
            {
                name = "shiyanruntimeinfo",
                localizedName = kDeviceName,
                desiredInteractionProfile = profile,
                manufacturer = "Shiyan",
                serialNumber = "",
                deviceInfos = new List<DeviceConfig>
                {
                    new DeviceConfig
                    {
                        characteristics = InputDeviceCharacteristics.TrackedDevice,
                        userPath = "/user/head"
                    }
                },
                actions = new List<ActionConfig>
                {
                    // 交互模式：vr_sim ⇄ natural_mouse（G / UI 切换后当帧跟随）。
                    Axis1D("interactionmode", "Interaction Mode (0=vr_sim, 1=natural_mouse)",
                           "/input/interaction_mode/value"),
                    Binary("naturalmouse", "Natural Mouse",
                           "/input/interaction_mode/natural_mouse/click"),

                    // SBS 呈现窗口客户区像素尺寸（窗口缩放 / Alt+Enter 后自动跟随）。
                    Axis2D("presentationsize", "Presentation Size (pixels)",
                           "/input/presentation_size"),
                    Axis1D("presentationwidth", "Presentation Width (pixels)",
                           "/input/presentation_size/x"),
                    Axis1D("presentationheight", "Presentation Height (pixels)",
                           "/input/presentation_size/y"),
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
