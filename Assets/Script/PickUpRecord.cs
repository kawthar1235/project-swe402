using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class PickUpRecord : MonoBehaviour, IInteractable
{
    public GameManager gameManager;
    public Camera playerCamera;
    public Transform holdPoint;

    public float pickupDistance = 3f;
    public float heldWidth = 0.24f;
    public Vector3 heldRotation = new Vector3(10f, -20f, 10f);

    public bool IsHeld => held;

    private bool canPickUp;
    private bool held;
    private bool installed;

    private Rigidbody rb;
    private Collider[] recordColliders;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        recordColliders = GetComponentsInChildren<Collider>();

        rb.isKinematic = true;
        rb.useGravity = false;

        canPickUp = false;
    }

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    void Update()
    {
        if (!canPickUp || installed || playerCamera == null)
            return;

        bool clicked;
        bool dropPressed;
        Vector2 mousePosition;

#if ENABLE_INPUT_SYSTEM
        clicked = Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        dropPressed = Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame;

        mousePosition = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Vector2.zero;
#else
        clicked = Input.GetMouseButtonDown(0);
        dropPressed = Input.GetKeyDown(KeyCode.Space);
        mousePosition = Input.mousePosition;
#endif

        if (held)
        {
            if (dropPressed)
                Drop();

            return;
        }

        if (!clicked) return;

        Ray ray = Cursor.lockState == CursorLockMode.Locked
            ? playerCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f))
            : playerCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(
            ray, out RaycastHit hit, pickupDistance,
            ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<PickUpRecord>() == this)
                PickUp();
        }
    }

    public string GetMessage()
    {
        return !canPickUp || held || installed
            ? ""
            : "Click to pick up record";
    }

    public void Interact()
    {
        PickUp();
    }

    private void PickUp()
    {
        if (!canPickUp || held || installed) return;

        if (holdPoint == null || playerCamera == null)
        {
            Debug.LogWarning("Assign Hold Point and Player Camera.");
            return;
        }

        rb.isKinematic = true;
        rb.useGravity = false;

        foreach (Collider col in recordColliders)
            col.enabled = false;

        transform.SetParent(holdPoint, true);

        MeshFilter filter = GetComponent<MeshFilter>();

        if (filter != null && filter.sharedMesh != null)
        {
            Bounds bounds = filter.sharedMesh.bounds;
            Vector3 size = bounds.size;

            int thinAxis = 0;
            if (size.y < size[thinAxis]) thinAxis = 1;
            if (size.z < size[thinAxis]) thinAxis = 2;

            Vector3 normal = Vector3.zero;
            normal[thinAxis] = 1f;

            Quaternion faceCamera =
                Quaternion.FromToRotation(normal, Vector3.forward);

            transform.localRotation =
                Quaternion.Euler(heldRotation) * faceCamera;

            float diameter =
                Mathf.Max(size.x, Mathf.Max(size.y, size.z));

            float scale =
                heldWidth / Mathf.Max(diameter, 0.001f);

            transform.localScale = Vector3.one * scale;

            transform.localPosition =
                -(transform.localRotation *
                Vector3.Scale(bounds.center, transform.localScale));
        }
        else
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(heldRotation);
        }

        held = true;

        if (gameManager != null)
            gameManager.hasRecord = true;
    }

    private void Drop()
    {
        transform.SetParent(null, true);

        foreach (Collider col in recordColliders)
            col.enabled = true;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        held = false;

        if (gameManager != null)
            gameManager.hasRecord = false;
    }

    public void ReleaseFromPainting()
    {
        if (held || installed || canPickUp) return;

        canPickUp = true;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void PlaceInGramophone(Transform point)
    {
        if (!held || installed || point == null) return;

        held = false;
        installed = true;
        canPickUp = false;

        rb.isKinematic = true;
        rb.useGravity = false;

        foreach (Collider col in recordColliders)
            col.enabled = false;

        // ääÞá äÝÓ ÇáÞÑÕ Åáì ÇáÌåÇÒ ãÚ ÇáÍÝÇÙ Úáì ÍÌãå.
        transform.SetParent(point, true);

        MeshFilter filter = GetComponent<MeshFilter>();

        if (filter != null && filter.sharedMesh != null)
        {
            Bounds bounds = filter.sharedMesh.bounds;
            Vector3 size = bounds.size;

            int thinAxis = 0;
            if (size.y < size[thinAxis]) thinAxis = 1;
            if (size.z < size[thinAxis]) thinAxis = 2;

            Vector3 normal = Vector3.zero;
            normal[thinAxis] = 1f;

            // äÎáí ÓØÍ ÇáÃÓØæÇäÉ ãÓØÍðÇ ÝæÞ ÇáÌåÇÒ.
            transform.rotation = point.rotation *
                Quaternion.FromToRotation(normal, Vector3.up);

            // äæÓøØ Ôßá ÇáÞÑÕ Úáì ÇáäÞØÉ.
            transform.position =
                point.position - transform.TransformVector(bounds.center);
        }
        else
        {
            transform.position = point.position;
            transform.rotation = point.rotation;
        }

        if (gameManager != null)
            gameManager.hasRecord = false;
    }
}