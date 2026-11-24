
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public GameObject menuCanvas;
    public MonoBehaviour[] playerScripts;

    void Start()
    {
        menuCanvas.SetActive(true);

        Time.timeScale = 0f;

        foreach (MonoBehaviour script in playerScripts)
        {
            if (script != null)
                script.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StartGame()
    {
        menuCanvas.SetActive(false);

        Time.timeScale = 1f;

        foreach (MonoBehaviour script in playerScripts)
        {
            if (script != null)
                script.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Closed");
    }
}

