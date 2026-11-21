using UnityEngine;

public class Monster : MonoBehaviour
{
    public GameManager gameManager;
    public Transform corridorRestartPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("FlashlightBeam"))
        {
            if (gameManager.hasFlashlight)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.position =
                corridorRestartPoint.position;
        }
    }

    public void DefeatMonster()
{
    gameObject.SetActive(false);
}
}