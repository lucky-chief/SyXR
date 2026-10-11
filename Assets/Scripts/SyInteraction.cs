using UnityEngine;
using UnityEngine.InputSystem;

public class SyInteraction : MonoBehaviour
{
	[SerializeField]
	GameObject[] gameObjectsHideOnNaturalMouse;

	InputAction inputModeAction = new InputAction(type: InputActionType.Value, binding: "<ShiyanInteractionMode>/interactionmode");

	float m_lastMode = -1f;

	private void OnEnable()
	{
		inputModeAction.Enable();
	}

	private void OnDisable()
	{
		inputModeAction.Disable();
	}

	void Start()
	{
		m_lastMode = inputModeAction.ReadValue<float>();
		Debug.Log("Start Interaction mode changed to: " + m_lastMode);
	}

	void Update()
	{
		float mode = inputModeAction.ReadValue<float>();
		if (mode != m_lastMode)
		{
			m_lastMode = mode;
			ChangeMode();  
		}
	}

	void ChangeMode()
	{
		Debug.Log("Interaction mode changed to: " + m_lastMode);
		foreach (var obj in gameObjectsHideOnNaturalMouse)
		{
			obj.SetActive(m_lastMode < 0.5f); 
		}
	}
}
