using UnityEngine;

public class FlashlightPickup : MonoBehaviour, IInteractable
{
    public GameManager gameManager;

    public string GetMessage()
    {
        return "Press E to take the flashlight";
    }

    public void Interact()
    {
        gameManager.hasFlashlight = true;
        gameObject.SetActive(false);
    }
}