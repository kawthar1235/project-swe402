using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float turnSpeed = 120f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float forward = Input.GetAxis("Vertical");
        float turn = Input.GetAxis("Horizontal");

        Vector3 movement = transform.forward * forward * moveSpeed;

        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);

        Quaternion rotation = Quaternion.Euler(
            0f,
            turn * turnSpeed * Time.fixedDeltaTime,
            0f
        );

        rb.MoveRotation(rb.rotation * rotation);
    }
}