using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Rigidbody))]
public class KeyPickup : MonoBehaviour, IInteractable
{
    public GameManager gameManager;
    public InventoryUI inventoryUI;
    public Camera playerCamera;
    public Transform holdPoint;

    public float pickupDistance = 3f;
    public float heldWidth = 0.18f;
    public Vector3 heldRotation = new Vector3(0f, 0f, 90f);

    [Range(0f, 1f)]
    public float pickupVolume = 0.35f;

    public bool IsHeld => held;
    public static KeyPickup HeldKey { get; private set; }

    private bool available;
    private bool held;
    private bool consumed;

    private Rigidbody body;
    private Collider[] colliders;
    private bool[] colliderStates;
    private Bounds modelBounds;

    private KeyFlashPickupFeedback feedback;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();

        ChestPuzzleTools.EnsureCollider(gameObject);

        colliders = GetComponentsInChildren<Collider>(true);
        colliderStates = new bool[colliders.Length];

        for (int i = 0; i < colliders.Length; i++)
            colliderStates[i] = colliders[i].enabled;

        modelBounds = ChestPuzzleTools.LocalBounds(transform);

        body.isKinematic = true;
        body.useGravity = false;

        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();
        feedback = new KeyFlashPickupFeedback(transform, true);
    }

    private void ResolveReferences()
    {
        playerCamera =
            ChestPuzzleTools.ResolveCamera(playerCamera);

        if (holdPoint == null && playerCamera != null)
            holdPoint = playerCamera.transform.Find("HoldPoint");
    }

    private void Update()
    {
        if (consumed || ChestPuzzleTools.Inspecting)
        {
            feedback?.Highlight(false, playerCamera);
            return;
        }

        if (held)
        {
            feedback?.Highlight(false, playerCamera);

            if (ChestPuzzleTools.SpacePressed())
                Drop();

            return;
        }

        if (playerCamera == null)
            ResolveReferences();

        bool hovered = false;

        if (available &&
            ChestPuzzleTools.Raycast(
                playerCamera,
                pickupDistance,
                out RaycastHit hit))
        {
            hovered =
                hit.collider.GetComponentInParent<KeyPickup>() == this;
        }

        feedback?.Highlight(hovered, playerCamera);

        if (hovered && ChestPuzzleTools.Clicked())
            Interact();
    }

    public string GetMessage()
    {
        return "";
    }

    public void Reveal()
    {
        available = true;
    }

    public void Interact()
    {
        if (!available ||
            held ||
            consumed ||
            ChestPuzzleTools.Inspecting)
            return;

        if (HeldKey != null && HeldKey != this)
            return;

        ResolveReferences();

        if (holdPoint == null || playerCamera == null)
        {
            Debug.LogWarning(
                "Key: assign Player Camera and Hold Point.",
                this);
            return;
        }

        foreach (PickUpRecord record in
            holdPoint.GetComponentsInChildren<PickUpRecord>())
        {
            if (record.IsHeld)
                return;
        }

        foreach (FlashlightPickup flashlight in
            holdPoint.GetComponentsInChildren<FlashlightPickup>())
        {
            if (flashlight.IsHeld)
                return;
        }

        feedback?.Highlight(false, playerCamera);

        body.isKinematic = true;
        body.useGravity = false;

        foreach (Collider col in colliders)
        {
            if (col != null)
                col.enabled = false;
        }

        transform.SetParent(holdPoint, false);
        transform.localRotation = Quaternion.Euler(heldRotation);

        float diameter = Mathf.Max(
            modelBounds.size.x,
            Mathf.Max(modelBounds.size.y, modelBounds.size.z));

        transform.localScale = Vector3.one *
            (heldWidth / Mathf.Max(diameter, 0.001f));

        transform.localPosition =
            -(transform.localRotation *
              Vector3.Scale(modelBounds.center, transform.localScale));

        held = true;
        HeldKey = this;

        if (gameManager != null)
            gameManager.hasKey = true;

        if (inventoryUI != null)
            inventoryUI.CollectKey();

        feedback?.PlaySound(pickupVolume);
    }

    public void Drop()
    {
        if (!held || consumed)
            return;

        feedback?.Highlight(false, playerCamera);

        transform.SetParent(null, true);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = colliderStates[i];
        }

        body.isKinematic = false;
        body.useGravity = true;

        body.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.WakeUp();

        ClearHeld();
    }

    public bool Consume()
    {
        if (!held || consumed)
            return false;

        consumed = true;

        feedback?.Highlight(false, playerCamera);

        ClearHeld();
        gameObject.SetActive(false);

        return true;
    }

    private void ClearHeld()
    {
        held = false;

        if (HeldKey == this)
            HeldKey = null;

        if (gameManager != null)
            gameManager.hasKey = false;

        if (inventoryUI != null)
            inventoryUI.UseKey();
    }

    private void OnDisable()
    {
        feedback?.Highlight(false, playerCamera);

        if (held)
            ClearHeld();
    }

    private void OnDestroy()
    {
        if (HeldKey == this)
            HeldKey = null;

        feedback?.Dispose();
    }
}


// Shared feedback for KeyPickup and FlashlightPickup.
// Keep this class in KeyPickup.cs.
public sealed class KeyFlashPickupFeedback
{
    private sealed class Part
    {
        public Renderer source;
        public Renderer outline;
        public Vector3 center;
    }

    private readonly List<Part> parts = new List<Part>();
    private readonly bool flat;

    private Material material;
    private AudioClip sound;

    private const float Expansion = 1.045f;

    public KeyFlashPickupFeedback(Transform root, bool flatObject)
    {
        flat = flatObject;
        sound = GenerateSound();

        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
        {
            Debug.LogWarning(
                "Pickup outline: URP Unlit shader was not found.",
                root);
            return;
        }

        material = new Material(shader);
        material.mainTexture = Texture2D.whiteTexture;
        material.SetColor("_BaseColor", Color.red);

        material.renderQueue = (int)RenderQueue.Geometry + 1;

        material.SetFloat(
            "_Cull",
            flat ? (float)CullMode.Off : (float)CullMode.Front);

        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        if (material.HasProperty("_ZWriteControl"))
            material.SetFloat("_ZWriteControl", 2f);

        Renderer[] originals =
            root.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer original in originals)
        {
            Mesh mesh;
            Renderer outline;
            GameObject copy;

            if (original is MeshRenderer)
            {
                MeshFilter originalFilter =
                    original.GetComponent<MeshFilter>();

                if (originalFilter == null ||
                    originalFilter.sharedMesh == null)
                    continue;

                mesh = originalFilter.sharedMesh;
                copy = CreateCopy(original.transform);

                copy.AddComponent<MeshFilter>().sharedMesh = mesh;
                outline = copy.AddComponent<MeshRenderer>();
            }
            else if (original is SkinnedMeshRenderer skinned)
            {
                if (skinned.sharedMesh == null)
                    continue;

                mesh = skinned.sharedMesh;
                copy = CreateCopy(original.transform);

                SkinnedMeshRenderer skinCopy =
                    copy.AddComponent<SkinnedMeshRenderer>();

                skinCopy.sharedMesh = mesh;
                skinCopy.bones = skinned.bones;
                skinCopy.rootBone = skinned.rootBone;
                skinCopy.localBounds = skinned.localBounds;
                skinCopy.updateWhenOffscreen = true;

                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    skinCopy.SetBlendShapeWeight(
                        i, skinned.GetBlendShapeWeight(i));
                }

                outline = skinCopy;
            }
            else
            {
                continue;
            }

            copy.layer = original.gameObject.layer;

            Vector3 center = mesh.bounds.center;

            copy.transform.localScale =
                Vector3.one * Expansion;

            copy.transform.localPosition =
                center * (1f - Expansion);

            Material[] materials = new Material[
                Mathf.Max(mesh.subMeshCount, 1)];

            for (int i = 0; i < materials.Length; i++)
                materials[i] = material;

            outline.sharedMaterials = materials;
            outline.shadowCastingMode = ShadowCastingMode.Off;
            outline.receiveShadows = false;
            outline.lightProbeUsage = LightProbeUsage.Off;
            outline.reflectionProbeUsage = ReflectionProbeUsage.Off;
            outline.enabled = false;

            parts.Add(new Part
            {
                source = original,
                outline = outline,
                center = center
            });
        }
    }

    private static GameObject CreateCopy(Transform parent)
    {
        GameObject copy = new GameObject("Auto Pickup Outline");
        copy.transform.SetParent(parent, false);
        return copy;
    }

    public void Highlight(bool visible, Camera camera)
    {
        foreach (Part part in parts)
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

            Vector3 position =
                part.source.transform.TransformPoint(
                    part.center * (1f - Expansion));

            if (flat && camera != null)
            {
                Vector3 away =
                    part.source.bounds.center -
                    camera.transform.position;

                position += away.normalized * 0.001f;
            }

            part.outline.transform.position = position;
        }
    }

    public void PlaySound(float volume)
    {
        if (sound == null)
            return;

        GameObject audioObject =
            new GameObject("Pickup Sound");

        AudioSource source =
            audioObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = Mathf.Clamp01(volume);
        source.clip = sound;
        source.Play();

        Object.Destroy(audioObject, sound.length + 0.1f);
    }

    private static AudioClip GenerateSound()
    {
        const int rate = 22050;
        const float duration = 0.13f;

        float[] samples = new float[
            Mathf.CeilToInt(rate * duration)];

        System.Random random = new System.Random();
        float filtered = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;

            float noise =
                (float)(random.NextDouble() * 2.0 - 1.0);

            filtered = Mathf.Lerp(filtered, noise, 0.22f);

            float tap = Mathf.Sin(
                2f * Mathf.PI * (190f * t - 300f * t * t));

            tap *= Mathf.Exp(-t * 65f);

            float handling = filtered * Mathf.Exp(-t * 28f);
            float attack = 1f - Mathf.Exp(-t * 600f);
            float fade = Mathf.Clamp01((duration - t) / 0.025f);

            samples[i] =
                (tap * 0.35f + handling * 0.45f) *
                attack * fade;
        }

        AudioClip clip = AudioClip.Create(
            "Soft Pickup", samples.Length, 1, rate, false);

        clip.SetData(samples, 0);
        return clip;
    }

    public void Dispose()
    {
        foreach (Part part in parts)
        {
            if (part.outline != null)
                Object.Destroy(part.outline.gameObject);
        }

        parts.Clear();

        if (material != null)
            Object.Destroy(material);

        if (sound != null)
            Object.Destroy(sound);
    }
}