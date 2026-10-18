using UnityEngine;

namespace com.unity.shiyantech.xr
{
    /// <summary>
    /// 视延XR 项目配置：目标屏幕的单个 view 像素尺寸与屏幕物理尺寸。
    /// 配置文件由 Project Settings &gt; 视延XR项目配置 面板在使用方工程内创建，
    /// 位于 Assets/Resources/ShiyanXR/ShiyanXRProjectSettings.asset，运行时通过 <see cref="Instance"/> 读取。
    /// </summary>
    public sealed class ShiyanXRProjectSettings : ScriptableObject
    {
        /// <summary>配置文件所在的 Resources 子目录。</summary>
        public const string ResourceFolder = "ShiyanXR";

        /// <summary>配置文件名（不含扩展名）。</summary>
        public const string ResourceName = "ShiyanXRProjectSettings";

        public const int DefaultViewPixelWidth = 3840;
        public const int DefaultViewPixelHeight = 2160;
        public const float DefaultScreenPhysicalWidth = 0.6974f;
		public const float DefaultScreenPhysicalHeight = 0.3923f;

		[SerializeField] int m_ViewPixelWidth = DefaultViewPixelWidth;
        [SerializeField] int m_ViewPixelHeight = DefaultViewPixelHeight;
        [SerializeField] float m_ScreenPhysicalWidth = DefaultScreenPhysicalWidth;
        [SerializeField] float m_ScreenPhysicalHeight = DefaultScreenPhysicalHeight;

        static ShiyanXRProjectSettings s_Instance;

        /// <summary>
        /// 运行时可读的项目配置。配置文件不存在时返回一份默认配置，并给出一次警告。
        /// </summary>
        public static ShiyanXRProjectSettings Instance
        {
            get
            {
                if (s_Instance != null) return s_Instance;

                s_Instance = Resources.Load<ShiyanXRProjectSettings>($"{ResourceFolder}/{ResourceName}");
                if (s_Instance != null) return s_Instance;

                Debug.LogWarning($"[ShiyanXR] 未找到项目配置文件 Assets/Resources/{ResourceFolder}/{ResourceName}.asset，本次运行使用默认屏幕参数。" +
                                 "可在 Project Settings > 视延XR项目配置 中创建。");
                s_Instance = CreateInstance<ShiyanXRProjectSettings>();
                return s_Instance;
            }
        }

        /// <summary>屏幕单个 view 的像素宽。</summary>
        public int ViewPixelWidth => m_ViewPixelWidth;

        /// <summary>屏幕单个 view 的像素高。</summary>
        public int ViewPixelHeight => m_ViewPixelHeight;

        /// <summary>屏幕物理宽，单位：米。</summary>
        public float ScreenPhysicalWidth => m_ScreenPhysicalWidth;

        /// <summary>屏幕物理高，单位：米。</summary>
        public float ScreenPhysicalHeight => m_ScreenPhysicalHeight;
    }
}
