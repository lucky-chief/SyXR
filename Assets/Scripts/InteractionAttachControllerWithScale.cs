// Assets/Scripts/InteractionAttachControllerWithScale.cs
//
// 在 InteractionAttachController（负责"物体贴到 attach 点"的推拉/旋转）上补"摇杆缩放"。
//
// 为什么放在这里、又为什么要绕一下：
//   * XRI 原生的缩放路线是：交互器实现 IXRScaleValueProvider → 物体上的
//     XRGeneralGrabTransformer 取 scaleValue 并写 localScale。
//   * 但 NearFarInteractor 没实现 IXRScaleValueProvider，而且
//     XRGeneralGrabTransformer 只在"抓住它的那个交互器"上找 provider
//     （XRGeneralGrabTransformer.OnGrab: `interactor as IXRScaleValueProvider`），
//     所以挂在别处的 provider 拿不到。
//   * 这里换个办法：attach controller 和交互器本来就在同一个 GameObject 上，
//     于是直接 GetComponent<IXRSelectInteractor>() → interactablesSelected[0] 拿到
//     当前被抓住的物体，自己套 localScale。缩放数学与 XRI 的
//     ComputeNewOneHandedScale 一致：scale += input * dt * speed * 抓取时的比例，再夹比例。
//     （这套里 No provider → XRGeneralGrabTransformer 不会碰 scale，所以不会打架。）
//
// 用法：
//   1) 把交互器上原来的 InteractionAttachController 换成这个组件；
//      基类字段照原样再指一遍：Use Manipulation Input 勾上 + Manipulation Input
//      ← XRI <手> Interaction/Manipulation（Transform To Follow 默认是自己，不用指）。
//   2) 新的两个：
//      Scale Toggle Input    ← XRI <手> Interaction/Scale Toggle     ({手}/{Primary2DAxisClick})
//      Scale Over Time Input ← XRI <手> Interaction/Scale Over Time  ({手}/{Primary2DAxis})
//   3) 手感：Scale Speed / Min Scale Ratio / Max Scale Ratio。
//
// 行为：抓住物体 → 按摇杆（= Scale Toggle）切到缩放（此时基类的推拉/旋转自动让位，
//       即"推拉旋转 ⇄ 缩放"）→ 摇杆上下缩放；再按一次切回。

using System.Collections.Generic;
using UnityEngine;

using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class InteractionAttachControllerWithScale : InteractionAttachController
{
    [SerializeField]
    XRInputButtonReader m_ScaleToggleInput = new XRInputButtonReader("Scale Toggle");

    [SerializeField]
    XRInputValueReader<Vector2> m_ScaleOverTimeInput = new XRInputValueReader<Vector2>("Scale Over Time");

    [SerializeField]
    float m_ScaleSpeed = 0.5f;

    [SerializeField]
    float m_MinScaleRatio = 0.2f;

    [SerializeField]
    float m_MaxScaleRatio = 5f;

    IXRSelectInteractor m_Interactor;
    UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable m_GrabbedInteractable;
    Vector3 m_BaseScale = Vector3.one;
    Vector3 m_ScaleProportions = Vector3.one;
    bool m_ScaleInputActive;

    protected override void Awake()
    {
        base.Awake();
        m_Interactor = GetComponent<IXRSelectInteractor>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        m_ScaleToggleInput.EnableDirectActionIfModeUsed();
        m_ScaleOverTimeInput.EnableDirectActionIfModeUsed();
    }

    protected override void OnDisable()
    {
        m_ScaleToggleInput.DisableDirectActionIfModeUsed();
        m_ScaleOverTimeInput.DisableDirectActionIfModeUsed();
        base.OnDisable();
    }

    void Update()
    {
        if (m_Interactor == null)
            return;

        // 当前被抓住的物体（取第一个；抓住新物体时重新采样基准比例）
        UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable grabbed = null;
        List<UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable> selected = m_Interactor.interactablesSelected;
        if (selected != null && selected.Count > 0)
            grabbed = selected[0];

        if (!ReferenceEquals(grabbed, m_GrabbedInteractable))
        {
            m_GrabbedInteractable = grabbed;
            if (grabbed != null)
            {
                m_BaseScale = grabbed.transform.localScale;
                var maxAbs = Mathf.Max(Mathf.Abs(m_BaseScale.x), Mathf.Abs(m_BaseScale.y),
                                       Mathf.Abs(m_BaseScale.z));
                m_ScaleProportions = maxAbs > 1e-6f ? m_BaseScale / maxAbs : Vector3.one;
            }
        }

        // 摇杆按下：切换 推拉旋转 ⇄ 缩放（缩放开时把基类的位移/旋转输入让出去，避免同时生效）
        if (m_ScaleToggleInput.ReadWasPerformedThisFrame())
        {
            Debug.Log($"[InteractionAttachControllerWithScale] Scale Toggle: {m_ScaleInputActive} → {!m_ScaleInputActive}");
            m_ScaleInputActive = !m_ScaleInputActive;
            useManipulationInput = !m_ScaleInputActive;
        }

        if (!m_ScaleInputActive || m_GrabbedInteractable == null)
            return;
        if (!m_ScaleOverTimeInput.TryReadValue(out var v) || Mathf.Approximately(v.y, 0f))
            return;

        // 与 XRI 的 ComputeNewOneHandedScale 同款：scale += input * dt * speed * 初始比例
        var amount = v.y * Time.deltaTime * m_ScaleSpeed;
        var target = m_GrabbedInteractable.transform.localScale + m_ScaleProportions * amount;

        // 夹比例：放大时封顶、缩小时封底（都以"抓取时的大小"为基准）
        var min = m_BaseScale * m_MinScaleRatio;
        var max = m_BaseScale * m_MaxScaleRatio;
        var clamped = new Vector3(
            amount > 0f ? Mathf.Min(target.x, max.x) : Mathf.Max(target.x, min.x),
            amount > 0f ? Mathf.Min(target.y, max.y) : Mathf.Max(target.y, min.y),
            amount > 0f ? Mathf.Min(target.z, max.z) : Mathf.Max(target.z, min.z));

        m_GrabbedInteractable.transform.localScale = clamped;
    }
}
