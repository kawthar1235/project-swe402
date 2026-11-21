using UnityEngine;

public class TVInteraction : MonoBehaviour, IInteractable
{
    public GameObject videoScreen;
    public GameManager gameManager;

    public string GetMessage()
    {
        return "Press E to turn on the TV";
    }

    public void Interact()
    {
        videoScreen.SetActive(true);
    }
}