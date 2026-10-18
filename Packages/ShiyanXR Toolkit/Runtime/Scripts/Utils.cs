using UnityEngine;
using UnityEngine.InputSystem;

namespace com.unity.shiyantech.xr
{
    public static class Utils
    {
        public static bool IsDisabledReferenceAction(InputActionProperty property) =>
            property.reference != null && property.reference.action != null && !property.reference.action.enabled;
    }
}
