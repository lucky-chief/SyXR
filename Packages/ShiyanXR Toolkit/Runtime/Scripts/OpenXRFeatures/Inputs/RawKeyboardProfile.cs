// Curated key set for the first Unity integration. The runtime accepts a wider set of
// keys (see docs/Windows适配.md); add more controls/actions here as needed.
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
    /// Exposes raw keyboard key states through OpenXR actions under /user/keyboard.
    /// The runtime claims /interaction_profiles/shiyan/raw_keyboard.
    /// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(
        UiName = "Shiyan Raw Keyboard",
        Company = "Shiyan",
        Desc = "Exposes raw keyboard key states through OpenXR actions (/user/keyboard).",
        FeatureId = featureId,
        Version = "0.1.0",
        Category = FeatureCategory.Interaction)]
#endif
    public class RawKeyboardProfile : OpenXRInteractionFeature
    {
        public const string featureId = "com.shiyan.openxr.rawkeyboard";
        public const string profile = "/interaction_profiles/shiyan/raw_keyboard";
        const string kDeviceName = "Shiyan Raw Keyboard OpenXR";

        [Preserve, InputControlLayout(displayName = kDeviceName)]
        public class RawKeyboard : OpenXRDevice
        {
            [Preserve, InputControl(name = "keya", layout = "Button", displayName = "A")]
            public ButtonControl keyA { get; private set; }

            [Preserve, InputControl(name = "keyb", layout = "Button", displayName = "B")]
            public ButtonControl keyB { get; private set; }

            [Preserve, InputControl(name = "keyc", layout = "Button", displayName = "C")]
            public ButtonControl keyC { get; private set; }

            [Preserve, InputControl(name = "keyd", layout = "Button", displayName = "D")]
            public ButtonControl keyD { get; private set; }

            [Preserve, InputControl(name = "keye", layout = "Button", displayName = "E")]
            public ButtonControl keyE { get; private set; }

            [Preserve, InputControl(name = "keyf", layout = "Button", displayName = "F")]
            public ButtonControl keyF { get; private set; }

            [Preserve, InputControl(name = "keyg", layout = "Button", displayName = "G")]
            public ButtonControl keyG { get; private set; }

            [Preserve, InputControl(name = "keyh", layout = "Button", displayName = "H")]
            public ButtonControl keyH { get; private set; }

            [Preserve, InputControl(name = "keyi", layout = "Button", displayName = "I")]
            public ButtonControl keyI { get; private set; }

            [Preserve, InputControl(name = "keyj", layout = "Button", displayName = "J")]
            public ButtonControl keyJ { get; private set; }

            [Preserve, InputControl(name = "keyk", layout = "Button", displayName = "K")]
            public ButtonControl keyK { get; private set; }

            [Preserve, InputControl(name = "keyl", layout = "Button", displayName = "L")]
            public ButtonControl keyL { get; private set; }

            [Preserve, InputControl(name = "keym", layout = "Button", displayName = "M")]
            public ButtonControl keyM { get; private set; }

            [Preserve, InputControl(name = "keyn", layout = "Button", displayName = "N")]
            public ButtonControl keyN { get; private set; }

            [Preserve, InputControl(name = "keyo", layout = "Button", displayName = "O")]
            public ButtonControl keyO { get; private set; }

            [Preserve, InputControl(name = "keyp", layout = "Button", displayName = "P")]
            public ButtonControl keyP { get; private set; }

            [Preserve, InputControl(name = "keyq", layout = "Button", displayName = "Q")]
            public ButtonControl keyQ { get; private set; }

            [Preserve, InputControl(name = "keyr", layout = "Button", displayName = "R")]
            public ButtonControl keyR { get; private set; }

            [Preserve, InputControl(name = "keys", layout = "Button", displayName = "S")]
            public ButtonControl keyS { get; private set; }

            [Preserve, InputControl(name = "keyt", layout = "Button", displayName = "T")]
            public ButtonControl keyT { get; private set; }

            [Preserve, InputControl(name = "keyu", layout = "Button", displayName = "U")]
            public ButtonControl keyU { get; private set; }

            [Preserve, InputControl(name = "keyv", layout = "Button", displayName = "V")]
            public ButtonControl keyV { get; private set; }

            [Preserve, InputControl(name = "keyw", layout = "Button", displayName = "W")]
            public ButtonControl keyW { get; private set; }

            [Preserve, InputControl(name = "keyx", layout = "Button", displayName = "X")]
            public ButtonControl keyX { get; private set; }

            [Preserve, InputControl(name = "keyy", layout = "Button", displayName = "Y")]
            public ButtonControl keyY { get; private set; }

            [Preserve, InputControl(name = "keyz", layout = "Button", displayName = "Z")]
            public ButtonControl keyZ { get; private set; }

            [Preserve, InputControl(name = "numzero", layout = "Button", displayName = "0")]
            public ButtonControl numZero { get; private set; }

            [Preserve, InputControl(name = "numone", layout = "Button", displayName = "1")]
            public ButtonControl numOne { get; private set; }

            [Preserve, InputControl(name = "numtwo", layout = "Button", displayName = "2")]
            public ButtonControl numTwo { get; private set; }

            [Preserve, InputControl(name = "numthree", layout = "Button", displayName = "3")]
            public ButtonControl numThree { get; private set; }

            [Preserve, InputControl(name = "numfour", layout = "Button", displayName = "4")]
            public ButtonControl numFour { get; private set; }

            [Preserve, InputControl(name = "numfive", layout = "Button", displayName = "5")]
            public ButtonControl numFive { get; private set; }

            [Preserve, InputControl(name = "numsix", layout = "Button", displayName = "6")]
            public ButtonControl numSix { get; private set; }

            [Preserve, InputControl(name = "numseven", layout = "Button", displayName = "7")]
            public ButtonControl numSeven { get; private set; }

            [Preserve, InputControl(name = "numeight", layout = "Button", displayName = "8")]
            public ButtonControl numEight { get; private set; }

            [Preserve, InputControl(name = "numnine", layout = "Button", displayName = "9")]
            public ButtonControl numNine { get; private set; }

            [Preserve, InputControl(name = "fone", layout = "Button", displayName = "F1")]
            public ButtonControl fOne { get; private set; }

            [Preserve, InputControl(name = "ftwo", layout = "Button", displayName = "F2")]
            public ButtonControl fTwo { get; private set; }

            [Preserve, InputControl(name = "fthree", layout = "Button", displayName = "F3")]
            public ButtonControl fThree { get; private set; }

            [Preserve, InputControl(name = "ffour", layout = "Button", displayName = "F4")]
            public ButtonControl fFour { get; private set; }

            [Preserve, InputControl(name = "ffive", layout = "Button", displayName = "F5")]
            public ButtonControl fFive { get; private set; }

            [Preserve, InputControl(name = "fsix", layout = "Button", displayName = "F6")]
            public ButtonControl fSix { get; private set; }

            [Preserve, InputControl(name = "fseven", layout = "Button", displayName = "F7")]
            public ButtonControl fSeven { get; private set; }

            [Preserve, InputControl(name = "feight", layout = "Button", displayName = "F8")]
            public ButtonControl fEight { get; private set; }

            [Preserve, InputControl(name = "fnine", layout = "Button", displayName = "F9")]
            public ButtonControl fNine { get; private set; }

            [Preserve, InputControl(name = "ften", layout = "Button", displayName = "F10")]
            public ButtonControl fTen { get; private set; }

            [Preserve, InputControl(name = "feleven", layout = "Button", displayName = "F11")]
            public ButtonControl fEleven { get; private set; }

            [Preserve, InputControl(name = "ftwelve", layout = "Button", displayName = "F12")]
            public ButtonControl fTwelve { get; private set; }

            [Preserve, InputControl(name = "space", layout = "Button", displayName = "Space")]
            public ButtonControl space { get; private set; }

            [Preserve, InputControl(name = "enter", layout = "Button", displayName = "Enter")]
            public ButtonControl enter { get; private set; }

            [Preserve, InputControl(name = "escape", layout = "Button", displayName = "Escape")]
            public ButtonControl escape { get; private set; }

            [Preserve, InputControl(name = "tab", layout = "Button", displayName = "Tab")]
            public ButtonControl tab { get; private set; }

            [Preserve, InputControl(name = "backspace", layout = "Button", displayName = "Backspace")]
            public ButtonControl backspace { get; private set; }

            [Preserve, InputControl(name = "delete", layout = "Button", displayName = "Delete")]
            public ButtonControl delete { get; private set; }

            [Preserve, InputControl(name = "insert", layout = "Button", displayName = "Insert")]
            public ButtonControl insert { get; private set; }

            [Preserve, InputControl(name = "home", layout = "Button", displayName = "Home")]
            public ButtonControl home { get; private set; }

            [Preserve, InputControl(name = "end", layout = "Button", displayName = "End")]
            public ButtonControl end { get; private set; }

            [Preserve, InputControl(name = "pageup", layout = "Button", displayName = "PageUp")]
            public ButtonControl pageUp { get; private set; }

            [Preserve, InputControl(name = "pagedown", layout = "Button", displayName = "PageDown")]
            public ButtonControl pageDown { get; private set; }

            [Preserve, InputControl(name = "leftarrow", layout = "Button", displayName = "Left Arrow")]
            public ButtonControl leftArrow { get; private set; }

            [Preserve, InputControl(name = "rightarrow", layout = "Button", displayName = "Right Arrow")]
            public ButtonControl rightArrow { get; private set; }

            [Preserve, InputControl(name = "uparrow", layout = "Button", displayName = "Up Arrow")]
            public ButtonControl upArrow { get; private set; }

            [Preserve, InputControl(name = "downarrow", layout = "Button", displayName = "Down Arrow")]
            public ButtonControl downArrow { get; private set; }

            [Preserve, InputControl(name = "leftshift", layout = "Button", displayName = "Left Shift")]
            public ButtonControl leftShift { get; private set; }

            [Preserve, InputControl(name = "rightshift", layout = "Button", displayName = "Right Shift")]
            public ButtonControl rightShift { get; private set; }

            [Preserve, InputControl(name = "leftctrl", layout = "Button", displayName = "Left Ctrl")]
            public ButtonControl leftCtrl { get; private set; }

            [Preserve, InputControl(name = "rightctrl", layout = "Button", displayName = "Right Ctrl")]
            public ButtonControl rightCtrl { get; private set; }

            [Preserve, InputControl(name = "leftalt", layout = "Button", displayName = "Left Alt")]
            public ButtonControl leftAlt { get; private set; }

            [Preserve, InputControl(name = "rightalt", layout = "Button", displayName = "Right Alt")]
            public ButtonControl rightAlt { get; private set; }

            protected override void FinishSetup()
            {
                base.FinishSetup();
                Debug.Log("[ShiyanRawInput] RawKeyboard FinishSetup (device created)");
                keyA = GetChildControl<ButtonControl>("keya");
                keyB = GetChildControl<ButtonControl>("keyb");
                keyC = GetChildControl<ButtonControl>("keyc");
                keyD = GetChildControl<ButtonControl>("keyd");
                keyE = GetChildControl<ButtonControl>("keye");
                keyF = GetChildControl<ButtonControl>("keyf");
                keyG = GetChildControl<ButtonControl>("keyg");
                keyH = GetChildControl<ButtonControl>("keyh");
                keyI = GetChildControl<ButtonControl>("keyi");
                keyJ = GetChildControl<ButtonControl>("keyj");
                keyK = GetChildControl<ButtonControl>("keyk");
                keyL = GetChildControl<ButtonControl>("keyl");
                keyM = GetChildControl<ButtonControl>("keym");
                keyN = GetChildControl<ButtonControl>("keyn");
                keyO = GetChildControl<ButtonControl>("keyo");
                keyP = GetChildControl<ButtonControl>("keyp");
                keyQ = GetChildControl<ButtonControl>("keyq");
                keyR = GetChildControl<ButtonControl>("keyr");
                keyS = GetChildControl<ButtonControl>("keys");
                keyT = GetChildControl<ButtonControl>("keyt");
                keyU = GetChildControl<ButtonControl>("keyu");
                keyV = GetChildControl<ButtonControl>("keyv");
                keyW = GetChildControl<ButtonControl>("keyw");
                keyX = GetChildControl<ButtonControl>("keyx");
                keyY = GetChildControl<ButtonControl>("keyy");
                keyZ = GetChildControl<ButtonControl>("keyz");
                numZero = GetChildControl<ButtonControl>("numzero");
                numOne = GetChildControl<ButtonControl>("numone");
                numTwo = GetChildControl<ButtonControl>("numtwo");
                numThree = GetChildControl<ButtonControl>("numthree");
                numFour = GetChildControl<ButtonControl>("numfour");
                numFive = GetChildControl<ButtonControl>("numfive");
                numSix = GetChildControl<ButtonControl>("numsix");
                numSeven = GetChildControl<ButtonControl>("numseven");
                numEight = GetChildControl<ButtonControl>("numeight");
                numNine = GetChildControl<ButtonControl>("numnine");
                fOne = GetChildControl<ButtonControl>("fone");
                fTwo = GetChildControl<ButtonControl>("ftwo");
                fThree = GetChildControl<ButtonControl>("fthree");
                fFour = GetChildControl<ButtonControl>("ffour");
                fFive = GetChildControl<ButtonControl>("ffive");
                fSix = GetChildControl<ButtonControl>("fsix");
                fSeven = GetChildControl<ButtonControl>("fseven");
                fEight = GetChildControl<ButtonControl>("feight");
                fNine = GetChildControl<ButtonControl>("fnine");
                fTen = GetChildControl<ButtonControl>("ften");
                fEleven = GetChildControl<ButtonControl>("feleven");
                fTwelve = GetChildControl<ButtonControl>("ftwelve");
                space = GetChildControl<ButtonControl>("space");
                enter = GetChildControl<ButtonControl>("enter");
                escape = GetChildControl<ButtonControl>("escape");
                tab = GetChildControl<ButtonControl>("tab");
                backspace = GetChildControl<ButtonControl>("backspace");
                delete = GetChildControl<ButtonControl>("delete");
                insert = GetChildControl<ButtonControl>("insert");
                home = GetChildControl<ButtonControl>("home");
                end = GetChildControl<ButtonControl>("end");
                pageUp = GetChildControl<ButtonControl>("pageup");
                pageDown = GetChildControl<ButtonControl>("pagedown");
                leftArrow = GetChildControl<ButtonControl>("leftarrow");
                rightArrow = GetChildControl<ButtonControl>("rightarrow");
                upArrow = GetChildControl<ButtonControl>("uparrow");
                downArrow = GetChildControl<ButtonControl>("downarrow");
                leftShift = GetChildControl<ButtonControl>("leftshift");
                rightShift = GetChildControl<ButtonControl>("rightshift");
                leftCtrl = GetChildControl<ButtonControl>("leftctrl");
                rightCtrl = GetChildControl<ButtonControl>("rightctrl");
                leftAlt = GetChildControl<ButtonControl>("leftalt");
                rightAlt = GetChildControl<ButtonControl>("rightalt");
            }
        }

        protected override InteractionProfileType GetInteractionProfileType()
            => InteractionProfileType.Device;

        protected override string GetDeviceLayoutName() => nameof(RawKeyboard);

        protected override void RegisterDeviceLayout()
        {
            Debug.Log("[ShiyanRawInput] RawKeyboard RegisterDeviceLayout");
            InputSystem.RegisterLayout(typeof(RawKeyboard),
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.InterfaceMatchAnyVersion)
                    .WithProduct(kDeviceName));
        }

        protected override void UnregisterDeviceLayout()
        {
            InputSystem.RemoveLayout(nameof(RawKeyboard));
        }

        protected override void RegisterActionMapsWithRuntime()
        {
            Debug.Log("[ShiyanRawInput] RawKeyboard RegisterActionMapsWithRuntime");
            AddActionMap(new ActionMapConfig
            {
                name = "shiyanrawkeyboard",
                localizedName = kDeviceName,
                desiredInteractionProfile = profile,
                manufacturer = "Shiyan",
                serialNumber = "",
                deviceInfos = new List<DeviceConfig>
                {
                    new DeviceConfig
                    {
                        characteristics = InputDeviceCharacteristics.TrackedDevice,
                        userPath = "/user/keyboard"
                    }
                },
                actions = new List<ActionConfig>
                {
                new ActionConfig
                {
                    name = "keya",
                    localizedName = "A",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_a/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyb",
                    localizedName = "B",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_b/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyc",
                    localizedName = "C",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_c/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyd",
                    localizedName = "D",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_d/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keye",
                    localizedName = "E",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_e/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyf",
                    localizedName = "F",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_f/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyg",
                    localizedName = "G",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_g/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyh",
                    localizedName = "H",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_h/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyi",
                    localizedName = "I",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_i/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyj",
                    localizedName = "J",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_j/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyk",
                    localizedName = "K",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_k/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyl",
                    localizedName = "L",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_l/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keym",
                    localizedName = "M",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_m/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyn",
                    localizedName = "N",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_n/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyo",
                    localizedName = "O",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_o/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyp",
                    localizedName = "P",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_p/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyq",
                    localizedName = "Q",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_q/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyr",
                    localizedName = "R",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_r/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keys",
                    localizedName = "S",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_s/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyt",
                    localizedName = "T",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_t/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyu",
                    localizedName = "U",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_u/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyv",
                    localizedName = "V",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_v/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyw",
                    localizedName = "W",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_w/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyx",
                    localizedName = "X",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_x/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyy",
                    localizedName = "Y",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_y/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "keyz",
                    localizedName = "Z",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_z/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "numzero",
                    localizedName = "0",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_0/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "numone",
                    localizedName = "1",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_1/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "numtwo",
                    localizedName = "2",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_2/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "numthree",
                    localizedName = "3",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_3/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "numfour",
                    localizedName = "4",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_4/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "numfive",
                    localizedName = "5",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_5/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "numsix",
                    localizedName = "6",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_6/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "numseven",
                    localizedName = "7",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_7/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "numeight",
                    localizedName = "8",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_8/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "numnine",
                    localizedName = "9",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_9/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "fone",
                    localizedName = "F1",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_f1/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "ftwo",
                    localizedName = "F2",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_f2/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "fthree",
                    localizedName = "F3",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_f3/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "ffour",
                    localizedName = "F4",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_f4/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "ffive",
                    localizedName = "F5",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_f5/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "fsix",
                    localizedName = "F6",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_f6/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "fseven",
                    localizedName = "F7",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_f7/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "feight",
                    localizedName = "F8",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_f8/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "fnine",
                    localizedName = "F9",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_f9/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "ften",
                    localizedName = "F10",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_f10/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "feleven",
                    localizedName = "F11",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_f11/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "ftwelve",
                    localizedName = "F12",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_f12/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "space",
                    localizedName = "Space",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_space/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "enter",
                    localizedName = "Enter",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_enter/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "escape",
                    localizedName = "Escape",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_escape/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "tab",
                    localizedName = "Tab",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_tab/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "backspace",
                    localizedName = "Backspace",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_backspace/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "delete",
                    localizedName = "Delete",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_delete/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "insert",
                    localizedName = "Insert",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_insert/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "home",
                    localizedName = "Home",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_home/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "end",
                    localizedName = "End",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_end/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "pageup",
                    localizedName = "PageUp",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_page_up/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "pagedown",
                    localizedName = "PageDown",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_page_down/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "leftarrow",
                    localizedName = "Left Arrow",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_left/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "rightarrow",
                    localizedName = "Right Arrow",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_right/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "uparrow",
                    localizedName = "Up Arrow",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_up/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "downarrow",
                    localizedName = "Down Arrow",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_down/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "leftshift",
                    localizedName = "Left Shift",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_left_shift/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "rightshift",
                    localizedName = "Right Shift",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_right_shift/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "leftctrl",
                    localizedName = "Left Ctrl",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_left_ctrl/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "rightctrl",
                    localizedName = "Right Ctrl",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_right_ctrl/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "leftalt",
                    localizedName = "Left Alt",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_left_alt/click",
                            interactionProfileName = profile
                        }
                    }
                },
                new ActionConfig
                {
                    name = "rightalt",
                    localizedName = "Right Alt",
                    type = ActionType.Binary,
                    bindings = new List<ActionBinding>
                    {
                        new ActionBinding
                        {
                            interactionPath = "/input/key_right_alt/click",
                            interactionProfileName = profile
                        }
                    }
                },
                }
            });
        }
    }
}
