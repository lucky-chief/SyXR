using System.Linq;
using Shiyan.OpenXR.RawInput;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShiyanMouseTest : MonoBehaviour
{
    [SerializeField] InputActionReference mouseMoveAction;
    [SerializeField] Text text;

    void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    static void OnDeviceChange(UnityEngine.InputSystem.InputDevice device,
                               InputDeviceChange change)
    {
        Debug.Log($"DEVICE {change} name='{device.name}' layout='{device.layout}' " +
                  $"product='{device.description.product}' " +
                  $"interface='{device.description.interfaceName}'");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (var device in InputSystem.devices)
        {
            Debug.Log($"INPUTDEV name='{device.name}' layout='{device.layout}' " +
                      $"product='{device.description.product}' " +
                      $"interface='{device.description.interfaceName}'");
        }

        foreach (var layout in InputSystem.ListLayouts())
        {
            if (layout.Contains("Raw") || layout.Contains("Mouse"))
                Debug.Log($"LAYOUT {layout}");
        }

        var rawMouse = InputSystem.devices.FirstOrDefault(
            d => d.name == "ShiyanRawMouseOpenXR");
        if (rawMouse != null)
        {
            Debug.Log($"RAWDEV type='{rawMouse.GetType().FullName}' " +
                      $"layout='{rawMouse.layout}'");
            foreach (var control in rawMouse.allControls)
                Debug.Log($"  RAWCTRL {control.path} ({control.GetType().Name})");
        }
        else
        {
            Debug.Log("RAWDEV not found by name ShiyanRawMouseOpenXR");
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 v = mouseMoveAction.action.ReadValue<Vector2>();

        var rawMouse = InputSystem.GetDevice<RawMouseProfile.RawMouse>();
        var rawMouseDevice = InputSystem.devices.FirstOrDefault(
            d => d.name == "ShiyanRawMouseOpenXR");
        var rawPosition = rawMouseDevice?["position"] as UnityEngine.InputSystem.Controls.Vector2Control;
        var rawPositionPx = rawMouseDevice?["positionpx"] as UnityEngine.InputSystem.Controls.Vector2Control;
        var nativeMouse = Mouse.current;

        string rawText = rawMouse != null
            ? $"raw pos={rawMouse.position.ReadValue()} px={rawMouse.positionPx.ReadValue()}"
            : "raw device: null";
        string generatedText = rawMouseDevice != null
            ? $"gen pos={rawPosition?.ReadValue()} px={rawPositionPx?.ReadValue()} layout={rawMouseDevice.layout}"
            : "generated device: null";
        string nativeText = nativeMouse != null
            ? $"native={nativeMouse.position.ReadValue()}"
            : "native: null";

        text.text = $"{v}\n{rawText}\n{generatedText}\n{nativeText}";
    }
}
