using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// 给 uGUI / XRI 一台"像素空间可用"的事件相机，修掉所有拖拽类组件的共性问题。
///
/// 本工程的相机走屏幕空间模型：Camera.main 的 Transform 停在屏幕平面上，
/// 对 UI 命中点的 <c>WorldToScreenPoint</c> 会退化（返回 (0,0)，`pressRaycast.screenPosition` 也是 0），
/// 于是 XRI 里 <c>eventData.position</c> 恒为 0 → `delta` 恒为 0 →
/// <c>UIInputModule.ProcessPointerButtonDrag</c> 的 <c>if (!eventData.IsPointerMoving()) return;</c> 直接拦掉，
/// 所有依赖 IDragHandler 的组件（Slider / ScrollRect / Scrollbar / InputField…）一起失效；
/// 同时 <c>Graphic.Raycast(screenPos, eventCamera)</c> 用的是同一个屏幕点，
/// 屏幕点退化后还会出现"部分 UI 能点、部分点不到"的现象。
///
/// 工程里"屏幕像素空间"本来就有唯一定义：<see cref="ScreenPointToRayHelper"/> 的 view / projection 矩阵
/// （控制器发射线就是用它）。这里建一台**只做坐标换算、不参与渲染**的相机，直接采用这两个矩阵，
/// 再把它接到 Canvas.worldCamera 与 XRI 的 <c>UIInputModule.uiCamera</c> 上，
/// 使 <c>eventData.position</c> 与 <c>pressEventCamera</c> 自洽 —— uGUI 自己的拖动 / 点击逻辑随之恢复，
/// 不需要给每个组件写 handler。
///
/// 挂在世界空间画布上即可（本工程是 0-World Canvas），无需连线：helper 与输入模块都是运行时自动找。
/// 运行期（真机 / PC 构建 / Editor Play 模式）生效，非 Play 的编辑模式不介入，避免往场景里塞临时相机；
/// OnDisable 会还原原来的相机引用并销毁临时相机。
/// </summary>
[RequireComponent(typeof(Canvas))]
public class SyUIEventCamera : MonoBehaviour
{
    [SerializeField] ScreenPointToRayHelper m_RayHelper;

    Canvas m_Canvas;
    Camera m_EventCamera;
    UIInputModule m_UIInputModule;

    Camera m_PreviousCanvasCamera;
    Camera m_PreviousModuleCamera;

    void OnEnable()
    {
        // 只在运行期介入：真机 / PC 构建 / Editor Play 模式表现一致
        if (!Application.isPlaying)
            return;

        m_Canvas = GetComponent<Canvas>();
        if (m_Canvas == null)
            return;

        var helper = ResolveRayHelper();
        if (helper == null)
        {
            Debug.LogWarning($"[{nameof(SyUIEventCamera)}] 场景里找不到 {nameof(ScreenPointToRayHelper)}，事件相机未建立。", this);
            return;
        }

        // 只做坐标换算：不渲染任何内容
        var cameraObject = new GameObject("UI Event Camera");
        cameraObject.transform.SetParent(transform, false);
        m_EventCamera = cameraObject.AddComponent<Camera>();
        m_EventCamera.cullingMask = 0;
        m_EventCamera.clearFlags = CameraClearFlags.Nothing;
        m_EventCamera.depth = -100f;
        m_EventCamera.useOcclusionCulling = false;

        SyncMatrices(helper);

        m_PreviousCanvasCamera = m_Canvas.worldCamera;
        m_Canvas.worldCamera = m_EventCamera;

        m_UIInputModule = EventSystem.current != null ? EventSystem.current.currentInputModule as UIInputModule : null;
        if (m_UIInputModule != null)
        {
            m_PreviousModuleCamera = m_UIInputModule.uiCamera;
            m_UIInputModule.uiCamera = m_EventCamera;
        }
        else
        {
            Debug.Log($"[{nameof(SyUIEventCamera)}] EventSystem 尚未就绪，XRUIInputModule 将在后续帧补挂。");
        }

        Debug.Log($"[{nameof(SyUIEventCamera)}] UI 事件相机已切换到 {nameof(ScreenPointToRayHelper)} 的屏幕模型：" +
            $"原画布相机={(m_PreviousCanvasCamera != null ? m_PreviousCanvasCamera.name : "null")}," +
            $" 原 uiCamera={(m_PreviousModuleCamera != null ? m_PreviousModuleCamera.name : "null")}," +
            $" 画布中心在原相机下的像素={(m_PreviousCanvasCamera != null ? m_PreviousCanvasCamera.WorldToScreenPoint(transform.position).ToString() : "-")}," +
            $" 在新相机下={(m_EventCamera != null ? m_EventCamera.WorldToScreenPoint(transform.position).ToString() : "-")}");
    }

    /// <summary>helper 的 V / P 随 rig 与 provider 变化，每帧同步一次。</summary>
    void LateUpdate()
    {
        if (m_EventCamera == null)
            return;

        var helper = ResolveRayHelper();
        if (helper != null)
            SyncMatrices(helper);

        // EventSystem / 输入模块可能晚于本组件就绪，补挂一次
        if (m_UIInputModule == null && EventSystem.current != null && EventSystem.current.currentInputModule is UIInputModule module)
        {
            m_PreviousModuleCamera = module.uiCamera;
            module.uiCamera = m_EventCamera;
            m_UIInputModule = module;
            Debug.Log($"[{nameof(SyUIEventCamera)}] 补挂完成：UIInputModule.uiCamera -> {m_EventCamera.name}");
        }
    }

    void OnDisable()
    {
        if (m_Canvas != null)
            m_Canvas.worldCamera = m_PreviousCanvasCamera;

        if (m_UIInputModule != null)
            m_UIInputModule.uiCamera = m_PreviousModuleCamera;

        if (m_EventCamera != null)
        {
            Destroy(m_EventCamera.gameObject);
            m_EventCamera = null;
        }
    }

    ScreenPointToRayHelper ResolveRayHelper()
    {
        if (m_RayHelper == null)
            m_RayHelper = FindAnyObjectByType<ScreenPointToRayHelper>();

        return m_RayHelper;
    }

    /// <summary>
    /// 直接采用 <see cref="ScreenPointToRayHelper"/> 当帧的 view / projection：不自己重算，
    /// 因为 helper 会按当前 provider（Constant / 左右眼 / 眉心）刷新这对矩阵，
    /// 用它才能保证与控制器发射线用的是同一套屏幕空间。
    /// </summary>
    void SyncMatrices(ScreenPointToRayHelper helper)
    {
        helper.RefreshMatrices();

        m_EventCamera.worldToCameraMatrix = helper.currentViewMatrix;
        m_EventCamera.projectionMatrix = helper.currentProjectionMatrix;
    }
}
