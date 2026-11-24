using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(FlashlightPickup))]
public class FlashlightControl : MonoBehaviour
{
    public Light flashlightLight;

    private FlashlightPickup pickup;

    private void Awake()
    {
        pickup = GetComponent<FlashlightPickup>();

        if (flashlightLight == null)
            flashlightLight = GetComponentInChildren<Light>(true);

        if (flashlightLight != null)
            flashlightLight.enabled = false;
    }

    private void Update()
    {
        if (flashlightLight == null ||
            pickup == null ||
            !pickup.IsHeld ||
            ChestPuzzleTools.Inspecting)
            return;

        if (TogglePressed())
            flashlightLight.enabled = !flashlightLight.enabled;
    }

    private void LateUpdate()
    {
        if (flashlightLight == null ||
            pickup == null ||
            !pickup.IsHeld ||
            pickup.playerCamera == null)
            return;

        // Aim the beam toward the center of the camera view.
        Transform cameraTransform = pickup.playerCamera.transform;

        Vector3 target =
            cameraTransform.position +
            cameraTransform.forward * 30f;

        Vector3 direction =
            target - flashlightLight.transform.position;

        if (direction.sqrMagnitude > 0.001f)
        {
            flashlightLight.transform.rotation =
                Quaternion.LookRotation(
                    direction,
                    cameraTransform.up
                );
        }
    }

    private static bool TogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
    return Keyboard.current != null &&
        Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }
}