using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float turnSpeed = 100f;

    [Header("Movement smoothing")]
    public float acceleration = 8f;
    public float deceleration = 12f;
    public float turnAcceleration = 400f;

    [Header("Animation")]
    public Animator animator;

    private Rigidbody rb;

    private float forwardInput;
    private float turnInput;

    private float currentSpeed;
    private float currentTurnSpeed;
    private float speedMultiplier = 1f;

    private Vector3 previousPosition;

    private RuntimeAnimatorController cachedController;
    private bool hasSpeedParameter;

    private static readonly int SpeedParameter =
        Animator.StringToHash("Speed");

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.constraints |=
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;

        previousPosition = transform.position;
    }

    private void OnEnable()
    {
        speedMultiplier = Mathf.Lerp(
            0.75f,
            1.25f,
            Mathf.Clamp01(
                PlayerPrefs.GetFloat("MovementSpeed", 0.5f)));

        forwardInput = 0f;
        turnInput = 0f;
        currentSpeed = 0f;
        currentTurnSpeed = 0f;

        previousPosition = transform.position;
    }

    private void Update()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        forwardInput = Input.GetAxisRaw("Vertical");
        turnInput = Input.GetAxisRaw("Horizontal");
#elif ENABLE_INPUT_SYSTEM
        forwardInput = 0f;
        turnInput = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed ||
                Keyboard.current.upArrowKey.isPressed)
                forwardInput += 1f;

            if (Keyboard.current.sKey.isPressed ||
                Keyboard.current.downArrowKey.isPressed)
                forwardInput -= 1f;

            if (Keyboard.current.dKey.isPressed ||
                Keyboard.current.rightArrowKey.isPressed)
                turnInput += 1f;

            if (Keyboard.current.aKey.isPressed ||
                Keyboard.current.leftArrowKey.isPressed)
                turnInput -= 1f;
        }
#else
        forwardInput = 0f;
        turnInput = 0f;
#endif

        forwardInput = Mathf.Clamp(forwardInput, -1f, 1f);
        turnInput = Mathf.Clamp(turnInput, -1f, 1f);
    }

    private void FixedUpdate()
    {
        if (rb.isKinematic)
        {
            currentSpeed = 0f;
            currentTurnSpeed = 0f;
            return;
        }

        float targetSpeed =
            forwardInput * moveSpeed * speedMultiplier;

        bool stopping =
            Mathf.Abs(targetSpeed) < 0.01f ||
            currentSpeed * targetSpeed < 0f;

        float speedChange = stopping
            ? deceleration
            : acceleration;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            Mathf.Max(speedChange, 0.1f) * Time.fixedDeltaTime);

        currentTurnSpeed = Mathf.MoveTowards(
            currentTurnSpeed,
            turnInput * turnSpeed,
            Mathf.Max(turnAcceleration, 1f) * Time.fixedDeltaTime);

        Vector3 movement =
            rb.rotation * Vector3.forward * currentSpeed;

        rb.linearVelocity = new Vector3(
            movement.x,
            rb.linearVelocity.y,
            movement.z);

        rb.angularVelocity = Vector3.zero;

        Quaternion rotation = Quaternion.Euler(
            0f,
            currentTurnSpeed * Time.fixedDeltaTime,
            0f);

        rb.MoveRotation(rb.rotation * rotation);
    }

    private void LateUpdate()
    {
        Vector3 displacement =
            transform.position - previousPosition;

        previousPosition = transform.position;
        displacement.y = 0f;

        if (animator == null ||
            animator.runtimeAnimatorController == null)
            return;

        if (cachedController != animator.runtimeAnimatorController)
        {
            cachedController = animator.runtimeAnimatorController;
            hasSpeedParameter = false;

            foreach (AnimatorControllerParameter parameter in
                animator.parameters)
            {
                if (parameter.nameHash == SpeedParameter &&
                    parameter.type == AnimatorControllerParameterType.Float)
                {
                    hasSpeedParameter = true;
                    break;
                }
            }
        }

        if (!hasSpeedParameter || Time.deltaTime <= 0f)
            return;

        float actualSpeed =
            displacement.magnitude / Time.deltaTime;

        float maximumSpeed =
            Mathf.Max(moveSpeed * speedMultiplier, 0.01f);

        // Avoid a walking spike after teleporting to a checkpoint.
        float animationSpeed = actualSpeed > maximumSpeed * 3f
            ? 0f
            : Mathf.Clamp01(actualSpeed / maximumSpeed);

        animator.SetFloat(
            SpeedParameter,
            animationSpeed,
            0.12f,
            Time.deltaTime);
    }

    private void OnDisable()
    {
        forwardInput = 0f;
        turnInput = 0f;
        currentSpeed = 0f;
        currentTurnSpeed = 0f;

        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = new Vector3(
                0f, rb.linearVelocity.y, 0f);
        }

        if (animator != null &&
            animator.runtimeAnimatorController != null &&
            hasSpeedParameter)
        {
            animator.SetFloat(SpeedParameter, 0f);
        }
    }
}