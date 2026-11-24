using UnityEngine;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public float interactionDistance = 3f;
    public TMP_Text messageText;

    void Update()
    {
        if (playerCamera == null)
        {
            ClearMessage();
            return;
        }

        Vector2 mousePosition;
        bool clicked;
        bool pressedE;

#if ENABLE_INPUT_SYSTEM
        mousePosition = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Vector2.zero;

        clicked = Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        pressedE = Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame;
#else
        mousePosition = Input.mousePosition;
        clicked = Input.GetMouseButtonDown(0);
        pressedE = Input.GetKeyDown(KeyCode.E);
#endif

        Ray ray = Cursor.lockState == CursorLockMode.Locked
            ? playerCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f))
            : playerCamera.ScreenPointToRay(mousePosition);

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            interactionDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        System.Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit hit in hits)
        {
            // åĞÇ ÇáÓßÑÈÊ ãæÌæÏ Úáì Player:
            // äÊÌÇåá ßæáíÏÑÇÊ ÇááÇÚÈÉ æÃÈäÇÆåÇ.
            if (hit.collider.transform.IsChildOf(transform))
                continue;

            IInteractable target =
                hit.collider.GetComponentInParent<IInteractable>();

            if (target != null)
            {
                string message = target.GetMessage();

                if (messageText != null)
                    messageText.text = message;

                if (!string.IsNullOrEmpty(message) &&
                    (clicked || pressedE))
                {
                    target.Interact();
                }

                return;
            }

            // Ãæá ÌÓã ÂÎÑ íÍÌÈ ÇáÊİÇÚá ãÚ ãÇ æÑÇÁå.
            break;
        }

        ClearMessage();
    }

    private void ClearMessage()
    {
        if (messageText != null)
            messageText.text = "";
    }

    void OnDisable()
    {
        ClearMessage();
    }
}