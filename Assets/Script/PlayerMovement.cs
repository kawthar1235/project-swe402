
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float turnSpeed = 120f;

    private Rigidbody rb;
    private float forward;
    private float turn;

    private float speedMultiplier = 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Load saved movement speed setting
        speedMultiplier = Mathf.Lerp(
            0.75f,
            1.25f,
            Mathf.Clamp01(
                PlayerPrefs.GetFloat("MovementSpeed", 0.5f)
            )
        );
    }

    void Update()
    {
        forward = Input.GetAxis("Vertical");
        turn = Input.GetAxis("Horizontal");
    }

    void FixedUpdate()
    {
        rb.angularVelocity = Vector3.zero;
        Vector3 movement = rb.rotation * Vector3.forward;

        movement *= forward * moveSpeed * speedMultiplier;

        rb.linearVelocity = new Vector3(
            movement.x,
            rb.linearVelocity.y,
            movement.z
        );

        Quaternion rotation = Quaternion.Euler(
            0f,
            turn * turnSpeed * Time.fixedDeltaTime,
            0f
        );

        rb.MoveRotation(rb.rotation * rotation);
    }
}
