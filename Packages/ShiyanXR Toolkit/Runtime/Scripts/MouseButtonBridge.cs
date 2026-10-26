using UnityEngine;

/// <summary>
/// Android 侧鼠标按键桥的托管封装，Java 实现在
/// Runtime/Plugins/Android/com/shiyantech/input/MouseButtonBridge.java。
/// 起因：本设备无线鼠标"原地按下不移动"时事件不会进新版 Input System（旧的 Input.GetMouseButton(0) 能拿到），
/// 因此改由 Android 侧直接读 MotionEvent / KeyEvent 的按键状态，这里轮询，避免依赖旧输入系统。
/// 非 Android 平台一律返回 false。
/// </summary>
public static class MouseButtonBridge
{
#if UNITY_ANDROID && !UNITY_EDITOR
    const string k_JavaClassName = "com.shiyantech.input.MouseButtonBridge";
    const string k_UnityPlayerClassName = "com.unity3d.player.UnityPlayer";

    static AndroidJavaClass s_Bridge;
    static bool s_InstallFailed;

    static AndroidJavaClass Bridge
    {
        get
        {
            if (s_Bridge != null || s_InstallFailed)
                return s_Bridge;

            try
            {
                using (var player = new AndroidJavaClass(k_UnityPlayerClassName))
                {
                    var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    s_Bridge = new AndroidJavaClass(k_JavaClassName);
                    s_Bridge.CallStatic("install", activity);
                }
            }
            catch (System.Exception e)
            {
                s_InstallFailed = true;
                s_Bridge = null;
                Debug.LogWarning($"[MouseButtonBridge] 安装失败，按键判断退回 Input System：{e.Message}");
            }

            return s_Bridge;
        }
    }

    /// <summary>左键当前是否按下</summary>
    public static bool IsLeftPressed()
    {
        return Bridge != null && Bridge.CallStatic<bool>("isLeftPressed");
    }

    /// <summary>右键当前是否按下</summary>
    public static bool IsRightPressed()
    {
        return Bridge != null && Bridge.CallStatic<bool>("isRightPressed");
    }

    /// <summary>自上次调用以来是否按下过左键（读取后清零）</summary>
    public static bool ConsumeLeftClick()
    {
        return Bridge != null && Bridge.CallStatic<bool>("consumeLeftClick");
    }

    /// <summary>自上次调用以来的滚轮增量（读取后清零）</summary>
    public static Vector2 ConsumeScroll()
    {
        if (Bridge == null)
            return Vector2.zero;

        return new Vector2(
            Bridge.CallStatic<float>("consumeScrollX"),
            Bridge.CallStatic<float>("consumeScrollY"));
    }
#else
    /// <summary>左键当前是否按下（非 Android 平台恒为 false）</summary>
    public static bool IsLeftPressed() => false;

    /// <summary>右键当前是否按下（非 Android 平台恒为 false）</summary>
    public static bool IsRightPressed() => false;

    /// <summary>自上次调用以来是否按下过左键（非 Android 平台恒为 false）</summary>
    public static bool ConsumeLeftClick() => false;

    /// <summary>自上次调用以来的滚轮增量（非 Android 平台恒为零）</summary>
    public static Vector2 ConsumeScroll() => Vector2.zero;
#endif
}
