using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using com.unity.shiyantech.xr;

namespace com.shiyanxr.editor
{
    /// <summary>
    /// 把 <see cref="ShiyanXRSbsPresentFeature"/> 装到工程的 URP renderer 资产上。
    ///
    /// 两步都不能少：
    /// 1) RendererFeature 只有被 renderer 资产引用才会入队（URP 的硬性要求）；
    /// 2) ScriptableRenderer 构造时把 feature 列表拷贝了一份，所以只写资产不够 ——
    ///    还要 SetDirty() 让 URP 丢掉缓存的 renderer 重建，否则运行中的 renderer 根本不认新 feature，
    ///    AddRenderPasses 不会被调用，pass 也就永远不会执行。
    ///
    /// 只动工程 Assets/ 下的 renderer；包内资产不碰（URP 自带的 renderer 不属于宿主工程）。
    /// </summary>
    static class ShiyanXRSbsFeatureInstaller
    {
        const string k_FeatureName = "ShiyanXR SBS Present";
        const string k_ProjectRoot = "Assets/";

        public static void EnsureInstalled()
        {
            int projectRendererCount = 0;
            bool changed = false;

            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRendererData"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                if (data == null) continue;
                if (!path.StartsWith(k_ProjectRoot, StringComparison.Ordinal)) continue;

                projectRendererCount++;
                if (FindInstalled(data) != null) continue;

                var feature = ScriptableObject.CreateInstance<ShiyanXRSbsPresentFeature>();
                feature.name = k_FeatureName;
                AssetDatabase.AddObjectToAsset(feature, data);
                data.rendererFeatures.Add(feature);

                // 两个机制都要：前者让改动落盘，后者（ScriptableRendererData.SetDirty 置 isInvalidated）
                // 让 URP 重建 renderer —— ScriptableRenderer 构造时会拷一份 feature 列表，不重建就不认新 feature
                EditorUtility.SetDirty(data);
                data.SetDirty();

                changed = true;
                Debug.Log($"[ShiyanXR] 已为 URP renderer 资产装入 SBS 呈现 Feature：{path}");
            }

            if (changed) AssetDatabase.SaveAssets();

            if (projectRendererCount == 0)
                Debug.LogWarning("[ShiyanXR] 工程 Assets/ 下没找到 URP renderer 资产，Both（SBS）无法呈现。");
        }

        static ShiyanXRSbsPresentFeature FindInstalled(UniversalRendererData data)
        {
            foreach (var feature in data.rendererFeatures)
            {
                if (feature is ShiyanXRSbsPresentFeature installed) return installed;
            }

            return null;
        }
    }
}
