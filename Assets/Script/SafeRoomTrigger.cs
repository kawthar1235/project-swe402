using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class SafeRoomTrigger : MonoBehaviour
{
    public SafeRoomDoor safeRoomDoor;

    private bool activated;

    void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activated || safeRoomDoor == null)
            return;

        bool isPlayer =
            other.GetComponentInParent<PlayerMovement>() != null ||
            other.CompareTag("Player");

        if (!isPlayer)
            return;

        activated = true;
        safeRoomDoor.StartLockdown();
    }
}