using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public float height = 1.4f;
    public float forwardOffset = 0f;

    public float sensitivity = 0.1f;

    public float horizontalLimit = 10f;
    public float lookUpLimit = 50f;
    public float lookDownLimit = 150f;

    private float yawOffset;
    private float pitch;
    private bool looking;

    void Start()
    {
        SetLooking(false);
    }

    void Update()
    {
        Vector2 mouseDelta = Vector2.zero;
        bool rightClicked = false;
        bool escapePressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            mouseDelta = Mouse.current.delta.ReadValue();
            rightClicked = Mouse.current.rightButton.wasPressedThisFrame;
        }

        if (Keyboard.current != null)
            escapePressed = Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        mouseDelta = new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")
        ) * 10f;

        rightClicked = Input.GetMouseButtonDown(1);
        escapePressed = Input.GetKeyDown(KeyCode.Escape);
#endif

        if (escapePressed)
        {
            SetLooking(false);
            return;
        }

        if (rightClicked)
        {
            SetLooking(!looking);
            return;
        }

        if (!looking) return;

        yawOffset += mouseDelta.x * sensitivity;
        pitch -= mouseDelta.y * sensitivity;

        yawOffset = Mathf.Clamp(
            yawOffset, -horizontalLimit, horizontalLimit
        );

        pitch = Mathf.Clamp(
            pitch, -lookUpLimit, lookDownLimit
        );
    }

    void LateUpdate()
    {
        if (player == null) return;

        transform.position = player.position
            + Vector3.up * height
            + player.forward * forwardOffset;

        float yaw = player.eulerAngles.y + yawOffset;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void SetLooking(bool enabled)
    {
        looking = enabled;

        Cursor.lockState = enabled
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !enabled;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}