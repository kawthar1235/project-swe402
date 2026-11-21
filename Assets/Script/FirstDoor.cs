using UnityEngine;

public class FirstDoor : MonoBehaviour, IInteractable
{
    public GameManager gameManager;
    public Animator animator;

    public string GetMessage()
    {
        return gameManager.hasKey
            ? "Press E to open the door"
            : "The door is locked";
    }

    public void Interact()
    {
        if (gameManager.hasKey)
        {
            animator.SetTrigger("Open");
            gameManager.firstDoorOpened = true;
        }
    }
}