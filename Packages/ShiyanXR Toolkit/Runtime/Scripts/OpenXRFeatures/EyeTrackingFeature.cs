using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.OpenXR.Features;
namespace com.unity.shiyantech.xr
{
#if UNITY_EDITOR
    [UnityEditor.XR.OpenXR.Features.OpenXRFeature(UiName = "视延科技眼追",
        BuildTargetGroups = new[] { BuildTargetGroup.Android },
        Category = "XR Tracking",
        Company = "ShiYanTech",
        Desc = "麻利麻利哄。",
        DocumentationLink = "https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.14/manual/features.html#defining-a-feature",
        OpenxrExtensionStrings = "", // this extension doesn't exist, a log message will be printed that it couldn't be enabled
        Version = "1.0.0",
        FeatureId = featureId)]
#endif
    public class EyeTrackingFeature : OpenXRFeature
    {
        public const string ExtensionName = "XR_EXT_eye_gaze_interaction";
        public const string featureId = "com.shiyantech.EyeGazeFeature";
    }
}
