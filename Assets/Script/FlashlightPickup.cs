using UnityEngine;

public class FlashlightPickup : MonoBehaviour, IInteractable
{
    public GameManager gameManager;
    public Camera playerCamera;
    public Transform holdPoint;

    public float pickupDistance = 3f;
    public float heldLength = 0.22f;
    public Vector3 heldRotation = Vector3.zero;

    public bool IsHeld => held;

    private bool held;
    private Rigidbody body;
    private Collider[] colliders;
    private bool[] colliderStates;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();

        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();

        ChestPuzzleTools.EnsureCollider(gameObject);

        colliders = GetComponentsInChildren<Collider>(true);
        colliderStates = new bool[colliders.Length];

        for (int i = 0; i < colliders.Length; i++)
            colliderStates[i] = colliders[i].enabled;

        // Keep the flashlight in its starting position until picked up.
        body.isKinematic = true;
        body.useGravity = false;

        Animator oldAnimator = GetComponent<Animator>();

        if (oldAnimator != null)
            oldAnimator.enabled = false;
    }

    private void Start()
    {
        playerCamera =
            ChestPuzzleTools.ResolveCamera(playerCamera);

        if (holdPoint == null && playerCamera != null)
            holdPoint = playerCamera.transform.Find("HoldPoint");
    }

    private void Update()
    {
        if (ChestPuzzleTools.Inspecting)
            return;

        if (held)
        {
            if (ChestPuzzleTools.SpacePressed())
                Drop();

            return;
        }

        if (!ChestPuzzleTools.Clicked())
            return;

        if (ChestPuzzleTools.Raycast(
            playerCamera,
            pickupDistance,
            out RaycastHit hit))
        {
            if (hit.collider
                .GetComponentInParent<FlashlightPickup>() == this)
            {
                Interact();
            }
        }
    }

    public string GetMessage()
    {
        return held ? "" : "Click to take flashlight";
    }

    public void Interact()
    {
        if (held || ChestPuzzleTools.Inspecting)
            return;

        if (holdPoint == null || playerCamera == null)
        {
            Debug.LogWarning(
                "Assign Player Camera and Hold Point.", this);
            return;
        }

        // Prevent picking up the flashlight over an already held item.
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

        Bounds bounds = ChestPuzzleTools.LocalBounds(transform);
        Vector3 size = bounds.size;

        // Align the longest model axis with the camera's forward axis.
        int longAxis = 0;

        if (size.y > size[longAxis])
            longAxis = 1;

        if (size.z > size[longAxis])
            longAxis = 2;

        Vector3 modelForward = Vector3.zero;
        modelForward[longAxis] = 1f;

        Quaternion alignment = Quaternion.FromToRotation(
            modelForward,
            Vector3.forward
        );

        body.isKinematic = true;
        body.useGravity = false;

        foreach (Collider col in colliders)
            col.enabled = false;

        transform.SetParent(holdPoint, false);

        transform.localRotation =
            Quaternion.Euler(heldRotation) * alignment;

        float length = Mathf.Max(
            size.x,
            Mathf.Max(size.y, size.z)
        );

        float scale =
            heldLength / Mathf.Max(length, 0.001f);

        transform.localScale = Vector3.one * scale;

        // Center the mesh on the existing hand position.
        transform.localPosition = -(
            transform.localRotation *
            Vector3.Scale(bounds.center, transform.localScale)
        );

        held = true;

        if (gameManager != null)
            gameManager.hasFlashlight = true;
    }

    private void Drop()
    {
        transform.SetParent(null, true);

        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = colliderStates[i];

        body.isKinematic = false;
        body.useGravity = true;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        held = false;

        if (gameManager != null)
            gameManager.hasFlashlight = false;
    }

    private void OnDisable()
    {
        if (held && gameManager != null)
            gameManager.hasFlashlight = false;

        held = false;
    }
}