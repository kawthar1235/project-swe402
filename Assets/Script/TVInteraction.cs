using UnityEngine;

public class TVInteraction : MonoBehaviour, IInteractable
{
    public GameManager gameManager;
    public Camera playerCamera;

    [Header("Screen")]
    public GameObject videoScreen;
    public Texture2D screenImage;

    [Header("Optional sound")]
    public AudioClip tvSound;

    [Range(0f, 1f)]
    public float volume = 0.4f;

    [Header("Effects")]
    [Range(0f, 0.3f)]
    public float noiseStrength = 0.07f;

    public float glowIntensity = 2f;
    public float interactionDistance = 3f;

    private const int Width = 192;
    private const int Height = 144;

    private bool isOn;
    private bool ready;
    private int lastClickFrame = -1;

    private float switchedOnTime;
    private float nextFrameTime;

    private Renderer screenRenderer;
    private Material screenMaterial;
    private Texture2D outputTexture;

    private Color32[] originalPixels;
    private Color32[] outputPixels;

    private AudioSource speaker;
    private AudioClip generatedStatic;
    private Light screenGlow;

    private System.Random random = new System.Random();

    private void Start()
    {
        playerCamera =
            ChestPuzzleTools.ResolveCamera(playerCamera);

        if (videoScreen == null ||
            videoScreen == gameObject ||
            screenImage == null)
        {
            Debug.LogWarning(
                "Assign the TVScreen child and a screen image.",
                this
            );
            return;
        }

        screenRenderer = videoScreen.GetComponent<Renderer>();

        if (screenRenderer == null)
        {
            Debug.LogWarning(
                "TVScreen must have a Renderer.", this);
            return;
        }

        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Unlit/Texture");

        if (shader == null)
        {
            Debug.LogWarning(
                "No supported screen shader was found.", this);
            return;
        }

        // Add a collider to the TV body for click interaction.
        if (GetComponent<Collider>() == null)
        {
            Bounds bounds =
                ChestPuzzleTools.LocalBounds(transform);

            BoxCollider box =
                gameObject.AddComponent<BoxCollider>();

            box.center = bounds.center;
            box.size = Vector3.Max(
                bounds.size,
                Vector3.one * 0.01f
            );
        }

        PrepareImage();

        screenMaterial = new Material(shader);
        screenMaterial.mainTexture = outputTexture;
        screenRenderer.sharedMaterial = screenMaterial;

        // Create a soft light in front of the screen.
        GameObject lightObject =
            new GameObject("TVGlow");

        lightObject.transform.SetParent(
            videoScreen.transform, false);

        lightObject.transform.position =
            videoScreen.transform.position -
            videoScreen.transform.forward * 0.15f;

        screenGlow = lightObject.AddComponent<Light>();
        screenGlow.type = LightType.Point;
        screenGlow.color = new Color(0.65f, 0.8f, 1f);
        screenGlow.range = 3f;
        screenGlow.shadows = LightShadows.None;
        screenGlow.enabled = false;

        speaker = gameObject.AddComponent<AudioSource>();
        speaker.playOnAwake = false;
        speaker.loop = true;
        speaker.spatialBlend = 1f;
        speaker.minDistance = 1f;
        speaker.maxDistance = 8f;
        speaker.dopplerLevel = 0f;

        if (tvSound != null)
        {
            speaker.clip = tvSound;
        }
        else
        {
            generatedStatic = CreateStaticSound();
            speaker.clip = generatedStatic;
        }

        videoScreen.SetActive(false);
        ready = true;
    }

    private void PrepareImage()
    {
        // Copy the image without requiring Read/Write in import settings.
        RenderTexture previous = RenderTexture.active;

        RenderTexture temporary =
            RenderTexture.GetTemporary(
                Width, Height, 0,
                RenderTextureFormat.ARGB32
            );

        outputTexture = new Texture2D(
            Width, Height,
            TextureFormat.RGBA32,
            false
        );

        outputTexture.filterMode = FilterMode.Bilinear;
        outputTexture.wrapMode = TextureWrapMode.Clamp;

        try
        {
            Graphics.Blit(screenImage, temporary);
            RenderTexture.active = temporary;

            outputTexture.ReadPixels(
                new Rect(0, 0, Width, Height),
                0, 0
            );

            outputTexture.Apply();

            originalPixels = outputTexture.GetPixels32();
            outputPixels = new Color32[originalPixels.Length];
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
        }
    }

    private void Update()
    {
        if (!ready)
            return;

        if (isOn)
        {
            speaker.volume = volume;

            screenGlow.intensity = Mathf.Max(
                0f,
                glowIntensity *
                (0.9f + (float)random.NextDouble() * 0.2f)
            );

            // Update the noisy picture at a low frame rate.
            if (Time.time >= nextFrameTime)
            {
                nextFrameTime = Time.time + 1f / 12f;
                UpdatePicture();
            }
        }

        if (ChestPuzzleTools.Inspecting ||
            !ChestPuzzleTools.Clicked())
            return;

        if (ChestPuzzleTools.Raycast(
            playerCamera,
            interactionDistance,
            out RaycastHit hit))
        {
            if (hit.collider
                .GetComponentInParent<TVInteraction>() == this)
            {
                Interact();
            }
        }
    }

    public string GetMessage()
    {
        if (!ready)
            return "";

        return isOn
            ? "Click to turn off TV"
            : "Click to turn on TV";
    }

    public void Interact()
    {
        if (!ready ||
            ChestPuzzleTools.Inspecting ||
            lastClickFrame == Time.frameCount)
            return;

        lastClickFrame = Time.frameCount;
        isOn = !isOn;

        videoScreen.SetActive(isOn);
        screenGlow.enabled = isOn;

        if (isOn)
        {
            switchedOnTime = Time.time;
            nextFrameTime = 0f;

            UpdatePicture();

            speaker.volume = volume;
            speaker.Play();
        }
        else
        {
            speaker.Stop();
        }
    }

    private void UpdatePicture()
    {
        bool starting =
            Time.time - switchedOnTime < 0.7f;

        int rollingLine =
            (int)(Time.time * 45f) % Height;

        for (int y = 0; y < Height; y++)
        {
            float scanline = y % 3 == 0 ? 0.78f : 1f;

            for (int x = 0; x < Width; x++)
            {
                int index = y * Width + x;
                Color32 source = originalPixels[index];

                float noise =
                    (float)random.NextDouble();

                if (starting)
                {
                    byte gray = (byte)(noise * 255f);

                    outputPixels[index] =
                        new Color32(gray, gray, gray, 255);

                    continue;
                }

                float grain =
                    (noise - 0.5f) * noiseStrength * 255f;

                float band =
                    Mathf.Abs(y - rollingLine) < 2
                    ? 0.82f
                    : 1f;

                float brightness = scanline * band;

                outputPixels[index] = new Color32(
                    ToByte(source.r * brightness + grain),
                    ToByte(source.g * brightness + grain),
                    ToByte(source.b * brightness + grain),
                    255
                );
            }
        }

        outputTexture.SetPixels32(outputPixels);
        outputTexture.Apply(false);
    }

    private static byte ToByte(float value)
    {
        return (byte)Mathf.Clamp(value, 0f, 255f);
    }

    private AudioClip CreateStaticSound()
    {
        const int sampleRate = 22050;
        float[] samples = new float[sampleRate * 2];

        for (int i = 0; i < samples.Length; i++)
        {
            float noise =
                (float)(random.NextDouble() * 2.0 - 1.0);

            float hum = Mathf.Sin(
                2f * Mathf.PI * 60f *
                i / sampleRate
            );

            samples[i] =
                noise * 0.2f + hum * 0.05f;
        }

        AudioClip clip = AudioClip.Create(
            "TV Static",
            samples.Length,
            1,
            sampleRate,
            false
        );

        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDisable()
    {
        isOn = false;

        if (speaker != null)
            speaker.Stop();

        if (screenGlow != null)
            screenGlow.enabled = false;

        if (videoScreen != null && videoScreen != gameObject)
            videoScreen.SetActive(false);
    }

    private void OnDestroy()
    {
        if (screenMaterial != null)
            Destroy(screenMaterial);

        if (outputTexture != null)
            Destroy(outputTexture);

        if (generatedStatic != null)
            Destroy(generatedStatic);
    }
}