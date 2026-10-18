package com.shiyantech.input;

import android.app.Activity;
import android.util.Log;
import android.view.KeyEvent;
import android.view.MotionEvent;
import android.view.SurfaceView;
import android.view.View;
import android.view.ViewGroup;

/**
 * 鼠标按键桥：把 Android 侧的鼠标左键 / 右键状态交给 Unity 侧轮询。
 *
 * 背景：本设备的无线鼠标（dongle: "YICHIP Wireless Device Mouse"）在"原地按下不移动"时，
 * 事件不会被新版 Input System 收到（旧的 Input.GetMouseButton(0) 能收到），因此在 Android 侧
 * 直接读取 MotionEvent / KeyEvent 的按键状态供 C# 轮询，从而不依赖旧输入系统。
 *
 * 所有监听器一律返回 false，不消费事件，Unity 自身的输入处理不受影响。
 */
public final class MouseButtonBridge
{
    private static final String TAG = "MouseButtonBridge";
    private static final int BUTTON_PRIMARY = 1; // MotionEvent.getButtonState() 中的主键位
    private static final int BUTTON_SECONDARY = 2; // MotionEvent.getButtonState() 中的次键位（鼠标右键）
    /** Unity 承载输入的那个视图（2022.3 上是 FrameLayout，见 attachToInputTargets 的说明） */
    private static final String UNITY_PLAYER_VIEW_CLASS = "com.unity3d.player.UnityPlayer";

    private static volatile boolean sInstalled;
    private static volatile boolean sLeftPressed;
    private static volatile boolean sRightPressed;
    private static volatile boolean sClickLatched;

    private MouseButtonBridge() {}

    /** 左键当前是否按下 */
    public static boolean isLeftPressed()
    {
        return sLeftPressed;
    }

    /** 右键当前是否按下 */
    public static boolean isRightPressed()
    {
        return sRightPressed;
    }

    /** 自上次调用以来是否按下过左键（读取后清零，用于兜住一次更新内完成的按下+抬起） */
    public static boolean consumeLeftClick()
    {
        boolean latched = sClickLatched;
        sClickLatched = false;
        return latched;
    }

    /** 在主线程挂监听，重复调用安全；activity 由 C# 侧传入 */
    public static void install(final Activity activity)
    {
        if (sInstalled || activity == null)
            return;

        sInstalled = true;
        activity.runOnUiThread(new Runnable()
        {
            @Override
            public void run()
            {
                attachToInputTargets(activity.getWindow().getDecorView(), true);
            }
        });
    }

    /**
     * 把监听挂到真正会拿到鼠标事件的视图上。
     *
     * 不能只挂 decorView：Unity 的 UnityPlayer 视图自己重写了 onTouchEvent / onGenericMotionEvent /
     * onKeyDown，事件会被它（或它内部的 SurfaceView 子视图）消费而不再向上冒泡，挂在 decorView 上收不到。
     * 因此这里沿视图树给 decorView、UnityPlayer 视图和所有 SurfaceView 都挂一份，谁收到就算谁的。
     */
    private static void attachToInputTargets(View view, boolean isRoot)
    {
        if (view == null)
            return;

        if (isRoot || view instanceof SurfaceView || UNITY_PLAYER_VIEW_CLASS.equals(view.getClass().getName()))
        {
            view.setOnTouchListener(TOUCH_LISTENER);
            view.setOnGenericMotionListener(GENERIC_MOTION_LISTENER);
            view.setOnKeyListener(KEY_LISTENER);
        }

        if (!(view instanceof ViewGroup))
            return;

        ViewGroup group = (ViewGroup) view;
        for (int i = 0; i < group.getChildCount(); ++i)
            attachToInputTargets(group.getChildAt(i), false);
    }

    private static final View.OnGenericMotionListener GENERIC_MOTION_LISTENER = new View.OnGenericMotionListener()
    {
        @Override
        public boolean onGenericMotion(View view, MotionEvent event)
        {
            // 鼠标的悬停/移动/滚轮走 generic motion，按下时的 buttonState 也在这里带上
            updateFromMotionEvent(event);
            return false;
        }
    };

    private static final View.OnTouchListener TOUCH_LISTENER = new View.OnTouchListener()
    {
        @Override
        public boolean onTouch(View view, MotionEvent event)
        {
            // 鼠标的按下/抬起在 Android 上走 touch 通道（ACTION_DOWN / ACTION_UP）
            updateFromMotionEvent(event);
            return false;
        }
    };

    private static final View.OnKeyListener KEY_LISTENER = new View.OnKeyListener()
    {
        @Override
        public boolean onKey(View view, int keyCode, KeyEvent event)
        {
            // 少数设备把鼠标按键当按键事件上报（BTN_LEFT -> KEYCODE_BUTTON_1，BTN_RIGHT -> KEYCODE_BUTTON_2）
            final boolean isActionDown = event.getAction() == KeyEvent.ACTION_DOWN;
            if (!isActionDown && event.getAction() != KeyEvent.ACTION_UP)
                return false;

            if (keyCode == KeyEvent.KEYCODE_BUTTON_1)
                setLeftPressed(isActionDown);
            else if (keyCode == KeyEvent.KEYCODE_BUTTON_2)
                setRightPressed(isActionDown);

            return false;
        }
    };

    private static void updateFromMotionEvent(MotionEvent event)
    {
        if (event == null)
            return;

        // 位置类事件 buttonState 为 0（未按键），只有真正按下时才会带上对应的键位
        final int buttonState = event.getButtonState();
        setLeftPressed((buttonState & BUTTON_PRIMARY) != 0);
        setRightPressed((buttonState & BUTTON_SECONDARY) != 0);
    }

    private static void setLeftPressed(boolean pressed)
    {
        if (pressed == sLeftPressed)
            return;

        if (pressed)
        {
            sClickLatched = true;
            Log.d(TAG, "left button pressed");
        }
        else
        {
            Log.d(TAG, "left button released");
        }

        sLeftPressed = pressed;
    }

    /** 右键的按下/抬起写法跟左键一致，只是没有 latch（右键目前只用按住状态） */
    private static void setRightPressed(boolean pressed)
    {
        if (pressed == sRightPressed)
            return;

        Log.d(TAG, pressed ? "right button pressed" : "right button released");

        sRightPressed = pressed;
    }
}
