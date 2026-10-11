// Assets/Scripts/AttachScaleExtension.cs
//
// 给"已经配好推拉/旋转"的 InteractionAttachController 补一个"摇杆缩放"，**不替换**原组件
// （换组件会丢 Manipulation Input 等字段，导致推拉失效）。
// 挂在同一个 GameObject（交互器）上即可。
//
// 背景（为什么不用 XRI 原生的 provider+transformer 路线）：
//   XRGeneralGrabTransformer.OnGrab 里是 `interactor as IXRScaleValueProvider` —— 只从
//   "抓住它的那个交互器"上找 provider；NearFarInteractor 没实现它，所以挂别处也拿不到。
//   这里改为直接写被抓住物体的 localScale（缩放数学照抄 XRI 的 ComputeNewOneHandedScale）。
//
// 用法：
//   1) 保持原来的 InteractionAttachController 不动（推拉/旋转照旧）；
//   2) 同一 GameObject 上加本组件；
//   3) Scale Toggle Input    ← XRI <手> Interaction/Scale Toggle     ({手}/{Primary2DAxisClick})
//      Scale Over Time Input ← XRI <手> Interaction/Scale Over Time  ({手}/{Primary2DAxis})
//      ⚠️ 两个都别指成 Manipulation / Move 之类的轴动作，否则"推摇杆=切换缩放"。
//   4) 行为不对时把 Log Debug 勾上，按一下摇杆/推一下摇杆，把 Console 里的
//      [AttachScale] 行发我。

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;   // IXRSelectInteractable
using UnityEngine.XR.Interaction.Toolkit.Interactors;      // IXRSelectInteractor

[RequireComponent(typeof(InteractionAttachController))]
public class AttachScaleExtension : MonoBehaviour
{
    [Header("输入（来自 XRI Default Input Actions 的 XRI <手> Interaction 组）")]
    [SerializeField]
    XRInputButtonReader m_ScaleToggleInput = new XRInputButtonReader("Scale Toggle");

    [SerializeField]
    XRInputValueReader<Vector2> m_ScaleOverTimeInput = new XRInputValueReader<Vector2>("Scale Over Time");

    [Header("手感")]
    [SerializeField]
    float m_ScaleSpeed = 0.5f;

    [SerializeField]
    float m_MinScaleRatio = 0.2f;

    [SerializeField]
    float m_MaxScaleRatio = 5f;

    [Header("行为")]
    [Tooltip("进缩放时把 InteractionAttachController 的推拉/旋转让出去（做成“推拉旋转 ⇄ 缩放”切换）")]
    [SerializeField]
    bool m_DisableManipulationWhileScaling = true;

    [Tooltip("在 Console 打诊断日志")]
    [SerializeField]
    bool m_LogDebug = false;

    InteractionAttachController m_AttachController;
    IXRSelectInteractor m_Interactor;
    IXRSelectInteractable m_GrabbedInteractable;
    Vector3 m_BaseScale = Vector3.one;
    Vector3 m_ScaleProportions = Vector3.one;
    bool m_ScaleInputActive;

    // 缩放的"当前值/写入口"：
    // XRGrabInteractable 在 m_TrackScale 打开时每帧都会 ApplyTargetScale() →
    // m_Transform.localScale = m_TargetLocalScale；而且如果物体有 visuals 子物体
    // （m_PredictedVisualsTransform），它还会用比例把视觉抵消。所以直接写
    // transform.localScale 是白写，必须走它自己的 targetLocalScale。
    static Vector3 CurrentScale(IXRSelectInteractable interactable)
    {
        return interactable is XRGrabInteractable grab
            ? grab.GetTargetLocalScale()
            : interactable.transform.localScale;
    }

    static void ApplyScale(IXRSelectInteractable interactable, Vector3 scale)
    {
        if (interactable is XRGrabInteractable grab)
            grab.SetTargetLocalScale(scale);
        else
            interactable.transform.localScale = scale;
    }

    protected virtual void Awake()
    {
        // 用 InParent：组件放在交互器本身上、或放在它下面的子物体上都能找到
        m_AttachController = GetComponentInParent<InteractionAttachController>();
        m_Interactor = GetComponentInParent<IXRSelectInteractor>();
        if (m_Interactor == null)
            Debug.LogWarning("[AttachScale] 找不到 IXRSelectInteractor（交互器），缩放拿不到被抓住的物体。", this);
        else if (m_LogDebug)
            Debug.Log($"[AttachScale] interactor = {m_Interactor}", this);
    }

    protected virtual void OnEnable()
    {
        m_ScaleToggleInput.EnableDirectActionIfModeUsed();
        m_ScaleOverTimeInput.EnableDirectActionIfModeUsed();
    }

    protected virtual void OnDisable()
    {
        m_ScaleToggleInput.DisableDirectActionIfModeUsed();
        m_ScaleOverTimeInput.DisableDirectActionIfModeUsed();
    }

    void Update()
    {
        if (m_Interactor == null)
            return;

        // 当前被抓住的物体（取第一个）；换物体时重新采样基准大小
        IXRSelectInteractable grabbed = null;
        List<IXRSelectInteractable> selected = m_Interactor.interactablesSelected;
        if (selected != null && selected.Count > 0)
            grabbed = selected[0];

        if (!ReferenceEquals(grabbed, m_GrabbedInteractable))
        {
            m_GrabbedInteractable = grabbed;
            if (grabbed != null)
            {
                m_BaseScale = CurrentScale(grabbed);
                var maxAbs = Mathf.Max(Mathf.Abs(m_BaseScale.x), Mathf.Abs(m_BaseScale.y),
                                       Mathf.Abs(m_BaseScale.z));
                m_ScaleProportions = maxAbs > 1e-6f ? m_BaseScale / maxAbs : Vector3.one;
                if (m_LogDebug)
                    Debug.Log($"[AttachScale] grab {grabbed.transform.name} baseScale={m_BaseScale}", this);
            }
        }

        // 摇杆按下：切换 推拉旋转 ⇄ 缩放
        if (m_ScaleToggleInput.ReadWasPerformedThisFrame())
        {
            m_ScaleInputActive = !m_ScaleInputActive;
            if (m_DisableManipulationWhileScaling && m_AttachController != null)
                m_AttachController.useManipulationInput = !m_ScaleInputActive;
            //Debug.Log($"[AttachScale] mode = {(m_ScaleInputActive ? "SCALE" : "translate/rotate")}", this);
        }

        if (m_LogDebug && Time.frameCount % 60 == 0)
        {
            var read = m_ScaleOverTimeInput.TryReadValue(out var dbg);
            //Debug.Log($"[AttachScale] active={m_ScaleInputActive} selected={selected?.Count ?? 0} " +
            //          $"manipEnabled={(m_AttachController != null ? m_AttachController.useManipulationInput : false)} " +
            //          $"scaleInputRead={(read ? "ok" : "FAIL(没接到值)")} value={dbg}", this);
        }

        if (!m_ScaleInputActive || m_GrabbedInteractable == null)
            return;
        if (!m_ScaleOverTimeInput.TryReadValue(out var v) || Mathf.Approximately(v.y, 0f))
            return;

        // 与 XRI 的 ComputeNewOneHandedScale 同款：scale += input * dt * speed * 抓取时比例
        var amount = v.y * Time.deltaTime * m_ScaleSpeed;
        var target = CurrentScale(m_GrabbedInteractable) + m_ScaleProportions * amount;

        var min = m_BaseScale * m_MinScaleRatio;
        var max = m_BaseScale * m_MaxScaleRatio;
        var clamped = new Vector3(
            amount > 0f ? Mathf.Min(target.x, max.x) : Mathf.Max(target.x, min.x),
            amount > 0f ? Mathf.Min(target.y, max.y) : Mathf.Max(target.y, min.y),
            amount > 0f ? Mathf.Min(target.z, max.z) : Mathf.Max(target.z, min.z));

        // 走 XRGrabInteractable 的通道（它每帧 ApplyTargetScale()，直接写 localScale 会被覆盖）
        ApplyScale(m_GrabbedInteractable, clamped);
    }
}
