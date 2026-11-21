using UnityEngine;

public class SafeRoomDoor : MonoBehaviour
{
    public Animator animator;
    public GameManager gameManager;

    public void CloseDoor()
    {
        animator.SetTrigger("Close");
    }

    public void OpenDoor()
    {
        animator.SetTrigger("Open");
    }
}