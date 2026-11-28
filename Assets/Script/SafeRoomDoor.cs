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

    [Header("Optional opening sound")]
    public AudioClip doorOpenSound;

    [Range(0f, 1f)]
    public float openingVolume = 0.55f;

    [Header("Optional closing sound")]
    public AudioClip doorCloseSound;

    [Range(0f, 1f)]
    public float closingVolume = 0.55f;

    [Header("Sound distance")]
    public float soundDistance = 10f;

    [Header("Countdown effects")]
    [Range(0f, 0.3f)]
    public float redFlashStrength = 0.12f;

    [Range(0f, 0.15f)]
    public float noiseStrength = 0.045f;

    private bool isOpen;
    private bool locked;
    private bool timerStarted;
    private bool lockdownRunning;
    private bool initialized;
    private bool closingSoundPending;

    private float currentAngle;
    private int lastClickFrame = -1;
    private int countdown = -1;

    private Vector3 leftClosedPosition;
    private Vector3 rightClosedPosition;

    private Quaternion leftClosedRotation;
    private Quaternion rightClosedRotation;

    private AudioSource speaker;
    private AudioClip generatedOpen;
    private AudioClip generatedClose;

    private Texture2D noiseTexture;
    private GUIStyle countdownStyle;

    private KeyFlashPickupFeedback leftFeedback;
    private KeyFlashPickupFeedback rightFeedback;

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
                "Assign both doors and both hinges.",
                this);
            return false;
        }

        playerCamera =
            ChestPuzzleTools.ResolveCamera(playerCamera);

        foreach (Animator oldAnimator in
            GetComponentsInChildren<Animator>(true))
        {
            oldAnimator.enabled = false;
        }

        leftClosedPosition = leftDoor.position;
        rightClosedPosition = rightDoor.position;

        leftClosedRotation = leftDoor.rotation;
        rightClosedRotation = rightDoor.rotation;

        speaker = gameObject.AddComponent<AudioSource>();
        speaker.playOnAwake = false;
        speaker.loop = false;
        speaker.spatialBlend = 1f;
        speaker.minDistance = 1f;
        speaker.maxDistance = Mathf.Max(soundDistance, 1.1f);
        speaker.rolloffMode = AudioRolloffMode.Linear;
        speaker.dopplerLevel = 0f;

        leftFeedback =
            new KeyFlashPickupFeedback(leftDoor, true);

        rightFeedback =
            new KeyFlashPickupFeedback(rightDoor, true);

        initialized = true;
        return true;
    }

    private void Update()
    {
        if (!initialized || !Ready)
            return;

        MoveDoors();

        if (playerCamera == null)
        {
            playerCamera =
                ChestPuzzleTools.ResolveCamera(playerCamera);
        }

        if (ChestPuzzleTools.Inspecting)
        {
            Highlight(false);
            return;
        }

        bool hovered = false;

        if (ChestPuzzleTools.Raycast(
            playerCamera,
            interactionDistance,
            out RaycastHit hit))
        {
            hovered =
                hit.collider.GetComponentInParent<SafeRoomDoor>()
                == this;
        }

        Highlight(hovered);

        if (hovered && ChestPuzzleTools.Clicked())
            Interact();
    }

    private void Highlight(bool visible)
    {
        leftFeedback?.Highlight(visible, playerCamera);
        rightFeedback?.Highlight(visible, playerCamera);
    }

    private void MoveDoors()
    {
        float targetAngle = isOpen ? openAngle : 0f;

        if (currentAngle == targetAngle)
            return;

        float nextAngle = Mathf.MoveTowards(
            currentAngle,
            targetAngle,
            Mathf.Max(rotationSpeed, 1f) * Time.deltaTime);

        float change = nextAngle - currentAngle;

        leftDoor.RotateAround(
            leftHinge.position,
            leftHinge.up,
            change);

        rightDoor.RotateAround(
            rightHinge.position,
            rightHinge.up,
            -change);

        currentAngle = nextAngle;

        if (!isOpen && currentAngle == 0f)
        {
            leftDoor.SetPositionAndRotation(
                leftClosedPosition,
                leftClosedRotation);

            rightDoor.SetPositionAndRotation(
                rightClosedPosition,
                rightClosedRotation);

            if (closingSoundPending)
            {
                closingSoundPending = false;
                PlayDoorSound(false);
            }
        }
    }

    private void SetDoorOpen(bool open)
    {
        if (isOpen == open)
            return;

        isOpen = open;

        if (open)
        {
            closingSoundPending = false;
            PlayDoorSound(true);
        }
        else
        {
            // Play the closing impact when the panels reach the frame.
            closingSoundPending = currentAngle != 0f;
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

        lastClickFrame = Time.frameCount;
        SetDoorOpen(!isOpen);
    }

    public void StartLockdown()
    {
        if (timerStarted || !isActiveAndEnabled)
            return;

        if (!Initialize())
            return;

        timerStarted = true;
        lockdownRunning = true;

        StartCoroutine(Lockdown());
    }

    private IEnumerator Lockdown()
    {
        locked = true;
        countdown = -1;

        SetDoorOpen(false);

        // Wait until both panels finish closing.
        while (currentAngle != 0f)
            yield return null;

        float remaining = Mathf.Max(lockSeconds, 0f);

        while (remaining > 0f)
        {
            countdown = remaining <= 5f
                ? Mathf.CeilToInt(remaining)
                : -1;

            float step = Mathf.Min(1f, remaining);

            yield return new WaitForSeconds(step);

            remaining = Mathf.Max(0f, remaining - step);
        }

        countdown = -1;
        locked = false;
        lockdownRunning = false;

        // Automatic opening uses the same opening sound.
        SetDoorOpen(true);
    }

    // Keep compatibility with existing GameManager calls.
    public void CloseDoor()
    {
        if (!Initialize() || locked)
            return;

        SetDoorOpen(false);
    }

    public void OpenDoor()
    {
        if (!Initialize() || locked)
            return;

        SetDoorOpen(true);
    }

    private void PlayDoorSound(bool opening)
    {
        if (speaker == null)
            return;

        AudioClip clip;

        if (opening)
        {
            clip = doorOpenSound;

            if (clip == null)
            {
                if (generatedOpen == null)
                    generatedOpen = CreateDoorSound(true);

                clip = generatedOpen;
            }
        }
        else
        {
            clip = doorCloseSound;

            if (clip == null)
            {
                if (generatedClose == null)
                    generatedClose = CreateDoorSound(false);

                clip = generatedClose;
            }
        }

        speaker.Stop();

        speaker.PlayOneShot(
            clip,
            opening ? openingVolume : closingVolume);
    }

    private static AudioClip CreateDoorSound(bool opening)
    {
        const int rate = 22050;
        float duration = opening ? 0.9f : 0.4f;

        float[] samples =
            new float[Mathf.CeilToInt(rate * duration)];

        System.Random random = new System.Random();

        float filteredNoise = 0f;
        float phase = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;

            float noise =
                (float)(random.NextDouble() * 2.0 - 1.0);

            filteredNoise =
                Mathf.Lerp(filteredNoise, noise, 0.22f);

            float value;

            if (opening)
            {
                float frequency =
                    165f +
                    Mathf.Sin(t * 8f) * 55f +
                    Mathf.Sin(t * 23f) * 12f;

                phase +=
                    2f * Mathf.PI * frequency / rate;

                float creak =
                    Mathf.Sin(phase) * 0.55f +
                    Mathf.Sin(phase * 2f) * 0.18f +
                    Mathf.Sin(phase * 3f) * 0.07f;

                float friction =
                    0.55f +
                    0.45f * Mathf.Abs(Mathf.Sin(t * 19f));

                float envelope =
                    Mathf.Clamp01(t / 0.06f) *
                    Mathf.Clamp01((duration - t) / 0.2f);

                value =
                    (creak * 0.32f +
                     filteredNoise * 0.16f) *
                    friction * envelope;
            }
            else
            {
                float bass =
                    Mathf.Sin(2f * Mathf.PI * 75f * t) *
                    Mathf.Exp(-t * 18f);

                float wood =
                    Mathf.Sin(2f * Mathf.PI * 165f * t) *
                    Mathf.Exp(-t * 32f);

                float impact =
                    filteredNoise * Mathf.Exp(-t * 42f);

                float latchTime = t - 0.06f;
                float latch = 0f;

                if (latchTime >= 0f)
                {
                    latch =
                        noise *
                        Mathf.Exp(-latchTime * 90f) *
                        0.15f;
                }

                value =
                    (bass * 0.48f +
                     wood * 0.15f +
                     impact * 0.4f +
                     latch) *
                    Mathf.Clamp01(t / 0.002f) *
                    Mathf.Clamp01((duration - t) / 0.05f);
            }

            samples[i] = Mathf.Clamp(value, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(
            opening ? "Generated Door Creak" : "Generated Door Close",
            samples.Length,
            1,
            rate,
            false);

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

        float pulse =
            0.5f + 0.5f *
            Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f);

        GUI.color = new Color(
            1f, 0.02f, 0.02f,
            Mathf.Lerp(
                redFlashStrength * 0.2f,
                redFlashStrength,
                pulse));

        Rect screen = new Rect(
            0f, 0f, Screen.width, Screen.height);

        GUI.DrawTexture(screen, Texture2D.whiteTexture);

        CreateNoiseTexture();

        float frame =
            Mathf.Floor(Time.unscaledTime * 12f);

        GUI.color =
            new Color(1f, 1f, 1f, noiseStrength);

        GUI.DrawTextureWithTexCoords(
            screen,
            noiseTexture,
            new Rect(
                Mathf.Repeat(frame * 0.37f, 1f),
                Mathf.Repeat(frame * 0.61f, 1f),
                Screen.width / 64f,
                Screen.height / 64f));

        GUI.color = Color.white;

        if (countdownStyle == null)
        {
            countdownStyle = new GUIStyle(GUI.skin.label);
            countdownStyle.alignment = TextAnchor.MiddleCenter;
            countdownStyle.fontStyle = FontStyle.Bold;
        }

        countdownStyle.fontSize = Mathf.Max(
            1, Mathf.RoundToInt(Screen.height * 0.12f));

        Rect area = new Rect(
            0f,
            Screen.height * 0.18f,
            Screen.width,
            Screen.height * 0.2f);

        countdownStyle.normal.textColor =
            new Color(0f, 0f, 0f, 0.9f);

        GUI.Label(
            new Rect(
                area.x + 3f,
                area.y + 3f,
                area.width,
                area.height),
            countdown.ToString(),
            countdownStyle);

        countdownStyle.normal.textColor =
            new Color(1f, 0.9f, 0.85f);

        GUI.Label(
            area,
            countdown.ToString(),
            countdownStyle);

        GUI.color = oldColor;
        GUI.depth = oldDepth;
    }

    private void CreateNoiseTexture()
    {
        if (noiseTexture != null)
            return;

        const int size = 64;

        noiseTexture = new Texture2D(
            size, size, TextureFormat.RGBA32, false);

        noiseTexture.wrapMode = TextureWrapMode.Repeat;
        noiseTexture.filterMode = FilterMode.Point;

        Color32[] pixels = new Color32[size * size];
        System.Random random = new System.Random(7392);

        for (int i = 0; i < pixels.Length; i++)
        {
            byte value = (byte)random.Next(0, 256);

            pixels[i] =
                new Color32(value, value, value, 255);
        }

        noiseTexture.SetPixels32(pixels);
        noiseTexture.Apply(false, true);
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        countdown = -1;
        locked = false;

        if (lockdownRunning)
        {
            timerStarted = false;
            lockdownRunning = false;
        }

        Highlight(false);

        if (speaker != null)
            speaker.Stop();
    }

    private void OnDestroy()
    {
        leftFeedback?.Dispose();
        rightFeedback?.Dispose();

        if (generatedOpen != null)
            Destroy(generatedOpen);

        if (generatedClose != null)
            Destroy(generatedClose);

        if (noiseTexture != null)
            Destroy(noiseTexture);

        if (speaker != null)
            Destroy(speaker);
    }
}