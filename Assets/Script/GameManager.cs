/*using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public bool hasRecord;
    public bool recordPlaying;
    public bool recordFixed;
    public bool hasKey;
    public bool firstDoorOpened;
    public bool hasFlashlight;

    public SafeRoomDoor safeRoomDoor;
    public GameObject keypadUI;

    public void OpenKeypad(WoodenBox box)
    {
        keypadUI.SetActive(true);
    }

    public void StartSafeRoomTimer()
    {
        StartCoroutine(SafeRoomCountdown());
    }

    IEnumerator SafeRoomCountdown()
    {
        safeRoomDoor.CloseDoor();

        yield return new WaitForSeconds(30f);

        safeRoomDoor.OpenDoor();
    }

    public void TakeFlashlight()
    {
        hasFlashlight = true;
    }
}*/
using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public bool hasRecord;
    public bool recordPlaying;
    public bool recordFixed;
    public bool hasKey;
    public bool firstDoorOpened;
    public bool hasFlashlight;

    public SafeRoomDoor safeRoomDoor;
    public GameObject keypadUI;
    public GameObject winPanel;

    public void OpenKeypad(WoodenBox box)
    {
        keypadUI.SetActive(true);
    }

    public void StartSafeRoomTimer()
    {
        StartCoroutine(SafeRoomCountdown());
    }

    private IEnumerator SafeRoomCountdown()
    {
        safeRoomDoor.CloseDoor();

        yield return new WaitForSeconds(30f);

        safeRoomDoor.OpenDoor();
    }

    public void TakeFlashlight()
    {
        hasFlashlight = true;
    }

    public void WinGame()
    {
        Debug.Log("Player wins!");

        if (winPanel != null)
            winPanel.SetActive(true);

        Time.timeScale = 0f;
    }
}