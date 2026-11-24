using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MonsterChase : MonoBehaviour
{
    public Transform player;
    public float speed = 2f;
    public float stopDistance = 1.5f;

    private Rigidbody rb;
    private Animator animator;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
    }

    void FixedUpdate()
    {
        if (player == null)
        {
            Debug.LogWarning("Player is not assigned to MonsterChase.");
            return;
        }

        Vector3 direction = player.position - rb.position;
        direction.y = 0f;

        bool chasing = direction.magnitude > stopDistance;

        if (animator != null)
            animator.SetBool("IsRunning", chasing);

        if (!chasing) return;

        direction.Normalize();

        rb.MoveRotation(Quaternion.LookRotation(direction));

        rb.MovePosition(
            rb.position + direction * speed * Time.fixedDeltaTime
        );
    }
}