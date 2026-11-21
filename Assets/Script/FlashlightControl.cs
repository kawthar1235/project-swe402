using UnityEngine;

public class FlashlightControl : MonoBehaviour
{
    public GameObject flashlightLight;
    public GameManager gameManager;

    void Update()
    {
        if (gameManager.hasFlashlight &&
            Input.GetKeyDown(KeyCode.F))
        {
            flashlightLight.SetActive(
                !flashlightLight.activeSelf
            );
        }
    }
}