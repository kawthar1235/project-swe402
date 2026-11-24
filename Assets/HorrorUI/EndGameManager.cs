
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGameManager : MonoBehaviour
{
    public GameObject winPanel;
    public GameObject failPanel;
    public MonoBehaviour[] playerScripts;

    private bool gameEnded = false;

    void Awake()
    {
        winPanel.SetActive(false);
        failPanel.SetActive(false);
    }

    public void ShowWin()
    {
        if (gameEnded) return;

        winPanel.SetActive(true);
        EndGame();
    }

    public void ShowFail()
    {
        if (gameEnded) return;

        failPanel.SetActive(true);
        EndGame();
    }

    private void EndGame()
    {
        gameEnded = true;

        foreach (MonoBehaviour script in playerScripts)
        {
            if (script != null)
                script.enabled = false;
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
