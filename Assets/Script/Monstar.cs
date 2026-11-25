
/*
using UnityEngine;

public class Monster : MonoBehaviour
{
    public GameManager gameManager;
    public Transform corridorRestartPoint;

    private void OnCollisionEnter(Collision collision)
    {
        Transform playerTransform =
            collision.transform.CompareTag("Player")
            ? collision.transform
            : collision.transform.GetComponentInParent<Transform>();

        if (playerTransform == null ||
            !playerTransform.CompareTag("Player"))
            return;

        if (corridorRestartPoint == null)
        {
            Debug.LogWarning("Corridor Restart Point is not assigned.", this);
            return;
        }

        Rigidbody playerRb =
            playerTransform.GetComponent<Rigidbody>();

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;

            playerRb.position = corridorRestartPoint.position;
            playerRb.rotation = corridorRestartPoint.rotation;
        }
        else
        {
            playerTransform.SetPositionAndRotation(
                corridorRestartPoint.position,
                corridorRestartPoint.rotation
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Monster touched by: " + other.name);

        if (other.CompareTag("FlashlightBeam") &&
            gameManager != null &&
            gameManager.hasFlashlight)
        {
            Debug.Log("Monster defeated");
            DefeatMonster();
        }
    }

    public void DefeatMonster()
    {
        gameObject.SetActive(false);
    }
}*/
using UnityEngine;

public class Monster : MonoBehaviour
{
    public GameManager gameManager;
    public Transform corridorRestartPoint;

    private bool defeated;

    private void OnCollisionEnter(Collision collision)
    {
        if (defeated)
            return;

        PlayerMovement player =
            collision.collider.GetComponentInParent<PlayerMovement>();

        if (player == null)
            return;

        if (corridorRestartPoint == null)
        {
            Debug.LogWarning(
                "Corridor Restart Point is not assigned.",
                this
            );
            return;
        }

        Rigidbody playerRb =
            player.GetComponent<Rigidbody>();

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.position = corridorRestartPoint.position;
            playerRb.rotation = corridorRestartPoint.rotation;
        }
        else
        {
            player.transform.SetPositionAndRotation(
                corridorRestartPoint.position,
                corridorRestartPoint.rotation
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (defeated)
            return;

        if (!other.CompareTag("FlashlightBeam"))
            return;

        if (gameManager == null ||
            !gameManager.hasFlashlight)
            return;

        DefeatMonster();
    }

    public void DefeatMonster()
    {
        defeated = true;

        gameObject.SetActive(false);

        if (gameManager != null)
            gameManager.WinGame();
    }
}