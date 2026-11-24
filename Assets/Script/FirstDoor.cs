using System.Collections;
using UnityEngine;

public class FirstDoor : MonoBehaviour, IInteractable
{
    public GameManager gameManager;
    public Camera playerCamera;

    public Transform doorLeaf;
    public Transform doorHinge;

    public float interactionDistance = 3f;
    public float openAngle = 90f;
    public float rotationSpeed = 120f;

    private bool unlocked;
    private bool isOpen;
    private bool moving;

    private float currentAngle;
    private int lastClickFrame = -1;

    private Vector3 closedPosition;
    private Quaternion closedRotation;

    void Start()
    {
        playerCamera =
            ChestPuzzleTools.ResolveCamera(playerCamera);

        // ‰Êﬁ› «·√‰Ì„Ì‘‰ «·ﬁœÌ„ Õ Ï „« Ì⁄«ﬂ” Õ—ﬂ… «·ﬂÊœ.
        Animator parentAnimator = GetComponent<Animator>();

        if (parentAnimator != null)
            parentAnimator.enabled = false;

        if (doorLeaf != null)
        {
            foreach (Animator oldAnimator in
                doorLeaf.GetComponentsInChildren<Animator>(true))
            {
                oldAnimator.enabled = false;
            }

            closedPosition = doorLeaf.position;
            closedRotation = doorLeaf.rotation;
        }
    }

    void Update()
    {
        if (ChestPuzzleTools.Inspecting ||
            !ChestPuzzleTools.Clicked())
            return;

        if (ChestPuzzleTools.Raycast(
            playerCamera,
            interactionDistance,
            out RaycastHit hit))
        {
            if (hit.collider.GetComponentInParent<FirstDoor>() == this)
                Interact();
        }
    }

    public string GetMessage()
    {
        if (moving)
            return "";

        if (unlocked)
            return isOpen
                ? "Click to close door"
                : "Click to open door";

        return KeyPickup.HeldKey != null
            ? "Click to unlock door"
            : "The door is locked";
    }

    public void Interact()
    {
        if (moving ||
            ChestPuzzleTools.Inspecting ||
            lastClickFrame == Time.frameCount)
            return;

        lastClickFrame = Time.frameCount;

        if (doorLeaf == null || doorHinge == null)
        {
            Debug.LogWarning(
                "Assign Door Leaf and Door Hinge.", this);
            return;
        }

        if (!unlocked)
        {
            KeyPickup key = KeyPickup.HeldKey;

            if (key == null || !key.IsHeld)
                return;

            if (!key.Consume())
                return;

            unlocked = true;

            if (gameManager != null)
            {
                gameManager.hasKey = false;
                gameManager.firstDoorOpened = true;
            }
        }

        isOpen = !isOpen;
        StartCoroutine(MoveDoor());
    }

    private IEnumerator MoveDoor()
    {
        moving = true;

        float targetAngle = isOpen ? openAngle : 0f;

        while (Mathf.Abs(currentAngle - targetAngle) > 0.01f)
        {
            float nextAngle = Mathf.MoveTowards(
                currentAngle,
                targetAngle,
                Mathf.Max(rotationSpeed, 1f) * Time.deltaTime
            );

            // «·Õ—ﬂ… ÕÊ· „›’·… ÿ—› «·»«»° Ê·Ì” „—ﬂ“ «·„Ã”„.
            doorLeaf.RotateAround(
                doorHinge.position,
                doorHinge.up,
                nextAngle - currentAngle
            );

            currentAngle = nextAngle;

            yield return null;
        }

        if (!isOpen)
        {
            doorLeaf.SetPositionAndRotation(
                closedPosition,
                closedRotation
            );

            currentAngle = 0f;
        }

        moving = false;
    }
}