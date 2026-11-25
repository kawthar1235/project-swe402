
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject pausePanel;
    public GameObject settingsPanel;
    public GameObject pauseButton;

    [Header("Player")]
    public MonoBehaviour[] playerScripts;

    [Header("Scenes")]
    public string mainMenuScene = "MainMenu";

    private bool isPaused = false;

    void Start()
    {
        pausePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else if (Time.timeScale > 0f)
                PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isPaused || Time.timeScale == 0f)
            return;

        isPaused = true;

        pausePanel.SetActive(true);
        pauseButton.SetActive(false);

        Time.timeScale = 0f;

        foreach (MonoBehaviour script in playerScripts)
        {
            if (script != null)
                script.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;

        pausePanel.SetActive(false);
        pauseButton.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        Time.timeScale = 1f;

        foreach (MonoBehaviour script in playerScripts)
        {
            if (script != null)
                script.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenSettings()
    {
        if (settingsPanel == null) return;

        pausePanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel == null) return;

        settingsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(mainMenuScene);
    }
}

