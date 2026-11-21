using UnityEngine;

public class PaintingPuzzle : MonoBehaviour, IInteractable
{
    public GameObject vinylRecord;
    public bool paintingAdjusted = false;

    public string GetMessage()
    {
        return paintingAdjusted ? "" : "Press E to straighten the painting";
    }

    public void Interact()
    {
        if (paintingAdjusted) return;

        paintingAdjusted = true;

        transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        vinylRecord.SetActive(true);
    }
}