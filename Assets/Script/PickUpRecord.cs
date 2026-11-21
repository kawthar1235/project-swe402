using UnityEngine;

public class PickUpRecord : MonoBehaviour, IInteractable
{
    public GameManager gameManager;

    public string GetMessage()
    {
        return "Press E to take the vinyl record";
    }

    public void Interact()
    {
        gameManager.hasRecord = true;
        gameObject.SetActive(false);
    }
}