using UnityEngine;

public class KeyPickup : MonoBehaviour, IInteractable
{
    public GameManager gameManager;

    public string GetMessage()
    {
        return "Press E to take the key";
    }

    public void Interact()
    {
        gameManager.hasKey = true;
        gameObject.SetActive(false);
    }
}