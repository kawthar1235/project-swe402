using UnityEngine;

public class FirstDoor : MonoBehaviour, IInteractable
{
    public GameManager gameManager;
    public Animator animator;
    public Collider doorCollider;

    public string GetMessage()
    {
        return gameManager.hasKey
            ? "Press E to open the door"
            : "The door is locked";
    }

    public void Interact()
    {
        if (!gameManager.hasKey) return;

        animator.SetTrigger("Open");

        if (doorCollider != null)
        {
            doorCollider.enabled = false;
        }

        gameManager.firstDoorOpened = true;
    }
}