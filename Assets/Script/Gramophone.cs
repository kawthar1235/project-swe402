using UnityEngine;

public class Gramophone : MonoBehaviour, IInteractable
{
    public GameManager gameManager;
    public AudioSource reversedAudio;
    public AudioSource normalAudio;

    private bool playingNormally = false;

    public string GetMessage()
    {
        if (!gameManager.hasRecord)
            return "You need a vinyl record";

        return "Press E to play the record";
    }

    public void Interact()
    {
        if (!gameManager.hasRecord) return;

        reversedAudio.Play();
        gameManager.recordPlaying = true;
    }

    void Update()
    {
        if (gameManager.recordPlaying &&
            Input.GetKeyDown(KeyCode.R))
        {
            reversedAudio.Stop();
            normalAudio.Play();
            playingNormally = true;
            gameManager.recordFixed = true;
        }
    }
}