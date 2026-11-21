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
}