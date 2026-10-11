// Assets/Scripts/RuntimeInteractionModeFeature.cs
//
// 让工程能感知运行时当前是 vr_sim 还是 natural_mouse（含"变化"）。
//
// 运行时侧（shiyan-openxr docs §6.2.1）提供两条合成输入，挂在 /user/head 下：
//   /user/head/input/interaction_mode/value                 Float：0 = vr_sim，1 = natural_mouse
//   /user/head/input/interaction_mode/natural_mouse/click    Boolean：true = natural_mouse
// 挂 /user/head 是刻意的：运行时对 /user/head 不做 interaction-profile 过滤，
// 所以这边随便给一个 profile（下面用 khr/simple_controller）就能解析，且不影响手柄/手势。
//
// 本 feature 把它们注册成 Input System 设备/控件后，可以这样用：
//   InputAction a = new InputAction(type: InputActionType.Value, binding: "<ShiyanInteractionMode>/interactionmode");
//   float mode = a.ReadValue<float>();          // 0 / 1；每帧比较即知"变化"
//   bool natural = a.ReadValue<float>() > 0.5f;
//   // 或者 Button 那条：<ShiyanInteractionMode>/naturalmouse
//   if (buttonAction.WasPressedThisFrame()) { ... }   // 也可以订阅 started/canceled
//   （控件名就是下面 ActionConfig.name，规则是"只能小写 a-z"；也可以按 usage 绑 "*/{InteractionMode}"。）
// 也可以直接在 .inputactions 资产里加 binding（编辑器里手填上面的 path 字符串）。
//
// ⚠️ 这是自定义 feature：需要在 Project Settings → XR Plug-in Management → OpenXR
//    的功能列表里勾上（默认不启用），否则设备/控件不会注册。

using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
using UnityEngine.Scripting;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Input;

#if UNITY_EDITOR
// 编辑器靠这个属性发现/列出 feature —— 少了它，Project Settings 里根本看不到本 feature。
[UnityEditor.XR.OpenXR.Features.OpenXRFeature(
    UiName = "Shiyan Interaction Mode",
    BuildTargetGroups = new[] { UnityEditor.BuildTargetGroup.Standalone, UnityEditor.BuildTargetGroup.Android },
    Company = "Shiyan",
    Desc = "把运行时的交互模式合成输入（vr_sim / natural_mouse）注册进 Input System。",
    DocumentationLink = "",
    Version = "0.0.1",
    OpenxrExtensionStrings = "",
    Category = UnityEditor.XR.OpenXR.Features.FeatureCategory.Interaction,
    FeatureId = "com.shiyan.openxr.feature.interactionmode")]
#endif
[Preserve]
public class RuntimeInteractionModeFeature : OpenXRInteractionFeature
{
    /// <summary>Input System 里的布局名：绑定时写 &lt;ShiyanInteractionMode&gt;/...</summary>
    public const string kLayoutName = "ShiyanInteractionMode";

    /// <summary>设备"产品名"：必须与下面 ActionMapConfig.localizedName 一致（插件按它匹配 layout）。</summary>
    const string kDeviceLocalizedName = "Shiyan Interaction Mode";

    /// <summary>/user/head 不挑 profile，这个值本身不参与解析（但要有，且两处一致）。</summary>
    const string kProfile = "/interaction_profiles/khr/simple_controller";

    const string kUserPath = "/user/head";
    // ⚠️ interactionPath 必须是**相对**路径：插件会自动前缀设备的 userPath
    //    （包内 EyeGazeInteraction 就是 userPath="/user/eyes_ext" +
    //      interactionPath="/input/gaze_ext/pose"）。写成完整路径会被拼成
    //    "/user/head/user/head/..."，解析不到 → 读出来永远是 0。
    const string kValueInteraction = "/input/interaction_mode/value";
    const string kNaturalInteraction = "/input/interaction_mode/natural_mouse/click";

    /// <summary>自定义设备：两个控件（Float 的模式值 + Button 的 natural_mouse）。</summary>
    [Preserve, InputControlLayout(displayName = "Shiyan Interaction Mode (OpenXR)", isGenericTypeOfDevice = true)]
    public class ShiyanInteractionModeDevice : OpenXRDevice
    {
        [Preserve, InputControl(offset = 0, usages = new[] { "InteractionMode" })]
        public AxisControl interactionmode { get; private set; }

        [Preserve, InputControl(offset = 4, usages = new[] { "NaturalMouse" })]
        public ButtonControl naturalmouse { get; private set; }

        protected override void FinishSetup()
        {
            base.FinishSetup();
            interactionmode = GetChildControl<AxisControl>("interactionmode");
            naturalmouse = GetChildControl<ButtonControl>("naturalmouse");
        }
    }

    protected override void RegisterDeviceLayout()
    {
        // 注：包里那个 OpenXRLoaderEnabledForSelectedBuildTarget() 是 internal，外部程序集调不到，
        // 这里直接注册（编辑器里多注册一个 layout 无副作用）。
        InputSystem.RegisterLayout(typeof(ShiyanInteractionModeDevice), kLayoutName,
            matches: new InputDeviceMatcher()
                .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                .WithProduct(kDeviceLocalizedName));
    }

    protected override void UnregisterDeviceLayout()
    {
        InputSystem.RemoveLayout(kLayoutName);
    }

    protected override string GetDeviceLayoutName() => kLayoutName;

    protected override InteractionProfileType GetInteractionProfileType() =>
        typeof(ShiyanInteractionModeDevice).IsSubclassOf(typeof(XRController))
            ? InteractionProfileType.XRController
            : InteractionProfileType.Device;

    protected override void RegisterActionMapsWithRuntime()
    {
        ActionMapConfig actionMap = new ActionMapConfig
        {
            name = kLayoutName,
            localizedName = kDeviceLocalizedName,
            desiredInteractionProfile = kProfile,
            manufacturer = "",
            serialNumber = "",
            deviceInfos = new List<DeviceConfig>
            {
                new DeviceConfig
                {
                    characteristics = InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice,
                    userPath = kUserPath
                }
            },
            actions = new List<ActionConfig>
            {
                new ActionConfig
                {
                    name = "interactionmode",   // ActionConfig.name 只能用小写 a-z（Input System 控件名）
                    localizedName = "Interaction Mode",
                    type = ActionType.Axis1D,
                    usages = new List<string> { "InteractionMode" },
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding { interactionPath = kValueInteraction, interactionProfileName = kProfile }
                    }
                },
                new ActionConfig
                {
                    name = "naturalmouse",
                    localizedName = "Natural Mouse",
                    type = ActionType.Binary,
                    usages = new List<string> { "NaturalMouse" },
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding { interactionPath = kNaturalInteraction, interactionProfileName = kProfile }
                    }
                }
            }
        };

        AddActionMap(actionMap);
    }
}
