using UnityEngine;
using UnityEngine.Rendering;

public class ChestKeypadButton : MonoBehaviour
{
    public WoodenBox Owner { get; private set; }

    private string value;
    private bool enter;

    private Bounds buttonBounds;
    private LineRenderer border;
    private Material borderMaterial;

    private float flashUntil;
    private int lastPressedFrame = -1;

    // Share one pointer raycast across all buttons each frame.
    private static int rayFrame = -1;
    private static Camera rayCamera;
    private static bool rayFound;
    private static RaycastHit rayHit;

    public void Configure(
        WoodenBox owner, string digit, bool isEnter)
    {
        Owner = owner;
        value = digit;
        enter = isEnter;

        ChestPuzzleTools.EnsureCollider(gameObject);

        if (border == null)
            CreateBorder();
    }

    private void CreateBorder()
    {
        buttonBounds = ChestPuzzleTools.LocalBounds(transform);

        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader == null)
            return;

        borderMaterial = new Material(shader);

        GameObject outlineObject =
            new GameObject("Button Hover Border");

        outlineObject.transform.SetParent(transform, false);

        border = outlineObject.AddComponent<LineRenderer>();
        border.sharedMaterial = borderMaterial;
        border.useWorldSpace = true;
        border.positionCount = 4;
        border.loop = true;
        border.numCornerVertices = 2;
        border.shadowCastingMode = ShadowCastingMode.Off;
        border.receiveShadows = false;
        border.enabled = false;
    }

    private void Update()
    {
        if (Owner == null ||
            Owner.playerCamera == null ||
            ChestPuzzleTools.InspectingBox != Owner)
        {
            if (border != null)
                border.enabled = false;

            return;
        }

        Camera camera = Owner.playerCamera;

        if (rayFrame != Time.frameCount || rayCamera != camera)
        {
            rayFrame = Time.frameCount;
            rayCamera = camera;

            rayFound = ChestPuzzleTools.Raycast(
                camera, 100f, out rayHit);
        }

        bool hovered = rayFound &&
            rayHit.collider.GetComponentInParent<ChestKeypadButton>()
                == this;

        bool flashing = Time.unscaledTime < flashUntil;

        UpdateBorder(hovered || flashing, flashing);
    }

    private void UpdateBorder(bool visible, bool flashing)
    {
        if (border == null)
            return;

        border.enabled = visible;

        if (!visible)
            return;

        Color color = flashing ? Color.white : Color.red;

        if (borderMaterial.HasProperty("_BaseColor"))
            borderMaterial.SetColor("_BaseColor", color);

        if (borderMaterial.HasProperty("_Color"))
            borderMaterial.SetColor("_Color", color);

        Vector3 size = buttonBounds.size;

        // Find the thin axis of the button.
        int normalAxis = 0;

        if (size.y < size[normalAxis])
            normalAxis = 1;

        if (size.z < size[normalAxis])
            normalAxis = 2;

        int axisU = (normalAxis + 1) % 3;
        int axisV = (normalAxis + 2) % 3;

        Vector3 cameraLocal = transform.InverseTransformPoint(
            Owner.playerCamera.transform.position);

        float direction =
            cameraLocal[normalAxis] >= buttonBounds.center[normalAxis]
            ? 1f : -1f;

        float worldWidth = Mathf.Max(
            transform.TransformVector(
                Vector3.right * size.x).magnitude,
            transform.TransformVector(
                Vector3.up * size.y).magnitude);

        border.widthMultiplier =
            Mathf.Clamp(worldWidth * 0.035f, 0.0005f, 0.002f);

        for (int i = 0; i < 4; i++)
        {
            Vector3 point = buttonBounds.center;

            bool positiveU = i == 1 || i == 2;
            bool positiveV = i >= 2;

            point[axisU] += buttonBounds.extents[axisU] *
                (positiveU ? 1.07f : -1.07f);

            point[axisV] += buttonBounds.extents[axisV] *
                (positiveV ? 1.07f : -1.07f);

            point[normalAxis] +=
                buttonBounds.extents[normalAxis] * direction;

            Vector3 worldPoint = transform.TransformPoint(point);

            Vector3 outward = transform.TransformDirection(
                Axis(normalAxis) * direction).normalized;

            border.SetPosition(
                i, worldPoint + outward * 0.001f);
        }
    }

    private static Vector3 Axis(int index)
    {
        Vector3 axis = Vector3.zero;
        axis[index] = 1f;
        return axis;
    }

    public void Press()
    {
        if (Owner == null ||
            ChestPuzzleTools.InspectingBox != Owner ||
            lastPressedFrame == Time.frameCount)
            return;

        lastPressedFrame = Time.frameCount;
        flashUntil = Time.unscaledTime + 0.16f;

        if (enter)
            Owner.SubmitCode();
        else
            Owner.EnterNumber(value);
    }

    private void OnDisable()
    {
        if (border != null)
            border.enabled = false;
    }

    private void OnDestroy()
    {
        if (borderMaterial != null)
            Destroy(borderMaterial);
    }
}