using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using Unity.XR.CompositionLayers.Services;
using Object = UnityEngine.Object;
using UnityEngine.Rendering.Universal;
using com.unity.shiyantech.xr;

namespace Unity.XR.CompositionLayers
{
    /// <summary>
    /// Persistent infrastructure root for full-screen zero-parallax UI.
    /// Supports two modes:
    ///   Product  — renders via CompositionLayer → RenderTexture → OpenXR quad layer.
    ///   Dev      — renders via URP overlay camera on ShiyanDevicesSimulator stereo cameras.
    /// Switch modes via the OverlayUI Inspector button.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(int.MinValue + 1)]
    public sealed class OverlayUI : MonoBehaviour
    {
        const string k_RootObjectName = "Overlay UI";

        static readonly Vector2Int k_DefaultReferenceResolution = new(1920, 1080);

        [SerializeField]
        int m_LayerOrder = 1;

        [SerializeField]
        Vector2Int m_ReferenceResolution = k_DefaultReferenceResolution;

        [SerializeField, HideInInspector]
        CompositionLayer m_UILayerComponent;

        [SerializeField, HideInInspector]
        TexturesExtension m_TexturesExtension;

        [SerializeField, HideInInspector]
        Camera m_UICamera;

        [SerializeField, HideInInspector]
        Canvas m_UICanvas;

        [SerializeField, HideInInspector]
        EventSystem m_EventSystem;

        [SerializeField, HideInInspector]
        InputSystemUIInputModule m_InputSystemUIInputModule;

        [SerializeField, HideInInspector]
        RenderTexture m_CurrentRenderTexture;

        [SerializeField]
        bool m_IsDevelopmentMode;

        Vector2Int m_LastRenderSize;
        bool m_IsConfigurationValid;
        Rect m_LastCanvasRect;
        Vector3 m_LastCanvasLossyScale;

        public bool IsDevelopmentMode => m_IsDevelopmentMode;

        public Canvas UICanvas => m_UICanvas;

        public Camera UICamera => m_UICamera;

        public CompositionLayer UILayer => m_UILayerComponent;

        public RenderTexture CurrentRenderTexture => m_CurrentRenderTexture;

        internal EventSystem CurrentEventSystem => m_EventSystem;

        internal InputSystemUIInputModule CurrentInputModule => m_InputSystemUIInputModule;

        internal TexturesExtension CurrentTexturesExtension => m_TexturesExtension;

        void Awake()
        {
            Initialize();
        }

        void OnDestroy()
        {
#if !UNITY_EDITOR
            ReleaseRenderTexture();
#endif
        }

        void Update()
        {
            if (m_IsDevelopmentMode)
                return;

            RebuildRenderTargetIfNeeded();
            SynchronizeCanvasAndCamera();
        }

        void LateUpdate()
        {
            // 开发模式下 URP 自动渲染 Overlay Camera，不需要手动 Render
            if (m_IsDevelopmentMode)
                return;

            if (!m_IsConfigurationValid || m_UICamera == null || m_CurrentRenderTexture == null)
                return;

            Canvas.ForceUpdateCanvases();
            m_UICamera.Render();
        }

        public void RebuildRenderTargetIfNeeded()
        {
            Initialize();

            if (!m_IsConfigurationValid)
                return;

            var renderSize = GetRenderTargetSize();
            // GPU 资源可能在 Android 挂起/恢复后丢失，即使 C# 对象还在
            bool rtInvalid = m_CurrentRenderTexture != null && !m_CurrentRenderTexture.IsCreated();
            if (m_CurrentRenderTexture == null || rtInvalid || m_LastRenderSize != renderSize)
                RebuildRenderTarget(renderSize);
        }

#if !UNITY_EDITOR
        void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                // 从后台恢复：强制重建 RenderTexture，因为 GPU 资源可能已被释放
                Debug.Log("[OverlayUI] App resumed, forcing RenderTexture rebuild.");
                m_LastRenderSize = default; // 强制触发 RebuildRenderTarget
            }
        }
#endif

        /// <summary>
        /// Called by the custom Editor after switching modes.
        /// Applies camera render-type and runtime setup.
        /// </summary>
        public void ApplyModeSettings()
        {
            if (m_UICamera == null)
                m_UICamera = GetComponentInChildren<Camera>();
            if (m_UICamera == null)
                return;

            var uiData = m_UICamera.GetComponent<UniversalAdditionalCameraData>();
            if (m_IsDevelopmentMode)
            {
                ConfigureCameraForDevMode();
            }
            else
            {
                if (uiData != null)
                    uiData.renderType = CameraRenderType.Base;
                Initialize();
            }
        }

        void Initialize()
        {
            if (transform.parent != null)
                transform.SetParent(null, false);

            gameObject.name = k_RootObjectName;

            if (m_IsDevelopmentMode)
            {
                EnsureCamera();
                EnsureCanvas();
                ConfigureCameraForDevMode();
                m_IsConfigurationValid = m_UICamera != null && m_UICanvas != null;
                return;
            }

            EnsureLayer();
            EnsureCamera();
            EnsureCanvas();
            ConfigureCameraForCanvas();

            m_IsConfigurationValid = m_UILayerComponent != null
                && m_TexturesExtension != null
                && m_UICamera != null
                && m_UICanvas != null;

            if (!m_IsConfigurationValid)
                return;

            var renderSize = GetRenderTargetSize();
            if (m_CurrentRenderTexture == null || m_LastRenderSize != renderSize)
                RebuildRenderTarget(renderSize);
        }

        void ConfigureCameraForDevMode()
        {
            if (m_UICamera == null || m_UICanvas == null)
                return;

            m_UICamera.enabled = true;
            m_UICamera.targetTexture = null; // Overlay 模式下不需要 RT
            m_UICamera.clearFlags = CameraClearFlags.SolidColor;
            m_UICamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            m_UICamera.orthographic = true;
            m_UICamera.nearClipPlane = 0.1f;
            m_UICamera.rect = new Rect(0f, 0f, 1f, 1f);
            m_UICamera.gameObject.layer = m_UICanvas.gameObject.layer;
            m_UICamera.cullingMask = 1 << m_UICanvas.gameObject.layer;
            m_UICamera.allowMSAA = false;

            var uiData = m_UICamera.GetComponent<UniversalAdditionalCameraData>();
            if (uiData != null)
            {
                uiData.renderType = CameraRenderType.Overlay;
                uiData.renderShadows = false;
                uiData.renderPostProcessing = false;
                uiData.requiresDepthTexture = false;
                uiData.allowHDROutput = false;
                uiData.antialiasing = AntialiasingMode.None;
                uiData.dithering = false;
                uiData.stopNaN = false;
            }

            var cameraTransform = m_UICamera.transform;
            cameraTransform.SetParent(m_UICanvas.transform, false);
            cameraTransform.localScale = Vector3.one;
            cameraTransform.localPosition = new Vector3(0f, 0f, -100f);
            cameraTransform.localRotation = Quaternion.identity;

            if (m_UICanvas.worldCamera != m_UICamera)
                m_UICanvas.worldCamera = m_UICamera;
        }

        void EnsureLayer()
        {
            if (m_UILayerComponent == null)
            {
                m_UILayerComponent = transform.GetComponent<CompositionLayer>();
            }
            if (m_UILayerComponent == null)
            {
                Debug.LogError($"没有找到CompositionLayer组件");
                return;
            }

            m_UILayerComponent.Order = m_LayerOrder;

            if (m_UILayerComponent.LayerData is not UIQuadLayerData quadLayerData)
            {
                m_UILayerComponent.ChangeLayerDataType<UIQuadLayerData>();
                quadLayerData = m_UILayerComponent.LayerData as UIQuadLayerData;
            }

            if (quadLayerData != null)
            {
                quadLayerData.ApplyTransformScale = false;
                quadLayerData.Size = CalculateQuadSize(GetRenderTargetSize());
            }

            m_TexturesExtension = m_UILayerComponent.GetComponent<TexturesExtension>();
            if (m_TexturesExtension == null)
                m_TexturesExtension = m_UILayerComponent.gameObject.AddComponent<TexturesExtension>();

            m_TexturesExtension.TargetEye = TexturesExtension.TargetEyeEnum.Both;
            m_TexturesExtension.sourceTexture = TexturesExtension.SourceTextureEnum.LocalTexture;
            m_TexturesExtension.CustomRects = false;
        }

        void EnsureCamera()
        {
            if (m_UICamera == null)
            {
                m_UICamera = transform.GetComponentInChildren<Camera>();
            }

            if (m_UICamera == null)
            {
                Debug.LogError($"找不到UI 用的camera");
                return;
            }
        }

        void EnsureCanvas()
        {
            if (m_UICanvas == null)
            {
                var canvasTransform = transform.GetComponentInChildren<Canvas>();
                if (canvasTransform == null)
                {
                    Debug.LogError("没有Canvas组件");
                    return;
                }

                m_UICanvas = canvasTransform;
            }
            var canvasScaler = m_UICanvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (canvasScaler != null)
            {
                canvasScaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = m_ReferenceResolution;
            }
            else
            {
                Debug.LogError("没有CanvasScaler组件");
            }
        }

        void ConfigureCameraForCanvas()
        {
            if (m_UICamera == null || m_UICanvas == null)
                return;

            m_UICamera.enabled = false;
            m_UICamera.clearFlags = CameraClearFlags.SolidColor;
            m_UICamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            m_UICamera.orthographic = true;
            m_UICamera.nearClipPlane = 0.1f;
            m_UICamera.rect = new Rect(0f, 0f, 1f, 1f);
            m_UICamera.gameObject.layer = m_UICanvas.gameObject.layer;
            m_UICamera.cullingMask = 1 << m_UICanvas.gameObject.layer;
#if !UNITY_RENDER_PIPELINES_UNIVERSAL
            m_UICamera.stereoTargetEye = StereoTargetEyeMask.None;
            UniversalAdditionalCameraData camData = m_UICamera.GetComponent<UniversalAdditionalCameraData>();
            camData.renderShadows = false;
            camData.renderPostProcessing = false;
            camData.requiresDepthTexture = false;
            camData.allowHDROutput = false;
#endif
            m_UICamera.allowMSAA = false;

            var cameraTransform = m_UICamera.transform;
            cameraTransform.SetParent(m_UICanvas.transform, false);
            cameraTransform.localScale = Vector3.one;
            cameraTransform.localPosition = new Vector3(0f, 0f, -100f);
            cameraTransform.localRotation = Quaternion.identity;

            if (m_UICanvas.worldCamera != m_UICamera)
                m_UICanvas.worldCamera = m_UICamera;
        }

        void SynchronizeCanvasAndCamera()
        {
            if (!m_IsConfigurationValid || m_UICanvas == null || m_UICamera == null)
                return;

            var rectTransform = m_UICanvas.GetComponent<RectTransform>();
            if (rectTransform == null)
                return;

            var canvasRect = rectTransform.rect;
            var canvasScale = rectTransform.lossyScale;
            if (canvasRect == m_LastCanvasRect && canvasScale == m_LastCanvasLossyScale)
                return;

            float canvasWidth = Mathf.Max(0.0001f, canvasRect.width * canvasScale.x);
            float canvasHeight = Mathf.Max(0.0001f, canvasRect.height * canvasScale.y);
            m_UICamera.orthographicSize = canvasHeight * 0.5f;
            m_UICamera.aspect = canvasWidth / canvasHeight;

            m_LastCanvasRect = canvasRect;
            m_LastCanvasLossyScale = canvasScale;
        }

        void RebuildRenderTarget(Vector2Int renderSize)
        {
#if !UNITY_EDITOR
            ReleaseRenderTexture();

            m_CurrentRenderTexture = new RenderTexture(renderSize.x, renderSize.y, 0, RenderTextureFormat.ARGB32)
            {
                name = "OverlayUIRenderTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            m_CurrentRenderTexture.Create();

            m_LastRenderSize = renderSize;

            m_UICamera.targetTexture = m_CurrentRenderTexture;
            m_TexturesExtension.LeftTexture = m_CurrentRenderTexture;
            m_TexturesExtension.RightTexture = m_CurrentRenderTexture;

            if (m_UILayerComponent.LayerData is UIQuadLayerData quadLayerData)
            {
                quadLayerData.Size = CalculateQuadSize(renderSize);
            }
#endif
        }

        void ReleaseRenderTexture()
        {
            if (m_UICamera != null)
                m_UICamera.targetTexture = null;

            if (m_TexturesExtension != null)
            {
                m_TexturesExtension.LeftTexture = null;
                m_TexturesExtension.RightTexture = null;
            }

            if (m_CurrentRenderTexture == null)
                return;

            m_CurrentRenderTexture.Release();
            Object.Destroy(m_CurrentRenderTexture);
            m_CurrentRenderTexture = null;
        }

        Vector2Int GetRenderTargetSize()
        {
            return new Vector2Int(
                Mathf.Max(1, m_ReferenceResolution.x),
                Mathf.Max(1, m_ReferenceResolution.y));
        }

        Vector2 CalculateQuadSize(Vector2Int renderSize)
        {
            if (renderSize.y <= 0)
                return Vector2.one;

            float aspect = (float)renderSize.x / renderSize.y;
            return aspect >= 1f ? new Vector2(aspect, 1f) : new Vector2(1f, 1f / aspect);
        }
    }
}
