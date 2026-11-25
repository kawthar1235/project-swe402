
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuManager : MonoBehaviour
{
    [Header("Gameplay Scene")]

#if UNITY_EDITOR
    public SceneAsset gameplayScene;
#endif

    [SerializeField, HideInInspector]
    private string gameplayScenePath;

    [Header("UI Panels")]
    public GameObject mainMenuContent;
    public GameObject settingsPanel;

#if UNITY_EDITOR
    private void OnValidate()
    {
        gameplayScenePath = gameplayScene != null
            ? AssetDatabase.GetAssetPath(gameplayScene)
            : "";
    }
#endif

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        CloseSettings();
    }

    public void PlayGame()
    {
        if (string.IsNullOrEmpty(gameplayScenePath))
        {
            Debug.LogError("Gameplay Scene is not assigned!");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameplayScenePath);
    }

    public void OpenSettings()
    {
        mainMenuContent.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        mainMenuContent.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Game Closed");
        Application.Quit();
    }
}
