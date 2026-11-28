
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PauseManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject pausePanel;
    public GameObject settingsPanel;
    public GameObject pauseButton;

    [Header("Player")]
    public MonoBehaviour[] playerScripts;

    [Header("Main Menu Scene")]
#if UNITY_EDITOR
    public SceneAsset mainMenuScene;
#endif

    [SerializeField, HideInInspector]
    private string mainMenuScenePath;

    private bool isPaused;
    private bool[] previousStates;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;

#if UNITY_EDITOR
    void OnValidate()
    {
        mainMenuScenePath = mainMenuScene != null
            ? AssetDatabase.GetAssetPath(mainMenuScene)
            : "";
    }
#endif

    void Start()
    {
        Time.timeScale = 1f;
        isPaused = false;

        pausePanel.SetActive(false);
        pauseButton.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isPaused || Time.timeScale == 0f)
            return;

        isPaused = true;

        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        previousStates = new bool[playerScripts.Length];

        for (int i = 0; i < playerScripts.Length; i++)
        {
            if (playerScripts[i] != null)
            {
                previousStates[i] = playerScripts[i].enabled;
                playerScripts[i].enabled = false;
            }
        }

        pausePanel.SetActive(true);
        pauseButton.SetActive(false);

        Time.timeScale = 0f;

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

        for (int i = 0; i < playerScripts.Length; i++)
        {
            if (playerScripts[i] != null)
                playerScripts[i].enabled = previousStates[i];
        }

        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }

    public void OpenSettings()
    {
        if (!isPaused || settingsPanel == null)
            return;

        pausePanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (!isPaused || settingsPanel == null)
            return;

        settingsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    public void ReturnToMainMenu()
    {
        if (string.IsNullOrEmpty(mainMenuScenePath))
        {
            Debug.LogError("Main Menu Scene is not assigned!");
            return;
        }

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(mainMenuScenePath);
    }
}
