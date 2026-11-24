using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KeyPickup : MonoBehaviour, IInteractable
{
    public GameManager gameManager;
    public Camera playerCamera;
    public Transform holdPoint;
    public float pickupDistance = 3f;
    public float heldWidth = 0.18f;
    public Vector3 heldRotation = new Vector3(0f, 0f, 90f);

    public bool IsHeld => held;
    public static KeyPickup HeldKey { get; private set; }
    private bool available;
    private bool held;
    private bool consumed;
    private Rigidbody body;
    private Collider[] colliders;
    private bool[] colliderStates;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (body == null) body = gameObject.AddComponent<Rigidbody>();
        ChestPuzzleTools.EnsureCollider(gameObject);
        colliders = GetComponentsInChildren<Collider>(true);
        colliderStates = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
            colliderStates[i] = colliders[i].enabled;
        body.isKinematic = true;
        body.useGravity = false;
        playerCamera = ChestPuzzleTools.ResolveCamera(playerCamera);
        if (holdPoint == null && playerCamera != null)
            holdPoint = playerCamera.transform.Find("HoldPoint");
    }
    void Update()
    {
        if (ChestPuzzleTools.Inspecting || consumed) return;
        if (held)
        {
            if (ChestPuzzleTools.SpacePressed()) Drop();
            return;
        }
        if (!available || !ChestPuzzleTools.Clicked()) return;
        if (ChestPuzzleTools.Raycast(playerCamera, pickupDistance, out RaycastHit hit) &&
            hit.collider.GetComponentInParent<KeyPickup>() == this)
            Interact();
    }
    public string GetMessage()
    {
        return available && !held && !consumed ? "Click to take key" : "";
    }
    public void Reveal()
    {
        available = true;
    }
    public void Interact()
    {
        if (!available || held || consumed || ChestPuzzleTools.Inspecting) return;
        if (HeldKey != null && HeldKey != this) return;
        if (holdPoint == null)
        {
            Debug.LogWarning("Key: assign the existing Main Camera / HoldPoint.", this);
            return;
        }
        PickUpRecord record = holdPoint.GetComponentInChildren<PickUpRecord>();
        if (record != null && record.IsHeld)
        {
            Debug.Log("Put down the record before taking the key.", this);
            return;
        }
        Bounds bounds = ChestPuzzleTools.LocalBounds(transform);
        body.isKinematic = true;
        body.useGravity = false;
        foreach (Collider col in colliders) col.enabled = false;
        transform.SetParent(holdPoint, false);
        transform.localRotation = Quaternion.Euler(heldRotation);
        float diameter = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        transform.localScale = Vector3.one * (heldWidth / Mathf.Max(diameter, 0.001f));
        transform.localPosition = -(transform.localRotation *
            Vector3.Scale(bounds.center, transform.localScale));
        held = true;
        HeldKey = this;
        if (gameManager != null) gameManager.hasKey = true;
    }
    public void Drop()
    {
        if (!held) return;
        transform.SetParent(null, true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = colliderStates[i];
        body.isKinematic = false;
        body.useGravity = true;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        ClearHeld();
    }
    public bool Consume()
    {
        if (!held || consumed) return false;
        consumed = true;
        ClearHeld();
        gameObject.SetActive(false);
        return true;
    }
    private void ClearHeld()
    {
        held = false;
        if (HeldKey == this) HeldKey = null;
        if (gameManager != null) gameManager.hasKey = false;
    }
    void OnDisable()
    {
        if (held) ClearHeld();
    }
}
