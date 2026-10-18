using System.Collections.Generic;
using com.unity.shiyantech.xr;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.shiyanxr.editor
{
    /// <summary>
    /// Project Settings &gt; 视延XR项目配置 面板。
    /// 配置文件属于使用方工程（包可能以 registry/git 方式只读安装），
    /// 首次打开面板时在使用方工程的 Assets/Resources/ShiyanXR 下创建。
    /// </summary>
    sealed class ShiyanXRProjectSettingsProvider : SettingsProvider
    {
        const string k_SettingsPath = "Project/视延XR项目配置";
        const string k_ResourcesFolder = "Assets/Resources";

        static string SettingsFolder => $"{k_ResourcesFolder}/{ShiyanXRProjectSettings.ResourceFolder}";
        static string SettingsAssetPath => $"{SettingsFolder}/{ShiyanXRProjectSettings.ResourceName}.asset";

        static readonly GUIContent k_ViewPixelWidthLabel =
            new GUIContent("单 View 像素宽", "屏幕单个 view 的水平像素数。");
        static readonly GUIContent k_ViewPixelHeightLabel =
            new GUIContent("单 View 像素高", "屏幕单个 view 的垂直像素数。");
        static readonly GUIContent k_ScreenPhysicalWidthLabel =
            new GUIContent("屏幕物理宽 (米)", "屏幕的物理宽度，单位：米。");
        static readonly GUIContent k_ScreenPhysicalHeightLabel =
            new GUIContent("屏幕物理高 (米)", "屏幕的物理高度，单位：米。");

        SerializedObject m_Settings;
        SerializedProperty m_ViewPixelWidth;
        SerializedProperty m_ViewPixelHeight;
        SerializedProperty m_ScreenPhysicalWidth;
        SerializedProperty m_ScreenPhysicalHeight;

        ShiyanXRProjectSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords)
            : base(path, scopes, keywords)
        {
            guiHandler = OnGUIHandler;
        }

        [SettingsProvider]
        static SettingsProvider CreateProvider()
        {
            return new ShiyanXRProjectSettingsProvider(k_SettingsPath, SettingsScope.Project, new[]
            {
                "ShiyanXR", "视延", "XR", "屏幕", "像素", "分辨率", "物理尺寸", "view",
            });
        }

        /// <summary>读取使用方工程内的配置文件；不存在则连同所需文件夹一起创建，运行时会通过 Resources 读取它。</summary>
        static ShiyanXRProjectSettings GetOrCreateSettingsAsset()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ShiyanXRProjectSettings>(SettingsAssetPath);
            if (settings != null) return settings;

            if (!AssetDatabase.IsValidFolder(k_ResourcesFolder))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(SettingsFolder))
                AssetDatabase.CreateFolder(k_ResourcesFolder, ShiyanXRProjectSettings.ResourceFolder);

            settings = ScriptableObject.CreateInstance<ShiyanXRProjectSettings>();
            AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ShiyanXR] 已创建项目配置文件 {SettingsAssetPath}，请随工程提交到版本库。");
            return settings;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_Settings = new SerializedObject(GetOrCreateSettingsAsset());
            m_ViewPixelWidth = m_Settings.FindProperty("m_ViewPixelWidth");
            m_ViewPixelHeight = m_Settings.FindProperty("m_ViewPixelHeight");
            m_ScreenPhysicalWidth = m_Settings.FindProperty("m_ScreenPhysicalWidth");
            m_ScreenPhysicalHeight = m_Settings.FindProperty("m_ScreenPhysicalHeight");
        }

        void OnGUIHandler(string searchContext)
        {
            if (m_Settings == null)
                OnActivate(searchContext, null);

            m_Settings.Update();

            EditorGUILayout.LabelField("屏幕", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_ViewPixelWidth, k_ViewPixelWidthLabel);
                EditorGUILayout.PropertyField(m_ViewPixelHeight, k_ViewPixelHeightLabel);
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(m_ScreenPhysicalWidth, k_ScreenPhysicalWidthLabel);
                EditorGUILayout.PropertyField(m_ScreenPhysicalHeight, k_ScreenPhysicalHeightLabel);
            }

            bool modified = m_Settings.ApplyModifiedProperties();
            // 像素尺寸至少为 1、物理尺寸必须为正，避免把无效配置写进配置文件。
            modified |= ClampToMin(m_ViewPixelWidth, 1);
            modified |= ClampToMin(m_ViewPixelHeight, 1);
            modified |= ClampToMin(m_ScreenPhysicalWidth, 0.001f);
            modified |= ClampToMin(m_ScreenPhysicalHeight, 0.001f);

            if (!modified) return;

            m_Settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(m_Settings.targetObject);
            AssetDatabase.SaveAssets();
        }

        static bool ClampToMin(SerializedProperty property, int min)
        {
            if (property.intValue >= min) return false;
            property.intValue = min;
            return true;
        }

        static bool ClampToMin(SerializedProperty property, float min)
        {
            if (property.floatValue >= min) return false;
            property.floatValue = min;
            return true;
        }
    }
}
