using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public float height = 1.7f;
    public float forwardOffset = 0f;

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 offset = player.up * height +
                         player.forward * forwardOffset;

        transform.position = player.position + offset;
        transform.rotation = player.rotation;
    }
}