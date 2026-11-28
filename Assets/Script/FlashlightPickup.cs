using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class FlashlightPickup : MonoBehaviour, IInteractable
{
    public GameManager gameManager;
    public Camera playerCamera;
    public Transform holdPoint;

    public float pickupDistance = 3f;
    public float heldLength = 0.22f;
    public Vector3 heldRotation = Vector3.zero;

    [Range(0f, 1f)]
    public float pickupVolume = 0.35f;

    public bool IsHeld => held;

    private bool canPickUp = true;
    private bool held;

    private Rigidbody body;
    private Collider[] colliders;
    private bool[] colliderStates;

    private Bounds modelBounds;
    private KeyFlashPickupFeedback feedback;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();

        colliders = GetComponentsInChildren<Collider>(true);
        colliderStates = new bool[colliders.Length];

        for (int i = 0; i < colliders.Length; i++)
            colliderStates[i] = colliders[i].enabled;

        modelBounds = ChestPuzzleTools.LocalBounds(transform);

        body.isKinematic = true;
        body.useGravity = false;

        Animator oldAnimator = GetComponent<Animator>();

        if (oldAnimator != null)
            oldAnimator.enabled = false;
    }

    private void Start()
    {
        ResolveReferences();
        feedback = new KeyFlashPickupFeedback(transform, false);
    }

    private void ResolveReferences()
    {
        playerCamera =
            ChestPuzzleTools.ResolveCamera(playerCamera);

        if (holdPoint == null && playerCamera != null)
            holdPoint = playerCamera.transform.Find("HoldPoint");
    }

    private void Update()
    {
        if (ChestPuzzleTools.Inspecting)
        {
            feedback?.Highlight(false, playerCamera);
            return;
        }

        // Check Space before returning from the held state.
        if (held)
        {
            feedback?.Highlight(false, playerCamera);

            if (ChestPuzzleTools.SpacePressed())
                Drop();

            return;
        }

        if (!canPickUp)
        {
            feedback?.Highlight(false, playerCamera);
            return;
        }

        if (playerCamera == null)
            ResolveReferences();

        bool hovered = false;

        if (ChestPuzzleTools.Raycast(
            playerCamera,
            pickupDistance,
            out RaycastHit hit))
        {
            hovered =
                hit.collider.GetComponentInParent<FlashlightPickup>()
                == this;
        }

        feedback?.Highlight(hovered, playerCamera);

        if (hovered && ChestPuzzleTools.Clicked())
            Interact();
    }

    public string GetMessage()
    {
        return "";
    }

    public void Interact()
    {
        if (!canPickUp ||
            held ||
            ChestPuzzleTools.Inspecting)
            return;

        ResolveReferences();

        if (holdPoint == null || playerCamera == null)
        {
            Debug.LogWarning(
                "Flashlight: assign Player Camera and Hold Point.",
                this);
            return;
        }

        foreach (PickUpRecord record in
            holdPoint.GetComponentsInChildren<PickUpRecord>())
        {
            if (record.IsHeld)
                return;
        }

        foreach (KeyPickup key in
            holdPoint.GetComponentsInChildren<KeyPickup>())
        {
            if (key.IsHeld)
                return;
        }

        feedback?.Highlight(false, playerCamera);

        Vector3 size = modelBounds.size;
        int longAxis = 0;

        if (size.y > size[longAxis])
            longAxis = 1;

        if (size.z > size[longAxis])
            longAxis = 2;

        Vector3 modelForward = Vector3.zero;
        modelForward[longAxis] = 1f;

        Quaternion alignment =
            Quaternion.FromToRotation(
                modelForward, Vector3.forward);

        body.isKinematic = true;
        body.useGravity = false;

        foreach (Collider col in colliders)
        {
            if (col != null)
                col.enabled = false;
        }

        transform.SetParent(holdPoint, false);

        transform.localRotation =
            Quaternion.Euler(heldRotation) * alignment;

        float length = Mathf.Max(
            size.x, Mathf.Max(size.y, size.z));

        float scale =
            heldLength / Mathf.Max(length, 0.001f);

        transform.localScale = Vector3.one * scale;

        transform.localPosition =
            -(transform.localRotation *
              Vector3.Scale(
                  modelBounds.center, transform.localScale));

        held = true;

        if (gameManager != null)
            gameManager.hasFlashlight = true;

        feedback?.PlaySound(pickupVolume);
    }

    public void Drop()
    {
        if (!held)
            return;

        feedback?.Highlight(false, playerCamera);

        transform.SetParent(null, true);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = colliderStates[i];
        }

        body.isKinematic = false;
        body.useGravity = true;

        body.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.WakeUp();

        held = false;

        if (gameManager != null)
            gameManager.hasFlashlight = false;
    }

    private void OnDisable()
    {
        feedback?.Highlight(false, playerCamera);

        if (held && gameManager != null)
            gameManager.hasFlashlight = false;

        held = false;
    }

    private void OnDestroy()
    {
        feedback?.Dispose();
    }
}