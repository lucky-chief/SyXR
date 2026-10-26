using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace com.shiyanxr.editor
{
    /// <summary>
    /// 按场景保存的面板设置：每个场景一条记录，键是场景资源的 GUID。
    ///
    /// 直接读写工程根目录 ProjectSettings 下的 JSON 文件，而不是用 ScriptableSingleton + .asset。
    /// 后者存档里带一个指向 MonoScript 的 m_Script 引用，脚本是“后加进已有文件”、或者在退出编辑器
    /// 这类时机写盘时，都可能被写成 m_Script: {fileID: 0}，下次打开就认不出类、加载不回来。
    /// 普通 JSON 没有这层依赖，也方便直接看内容和做 diff。
    ///
    /// 文件随工程提交到版本库后，同一工程换台机器打开也能拿回各场景的设置。
    /// </summary>
    static class ShiyanXREyePanelSettings
    {
        const string k_SettingsFileName = "ShiyanXREyePanelSettings.json";

        [Serializable]
        sealed class SceneEntry
        {
            public string sceneKey;
            // ShiyanXREyePanelState 的 JsonUtility 快照：字段增减时不用再同步一份存储结构
            public string settingsJson;
        }

        [Serializable]
        sealed class SettingsFile
        {
            public List<SceneEntry> scenes = new List<SceneEntry>();
        }

        // 整个文件只在第一次用到时读一次；改值只改这份内存数据，落盘由调用方合并
        static SettingsFile s_Settings;

        static SettingsFile Settings => s_Settings ??= Load();

        static string SettingsFilePath =>
            Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "ProjectSettings", k_SettingsFileName);

        internal static bool HasSettings(string sceneKey) => Find(sceneKey) != null;

        internal static string GetSettingsJson(string sceneKey)
        {
            var entry = Find(sceneKey);
            return entry != null ? entry.settingsJson : null;
        }

        /// <summary>写入某场景的快照，返回是否真的变了（没变就不必再写文件）。</summary>
        internal static bool SetSettingsJson(string sceneKey, string json)
        {
            var entry = Find(sceneKey);
            if (entry == null)
            {
                entry = new SceneEntry { sceneKey = sceneKey };
                Settings.scenes.Add(entry);
            }

            if (entry.settingsJson == json)
                return false;

            entry.settingsJson = json;
            return true;
        }

        /// <summary>把内存里的记录写回 JSON 文件。写失败只记日志，不影响面板继续使用。</summary>
        internal static void SaveNow()
        {
            try
            {
                string path = SettingsFilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                File.WriteAllText(path, JsonUtility.ToJson(Settings, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"[ShiyanXR] 保存 Eye Panel 设置失败：{e}");
            }
        }

        static SettingsFile Load()
        {
            var file = new SettingsFile();
            string path = SettingsFilePath;
            if (!File.Exists(path))
                return file;

            try
            {
                string json = File.ReadAllText(path);
                if (!string.IsNullOrEmpty(json))
                    JsonUtility.FromJsonOverwrite(json, file);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ShiyanXR] 读取 Eye Panel 设置失败，将使用默认值：{e}");
            }

            // JSON 被手工改坏 / 字段缺失时兜底，避免后面到处判空
            if (file.scenes == null)
                file.scenes = new List<SceneEntry>();

            return file;
        }

        static SceneEntry Find(string sceneKey)
        {
            foreach (var entry in Settings.scenes)
            {
                if (entry.sceneKey == sceneKey)
                    return entry;
            }

            return null;
        }
    }
}
