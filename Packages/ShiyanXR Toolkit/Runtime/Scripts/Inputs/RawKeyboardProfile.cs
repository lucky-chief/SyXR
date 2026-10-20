// Curated key set for the first Unity integration. The runtime accepts a wider set of
// keys (see docs/Windows适配.md); add more controls/actions here as needed.
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
        const string kDeviceName = "Shiyan Raw Keyboard (OpenXR)";

        [Preserve, InputControlLayout(displayName = kDeviceName)]
        public class RawKeyboard : UnityEngine.InputSystem.InputDevice
        {
            [Preserve, InputControl(name = "keya", layout = "Key", bit = (int)Key.A, displayName = "A")]
            public KeyControl keyA { get; private set; }

            [Preserve, InputControl(name = "keyb", layout = "Key", bit = (int)Key.B, displayName = "B")]
            public KeyControl keyB { get; private set; }

            [Preserve, InputControl(name = "keyc", layout = "Key", bit = (int)Key.C, displayName = "C")]
            public KeyControl keyC { get; private set; }

            [Preserve, InputControl(name = "keyd", layout = "Key", bit = (int)Key.D, displayName = "D")]
            public KeyControl keyD { get; private set; }

            [Preserve, InputControl(name = "keye", layout = "Key", bit = (int)Key.E, displayName = "E")]
            public KeyControl keyE { get; private set; }

            [Preserve, InputControl(name = "keyf", layout = "Key", bit = (int)Key.F, displayName = "F")]
            public KeyControl keyF { get; private set; }

            [Preserve, InputControl(name = "keyg", layout = "Key", bit = (int)Key.G, displayName = "G")]
            public KeyControl keyG { get; private set; }

            [Preserve, InputControl(name = "keyh", layout = "Key", bit = (int)Key.H, displayName = "H")]
            public KeyControl keyH { get; private set; }

            [Preserve, InputControl(name = "keyi", layout = "Key", bit = (int)Key.I, displayName = "I")]
            public KeyControl keyI { get; private set; }

            [Preserve, InputControl(name = "keyj", layout = "Key", bit = (int)Key.J, displayName = "J")]
            public KeyControl keyJ { get; private set; }

            [Preserve, InputControl(name = "keyk", layout = "Key", bit = (int)Key.K, displayName = "K")]
            public KeyControl keyK { get; private set; }

            [Preserve, InputControl(name = "keyl", layout = "Key", bit = (int)Key.L, displayName = "L")]
            public KeyControl keyL { get; private set; }

            [Preserve, InputControl(name = "keym", layout = "Key", bit = (int)Key.M, displayName = "M")]
            public KeyControl keyM { get; private set; }

            [Preserve, InputControl(name = "keyn", layout = "Key", bit = (int)Key.N, displayName = "N")]
            public KeyControl keyN { get; private set; }

            [Preserve, InputControl(name = "keyo", layout = "Key", bit = (int)Key.O, displayName = "O")]
            public KeyControl keyO { get; private set; }

            [Preserve, InputControl(name = "keyp", layout = "Key", bit = (int)Key.P, displayName = "P")]
            public KeyControl keyP { get; private set; }

            [Preserve, InputControl(name = "keyq", layout = "Key", bit = (int)Key.Q, displayName = "Q")]
            public KeyControl keyQ { get; private set; }

            [Preserve, InputControl(name = "keyr", layout = "Key", bit = (int)Key.R, displayName = "R")]
            public KeyControl keyR { get; private set; }

            [Preserve, InputControl(name = "keys", layout = "Key", bit = (int)Key.S, displayName = "S")]
            public KeyControl keyS { get; private set; }

            [Preserve, InputControl(name = "keyt", layout = "Key", bit = (int)Key.T, displayName = "T")]
            public KeyControl keyT { get; private set; }

            [Preserve, InputControl(name = "keyu", layout = "Key", bit = (int)Key.U, displayName = "U")]
            public KeyControl keyU { get; private set; }

            [Preserve, InputControl(name = "keyv", layout = "Key", bit = (int)Key.V, displayName = "V")]
            public KeyControl keyV { get; private set; }

            [Preserve, InputControl(name = "keyw", layout = "Key", bit = (int)Key.W, displayName = "W")]
            public KeyControl keyW { get; private set; }

            [Preserve, InputControl(name = "keyx", layout = "Key", bit = (int)Key.X, displayName = "X")]
            public KeyControl keyX { get; private set; }

            [Preserve, InputControl(name = "keyy", layout = "Key", bit = (int)Key.Y, displayName = "Y")]
            public KeyControl keyY { get; private set; }

            [Preserve, InputControl(name = "keyz", layout = "Key", bit = (int)Key.Z, displayName = "Z")]
            public KeyControl keyZ { get; private set; }

            [Preserve, InputControl(name = "numzero", layout = "Key", bit = (int)Key.Digit0, displayName = "0")]
            public KeyControl numZero { get; private set; }

            [Preserve, InputControl(name = "numone", layout = "Key", bit = (int)Key.Digit1, displayName = "1")]
            public KeyControl numOne { get; private set; }

            [Preserve, InputControl(name = "numtwo", layout = "Key", bit = (int)Key.Digit2, displayName = "2")]
            public KeyControl numTwo { get; private set; }

            [Preserve, InputControl(name = "numthree", layout = "Key", bit = (int)Key.Digit3, displayName = "3")]
            public KeyControl numThree { get; private set; }

            [Preserve, InputControl(name = "numfour", layout = "Key", bit = (int)Key.Digit4, displayName = "4")]
            public KeyControl numFour { get; private set; }

            [Preserve, InputControl(name = "numfive", layout = "Key", bit = (int)Key.Digit5, displayName = "5")]
            public KeyControl numFive { get; private set; }

            [Preserve, InputControl(name = "numsix", layout = "Key", bit = (int)Key.Digit6, displayName = "6")]
            public KeyControl numSix { get; private set; }

            [Preserve, InputControl(name = "numseven", layout = "Key", bit = (int)Key.Digit7, displayName = "7")]
            public KeyControl numSeven { get; private set; }

            [Preserve, InputControl(name = "numeight", layout = "Key", bit = (int)Key.Digit8, displayName = "8")]
            public KeyControl numEight { get; private set; }

            [Preserve, InputControl(name = "numnine", layout = "Key", bit = (int)Key.Digit9, displayName = "9")]
            public KeyControl numNine { get; private set; }

            [Preserve, InputControl(name = "fone", layout = "Key", bit = (int)Key.F1, displayName = "F1")]
            public KeyControl fOne { get; private set; }

            [Preserve, InputControl(name = "ftwo", layout = "Key", bit = (int)Key.F2, displayName = "F2")]
            public KeyControl fTwo { get; private set; }

            [Preserve, InputControl(name = "fthree", layout = "Key", bit = (int)Key.F3, displayName = "F3")]
            public KeyControl fThree { get; private set; }

            [Preserve, InputControl(name = "ffour", layout = "Key", bit = (int)Key.F4, displayName = "F4")]
            public KeyControl fFour { get; private set; }

            [Preserve, InputControl(name = "ffive", layout = "Key", bit = (int)Key.F5, displayName = "F5")]
            public KeyControl fFive { get; private set; }

            [Preserve, InputControl(name = "fsix", layout = "Key", bit = (int)Key.F6, displayName = "F6")]
            public KeyControl fSix { get; private set; }

            [Preserve, InputControl(name = "fseven", layout = "Key", bit = (int)Key.F7, displayName = "F7")]
            public KeyControl fSeven { get; private set; }

            [Preserve, InputControl(name = "feight", layout = "Key", bit = (int)Key.F8, displayName = "F8")]
            public KeyControl fEight { get; private set; }

            [Preserve, InputControl(name = "fnine", layout = "Key", bit = (int)Key.F9, displayName = "F9")]
            public KeyControl fNine { get; private set; }

            [Preserve, InputControl(name = "ften", layout = "Key", bit = (int)Key.F10, displayName = "F10")]
            public KeyControl fTen { get; private set; }

            [Preserve, InputControl(name = "feleven", layout = "Key", bit = (int)Key.F11, displayName = "F11")]
            public KeyControl fEleven { get; private set; }

            [Preserve, InputControl(name = "ftwelve", layout = "Key", bit = (int)Key.F12, displayName = "F12")]
            public KeyControl fTwelve { get; private set; }

            [Preserve, InputControl(name = "space", layout = "Key", bit = (int)Key.Space, displayName = "Space")]
            public KeyControl space { get; private set; }

            [Preserve, InputControl(name = "enter", layout = "Key", bit = (int)Key.Enter, displayName = "Enter")]
            public KeyControl enter { get; private set; }

            [Preserve, InputControl(name = "escape", layout = "Key", bit = (int)Key.Escape, displayName = "Escape")]
            public KeyControl escape { get; private set; }

            [Preserve, InputControl(name = "tab", layout = "Key", bit = (int)Key.Tab, displayName = "Tab")]
            public KeyControl tab { get; private set; }

            [Preserve, InputControl(name = "backspace", layout = "Key", bit = (int)Key.Backspace, displayName = "Backspace")]
            public KeyControl backspace { get; private set; }

            [Preserve, InputControl(name = "delete", layout = "Key", bit = (int)Key.Delete, displayName = "Delete")]
            public KeyControl delete { get; private set; }

            [Preserve, InputControl(name = "insert", layout = "Key", bit = (int)Key.Insert, displayName = "Insert")]
            public KeyControl insert { get; private set; }

            [Preserve, InputControl(name = "home", layout = "Key", bit = (int)Key.Home, displayName = "Home")]
            public KeyControl home { get; private set; }

            [Preserve, InputControl(name = "end", layout = "Key", bit = (int)Key.End, displayName = "End")]
            public KeyControl end { get; private set; }

            [Preserve, InputControl(name = "pageup", layout = "Key", bit = (int)Key.PageUp, displayName = "PageUp")]
            public KeyControl pageUp { get; private set; }

            [Preserve, InputControl(name = "pagedown", layout = "Key", bit = (int)Key.PageDown, displayName = "PageDown")]
            public KeyControl pageDown { get; private set; }

            [Preserve, InputControl(name = "leftarrow", layout = "Key", bit = (int)Key.LeftArrow, displayName = "Left Arrow")]
            public KeyControl leftArrow { get; private set; }

            [Preserve, InputControl(name = "rightarrow", layout = "Key", bit = (int)Key.RightArrow, displayName = "Right Arrow")]
            public KeyControl rightArrow { get; private set; }

            [Preserve, InputControl(name = "uparrow", layout = "Key", bit = (int)Key.UpArrow, displayName = "Up Arrow")]
            public KeyControl upArrow { get; private set; }

            [Preserve, InputControl(name = "downarrow", layout = "Key", bit = (int)Key.DownArrow, displayName = "Down Arrow")]
            public KeyControl downArrow { get; private set; }

            [Preserve, InputControl(name = "leftshift", layout = "Key", bit = (int)Key.LeftShift, displayName = "Left Shift")]
            public KeyControl leftShift { get; private set; }

            [Preserve, InputControl(name = "rightshift", layout = "Key", bit = (int)Key.RightShift, displayName = "Right Shift")]
            public KeyControl rightShift { get; private set; }

            [Preserve, InputControl(name = "leftctrl", layout = "Key", bit = (int)Key.LeftCtrl, displayName = "Left Ctrl")]
            public KeyControl leftCtrl { get; private set; }

            [Preserve, InputControl(name = "rightctrl", layout = "Key", bit = (int)Key.RightCtrl, displayName = "Right Ctrl")]
            public KeyControl rightCtrl { get; private set; }

            [Preserve, InputControl(name = "leftalt", layout = "Key", bit = (int)Key.LeftAlt, displayName = "Left Alt")]
            public KeyControl leftAlt { get; private set; }

            [Preserve, InputControl(name = "rightalt", layout = "Key", bit = (int)Key.RightAlt, displayName = "Right Alt")]
            public KeyControl rightAlt { get; private set; }

            protected override void FinishSetup()
            {
                base.FinishSetup();
                keyA = GetChildControl<KeyControl>("keya");
                keyB = GetChildControl<KeyControl>("keyb");
                keyC = GetChildControl<KeyControl>("keyc");
                keyD = GetChildControl<KeyControl>("keyd");
                keyE = GetChildControl<KeyControl>("keye");
                keyF = GetChildControl<KeyControl>("keyf");
                keyG = GetChildControl<KeyControl>("keyg");
                keyH = GetChildControl<KeyControl>("keyh");
                keyI = GetChildControl<KeyControl>("keyi");
                keyJ = GetChildControl<KeyControl>("keyj");
                keyK = GetChildControl<KeyControl>("keyk");
                keyL = GetChildControl<KeyControl>("keyl");
                keyM = GetChildControl<KeyControl>("keym");
                keyN = GetChildControl<KeyControl>("keyn");
                keyO = GetChildControl<KeyControl>("keyo");
                keyP = GetChildControl<KeyControl>("keyp");
                keyQ = GetChildControl<KeyControl>("keyq");
                keyR = GetChildControl<KeyControl>("keyr");
                keyS = GetChildControl<KeyControl>("keys");
                keyT = GetChildControl<KeyControl>("keyt");
                keyU = GetChildControl<KeyControl>("keyu");
                keyV = GetChildControl<KeyControl>("keyv");
                keyW = GetChildControl<KeyControl>("keyw");
                keyX = GetChildControl<KeyControl>("keyx");
                keyY = GetChildControl<KeyControl>("keyy");
                keyZ = GetChildControl<KeyControl>("keyz");
                numZero = GetChildControl<KeyControl>("numzero");
                numOne = GetChildControl<KeyControl>("numone");
                numTwo = GetChildControl<KeyControl>("numtwo");
                numThree = GetChildControl<KeyControl>("numthree");
                numFour = GetChildControl<KeyControl>("numfour");
                numFive = GetChildControl<KeyControl>("numfive");
                numSix = GetChildControl<KeyControl>("numsix");
                numSeven = GetChildControl<KeyControl>("numseven");
                numEight = GetChildControl<KeyControl>("numeight");
                numNine = GetChildControl<KeyControl>("numnine");
                fOne = GetChildControl<KeyControl>("fone");
                fTwo = GetChildControl<KeyControl>("ftwo");
                fThree = GetChildControl<KeyControl>("fthree");
                fFour = GetChildControl<KeyControl>("ffour");
                fFive = GetChildControl<KeyControl>("ffive");
                fSix = GetChildControl<KeyControl>("fsix");
                fSeven = GetChildControl<KeyControl>("fseven");
                fEight = GetChildControl<KeyControl>("feight");
                fNine = GetChildControl<KeyControl>("fnine");
                fTen = GetChildControl<KeyControl>("ften");
                fEleven = GetChildControl<KeyControl>("feleven");
                fTwelve = GetChildControl<KeyControl>("ftwelve");
                space = GetChildControl<KeyControl>("space");
                enter = GetChildControl<KeyControl>("enter");
                escape = GetChildControl<KeyControl>("escape");
                tab = GetChildControl<KeyControl>("tab");
                backspace = GetChildControl<KeyControl>("backspace");
                delete = GetChildControl<KeyControl>("delete");
                insert = GetChildControl<KeyControl>("insert");
                home = GetChildControl<KeyControl>("home");
                end = GetChildControl<KeyControl>("end");
                pageUp = GetChildControl<KeyControl>("pageup");
                pageDown = GetChildControl<KeyControl>("pagedown");
                leftArrow = GetChildControl<KeyControl>("leftarrow");
                rightArrow = GetChildControl<KeyControl>("rightarrow");
                upArrow = GetChildControl<KeyControl>("uparrow");
                downArrow = GetChildControl<KeyControl>("downarrow");
                leftShift = GetChildControl<KeyControl>("leftshift");
                rightShift = GetChildControl<KeyControl>("rightshift");
                leftCtrl = GetChildControl<KeyControl>("leftctrl");
                rightCtrl = GetChildControl<KeyControl>("rightctrl");
                leftAlt = GetChildControl<KeyControl>("leftalt");
                rightAlt = GetChildControl<KeyControl>("rightalt");
            }
        }

        protected override InteractionProfileType GetInteractionProfileType()
            => InteractionProfileType.Device;

        protected override string GetDeviceLayoutName() => nameof(RawKeyboard);

        protected override void RegisterDeviceLayout()
        {
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
                        characteristics = InputDeviceCharacteristics.None,
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
