using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FirstDoor : MonoBehaviour, IInteractable
{
    public GameManager gameManager;
    public Camera playerCamera;

    [Header("Door")]
    public Transform doorLeaf;
    public Transform doorHinge;

    public float interactionDistance = 3f;
    public float openAngle = 90f;
    public float rotationSpeed = 120f;

    [Header("Optional custom sounds")]
    [Tooltip("Leave empty to use the generated creaking sound.")]
    public AudioClip openSound;

    [Tooltip("Leave empty to use the generated closing sound.")]
    public AudioClip closeSound;

    [Range(0f, 1f)]
    public float soundVolume = 0.55f;

    [Header("Sound distance")]
    public float soundDistance = 9f;

    private bool unlocked;
    private bool isOpen;
    private bool moving;

    private float currentAngle;
    private int lastClickFrame = -1;

    private Vector3 closedPosition;
    private Quaternion closedRotation;

    private AudioSource speaker;
    private AudioClip generatedOpen;
    private AudioClip generatedClose;

    private KeyFlashPickupFeedback feedback;

    private void Start()
    {
        playerCamera =
            ChestPuzzleTools.ResolveCamera(playerCamera);

        Animator parentAnimator = GetComponent<Animator>();

        if (parentAnimator != null)
            parentAnimator.enabled = false;

        if (doorLeaf != null)
        {
            foreach (Animator oldAnimator in
                doorLeaf.GetComponentsInChildren<Animator>(true))
            {
                oldAnimator.enabled = false;
            }

            closedPosition = doorLeaf.position;
            closedRotation = doorLeaf.rotation;

            // Outline the moving door leaf.
            feedback = new KeyFlashPickupFeedback(doorLeaf, true);
        }

        speaker = GetComponent<AudioSource>();
        speaker.playOnAwake = false;
        speaker.loop = false;
        speaker.spatialBlend = 1f;
        speaker.minDistance = 1f;
        speaker.maxDistance = Mathf.Max(soundDistance, 1.1f);
        speaker.rolloffMode = AudioRolloffMode.Linear;
        speaker.dopplerLevel = 0f;
        speaker.volume = soundVolume;
        speaker.Stop();
    }

    private void Update()
    {
        if (speaker != null)
            speaker.volume = soundVolume;

        if (ChestPuzzleTools.Inspecting || moving)
        {
            feedback?.Highlight(false, playerCamera);
            return;
        }

        if (playerCamera == null)
        {
            playerCamera =
                ChestPuzzleTools.ResolveCamera(playerCamera);
        }

        bool hovered = false;

        if (ChestPuzzleTools.Raycast(
            playerCamera,
            interactionDistance,
            out RaycastHit hit))
        {
            hovered =
                hit.collider.GetComponentInParent<FirstDoor>() == this;
        }

        feedback?.Highlight(hovered, playerCamera);

        if (hovered && ChestPuzzleTools.Clicked())
            Interact();
    }

    public string GetMessage()
    {
        if (moving)
            return "";

        if (unlocked)
        {
            return isOpen
                ? "Click to close door"
                : "Click to open door";
        }

        return KeyPickup.HeldKey != null
            ? "Click to unlock door"
            : "The door is locked";
    }

    public void Interact()
    {
        if (moving ||
            ChestPuzzleTools.Inspecting ||
            lastClickFrame == Time.frameCount)
            return;

        lastClickFrame = Time.frameCount;

        if (doorLeaf == null || doorHinge == null)
        {
            Debug.LogWarning(
                "Assign Door Leaf and Door Hinge.",
                this);
            return;
        }

        if (!unlocked)
        {
            KeyPickup key = KeyPickup.HeldKey;

            if (key == null || !key.IsHeld)
                return;

            if (!key.Consume())
                return;

            unlocked = true;

            if (gameManager != null)
            {
                gameManager.hasKey = false;
                gameManager.firstDoorOpened = true;
            }
        }

        feedback?.Highlight(false, playerCamera);

        isOpen = !isOpen;
        StartCoroutine(MoveDoor());
    }

    private IEnumerator MoveDoor()
    {
        moving = true;

        float targetAngle = isOpen ? openAngle : 0f;

        // Play the creak when opening starts.
        if (isOpen)
            PlayOpenSound();

        while (Mathf.Abs(currentAngle - targetAngle) > 0.01f)
        {
            float nextAngle = Mathf.MoveTowards(
                currentAngle,
                targetAngle,
                Mathf.Max(rotationSpeed, 1f) * Time.deltaTime);

            doorLeaf.RotateAround(
                doorHinge.position,
                doorHinge.up,
                nextAngle - currentAngle);

            currentAngle = nextAngle;

            yield return null;
        }

        if (!isOpen)
        {
            doorLeaf.SetPositionAndRotation(
                closedPosition,
                closedRotation);

            currentAngle = 0f;

            // Play the closing impact when the door reaches its frame.
            PlayCloseSound();
        }

        moving = false;
    }

    private void PlayOpenSound()
    {
        AudioClip clip = openSound;

        if (clip == null)
        {
            if (generatedOpen == null)
                generatedOpen = CreateOpeningSound();

            clip = generatedOpen;
        }

        PlayDoorSound(clip);
    }

    private void PlayCloseSound()
    {
        AudioClip clip = closeSound;

        if (clip == null)
        {
            if (generatedClose == null)
                generatedClose = CreateClosingSound();

            clip = generatedClose;
        }

        PlayDoorSound(clip);
    }

    private void PlayDoorSound(AudioClip clip)
    {
        if (speaker == null || clip == null)
            return;

        speaker.volume = soundVolume;
        speaker.Stop();
        speaker.PlayOneShot(clip);
    }

    private static AudioClip CreateOpeningSound()
    {
        const int rate = 22050;
        const float duration = 0.9f;

        float[] samples =
            new float[Mathf.CeilToInt(rate * duration)];

        System.Random random = new System.Random();

        float phase = 0f;
        float filteredNoise = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;
            float progress = t / duration;

            float frequency =
                180f +
                Mathf.Sin(t * 8f) * 65f +
                Mathf.Sin(t * 23f) * 12f;

            phase += 2f * Mathf.PI * frequency / rate;

            float creak =
                Mathf.Sin(phase) * 0.55f +
                Mathf.Sin(phase * 2f) * 0.18f +
                Mathf.Sin(phase * 3f) * 0.07f;

            float noise =
                (float)(random.NextDouble() * 2.0 - 1.0);

            filteredNoise =
                Mathf.Lerp(filteredNoise, noise, 0.18f);

            float friction =
                0.55f +
                0.45f * Mathf.Abs(Mathf.Sin(t * 19f));

            float envelope =
                Mathf.Clamp01(t / 0.06f) *
                Mathf.Clamp01((duration - t) / 0.2f);

            envelope *= Mathf.Lerp(1f, 0.5f, progress);

            samples[i] =
                (creak * 0.32f +
                 filteredNoise * 0.16f) *
                friction * envelope;
        }

        AudioClip clip = AudioClip.Create(
            "Generated Door Creak",
            samples.Length, 1, rate, false);

        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateClosingSound()
    {
        const int rate = 22050;
        const float duration = 0.38f;

        float[] samples =
            new float[Mathf.CeilToInt(rate * duration)];

        System.Random random = new System.Random();

        float filteredNoise = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;

            float noise =
                (float)(random.NextDouble() * 2.0 - 1.0);

            filteredNoise =
                Mathf.Lerp(filteredNoise, noise, 0.3f);

            float thud =
                Mathf.Sin(2f * Mathf.PI * 85f * t) *
                Mathf.Exp(-t * 20f);

            float wood =
                Mathf.Sin(2f * Mathf.PI * 175f * t) *
                Mathf.Exp(-t * 35f);

            float impact =
                filteredNoise * Mathf.Exp(-t * 45f);

            float latchTime = t - 0.055f;
            float latch = 0f;

            if (latchTime >= 0f)
            {
                latch =
                    noise *
                    Mathf.Exp(-latchTime * 95f) * 0.15f;
            }

            float attack = Mathf.Clamp01(t / 0.002f);
            float fade =
                Mathf.Clamp01((duration - t) / 0.05f);

            samples[i] =
                (thud * 0.48f +
                 wood * 0.15f +
                 impact * 0.4f +
                 latch) * attack * fade;
        }

        AudioClip clip = AudioClip.Create(
            "Generated Door Close",
            samples.Length, 1, rate, false);

        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        moving = false;

        feedback?.Highlight(false, playerCamera);

        if (speaker != null)
            speaker.Stop();
    }

    private void OnDestroy()
    {
        feedback?.Dispose();

        if (generatedOpen != null)
            Destroy(generatedOpen);

        if (generatedClose != null)
            Destroy(generatedClose);
    }
}