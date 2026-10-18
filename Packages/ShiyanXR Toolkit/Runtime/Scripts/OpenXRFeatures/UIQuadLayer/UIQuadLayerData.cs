using System;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using UnityEngine;

namespace com.unity.shiyantech.xr
{
    /// <summary>
    /// Subclass of <see cref="QuadLayerData" /> used by SafeQuadLayerHandler.
    /// Exists so we can register a custom handler for our UI layers without
    /// affecting other quad layers that use the built-in handler.
    /// </summary>
    [Serializable]
    [CompositionLayerData(
        Provider = "Unity",
        Name = "UIQuad(For 视延)",
        IconPath = "",
        InspectorIcon = "",
        ListViewIcon = "LayerQuad",
        SupportTransform = true,
        Description = "定义一个适配视延Pad用于UI的quad layer。",
        SuggestedExtenstionTypes = new[] { typeof(TexturesExtension) }
     )]

    public class UIQuadLayerData : QuadLayerData
    {
        /// <summary>
        /// Used to copy values from another layer data instance
        /// </summary>
        /// <inheritdoc/>
        public override void CopyFrom(LayerData layerData)
        {
            if (layerData is QuadLayerData quadLayerData)
            {
                Size = quadLayerData.Size;
                ApplyTransformScale = quadLayerData.ApplyTransformScale;
            }
        }
    }
}
