using UnityEngine;

public class PaintingPuzzle : MonoBehaviour, IInteractable
{
    public GameObject vinylRecord;
    public bool paintingAdjusted = false;

    public float straightenedLocalZ = 0f;
    public Vector3 openLocalOffset = Vector3.zero;

    void Awake()
    {
        paintingAdjusted = false;

        if (vinylRecord != null)
            vinylRecord.SetActive(false);
    }

    public string GetMessage()
    {
        return paintingAdjusted
            ? ""
            : "Press E to straighten the painting";
    }

    public void Interact()
    {
        if (paintingAdjusted) return;

        if (vinylRecord == null)
        {
            Debug.LogWarning("Assign Vinyl Record on the painting.");
            return;
        }

        PickUpRecord record = vinylRecord.GetComponent<PickUpRecord>();

        if (record == null)
        {
            Debug.LogWarning("Vinyl Record must have PickUpRecord.");
            return;
        }

        paintingAdjusted = true;

        Vector3 angles = transform.localEulerAngles;
        angles.z = straightenedLocalZ;

        transform.localRotation = Quaternion.Euler(angles);
        transform.localPosition += openLocalOffset;

        vinylRecord.SetActive(true);
        record.ReleaseFromPainting();
    }
}