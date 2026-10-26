using UnityEngine;

namespace com.shiyanxr.editor
{
    /// <summary>
    /// 单个场景的面板状态载体。必须是 UnityEngine.Object，Unity 的 Undo 才能记录 / 撤销这些值；
    /// 对象本身不落盘（HideFlags.DontSave），每个场景的持久化由 <see cref="ShiyanXREyePanelSettings"/>
    /// 里的 JSON 快照负责。
    ///
    /// 单独放在与类同名的文件里，遵循 Unity 对 ScriptableObject 脚本的约定（文件名与类名一致）。
    /// </summary>
    internal sealed class ShiyanXREyePanelState : ScriptableObject
    {
        // public 字段会被 Unity 序列化，SerializedProperty 才能找到并记录 Undo
        public float convergencePlaneDistance = 1f;
        public float interocularDistance = 0.065f;
        public bool displayCameras = true;
        // 默认取红 / 蓝：与 ShiyanDevicesSimulator 的 leftCameraColor / rightCameraColor 同一视觉约定
        public Color leftCameraColor = Color.red;
        public Color rightCameraColor = Color.blue;
        // 默认取青色：与 ShiyanDevicesSimulator 的 screenColor（屏幕 / 焦平面）同一视觉约定
        public Color convergencePlaneColor = Color.cyan;
        public float convergencePlaneAlpha = 0.3f;
    }
}
