using UnityEngine;


public class Monster : MonoBehaviour
{
    public GameManager gameManager;
    public Transform corridorRestartPoint;

    /*private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("FlashlightBeam") &&
            gameManager != null &&
            gameManager.hasFlashlight)
        {
            DefeatMonster();
        }
    }*/

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") &&
            corridorRestartPoint != null)
        {
            collision.transform.position = corridorRestartPoint.position;
        }
    }

    public void DefeatMonster()
    {
        // Hide the monster only after the flashlight defeats it.
        gameObject.SetActive(false);
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
}