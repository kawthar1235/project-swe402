using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(AudioSource))]
public class PaintingPuzzle : MonoBehaviour, IInteractable
{
    public GameObject vinylRecord;
    public bool paintingAdjusted = false;

    public float straightenedLocalZ = 0f;
    public Vector3 openLocalOffset = Vector3.zero;

    [Header("Interaction")]
    public Camera playerCamera;

    [Tooltip("Ray distance from the gameplay camera.")]
    public float interactionDistance = 4f;

    [Tooltip("How close the player must stand to the painting.")]
    public float nearbyDistance = 1.6f;

    [Header("Thin black outline")]
    [Range(1.002f, 1.04f)]
    public float outlineScale = 1.009f;

    [Header("Optional custom sound")]
    public AudioClip adjustmentSound;

    [Range(0f, 1f)]
    public float soundVolume = 0.4f;

    private AudioSource speaker;
    private AudioClip generatedSound;

    private Material solidOutlineMaterial;
    private Material flatOutlineMaterial;

    private bool started;

    private sealed class OutlinePart
    {
        public Renderer source;
        public Renderer outline;
        public Vector3 center;
        public bool flat;
    }

    private readonly List<OutlinePart> outlines =
        new List<OutlinePart>();

    private void Awake()
    {
        paintingAdjusted = false;

        if (vinylRecord != null && vinylRecord != gameObject)
            vinylRecord.SetActive(false);

        speaker = GetComponent<AudioSource>();
        speaker.playOnAwake = false;
        speaker.loop = false;
        speaker.spatialBlend = 1f;
        speaker.minDistance = 1f;
        speaker.maxDistance = 7f;
        speaker.dopplerLevel = 0f;
        speaker.Stop();
    }

    private void Start()
    {
        playerCamera =
            ChestPuzzleTools.ResolveCamera(playerCamera);

        SetupCollider();
        CreateOutlines();

        started = true;
    }

    private bool IsRecordPart(Transform target)
    {
        return vinylRecord != null &&
            target.IsChildOf(vinylRecord.transform);
    }

    private void SetupCollider()
    {
        foreach (Collider existing in
            GetComponentsInChildren<Collider>(true))
        {
            if (IsRecordPart(existing.transform))
                continue;

            if (existing.enabled &&
                !existing.isTrigger &&
                existing.gameObject.activeInHierarchy)
                return;
        }

        Bounds bounds = default;
        bool found = false;

        foreach (Renderer part in
            GetComponentsInChildren<Renderer>(true))
        {
            if (IsRecordPart(part.transform) ||
                part is LineRenderer ||
                part is ParticleSystemRenderer)
                continue;

            Bounds local = part.localBounds;

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = local.center +
                    Vector3.Scale(local.extents, new Vector3(
                        (i & 1) == 0 ? -1f : 1f,
                        (i & 2) == 0 ? -1f : 1f,
                        (i & 4) == 0 ? -1f : 1f));

                Vector3 point = transform.InverseTransformPoint(
                    part.transform.TransformPoint(corner));

                if (!found)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }
        }

        if (!found)
        {
            Debug.LogWarning(
                "Painting: no visible mesh was found. Check the script is on the painting.",
                this);
            return;
        }

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = Vector3.Max(
            bounds.size, Vector3.one * 0.01f);

        box.isTrigger = false;
    }

    private void Update()
    {
        if (!started ||
            paintingAdjusted ||
            ChestPuzzleTools.Inspecting)
        {
            ShowOutline(false);
            return;
        }

        if (playerCamera == null)
        {
            playerCamera =
                ChestPuzzleTools.ResolveCamera(playerCamera);
        }

        bool hovered = IsPointedAt();

        ShowOutline(hovered);

        if (hovered && ChestPuzzleTools.Clicked())
            Interact();
    }

 private bool IsPointedAt()
{
    if (playerCamera == null)
        return false;

    if (!ChestPuzzleTools.Raycast(
        playerCamera,
        interactionDistance,
        out RaycastHit hit))
        return false;

    return hit.collider.GetComponentInParent<PaintingPuzzle>()
        == this;
}

    public string GetMessage()
    {
        return "";
    }

    public void Interact()
    {
        if (!started ||
            paintingAdjusted ||
            ChestPuzzleTools.Inspecting ||
            !IsPointedAt())
            return;

        if (vinylRecord == null || vinylRecord == gameObject)
        {
            Debug.LogWarning(
                "Painting: assign the separate vinyl record object.",
                this);
            return;
        }

        PickUpRecord record =
            vinylRecord.GetComponent<PickUpRecord>();

        if (record == null)
        {
            Debug.LogWarning(
                "Vinyl Record must have PickUpRecord.",
                this);
            return;
        }

        paintingAdjusted = true;
        ShowOutline(false);

        Vector3 angles = transform.localEulerAngles;
        angles.z = straightenedLocalZ;

        transform.localRotation = Quaternion.Euler(angles);
        transform.localPosition += openLocalOffset;

        PlayAdjustmentSound();

        vinylRecord.SetActive(true);
        record.ReleaseFromPainting();
    }

    private void CreateOutlines()
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
        {
            Debug.LogWarning(
                "Painting outline: URP Unlit shader was not found.",
                this);
            return;
        }

        solidOutlineMaterial =
            MakeOutlineMaterial(shader, CullMode.Front);

        flatOutlineMaterial =
            MakeOutlineMaterial(shader, CullMode.Off);

        MeshFilter[] sources =
            GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter source in sources)
        {
            if (IsRecordPart(source.transform))
                continue;

            MeshRenderer original =
                source.GetComponent<MeshRenderer>();

            if (original == null || source.sharedMesh == null)
                continue;

            Bounds bounds = source.sharedMesh.bounds;
            Vector3 size = bounds.size;

            float largest = Mathf.Max(
                size.x, Mathf.Max(size.y, size.z));

            float smallest = Mathf.Min(
                size.x, Mathf.Min(size.y, size.z));

            bool flat =
                smallest < Mathf.Max(largest * 0.02f, 0.0001f);

            GameObject copy =
                new GameObject("Painting Thin Black Outline");

            copy.layer = source.gameObject.layer;
            copy.transform.SetParent(source.transform, false);

            copy.AddComponent<MeshFilter>().sharedMesh =
                source.sharedMesh;

            MeshRenderer renderer =
                copy.AddComponent<MeshRenderer>();

            Material[] materials = new Material[
                Mathf.Max(source.sharedMesh.subMeshCount, 1)];

            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = flat
                    ? flatOutlineMaterial
                    : solidOutlineMaterial;
            }

            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.enabled = false;

            outlines.Add(new OutlinePart
            {
                source = original,
                outline = renderer,
                center = bounds.center,
                flat = flat
            });
        }
    }

    private Material MakeOutlineMaterial(
        Shader shader, CullMode cull)
    {
        Material material = new Material(shader);
        material.mainTexture = Texture2D.whiteTexture;
        material.SetColor("_BaseColor", Color.black);
        material.SetFloat("_Cull", (float)cull);
        material.renderQueue = (int)RenderQueue.Geometry + 1;

        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        if (material.HasProperty("_ZWriteControl"))
            material.SetFloat("_ZWriteControl", 2f);

        return material;
    }

    private void ShowOutline(bool visible)
    {
        float scale = Mathf.Clamp(outlineScale, 1.002f, 1.04f);

        foreach (OutlinePart part in outlines)
        {
            if (part.outline == null)
                continue;

            bool show =
                visible &&
                part.source != null &&
                part.source.enabled &&
                part.source.gameObject.activeInHierarchy;

            part.outline.enabled = show;

            if (!show)
                continue;

            part.outline.transform.localScale =
                Vector3.one * scale;

            Vector3 position =
                part.source.transform.TransformPoint(
                    part.center * (1f - scale));

            if (part.flat && playerCamera != null)
            {
                Vector3 away =
                    part.source.bounds.center -
                    playerCamera.transform.position;

                position += away.normalized * 0.001f;
            }

            part.outline.transform.position = position;
        }
    }

    private void PlayAdjustmentSound()
    {
        AudioClip clip = adjustmentSound;

        if (clip == null)
        {
            if (generatedSound == null)
                generatedSound = CreateWoodSound();

            clip = generatedSound;
        }

        speaker.PlayOneShot(clip, soundVolume);
    }

    private static AudioClip CreateWoodSound()
    {
        const int rate = 22050;
        const float duration = 0.36f;

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
                Mathf.Lerp(filteredNoise, noise, 0.16f);

            float frequency =
                145f + Mathf.Sin(t * 18f) * 24f;

            phase += 2f * Mathf.PI * frequency / rate;

            float creak =
                Mathf.Sin(phase) * 0.16f +
                Mathf.Sin(phase * 2f) * 0.04f;

            float envelope =
                Mathf.Clamp01(t / 0.035f) *
                Mathf.Clamp01((0.28f - t) / 0.08f);

            float rub =
                (filteredNoise * 0.35f + creak) * envelope;

            float tapTime = t - 0.25f;
            float tap = 0f;

            if (tapTime >= 0f)
            {
                tap =
                    (Mathf.Sin(
                        2f * Mathf.PI * 140f * tapTime) * 0.25f +
                     filteredNoise * 0.15f) *
                    Mathf.Exp(-tapTime * 65f) *
                    Mathf.Clamp01(tapTime / 0.002f);
            }

            float fade =
                Mathf.Clamp01((duration - t) / 0.025f);

            samples[i] = Mathf.Clamp(
                (rub + tap) * fade, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(
            "Painting Wood Adjustment",
            samples.Length, 1, rate, false);

        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDisable()
    {
        ShowOutline(false);
    }

    private void OnDestroy()
    {
        foreach (OutlinePart part in outlines)
        {
            if (part.outline != null)
                Destroy(part.outline.gameObject);
        }

        if (solidOutlineMaterial != null)
            Destroy(solidOutlineMaterial);

        if (flatOutlineMaterial != null)
            Destroy(flatOutlineMaterial);

        if (generatedSound != null)
            Destroy(generatedSound);
    }
}