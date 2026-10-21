using System;
using System.IO;
using Microsoft.Win32;
using UnityEditor;
using UnityEngine;

namespace Shiyan.OpenXR.RawInput.Editor
{
    /// <summary>
    /// Unity 的 OpenXR 插件在增删 interaction profile / feature 后会重新初始化 XR；
    /// 某些环境下进程里的 XR_RUNTIME_JSON 会停留在旧值（例如 Monado），导致下一次
    /// Play 直接连错 runtime，必须重启 Unity。
    ///
    /// 这个 Editor bootstrap 在每次 domain reload 时把 XR_RUNTIME_JSON 对齐到系统
    /// 当前 ActiveRuntime（HKCU\Software\Khronos\OpenXR\1），并提供菜单手动重设。
    /// </summary>
    [InitializeOnLoad]
    public static class ShiyanOpenXRRuntimeBootstrap
    {
        const string kActiveRuntimeKey = @"HKEY_CURRENT_USER\Software\Khronos\OpenXR\1";

        static ShiyanOpenXRRuntimeBootstrap()
        {
            SyncRuntimeJson("domain reload");
        }

        [MenuItem("Shiyan/Force OpenXR Runtime (XR_RUNTIME_JSON)")]
        public static void ForceOpenXRRuntime()
        {
            SyncRuntimeJson("menu");
        }

        static void SyncRuntimeJson(string reason)
        {
            string current = Environment.GetEnvironmentVariable("XR_RUNTIME_JSON");
            string active = Registry.GetValue(kActiveRuntimeKey, "ActiveRuntime", null) as string;

            Debug.Log($"[ShiyanOpenXR] ({reason}) XR_RUNTIME_JSON='{current}' " +
                      $"ActiveRuntime='{active}'");

            if (string.IsNullOrEmpty(active) || !File.Exists(active))
                return;

            if (string.Equals(current, active, StringComparison.OrdinalIgnoreCase))
                return;

            Environment.SetEnvironmentVariable("XR_RUNTIME_JSON", active);
            Debug.Log($"[ShiyanOpenXR] XR_RUNTIME_JSON -> '{active}'");
        }
    }
}
