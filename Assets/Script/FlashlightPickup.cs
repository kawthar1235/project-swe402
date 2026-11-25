
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

    public bool IsHeld => held;

    private bool canPickUp = true;
    private bool held;

    private Rigidbody body;
    private Collider[] colliders;
    private bool[] colliderStates;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();

        colliders = GetComponentsInChildren<Collider>(true);
        colliderStates = new bool[colliders.Length];

        for (int i = 0; i < colliders.Length; i++)
            colliderStates[i] = colliders[i].enabled;

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
        if (!canPickUp ||
            held ||
            ChestPuzzleTools.Inspecting)
            return;

        if (!ChestPuzzleTools.Clicked())
            return;

        if (ChestPuzzleTools.Raycast(
            playerCamera,
            pickupDistance,
            out RaycastHit hit))
        {
            if (hit.collider.GetComponentInParent<FlashlightPickup>()
                == this)
            {
                Interact();
            }
        }
    }

    public string GetMessage()
    {
        return !canPickUp || held
            ? ""
            : "Click to take flashlight";
    }

    public void Interact()
    {
        if (!canPickUp ||
            held ||
            ChestPuzzleTools.Inspecting)
            return;

        if (holdPoint == null || playerCamera == null)
        {
            Debug.LogWarning(
                "Assign Player Camera and Hold Point.",
                this
            );
            return;
        }

        foreach (PickUpRecord record in
            holdPoint.GetComponentsInChildren<PickUpRecord>())
        {
            if (record.IsHeld)
            {
                Debug.Log(
                    "Put down the record before taking the flashlight.",
                    this
                );
                return;
            }
        }

        foreach (KeyPickup key in
            holdPoint.GetComponentsInChildren<KeyPickup>())
        {
            if (key.IsHeld)
            {
                Debug.Log(
                    "Put down the key before taking the flashlight.",
                    this
                );
                return;
            }
        }

        Bounds bounds =
            ChestPuzzleTools.LocalBounds(transform);

        Vector3 size = bounds.size;

        int longAxis = 0;

        if (size.y > size[longAxis])
            longAxis = 1;

        if (size.z > size[longAxis])
            longAxis = 2;

        Vector3 modelForward = Vector3.zero;
        modelForward[longAxis] = 1f;

        Quaternion alignment =
            Quaternion.FromToRotation(
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

        transform.localScale =
            Vector3.one * scale;

        transform.localPosition =
            -(
                transform.localRotation *
                Vector3.Scale(
                    bounds.center,
                    transform.localScale
                )
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
        body.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

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