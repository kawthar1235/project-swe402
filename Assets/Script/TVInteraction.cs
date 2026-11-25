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
    public float volume = 0.04f;

    [Header("Effects")]
    [Range(0f, 0.3f)]
    public float noiseStrength = 0.07f;

    public float glowIntensity = 2f;
    public float interactionDistance = 3f;

    [Header("Hover outline")]
    public Color outlineColor = Color.red;
    public float outlineWidth = 0.005f;

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

    private LineRenderer hoverOutline;
    private Material hoverMaterial;
    private MeshFilter screenFilter;

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
        screenFilter = videoScreen.GetComponent<MeshFilter>();

        if (screenRenderer == null ||
            screenFilter == null ||
            screenFilter.sharedMesh == null)
        {
            Debug.LogWarning(
                "TVScreen must be a Quad with a Renderer and MeshFilter.",
                this
            );
            return;
        }

        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Unlit/Texture");

        if (shader == null)
        {
            Debug.LogWarning(
                "No supported screen shader was found.",
                this
            );
            return;
        }

        EnsureBodyCollider();
        PrepareImage();

        screenMaterial = new Material(shader);
        screenMaterial.mainTexture = outputTexture;
        screenRenderer.sharedMaterial = screenMaterial;

        CreateScreenLight();
        CreateSpeaker();
        CreateHoverOutline();

        // Only hide the screen, not the TV body or its collider.
        videoScreen.SetActive(false);

        ready = true;
    }

    private void EnsureBodyCollider()
    {
        // The TV body must remain clickable while the screen is off.
        foreach (Collider existing in GetComponents<Collider>())
        {
            if (existing.enabled && !existing.isTrigger)
                return;
        }

        Bounds bounds =
            ChestPuzzleTools.LocalBounds(transform);

        BoxCollider box =
            gameObject.AddComponent<BoxCollider>();

        box.center = bounds.center;

        box.size = Vector3.Max(
            bounds.size,
            Vector3.one * 0.01f
        );

        box.isTrigger = false;
    }

    private void PrepareImage()
    {
        // Copy the image without requiring Read/Write import settings.
        RenderTexture previous = RenderTexture.active;

        RenderTexture temporary =
            RenderTexture.GetTemporary(
                Width,
                Height,
                0,
                RenderTextureFormat.ARGB32
            );

        outputTexture = new Texture2D(
            Width,
            Height,
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
                0,
                0
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

    private void CreateScreenLight()
    {
        GameObject lightObject =
            new GameObject("TVGlow");

        lightObject.transform.SetParent(
            videoScreen.transform,
            false
        );

        lightObject.transform.position =
            videoScreen.transform.position -
            videoScreen.transform.forward * 0.15f;

        screenGlow = lightObject.AddComponent<Light>();
        screenGlow.type = LightType.Point;
        screenGlow.color = new Color(0.65f, 0.8f, 1f);
        screenGlow.range = 3f;
        screenGlow.shadows = LightShadows.None;
        screenGlow.enabled = false;
    }

    private void CreateSpeaker()
    {
        speaker = gameObject.AddComponent<AudioSource>();
        speaker.playOnAwake = false;
        speaker.loop = true;
        speaker.spatialBlend = 1f;
        speaker.minDistance = 1f;
        speaker.maxDistance = 8f;
        speaker.dopplerLevel = 0f;
        speaker.volume = volume;

        if (tvSound != null)
        {
            speaker.clip = tvSound;
        }
        else
        {
            generatedStatic = CreateStaticSound();
            speaker.clip = generatedStatic;
        }
    }

    private void CreateHoverOutline()
    {
        // Keep the outline outside the screen object so it works when off.
        GameObject border =
            new GameObject("TVHoverOutline");

        border.transform.SetParent(transform, false);

        hoverMaterial =
            new Material(screenMaterial.shader);

        if (hoverMaterial.HasProperty("_BaseColor"))
            hoverMaterial.SetColor("_BaseColor", outlineColor);
        else if (hoverMaterial.HasProperty("_Color"))
            hoverMaterial.SetColor("_Color", outlineColor);

        hoverOutline = border.AddComponent<LineRenderer>();
        hoverOutline.useWorldSpace = true;
        hoverOutline.loop = true;
        hoverOutline.positionCount = 4;
        hoverOutline.widthMultiplier =
            Mathf.Max(outlineWidth, 0.001f);

        hoverOutline.sharedMaterial = hoverMaterial;
        hoverOutline.numCornerVertices = 3;

        hoverOutline.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;

        hoverOutline.receiveShadows = false;
        hoverOutline.enabled = false;
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

            if (Time.time >= nextFrameTime)
            {
                nextFrameTime = Time.time + 1f / 12f;
                UpdatePicture();
            }
        }

        if (ChestPuzzleTools.Inspecting)
        {
            SetHoverOutline(false);
            return;
        }

        bool pointedAt = false;

        if (ChestPuzzleTools.Raycast(
            playerCamera,
            interactionDistance,
            out RaycastHit hit))
        {
            pointedAt =
                hit.collider.GetComponentInParent<TVInteraction>() == this;
        }

        SetHoverOutline(pointedAt);

        if (pointedAt && ChestPuzzleTools.Clicked())
            Interact();
    }

    private void SetHoverOutline(bool visible)
    {
        if (hoverOutline == null)
            return;

        hoverOutline.enabled = visible;

        if (!visible)
            return;

        Bounds bounds = screenFilter.sharedMesh.bounds;

        // Place the border slightly in front of the default Quad.
        float z = bounds.min.z - 0.005f;

        hoverOutline.SetPosition(
            0,
            videoScreen.transform.TransformPoint(
                new Vector3(bounds.min.x, bounds.min.y, z))
        );

        hoverOutline.SetPosition(
            1,
            videoScreen.transform.TransformPoint(
                new Vector3(bounds.max.x, bounds.min.y, z))
        );

        hoverOutline.SetPosition(
            2,
            videoScreen.transform.TransformPoint(
                new Vector3(bounds.max.x, bounds.max.y, z))
        );

        hoverOutline.SetPosition(
            3,
            videoScreen.transform.TransformPoint(
                new Vector3(bounds.min.x, bounds.max.y, z))
        );
    }

    public string GetMessage()
    {
        // The outline replaces the interaction message.
        return "";
    }

    public void Interact()
    {
        if (!ready ||
            ChestPuzzleTools.Inspecting ||
            lastClickFrame == Time.frameCount)
            return;

        // Prevent two scripts from toggling the TV in the same frame.
        lastClickFrame = Time.frameCount;

        isOn = !isOn;

        videoScreen.SetActive(isOn);
        screenGlow.enabled = isOn;

        if (isOn)
        {
            switchedOnTime = Time.time;
            nextFrameTime = Time.time + 1f / 12f;

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
            float scanline =
                y % 3 == 0 ? 0.78f : 1f;

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

        if (hoverOutline != null)
            hoverOutline.enabled = false;

        if (videoScreen != null &&
            videoScreen != gameObject)
        {
            videoScreen.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (screenMaterial != null)
            Destroy(screenMaterial);

        if (hoverMaterial != null)
            Destroy(hoverMaterial);

        if (outputTexture != null)
            Destroy(outputTexture);

        if (generatedStatic != null)
            Destroy(generatedStatic);

        if (hoverOutline != null)
            Destroy(hoverOutline.gameObject);

        if (screenGlow != null)
            Destroy(screenGlow.gameObject);

        if (speaker != null)
            Destroy(speaker);
    }

    [ContextMenu("Test TV Toggle")]
    private void TestTVToggle()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("Start Play Mode first.", this);
            return;
        }

        Debug.Log(
            $"TV test | Ready: {ready}" +
            $" | Camera: {(playerCamera != null ? playerCamera.name : "NONE")}" +
            $" | Inspecting: {ChestPuzzleTools.Inspecting}",
            this
        );

        Interact();
    }
}