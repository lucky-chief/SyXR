#if XR_COMPOSITION_LAYERS
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.XR.OpenXR.CompositionLayers;
using UnityEngine.XR.OpenXR.Features;

namespace com.unity.shiyantech.xr
{
    /// <summary>
    /// OpenXR Feature that registers SafeQuadLayerHandler for UIQuadLayerData,
    /// fixing the frozen-UI-after-resume issue on Android.
    /// </summary>
#if UNITY_EDITOR
    [UnityEditor.XR.OpenXR.Features.OpenXRFeature(
        UiName = "视延科技 UI Quad Layer Handler",
        BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android },
        Company = "Custom",
        Desc = "Overrides the built-in Quad layer handler with a version that re-fetches the " +
                "native render texture every frame, fixing black screen after Android resume.",
        DocumentationLink = "",
        FeatureId = "com.custom.openxr.features.uiquadlayer",
        OpenxrExtensionStrings = "",
        Version = "1.0.0")]
#endif
    public class UIQuadLayerFeature : OpenXRFeature
    {
        bool m_Subscribed;

        protected override void OnEnable()
        {
            if (OpenXRLayerProvider.isStarted)
                Register();
            else
            {
                OpenXRLayerProvider.Started += Register;
                m_Subscribed = true;
            }
        }

        protected override void OnDisable()
        {
            if (m_Subscribed)
            {
                OpenXRLayerProvider.Started -= Register;
                m_Subscribed = false;
            }
        }

        static void Register()
        {
            var handler = new UIQuadLayerHandler();
            OpenXRLayerProvider.RegisterLayerHandler(typeof(UIQuadLayerData), handler);
            Debug.Log("[SafeQuadLayer] Registered handler for UIQuadLayerData.");
        }
    }
}
#endif
