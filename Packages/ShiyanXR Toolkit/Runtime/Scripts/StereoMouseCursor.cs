using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class StereoMouseCursor : MonoBehaviour
{
    public Camera useCamera;
    public Transform cursorPrefab;
    public InputActionProperty mouseMoveAction;

    private Transform cursor;
    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
        //Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        if(cursorPrefab)
        {
            cursor = Instantiate(cursorPrefab);
        }
    }

    void LateUpdate()
    {
        if(!cursor || !useCamera)
        {
            return;
        }
        if (IsDisabledReferenceAction(mouseMoveAction)) return;

        if (mouseMoveAction.action.WasPerformedThisFrame())
        {
            Vector2 mousePos = mouseMoveAction.action.ReadValue<Vector2>();
            cursor.position = useCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 1f));
        }
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus) {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;
            //Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    bool IsDisabledReferenceAction(InputActionProperty property) =>
            property.reference != null && property.reference.action != null && !property.reference.action.enabled;
}
