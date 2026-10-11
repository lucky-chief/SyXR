// Assets/NearFarInteractorWithScale.cs
// 给 XRI 3.4 的 NearFarInteractor 补"摇杆缩放"：它本身没有 IXRScaleValueProvider，
// 导致 XRGrabInteractable 上默认的 XRGeneralGrabTransformer 拿不到 scaleValue。
// 用法：
//   1) 把右手交互器上的 NearFarInteractor 换成这个组件；
//   2) Scale Toggle Input   ← XRI Right Interaction/Scale Toggle     ({RightHand}/{Primary2DAxisClick})
//      Scale Over Time Input ← XRI Right Interaction/Scale Over Time  ({RightHand}/{Primary2DAxis})
//   3) 缩放快慢由物体上 XRGeneralGrabTransformer 的 oneHandedScaleSpeed 控制（默认 0.5）。
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
// UnityEngine 里也有一个 ScaleMode（Sprite/Image 用的），会和 XRI 的同名冲突 → 起个别名
using ScaleMode = UnityEngine.XR.Interaction.Toolkit.Interactors.ScaleMode;

public class NearFarInteractorWithScale : NearFarInteractor, IXRScaleValueProvider
{
	[SerializeField] ScaleMode m_ScaleMode = ScaleMode.ScaleOverTime;
	[SerializeField] XRInputButtonReader m_ScaleToggleInput = new XRInputButtonReader("Scale Toggle");
	[SerializeField] XRInputValueReader<Vector2> m_ScaleOverTimeInput = new XRInputValueReader<Vector2>("Scale Over Time");

	bool m_ScaleInputActive;
	float m_ScaleValue;
	InteractionAttachController m_AttachController;

	public ScaleMode scaleMode { get => m_ScaleMode; set => m_ScaleMode = value; }
	public float scaleValue => m_ScaleValue;

	protected override void Awake()
	{
		base.Awake();
		// 放进基类列表：Direct/Action 两种模式的 Enable/Disable 由 XRI 统一处理
		buttonReaders.Add(m_ScaleToggleInput);
		valueReaders.Add(m_ScaleOverTimeInput);
		m_AttachController = GetComponent<InteractionAttachController>();
	}

	public override void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
	{
		base.ProcessInteractor(updatePhase);
		if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic)
			return;

		if (m_ScaleToggleInput.ReadWasPerformedThisFrame())
		{
			m_ScaleInputActive = !m_ScaleInputActive;
			// 真正做成"推拉旋转 ⇄ 缩放"：缩放期间关掉 attach 那套位移/旋转，避免同时生效
			if (m_AttachController != null)
				m_AttachController.useManipulationInput = !m_ScaleInputActive;
		}

		m_ScaleValue = 0f;
		if (m_ScaleInputActive && m_ScaleOverTimeInput.TryReadValue(out var v))
			m_ScaleValue = v.y;   // 摇杆 Y：上放大 / 下缩小
	}
}