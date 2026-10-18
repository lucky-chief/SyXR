using System;
using System.Runtime.InteropServices;
using UnityEngine;
namespace com.unity.shiyantech.xr
{
    /// <summary>
    /// OpenXR Runtime 运行时配置管理器
    /// 
    /// 使用方法：
    /// 1. 将此脚本放到 Unity 项目的 Assets/Scripts 目录下
    /// 2. 在场景中创建一个 GameObject 并挂载此脚本
    /// 3. 通过 Inspector 或代码调用 SetConfig 方法设置参数
    /// 
    /// 示例:
    ///     OpenXRRuntimeConfig config = GetComponent<OpenXRRuntimeConfig>();
    ///     config.SetIPDScale(0.5f);
    ///     config.SetViewingDistance(0.4f);
    /// </summary>
    public class OpenXRRuntimeConfig : MonoBehaviour
    {
        #region 函数签名定义

        // 设置浮点配置
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int XrSetRuntimeConfigFloatEXT(string key, float value);

        // 获取浮点配置
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int XrGetRuntimeConfigFloatEXT(string key, out float value);

        // 设置布尔配置
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int XrSetRuntimeConfigBoolEXT(string key, int value);

        // 获取布尔配置
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int XrGetRuntimeConfigBoolEXT(string key, out int value);

        // 设置Vector3配置
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int XrSetRuntimeConfigVector3EXT(string key, ref Vector3 value);

        // 获取Vector3配置
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int XrGetRuntimeConfigVector3EXT(string key, out Vector3 value);

        // 重置配置
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int XrResetRuntimeConfigEXT();

        #endregion

        #region 私有成员

        private XrSetRuntimeConfigFloatEXT setFloatConfig;
        private XrGetRuntimeConfigFloatEXT getFloatConfig;
        private XrSetRuntimeConfigBoolEXT setBoolConfig;
        private XrGetRuntimeConfigBoolEXT getBoolConfig;
        private XrSetRuntimeConfigVector3EXT setVector3Config;
        private XrGetRuntimeConfigVector3EXT getVector3Config;
        private XrResetRuntimeConfigEXT resetConfig;

        private bool isInitialized = false;

        #endregion

        #region Unity 生命周期

        void Start()
        {
            InitializeExtensions();

            if (isInitialized)
            {
                Debug.Log("[OpenXRRuntimeConfig] 初始化成功，可以使用运行时配置功能");

                // 可选：启动时设置默认值
                // SetDefaultConfig();
            }
            else
            {
                Debug.LogError("[OpenXRRuntimeConfig] 初始化失败，请确保使用支持 XR_EXT_runtime_config 扩展的 Runtime");
            }
        }

        #endregion

        #region 初始化

        private void InitializeExtensions()
        {
            // #region agent log
            Debug.Log($"[DEBUG-NEW] InitializeExtensions using JNI Bridge - Platform={Application.platform}, IsEditor={Application.isEditor}");
            // #endregion

            try
            {
                // 初始化JNI桥接
                InitializeJNIBridge();

                if (jniBridge == null)
                {
                    Debug.LogError("[DEBUG-NEW] JNI Bridge is null, cannot proceed");
                    return;
                }

                // 创建委托（这些方法通过JNI调用native函数，性能高）
                setFloatConfig = Native_xrSetRuntimeConfigFloatEXT;
                getFloatConfig = Native_xrGetRuntimeConfigFloatEXT;
                setBoolConfig = Native_xrSetRuntimeConfigBoolEXT;
                getBoolConfig = Native_xrGetRuntimeConfigBoolEXT;
                setVector3Config = Native_xrSetRuntimeConfigVector3EXT;
                getVector3Config = Native_xrGetRuntimeConfigVector3EXT;
                resetConfig = Native_xrResetRuntimeConfigEXT;

                // #region agent log
                Debug.Log("[DEBUG-NEW] All delegates created (JNI Bridge mode)");
                // #endregion

                isInitialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[OpenXRRuntimeConfig] 初始化失败: {e.Message}\n{e.StackTrace}");
            }
        }

        // 使用Android JNI桥接（高性能方案）
        // Unity通过AndroidJavaObject调用Java，Java通过JNI调用C++ native函数
        private AndroidJavaObject jniBridge;

        private void InitializeJNIBridge()
        {
            try
            {
                // 获取Java桥接类
                jniBridge = new AndroidJavaObject("com.shiyantech.openxr.OpenXRRuntimeBridge");
                Debug.Log("[DEBUG-NEW] JNI Bridge object created successfully");

                // 测试JNI是否工作
                try
                {
                    string testResult = jniBridge.CallStatic<string>("testJNI");
                    Debug.Log($"[DEBUG-NEW] JNI Test result: {testResult}");
                    if (testResult != null && testResult.Contains("working"))
                    {
                        Debug.Log("[DEBUG-NEW] ✓ JNI Bridge is fully functional!");
                    }
                    else
                    {
                        Debug.LogWarning($"[DEBUG-NEW] ⚠ JNI test returned: {testResult}");
                    }
                }
                catch (Exception testEx)
                {
                    Debug.LogError($"[DEBUG-NEW] JNI Test failed: {testEx.Message}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[DEBUG-NEW] Failed to initialize JNI Bridge: {e.Message}");
                jniBridge = null;
            }
        }

        private int Native_xrSetRuntimeConfigFloatEXT(string key, float value)
        {
            if (jniBridge == null)
            {
                Debug.LogError("[DEBUG-NEW] JNI Bridge not initialized");
                return 1;
            }

            try
            {
                int result = jniBridge.CallStatic<int>("setFloat", key, value);
                Debug.Log($"[DEBUG-NEW] JNI SetFloat {key}={value:F6}, result={result}");
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DEBUG-NEW] JNI SetFloat failed: {e.Message}");
                return 1;
            }
        }

        private int Native_xrGetRuntimeConfigFloatEXT(string key, out float value)
        {
            value = 0f;

            if (jniBridge == null)
            {
                Debug.LogError("[DEBUG-NEW] JNI Bridge not initialized");
                return 1;
            }

            try
            {
                value = jniBridge.CallStatic<float>("getFloat", key);
                Debug.Log($"[DEBUG-NEW] JNI GetFloat {key}={value:F6}");
                return 0;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DEBUG-NEW] JNI GetFloat failed: {e.Message}");
                return 1;
            }
        }

        private int Native_xrSetRuntimeConfigBoolEXT(string key, int value)
        {
            if (jniBridge == null)
            {
                return 1;
            }

            try
            {
                bool boolValue = value != 0;
                int result = jniBridge.CallStatic<int>("setBool", key, boolValue);
                //Debug.Log($"[DEBUG-NEW] JNI SetBool {key}={boolValue}, result={result}");
                return result;
            }
            catch (Exception e)
            {
                //Debug.LogError($"[DEBUG-NEW] JNI SetBool failed: {e.Message}");
                return 1;
            }
        }

        private int Native_xrGetRuntimeConfigBoolEXT(string key, out int value)
        {
            value = 0;

            if (jniBridge == null)
            {
                return 1;
            }

            try
            {
                bool boolValue = jniBridge.CallStatic<bool>("getBool", key);
                value = boolValue ? 1 : 0;
                Debug.Log($"[DEBUG-NEW] JNI GetBool {key}={boolValue}");
                return 0;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DEBUG-NEW] JNI GetBool failed: {e.Message}");
                return 1;
            }
        }

        private int Native_xrSetRuntimeConfigVector3EXT(string key, ref Vector3 value)
        {
            if (jniBridge == null)
            {
                return 1;
            }

            try
            {
                int result = jniBridge.CallStatic<int>("setVector3", key, value.x, value.y, value.z);
                // Debug.Log($"[DEBUG-NEW] JNI SetVector3 {key}=({value.x:F6}, {value.y:F6}, {value.z:F6}), result={result}");
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DEBUG-NEW] JNI SetVector3 failed: {e.Message}");
                return 1;
            }
        }

        private int Native_xrGetRuntimeConfigVector3EXT(string key, out Vector3 value)
        {
            value = Vector3.zero;

            if (jniBridge == null)
            {
                return 1;
            }

            try
            {
                float[] result = jniBridge.CallStatic<float[]>("getVector3", key);
                if (result != null && result.Length == 3)
                {
                    value = new Vector3(result[0], result[1], result[2]);
                    Debug.Log($"[DEBUG-NEW] JNI GetVector3 {key}=({value.x:F6}, {value.y:F6}, {value.z:F6})");
                    return 0;
                }
                return 1;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DEBUG-NEW] JNI GetVector3 failed: {e.Message}");
                return 1;
            }
        }

        private int Native_xrResetRuntimeConfigEXT()
        {
            if (jniBridge == null)
            {
                return 1;
            }

            try
            {
                int result = jniBridge.CallStatic<int>("resetConfig");
                Debug.Log($"[DEBUG-NEW] JNI ResetConfig, result={result}");
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DEBUG-NEW] JNI ResetConfig failed: {e.Message}");
                return 1;
            }
        }

        #endregion

        #region 公共 API - 设置配置

        /// <summary>
        /// 设置 IPD 缩放因子
        /// </summary>
        /// <param name="scale">缩放值，范围 0.1-2.0，推荐 0.3-0.6</param>
        public bool SetIPDScale(float scale)
        {
            return SetFloatConfig("ipd_scale", scale);
        }

        /// <summary>
        /// 设置观看距离（米）
        /// </summary>
        /// <param name="distance">距离值，范围 0.1-2.0，推荐 0.3-0.5</param>
        public bool SetViewingDistance(float distance)
        {
            return SetFloatConfig("viewing_distance", distance);
        }

        /// <summary>
        /// 设置屏幕物理宽度（米）
        /// </summary>
        /// <param name="width">宽度值，范围 0.1-1.0</param>
        public bool SetScreenWidthMeters(float width)
        {
            return SetFloatConfig("screen_width_meters", width);
        }

        /// <summary>
        /// 设置屏幕物理高度（米）
        /// </summary>
        /// <param name="height">高度值，范围 0.05-0.5</param>
        public bool SetScreenHeightMeters(float height)
        {
            return SetFloatConfig("screen_height_meters", height);
        }

        /// <summary>
        /// 设置屏幕缩放因子
        /// </summary>
        /// <param name="scale">缩放值，范围 0.1-5.0</param>
        public bool SetScreenScale(float scale)
        {
            return SetFloatConfig("screen_scale", scale);
        }

        /// <summary>
        /// 设置眼追数据是否有效。无效时 Runtime 会回退到稳定默认视图。
        /// </summary>
        /// <param name="valid">true 表示使用实时眼位，false 表示停用实时离轴眼位</param>
        public bool SetEyeTrackingValid(bool valid)
        {
            return SetBoolConfig("eye_tracking_valid", valid);
        }

        /// <summary>
        /// 设置是否交换左右眼
        /// </summary>
        /// <param name="swap">true 表示交换，false 表示不交换</param>
        public bool SetSwapEyes(bool swap)
        {
            return SetBoolConfig("swap_eyes", swap);
        }

        /// <summary>
        /// 设置左眼位置（米）
        /// </summary>
        /// <param name="position">左眼位置 (x, y, z)</param>
        public bool SetLeftEyePos(Vector3 position)
        {
            return SetVector3Config("left_eye_pos", position);
        }

        /// <summary>
        /// 设置左眼位置（米）
        /// </summary>
        /// <param name="x">X坐标（米）</param>
        /// <param name="y">Y坐标（米）</param>
        /// <param name="z">Z坐标（米，通常是观看距离）</param>
        public bool SetLeftEyePos(float x, float y, float z)
        {
            return SetLeftEyePos(new Vector3(x, y, z));
        }

        /// <summary>
        /// 设置右眼位置（米）
        /// </summary>
        /// <param name="position">右眼位置 (x, y, z)</param>
        public bool SetRightEyePos(Vector3 position)
        {
            return SetVector3Config("right_eye_pos", position);
        }

        /// <summary>
        /// 设置右眼位置（米）
        /// </summary>
        /// <param name="x">X坐标（米）</param>
        /// <param name="y">Y坐标（米）</param>
        /// <param name="z">Z坐标（米，通常是观看距离）</param>
        public bool SetRightEyePos(float x, float y, float z)
        {
            return SetRightEyePos(new Vector3(x, y, z));
        }

        /// <summary>
        /// 重置所有配置为默认值
        /// </summary>
        public bool ResetAllConfig()
        {
            if (!isInitialized || resetConfig == null)
            {
                Debug.LogError("[OpenXRRuntimeConfig] 未初始化或不支持重置功能");
                return false;
            }

            int result = resetConfig();

            if (result == 0)
            {
                Debug.Log("[OpenXRRuntimeConfig] 已重置所有配置为默认值");
                return true;
            }
            else
            {
                Debug.LogError($"[OpenXRRuntimeConfig] 重置配置失败，错误码: {result}");
                return false;
            }
        }

        #endregion

        #region 公共 API - 获取配置

        /// <summary>
        /// 获取 IPD 缩放因子
        /// </summary>
        public float GetIPDScale()
        {
            GetFloatConfig("ipd_scale", out float value);
            return value;
        }

        /// <summary>
        /// 获取观看距离
        /// </summary>
        public float GetViewingDistance()
        {
            GetFloatConfig("viewing_distance", out float value);
            return value;
        }

        /// <summary>
        /// 获取屏幕物理宽度
        /// </summary>
        public float GetScreenWidthMeters()
        {
            GetFloatConfig("screen_width_meters", out float value);
            return value;
        }

        /// <summary>
        /// 获取屏幕物理高度
        /// </summary>
        public float GetScreenHeightMeters()
        {
            GetFloatConfig("screen_height_meters", out float value);
            return value;
        }

        /// <summary>
        /// 获取屏幕缩放因子
        /// </summary>
        public float GetScreenScale()
        {
            GetFloatConfig("screen_scale", out float value);
            return value;
        }

        /// <summary>
        /// 获取是否交换左右眼
        /// </summary>
        public bool GetSwapEyes()
        {
            if (!isInitialized || getBoolConfig == null)
                return false;

            int result = getBoolConfig("swap_eyes", out int value);
            return result == 0 && value != 0;
        }

        /// <summary>
        /// 获取左眼位置
        /// </summary>
        public Vector3 GetLeftEyePos()
        {
            GetVector3Config("left_eye_pos", out Vector3 value);
            return value;
        }

        /// <summary>
        /// 获取右眼位置
        /// </summary>
        public Vector3 GetRightEyePos()
        {
            GetVector3Config("right_eye_pos", out Vector3 value);
            return value;
        }

        #endregion

        #region 私有辅助方法

        private bool SetFloatConfig(string key, float value)
        {
            if (!isInitialized || setFloatConfig == null)
            {
                Debug.LogError("[OpenXRRuntimeConfig] 未初始化");
                return false;
            }

            int result = setFloatConfig(key, value);

            if (result == 0) // XR_SUCCESS = 0
            {
                Debug.Log($"[OpenXRRuntimeConfig] 设置成功: {key} = {value}");
                return true;
            }
            else
            {
                Debug.LogError($"[OpenXRRuntimeConfig] 设置失败: {key}, 错误码: {result}");
                return false;
            }
        }

        private bool GetFloatConfig(string key, out float value)
        {
            value = 0f;

            if (!isInitialized || getFloatConfig == null)
            {
                Debug.LogError("[OpenXRRuntimeConfig] 未初始化");
                return false;
            }

            int result = getFloatConfig(key, out value);

            if (result == 0)
            {
                Debug.Log($"[OpenXRRuntimeConfig] 获取成功: {key} = {value}");
                return true;
            }
            else
            {
                Debug.LogError($"[OpenXRRuntimeConfig] 获取失败: {key}, 错误码: {result}");
                return false;
            }
        }

        private bool SetBoolConfig(string key, bool value)
        {
            if (!isInitialized || setBoolConfig == null)
            {
                Debug.LogError("[OpenXRRuntimeConfig] 未初始化");
                return false;
            }

            int result = setBoolConfig(key, value ? 1 : 0);

            if (result == 0)
            {
                //Debug.Log($"[OpenXRRuntimeConfig] 设置S成功: {key} = {value}");
                return true;
            }
            else
            {
                Debug.LogError($"[OpenXRRuntimeConfig] 设置失败: {key}, 错误码: {result}");
                return false;
            }
        }

        private bool SetVector3Config(string key, Vector3 value)
        {
            if (!isInitialized || setVector3Config == null)
            {
                Debug.LogError("[OpenXRRuntimeConfig] 未初始化或不支持 Vector3 配置");
                return false;
            }

            int result = setVector3Config(key, ref value);

            if (result == 0)
            {
                //Debug.Log($"[OpenXRRuntimeConfig] 设置成功: {key} = ({value.x}, {value.y}, {value.z})");
                return true;
            }
            else
            {
                Debug.LogError($"[OpenXRRuntimeConfig] 设置失败: {key}, 错误码: {result}");
                return false;
            }
        }

        private bool GetVector3Config(string key, out Vector3 value)
        {
            value = Vector3.zero;

            if (!isInitialized || getVector3Config == null)
            {
                Debug.LogError("[OpenXRRuntimeConfig] 未初始化或不支持 Vector3 配置");
                return false;
            }

            int result = getVector3Config(key, out value);

            if (result == 0)
            {
                Debug.Log($"[OpenXRRuntimeConfig] 获取成功: {key} = ({value.x}, {value.y}, {value.z})");
                return true;
            }
            else
            {
                Debug.LogError($"[OpenXRRuntimeConfig] 获取失败: {key}, 错误码: {result}");
                return false;
            }
        }

        #endregion

        #region 预设配置

        /// <summary>
        /// 设置默认配置
        /// </summary>
        public void SetDefaultConfig()
        {
            SetIPDScale(0.45f);
            SetViewingDistance(0.35f);
            Debug.Log("[OpenXRRuntimeConfig] 已应用默认配置");
        }

        /// <summary>
        /// 强视差效果（适合演示）
        /// </summary>
        public void SetStrongStereoscopicEffect()
        {
            SetIPDScale(0.6f);
            SetViewingDistance(0.25f);
            Debug.Log("[OpenXRRuntimeConfig] 已应用强视差效果");
        }

        /// <summary>
        /// 舒适视差效果（适合长时间使用）
        /// </summary>
        public void SetComfortableStereoscopicEffect()
        {
            SetIPDScale(0.35f);
            SetViewingDistance(0.5f);
            Debug.Log("[OpenXRRuntimeConfig] 已应用舒适视差效果");
        }

        /// <summary>
        /// 极弱视差效果（最舒适）
        /// </summary>
        public void SetMinimalStereoscopicEffect()
        {
            SetIPDScale(0.3f);
            SetViewingDistance(0.7f);
            Debug.Log("[OpenXRRuntimeConfig] 已应用极弱视差效果");
        }

        #endregion
    }
}
