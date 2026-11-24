using System.Collections;
using UnityEngine;

public class SafeRoomDoor : MonoBehaviour, IInteractable
{
    [Header("Camera")]
    public Camera playerCamera;

    [Header("Door panels")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("Hinges")]
    public Transform leftHinge;
    public Transform rightHinge;

    [Header("Movement")]
    public float openAngle = 90f;
    public float rotationSpeed = 120f;
    public float interactionDistance = 3f;

    [Header("Lockdown")]
    public float lockSeconds = 30f;

    [Header("Opening sound")]
    public AudioClip doorOpenSound;

    [Range(0f, 1f)]
    public float openingVolume = 1f;

    [Header("Countdown effects")]
    [Range(0f, 0.3f)]
    public float redFlashStrength = 0.12f;

    [Range(0f, 0.15f)]
    public float noiseStrength = 0.045f;

    private bool isOpen;
    private bool locked;
    private bool timerStarted;
    private bool initialized;

    private float currentAngle;
    private int lastClickFrame = -1;
    private int countdown = -1;

    private Vector3 leftClosedPosition;
    private Vector3 rightClosedPosition;
    private Quaternion leftClosedRotation;
    private Quaternion rightClosedRotation;

    private AudioSource openingSpeaker;
    private AudioClip generatedImpact;
    private Texture2D noiseTexture;
    private GUIStyle countdownStyle;

    private bool Ready =>
        leftDoor != null &&
        rightDoor != null &&
        leftHinge != null &&
        rightHinge != null;

    private void Start()
    {
        Initialize();
    }

    private bool Initialize()
    {
        if (initialized)
            return true;

        if (!Ready)
        {
            Debug.LogWarning(
                "Assign both doors and both hinges.", this);
            return false;
        }

        playerCamera =
            ChestPuzzleTools.ResolveCamera(playerCamera);

        // Disable existing animation to prevent movement conflicts.
        foreach (Animator oldAnimator in
            GetComponentsInChildren<Animator>(true))
        {
            oldAnimator.enabled = false;
        }

        leftClosedPosition = leftDoor.position;
        rightClosedPosition = rightDoor.position;

        leftClosedRotation = leftDoor.rotation;
        rightClosedRotation = rightDoor.rotation;

        initialized = true;
        return true;
    }

    private void Update()
    {
        if (!initialized || !Ready)
            return;

        MoveDoors();

        if (ChestPuzzleTools.Inspecting ||
            !ChestPuzzleTools.Clicked())
            return;

        if (ChestPuzzleTools.Raycast(
            playerCamera,
            interactionDistance,
            out RaycastHit hit))
        {
            if (hit.collider
                .GetComponentInParent<SafeRoomDoor>() == this)
            {
                Interact();
            }
        }
    }

    private void MoveDoors()
    {
        float targetAngle = isOpen ? openAngle : 0f;

        if (currentAngle == targetAngle)
            return;

        float nextAngle = Mathf.MoveTowards(
            currentAngle,
            targetAngle,
            Mathf.Max(rotationSpeed, 1f) * Time.deltaTime
        );

        float change = nextAngle - currentAngle;

        // Rotate each panel around its outer hinge.
        leftDoor.RotateAround(
            leftHinge.position,
            leftHinge.up,
            change
        );

        rightDoor.RotateAround(
            rightHinge.position,
            rightHinge.up,
            -change
        );

        currentAngle = nextAngle;

        // Restore the exact closed pose to prevent accumulated drift.
        if (!isOpen && currentAngle == 0f)
        {
            leftDoor.SetPositionAndRotation(
                leftClosedPosition,
                leftClosedRotation
            );

            rightDoor.SetPositionAndRotation(
                rightClosedPosition,
                rightClosedRotation
            );
        }
    }

    public string GetMessage()
    {
        if (!initialized)
            return "";

        if (locked)
            return "The door is temporarily locked";

        return isOpen
            ? "Click to close door"
            : "Click to open door";
    }

    public void Interact()
    {
        if (!initialized ||
            !Ready ||
            locked ||
            ChestPuzzleTools.Inspecting ||
            lastClickFrame == Time.frameCount)
            return;

        // Prevent duplicate interaction from multiple input scripts.
        lastClickFrame = Time.frameCount;
        isOpen = !isOpen;
    }

    public void StartLockdown()
    {
        if (timerStarted || !isActiveAndEnabled)
            return;

        if (!Initialize())
            return;

        timerStarted = true;
        StartCoroutine(Lockdown());
    }

    private IEnumerator Lockdown()
    {
        locked = true;
        isOpen = false;
        countdown = -1;

        // Start the timer only after both panels are fully closed.
        while (currentAngle != 0f)
            yield return null;

        float remaining = Mathf.Max(lockSeconds, 0f);

        while (remaining > 0f)
        {
            // Show the countdown and effects during the final five seconds.
            countdown = remaining <= 5f
                ? Mathf.CeilToInt(remaining)
                : -1;

            float step = Mathf.Min(1f, remaining);

            yield return new WaitForSeconds(step);

            remaining = Mathf.Max(0f, remaining - step);
        }

        countdown = -1;
        locked = false;
        isOpen = true;

        PlayOpeningSound();
    }

    // Keep these methods compatible with existing GameManager references.
    public void CloseDoor()
    {
        if (!locked)
            isOpen = false;
    }

    public void OpenDoor()
    {
        if (!locked)
            isOpen = true;
    }

    private void PlayOpeningSound()
    {
        if (openingSpeaker == null)
        {
            openingSpeaker =
                gameObject.AddComponent<AudioSource>();

            openingSpeaker.playOnAwake = false;
            openingSpeaker.spatialBlend = 0f;
            openingSpeaker.loop = false;
        }

        AudioClip clip = doorOpenSound;

        // Generate a basic impact if no opening sound is assigned.
        if (clip == null)
        {
            if (generatedImpact == null)
                generatedImpact = CreateImpactSound();

            clip = generatedImpact;
        }

        openingSpeaker.PlayOneShot(clip, openingVolume);
    }

    private AudioClip CreateImpactSound()
    {
        const int sampleRate = 44100;
        const float duration = 0.8f;

        float[] samples =
            new float[Mathf.CeilToInt(sampleRate * duration)];

        System.Random random = new System.Random();

        for (int i = 0; i < samples.Length; i++)
        {
            float time = i / (float)sampleRate;

            float bass = Mathf.Sin(
                2f * Mathf.PI * 65f * time
            ) * Mathf.Exp(-time * 9f);

            float noise =
                (float)(random.NextDouble() * 2.0 - 1.0);

            float impact =
                noise * Mathf.Exp(-time * 24f);

            float metallic = Mathf.Sin(
                2f * Mathf.PI * 430f * time
            ) * Mathf.Exp(-time * 7f);

            float attack =
                Mathf.Clamp01(time / 0.002f);

            samples[i] = Mathf.Clamp(
                (bass * 0.55f +
                 impact * 0.3f +
                 metallic * 0.15f) * attack,
                -1f,
                1f
            );
        }

        AudioClip clip = AudioClip.Create(
            "Door Opening Impact",
            samples.Length,
            1,
            sampleRate,
            false
        );

        clip.SetData(samples, 0);
        return clip;
    }

    private void OnGUI()
    {
        if (countdown < 1)
            return;

        Color oldColor = GUI.color;
        int oldDepth = GUI.depth;

        GUI.depth = -100;

        // Draw a soft red pulse.
        float pulse = 0.5f + 0.5f *
            Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f);

        GUI.color = new Color(
            1f,
            0.02f,
            0.02f,
            Mathf.Lerp(
                redFlashStrength * 0.2f,
                redFlashStrength,
                pulse
            )
        );

        GUI.DrawTexture(
            new Rect(
                0f, 0f,
                Screen.width, Screen.height),
            Texture2D.whiteTexture
        );

        // Draw subtle animated screen noise.
        CreateNoiseTexture();

        float frame =
            Mathf.Floor(Time.unscaledTime * 12f);

        GUI.color =
            new Color(1f, 1f, 1f, noiseStrength);

        GUI.DrawTextureWithTexCoords(
            new Rect(
                0f, 0f,
                Screen.width, Screen.height),
            noiseTexture,
            new Rect(
                Mathf.Repeat(frame * 0.37f, 1f),
                Mathf.Repeat(frame * 0.61f, 1f),
                Screen.width / 64f,
                Screen.height / 64f
            )
        );

        // Draw the countdown above the screen effects.
        GUI.color = Color.white;

        if (countdownStyle == null)
        {
            countdownStyle =
                new GUIStyle(GUI.skin.label);

            countdownStyle.alignment =
                TextAnchor.MiddleCenter;

            countdownStyle.fontStyle =
                FontStyle.Bold;
        }

        countdownStyle.fontSize = Mathf.Max(
            1,
            Mathf.RoundToInt(Screen.height * 0.12f)
        );

        Rect area = new Rect(
            0f,
            Screen.height * 0.18f,
            Screen.width,
            Screen.height * 0.2f
        );

        countdownStyle.normal.textColor =
            new Color(0f, 0f, 0f, 0.9f);

        GUI.Label(
            new Rect(
                area.x + 3f,
                area.y + 3f,
                area.width,
                area.height),
            countdown.ToString(),
            countdownStyle
        );

        countdownStyle.normal.textColor =
            new Color(1f, 0.9f, 0.85f);

        GUI.Label(
            area,
            countdown.ToString(),
            countdownStyle
        );

        GUI.color = oldColor;
        GUI.depth = oldDepth;
    }

    private void CreateNoiseTexture()
    {
        if (noiseTexture != null)
            return;

        const int size = 64;

        noiseTexture = new Texture2D(
            size,
            size,
            TextureFormat.RGBA32,
            false
        );

        noiseTexture.wrapMode =
            TextureWrapMode.Repeat;

        noiseTexture.filterMode =
            FilterMode.Point;

        Color32[] pixels =
            new Color32[size * size];

        System.Random random =
            new System.Random(7392);

        for (int i = 0; i < pixels.Length; i++)
        {
            byte value =
                (byte)random.Next(0, 256);

            pixels[i] = new Color32(
                value, value, value, 255
            );
        }

        noiseTexture.SetPixels32(pixels);
        noiseTexture.Apply(false, true);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        countdown = -1;
        locked = false;

        if (openingSpeaker != null)
            openingSpeaker.Stop();
    }

    private void OnDestroy()
    {
        if (generatedImpact != null)
            Destroy(generatedImpact);

        if (noiseTexture != null)
            Destroy(noiseTexture);
    }
}