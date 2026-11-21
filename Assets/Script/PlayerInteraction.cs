using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public float interactionDistance = 3f;
    public TMP_Text messageText;

    void Update()
    {
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            IInteractable interactable =
                hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                messageText.text = interactable.GetMessage();

                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactable.Interact();
                }

                return;
            }
        }

        messageText.text = "";
    }
}