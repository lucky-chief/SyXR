using UnityEngine;
using UnityEngine.XR;
#if !UNITY_EDITOR
using com.shiyantech.syruntime.unity;
#endif

public class XRSettings : MonoBehaviour
{
    //[RuntimeInitializeOnLoadMethod]
    //static void SetSurfaceSize()
    //{
    //    Screen.SetResolution(Screen.width * 2, Screen.height, true);
    //}

    void Start()
    {
#if !UNITY_EDITOR && UNITY_ANDROID
        SyRuntimeBridge.AddWhitelist("com.openxr.test.gl");
        SyRuntimeBridge.Init();
        if(SyRuntimeBridge.IsReady())
        {
            SyRuntimeBridge.Set3DFlag(true);
            Debug.Log("SyRuntimeBridge is ready");
        }
        else
        {
            Debug.LogError("SyRuntimeBridge is not ready");
        }
#endif

        //Screen.SetResolution(7680, 2160, true);
        //UnityEngine.XR.XRSettings.useOcclusionMesh = false;
        //UnityEngine.XR.XRSettings.gameViewRenderMode = GameViewRenderMode.None;
#if UNITY_EDITOR_WINDOWS || UNITY_STANDALONE_WIN
        UnityEngine.XR.XRSettings.gameViewRenderMode = GameViewRenderMode.OcclusionMesh;
#endif
#if !UNITY_EDITOR
        HideCorsor();

        Camera cam = GetComponentInChildren<Camera>();
        if(cam != null)
        {
            cam.enabled = true;
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;
            cam.transform.localScale = Vector3.one;
            cam.stereoTargetEye = StereoTargetEyeMask.Both;
        }
#endif
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            HideCorsor();
        }
    }

    void HideCorsor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
    }
}
