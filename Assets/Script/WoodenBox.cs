using UnityEngine;

public class WoodenBox : MonoBehaviour, IInteractable
{
    public GameManager gameManager;
    public GameObject key;
    public Animator boxAnimator;

    private string enteredCode = "";

    public string GetMessage()
    {
        return "Enter the four-digit code";
    }

    public void Interact()
    {
        if (!gameManager.recordFixed) return;

        // Open your keypad UI here
        gameManager.OpenKeypad(this);
    }

    public void EnterNumber(string number)
    {
        enteredCode += number;

        if (enteredCode.Length == 4)
        {
            if (enteredCode == "7854")
            {
                boxAnimator.SetTrigger("Open");
                key.SetActive(true);
            }
            else
            {
                enteredCode = "";
                Debug.Log("Wrong code");
            }
        }
    }
}