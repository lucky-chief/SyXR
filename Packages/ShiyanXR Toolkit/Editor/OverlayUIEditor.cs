using com.unity.shiyantech.xr;
using Unity.XR.CompositionLayers;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace com.shiyanxr.editor
{
    [CustomEditor(typeof(OverlayUI))]
    public class OverlayUIEditor : Editor
    {
        const string k_LeftCameraName = "XR Origin (SY)/Camera Offset/Main Camera";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var overlay = (OverlayUI)target;
            var isDev = overlay.IsDevelopmentMode;

            EditorGUILayout.Space(10);

            GUI.backgroundColor = isDev
                ? new Color(0.4f, 0.8f, 0.4f)
                : new Color(0.8f, 0.6f, 0.4f);

            var buttonLabel = isDev
                ? "\u25c9 当前: 开发模式 (Overlay)"
                : "\u25c9 当前: 产品模式 (Composition Layer)";

            if (GUILayout.Button(buttonLabel, GUILayout.Height(35)))
            {
                SwitchMode(overlay, !isDev);
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                isDev
                    ? "Canvas 通过 Overlay Camera 渲染到 ShiyanDevicesSimulator 的主相机上。"
                    : "Canvas 渲染到 RenderTexture \u2192 CompositionLayer \u2192 OpenXR。",
                MessageType.Info);
        }

        void SwitchMode(OverlayUI overlay, bool toDevMode)
        {
            // 先写入序列化字段
            //var so = new SerializedObject(overlay);
            //so.FindProperty("m_IsDevelopmentMode").boolValue = toDevMode;
            //so.ApplyModifiedProperties();

            //var simulator = Object.FindObjectOfType<ShiyanDevicesSimulator>();
            //if (simulator == null)
            //{
            //    Debug.LogWarning("[OverlayUIEditor] ShiyanDevicesSimulator not found in scene.");
            //    return;
            //}

            //var cam = simulator.transform.Find(k_LeftCameraName);
            //var uiCam = overlay.UICamera;

            //// 把所有变更放到下一帧原子执行，避免 URP 在中间状态触发警告
            //EditorApplication.delayCall += () =>
            //{
            //    if (overlay == null || cam == null || uiCam == null)
            //        return;

            //    // 1. 清空 Camera Stack
            //    ClearCameraStack(cam, uiCam);

            //    // 2. 添加/移除组件 & 序列化引用
            //    if (toDevMode)
            //        SwitchToDevMode(overlay);
            //    else
            //        SwitchToProductMode(overlay);

            //    // 3. 配置相机（renderType, targetTexture 等）
            //    overlay.ApplyModeSettings();

            //    // 4. 同步输出属性 + 入栈
            //    if (toDevMode)
            //    {
            //        AddCameraToStack(cam, uiCam);
            //    }

            //    EditorUtility.SetDirty(overlay);
            //};
        }

        void SwitchToDevMode(OverlayUI overlay)
        {
            RemoveComponent<TexturesExtension>(overlay);
            RemoveComponent<CompositionLayer>(overlay);
            RemoveComponent<MeshCollider>(overlay);

            using (var so = new SerializedObject(overlay))
            {
                so.FindProperty("m_UILayerComponent").objectReferenceValue = null;
                so.FindProperty("m_TexturesExtension").objectReferenceValue = null;
                so.ApplyModifiedProperties();
            }

            Debug.Log("[OverlayUIEditor] Switched to Development mode (Overlay).");
        }

        void SwitchToProductMode(OverlayUI overlay)
        {
            var compLayer = AddComponent<CompositionLayer>(overlay);
            var texExt = AddComponent<TexturesExtension>(overlay);
            AddComponent<MeshCollider>(overlay);

            using (var so = new SerializedObject(overlay))
            {
                so.FindProperty("m_UILayerComponent").objectReferenceValue = compLayer;
                so.FindProperty("m_TexturesExtension").objectReferenceValue = texExt;
                so.ApplyModifiedProperties();
            }

            Debug.Log("[OverlayUIEditor] Switched to Product mode (Composition Layer).");
        }

        void ClearCameraStack(Transform camTransform, Camera overlayCamera)
        {
            if (camTransform == null || overlayCamera == null) return;
            var d = camTransform.GetComponent<UniversalAdditionalCameraData>();
            if (d != null) d.cameraStack.Remove(overlayCamera);
        }

        void AddCameraToStack(Transform camTransform, Camera overlayCamera)
        {
            if (camTransform == null || overlayCamera == null) return;
            var d = camTransform.GetComponent<UniversalAdditionalCameraData>();
            if (d == null) d = camTransform.gameObject.AddComponent<UniversalAdditionalCameraData>();
            if (!d.cameraStack.Contains(overlayCamera))
                d.cameraStack.Add(overlayCamera);
        }

        T AddComponent<T>(OverlayUI overlay) where T : Component
        {
            var comp = overlay.gameObject.GetComponent<T>();
            if (comp == null) comp = Undo.AddComponent<T>(overlay.gameObject);
            return comp;
        }

        void RemoveComponent<T>(OverlayUI overlay) where T : Component
        {
            var comp = overlay.GetComponent<T>();
            if (comp != null) Undo.DestroyObjectImmediate(comp);
        }
    }
}
