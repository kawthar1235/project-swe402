using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MonsterChase : MonoBehaviour
{
    public Transform player;
    public Transform corridorStart;
    public Transform corridorCorner;
    public Transform corridorEnd;

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
        if (player == null ||
            corridorStart == null ||
            corridorCorner == null ||
            corridorEnd == null)
            return;

        Vector3 playerTarget;
        float distanceToStartSegment =
            DistanceToSegment(player.position,
                              corridorStart.position,
                              corridorCorner.position);

        float distanceToEndSegment =
            DistanceToSegment(player.position,
                              corridorCorner.position,
                              corridorEnd.position);

        if (distanceToStartSegment < distanceToEndSegment)
        {
            playerTarget = ClosestPointOnSegment(
                player.position,
                corridorStart.position,
                corridorCorner.position
            );
        }
        else
        {
            playerTarget = ClosestPointOnSegment(
                player.position,
                corridorCorner.position,
                corridorEnd.position
            );
        }

        Vector3 direction = playerTarget - rb.position;
        direction.y = 0f;

        bool chasing = direction.magnitude > stopDistance;

        if (animator != null)
            animator.SetBool("IsRunning", chasing);

        if (!chasing)
            return;

        direction.Normalize();

        rb.MoveRotation(Quaternion.LookRotation(direction));
        rb.MovePosition(
            rb.position + direction * speed * Time.fixedDeltaTime
        );
    }

    Vector3 ClosestPointOnSegment(
        Vector3 point,
        Vector3 start,
        Vector3 end)
    {
        Vector3 line = end - start;
        float length = line.sqrMagnitude;

        if (length < 0.001f)
            return start;

        float t = Vector3.Dot(point - start, line) / length;
        t = Mathf.Clamp01(t);

        return start + line * t;
    }

    float DistanceToSegment(
        Vector3 point,
        Vector3 start,
        Vector3 end)
    {
        return Vector3.Distance(
            point,
            ClosestPointOnSegment(point, start, end)
        );
    }
}

/*using UnityEngine;

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
}*/