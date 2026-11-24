using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public static class ChestPuzzleTools
{
    public static WoodenBox InspectingBox;
    public static bool Inspecting => InspectingBox != null;

    public static bool Clicked()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }
    public static bool SpacePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }
    public static bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
    public static Vector2 MousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }
    public static Camera ResolveCamera(Camera assigned)
    {
        if (assigned != null) return assigned;
        if (Camera.main != null) return Camera.main;
        CameraFollow follow = Object.FindFirstObjectByType<CameraFollow>();
        return follow != null ? follow.GetComponent<Camera>() : null;
    }
    public static Transform Player(Camera camera)
    {
        CameraFollow follow = camera != null ? camera.GetComponent<CameraFollow>() : null;
        if (follow != null && follow.player != null) return follow.player;
        PlayerMovement movement = Object.FindFirstObjectByType<PlayerMovement>();
        return movement != null ? movement.transform : null;
    }
    public static Ray PointerRay(Camera camera)
    {
        return Cursor.lockState == CursorLockMode.Locked
            ? camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : camera.ScreenPointToRay(MousePosition());
    }
    public static bool Raycast(Camera camera, float distance, out RaycastHit nearest)
    {
        nearest = default;
        if (camera == null) return false;
        Transform player = Player(camera);
        RaycastHit[] hits = Physics.RaycastAll(PointerRay(camera), distance,
            ~0, QueryTriggerInteraction.Ignore);
        float closest = float.PositiveInfinity;
        bool found = false;
        foreach (RaycastHit hit in hits)
        {
            if (player != null && hit.transform.IsChildOf(player)) continue;
            if (hit.distance >= closest) continue;
            closest = hit.distance;
            nearest = hit;
            found = true;
        }
        return found;
    }
    // Includes child meshes, so this also works with the imported key prefab.
    public static Bounds LocalBounds(Transform root)
    {
        Bounds result = default;
        bool found = false;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Bounds bounds = renderer.localBounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1,
                                (i & 2) == 0 ? -1 : 1,
                                (i & 4) == 0 ? -1 : 1));
                Vector3 point = root.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                if (!found) { result = new Bounds(point, Vector3.zero); found = true; }
                else result.Encapsulate(point);
            }
        }
        return found ? result : new Bounds(Vector3.zero, Vector3.one * 0.1f);
    }
    public static void EnsureCollider(GameObject target)
    {
        if (target.GetComponentInChildren<Collider>(true) != null) return;
        Bounds bounds = LocalBounds(target.transform);
        BoxCollider box = target.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = Vector3.Max(bounds.size, Vector3.one * 0.002f);
    }
    public static AudioClip Tone(string name, float frequency, float duration)
    {
        const int rate = 22050;
        float[] data = new float[Mathf.CeilToInt(rate * duration)];
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)rate;
            float envelope = Mathf.Min(1f, t * 100f) * (1f - i / (float)data.Length);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.25f;
        }
        AudioClip clip = AudioClip.Create(name, data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
