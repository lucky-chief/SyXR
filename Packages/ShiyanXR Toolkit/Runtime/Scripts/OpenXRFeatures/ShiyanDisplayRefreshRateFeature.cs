using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

#if UNITY_EDITOR
using UnityEditor.XR.OpenXR.Features;
#endif
namespace com.unity.shiyantech.xr
{
#if UNITY_EDITOR
    [OpenXRFeature(
        UiName = "视延科技XR刷新率",
        BuildTargetGroups = new[] { UnityEditor.BuildTargetGroup.Android },
        Company = "ShiyanTech",
        Desc = "Requests an OpenXR display refresh rate through XR_FB_display_refresh_rate.",
        DocumentationLink = "",
        OpenxrExtensionStrings = "XR_FB_display_refresh_rate",
        Version = "1.0.0",
        FeatureId = "com.shiyantech.openxr.feature.display_refresh_rate")]
#endif
    public sealed class ShiyanDisplayRefreshRateFeature : OpenXRFeature
    {
        public const string FeatureId = "com.shiyantech.openxr.feature.display_refresh_rate";
        private const int XR_SUCCESS = 0;

        [DllImport("openxr_loader", EntryPoint = "xrGetInstanceProcAddr", CallingConvention = CallingConvention.Cdecl)]
        private static extern int XrGetInstanceProcAddr(
            ulong instance,
            [MarshalAs(UnmanagedType.LPStr)] string name,
            out IntPtr function);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int XrRequestDisplayRefreshRateFB(ulong session, float displayRefreshRate);

        public enum RefreshRateHz
        {
            [InspectorName("30 Hz")]
            Hz30 = 30,

            [InspectorName("60 Hz")]
            Hz60 = 60,

            [InspectorName("72 Hz")]
            Hz72 = 72,

            [InspectorName("90 Hz")]
            Hz90 = 90,

            [InspectorName("120 Hz")]
            Hz120 = 120,
        }

        [SerializeField]
        [Tooltip("The display refresh rate requested after the OpenXR session starts.")]
        private RefreshRateHz refreshRate = RefreshRateHz.Hz60;

        private ulong xrInstance;
        private ulong xrSession;
        private XrRequestDisplayRefreshRateFB requestDisplayRefreshRate;

        public RefreshRateHz SelectedRefreshRate
        {
            get => refreshRate;
            set => refreshRate = value;
        }

        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            Debug.Log($"[ShiyanDisplayRefreshRateFeature] OnInstanceCreate.");
            if (!base.OnInstanceCreate(xrInstance))
            {
                return false;
            }

            this.xrInstance = xrInstance;
            return true;
        }

        protected override void OnSessionCreate(ulong xrSession)
        {
            Debug.Log($"[ShiyanDisplayRefreshRateFeature] OnSessionCreate.");
            base.OnSessionCreate(xrSession);
            this.xrSession = xrSession;
            RequestConfiguredRefreshRate();
        }

        protected override void OnSessionDestroy(ulong xrSession)
        {
            this.xrSession = 0;
            base.OnSessionDestroy(xrSession);
        }

        protected override void OnInstanceDestroy(ulong xrInstance)
        {
            this.xrInstance = 0;
            xrSession = 0;
            requestDisplayRefreshRate = null;
            base.OnInstanceDestroy(xrInstance);
        }

        private void RequestConfiguredRefreshRate()
        {
            if (xrSession == 0)
            {
                Debug.LogWarning("[ShiyanDisplayRefreshRateFeature] Cannot request refresh rate before OpenXR session creation.");
                return;
            }

            if (!TryInitializeRequestFunction())
            {
                return;
            }

            float targetRate = (float)refreshRate;
            int result = requestDisplayRefreshRate(xrSession, targetRate);
            if (result == XR_SUCCESS)
            {
                Debug.Log($"[ShiyanDisplayRefreshRateFeature] Requested {targetRate:0} Hz display refresh rate.");
                return;
            }

            Debug.LogWarning(
                $"[ShiyanDisplayRefreshRateFeature] xrRequestDisplayRefreshRateFB({targetRate:0} Hz) failed with XrResult {result}.");
        }

        private bool TryInitializeRequestFunction()
        {
            if (requestDisplayRefreshRate != null)
            {
                return true;
            }

            if (xrInstance == 0)
            {
                Debug.LogError("[ShiyanDisplayRefreshRateFeature] Cannot resolve xrRequestDisplayRefreshRateFB before OpenXR instance creation.");
                return false;
            }

            int result = XrGetInstanceProcAddr(
                xrInstance,
                "xrRequestDisplayRefreshRateFB",
                out IntPtr functionPtr);
            if (result != XR_SUCCESS)
            {
                Debug.LogError(
                    $"[ShiyanDisplayRefreshRateFeature] xrGetInstanceProcAddr failed for xrRequestDisplayRefreshRateFB with XrResult {result}.");
                return false;
            }

            if (functionPtr == IntPtr.Zero)
            {
                Debug.LogError("[ShiyanDisplayRefreshRateFeature] xrRequestDisplayRefreshRateFB is unavailable. Check XR_FB_display_refresh_rate support.");
                return false;
            }

            requestDisplayRefreshRate =
                Marshal.GetDelegateForFunctionPointer<XrRequestDisplayRefreshRateFB>(functionPtr);
            return true;
        }
    }
}
