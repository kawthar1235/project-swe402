using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class PickUpRecord : MonoBehaviour, IInteractable
{
    public GameManager gameManager;
    public Camera playerCamera;
    public Transform holdPoint;

    public float pickupDistance = 3f;
    public float heldWidth = 0.24f;
    public Vector3 heldRotation = new Vector3(10f, 160f, 10f);

    [Range(0f, 1f)]
    public float pickupVolume = 0.35f;

    public bool IsHeld => held;

    private bool canPickUp;
    private bool held;
    private bool installed;

    private Rigidbody rb;
    private Collider[] recordColliders;
    private bool[] colliderStates;

    private PickupAutoFeedback feedback;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        recordColliders =
            GetComponentsInChildren<Collider>(true);

        colliderStates = new bool[recordColliders.Length];

        for (int i = 0; i < recordColliders.Length; i++)
            colliderStates[i] = recordColliders[i].enabled;

        rb.isKinematic = true;
        rb.useGravity = false;

        canPickUp = false;
    }

    private void Start()
    {
        ResolveReferences();
        feedback = new PickupAutoFeedback(transform, true);
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
        if (!canPickUp ||
            installed ||
            ChestPuzzleTools.Inspecting)
        {
            feedback?.SetHighlighted(false, playerCamera);
            return;
        }

        if (playerCamera == null)
            ResolveReferences();

        if (held)
        {
            feedback?.SetHighlighted(false, playerCamera);

            if (ChestPuzzleTools.SpacePressed())
                Drop();

            return;
        }

        bool hovered = false;

        if (ChestPuzzleTools.Raycast(
            playerCamera,
            pickupDistance,
            out RaycastHit hit))
        {
            hovered =
                hit.collider.GetComponentInParent<PickUpRecord>() == this;
        }

        feedback?.SetHighlighted(hovered, playerCamera);

        if (hovered && ChestPuzzleTools.Clicked())
            PickUp();
    }

    public string GetMessage()
    {
        return "";
    }

    public void Interact()
    {
        PickUp();
    }

    private void PickUp()
    {
        if (!canPickUp ||
            held ||
            installed ||
            ChestPuzzleTools.Inspecting)
            return;

        ResolveReferences();

        if (holdPoint == null || playerCamera == null)
        {
            Debug.LogWarning(
                "Record: assign Player Camera and Hold Point.",
                this);
            return;
        }

        feedback?.SetHighlighted(false, playerCamera);

        rb.isKinematic = true;
        rb.useGravity = false;

        foreach (Collider col in recordColliders)
        {
            if (col != null)
                col.enabled = false;
        }

        transform.SetParent(holdPoint, true);

        MeshFilter filter = GetComponent<MeshFilter>();

        if (filter != null && filter.sharedMesh != null)
        {
            Bounds bounds = filter.sharedMesh.bounds;
            Vector3 size = bounds.size;

            int thinAxis = 0;

            if (size.y < size[thinAxis])
                thinAxis = 1;

            if (size.z < size[thinAxis])
                thinAxis = 2;

            Vector3 normal = Vector3.zero;
            normal[thinAxis] = 1f;

            Quaternion alignment =
                Quaternion.FromToRotation(
                    normal, Vector3.forward);

            transform.localRotation =
                Quaternion.Euler(heldRotation) * alignment;

            float diameter = Mathf.Max(
                size.x, Mathf.Max(size.y, size.z));

            transform.localScale = Vector3.one *
                (heldWidth / Mathf.Max(diameter, 0.001f));

            transform.localPosition =
                -(transform.localRotation *
                  Vector3.Scale(bounds.center, transform.localScale));
        }
        else
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(heldRotation);
        }

        held = true;

        if (gameManager != null)
            gameManager.hasRecord = true;

        feedback?.PlayPickup(pickupVolume);
    }

    private void Drop()
    {
        if (!held || installed)
            return;

        transform.SetParent(null, true);

        for (int i = 0; i < recordColliders.Length; i++)
        {
            if (recordColliders[i] != null)
                recordColliders[i].enabled = colliderStates[i];
        }

        rb.isKinematic = false;
        rb.useGravity = true;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        held = false;

        if (gameManager != null)
            gameManager.hasRecord = false;
    }

    public void ReleaseFromPainting()
    {
        if (held || installed || canPickUp)
            return;

        canPickUp = true;

        rb.isKinematic = false;
        rb.useGravity = true;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;
    }

    public void PlaceInGramophone(Transform point)
    {
        if (!held || installed || point == null)
            return;

        feedback?.SetHighlighted(false, playerCamera);

        held = false;
        installed = true;
        canPickUp = false;

        rb.isKinematic = true;
        rb.useGravity = false;

        foreach (Collider col in recordColliders)
        {
            if (col != null)
                col.enabled = false;
        }

        transform.SetParent(point, true);

        MeshFilter filter = GetComponent<MeshFilter>();

        if (filter != null && filter.sharedMesh != null)
        {
            Bounds bounds = filter.sharedMesh.bounds;
            Vector3 size = bounds.size;

            int thinAxis = 0;

            if (size.y < size[thinAxis])
                thinAxis = 1;

            if (size.z < size[thinAxis])
                thinAxis = 2;

            Vector3 normal = Vector3.zero;
            normal[thinAxis] = 1f;

            transform.rotation = point.rotation *
                Quaternion.FromToRotation(normal, Vector3.up);

            transform.position =
                point.position -
                transform.TransformVector(bounds.center);
        }
        else
        {
            transform.position = point.position;
            transform.rotation = point.rotation;
        }

        if (gameManager != null)
            gameManager.hasRecord = false;
    }

    private void OnDisable()
    {
        feedback?.SetHighlighted(false, playerCamera);

        if (held)
        {
            held = false;

            if (gameManager != null)
                gameManager.hasRecord = false;
        }
    }

    private void OnDestroy()
    {
        feedback?.Dispose();
    }
}


// Shared by the record and flashlight.
// Keep this class in the same file.
public sealed class PickupAutoFeedback
{
    private sealed class OutlinePart
    {
        public Renderer source;
        public Renderer outline;
        public Vector3 center;
    }

    private readonly List<OutlinePart> parts =
        new List<OutlinePart>();

    private readonly bool flatObject;
    private Material outlineMaterial;
    private AudioClip pickupClip;

    private const float Growth = 1.04f;

    public PickupAutoFeedback(Transform root, bool flat)
    {
        flatObject = flat;
        pickupClip = CreatePickupSound();

        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
        {
            Debug.LogWarning(
                "Pickup outline: URP Unlit shader was not found.",
                root);
            return;
        }

        outlineMaterial = new Material(shader);

        outlineMaterial.SetColor("_BaseColor", Color.red);
        outlineMaterial.mainTexture = Texture2D.whiteTexture;

        outlineMaterial.renderQueue =
            (int)RenderQueue.Geometry + 1;

        // Flat objects use a slightly larger copy behind the surface.
        // Closed objects use the expanded back faces.
        outlineMaterial.SetFloat(
            "_Cull",
            flatObject ? (float)CullMode.Off : (float)CullMode.Front);

        if (outlineMaterial.HasProperty("_ZWrite"))
            outlineMaterial.SetFloat("_ZWrite", 0f);

        Renderer[] originals =
            root.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer source in originals)
        {
            GameObject copy = null;
            Renderer outline = null;
            Mesh mesh = null;

            if (source is MeshRenderer)
            {
                MeshFilter originalFilter =
                    source.GetComponent<MeshFilter>();

                if (originalFilter == null ||
                    originalFilter.sharedMesh == null)
                    continue;

                mesh = originalFilter.sharedMesh;
                copy = CreateCopy(source.transform);

                copy.AddComponent<MeshFilter>().sharedMesh = mesh;
                outline = copy.AddComponent<MeshRenderer>();
            }
            else if (source is SkinnedMeshRenderer skinned)
            {
                if (skinned.sharedMesh == null)
                    continue;

                mesh = skinned.sharedMesh;
                copy = CreateCopy(source.transform);

                SkinnedMeshRenderer skinCopy =
                    copy.AddComponent<SkinnedMeshRenderer>();

                skinCopy.sharedMesh = skinned.sharedMesh;
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

            copy.layer = source.gameObject.layer;

            Vector3 center = mesh.bounds.center;

            copy.transform.localScale = Vector3.one * Growth;
            copy.transform.localPosition = center * (1f - Growth);

            Material[] materials =
                new Material[Mathf.Max(mesh.subMeshCount, 1)];

            for (int i = 0; i < materials.Length; i++)
                materials[i] = outlineMaterial;

            outline.sharedMaterials = materials;
            outline.shadowCastingMode = ShadowCastingMode.Off;
            outline.receiveShadows = false;
            outline.lightProbeUsage = LightProbeUsage.Off;
            outline.reflectionProbeUsage = ReflectionProbeUsage.Off;
            outline.enabled = false;

            parts.Add(new OutlinePart
            {
                source = source,
                outline = outline,
                center = center
            });
        }
    }

    private static GameObject CreateCopy(Transform source)
    {
        GameObject copy = new GameObject("Auto Red Outline");
        copy.transform.SetParent(source, false);
        return copy;
    }

    public void SetHighlighted(bool visible, Camera camera)
    {
        foreach (OutlinePart part in parts)
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

            Transform source = part.source.transform;

            Vector3 position = source.TransformPoint(
                part.center * (1f - Growth));

            if (flatObject && camera != null)
            {
                Vector3 away =
                    part.source.bounds.center -
                    camera.transform.position;

                position += away.normalized * 0.001f;
            }

            part.outline.transform.position = position;
        }
    }

    public void PlayPickup(float volume)
    {
        if (pickupClip == null)
            return;

        GameObject soundObject =
            new GameObject("Auto Pickup Sound");

        AudioSource source =
            soundObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = false;
        source.volume = Mathf.Clamp01(volume);
        source.clip = pickupClip;
        source.Play();

        Object.Destroy(soundObject, pickupClip.length + 0.1f);
    }

    private static AudioClip CreatePickupSound()
    {
        const int rate = 22050;
        const float duration = 0.13f;

        float[] samples = new float[
            Mathf.CeilToInt(rate * duration)];

        System.Random random = new System.Random();
        float filteredNoise = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;

            float noise =
                (float)(random.NextDouble() * 2.0 - 1.0);

            filteredNoise =
                Mathf.Lerp(filteredNoise, noise, 0.22f);

            float attack = 1f - Mathf.Exp(-t * 600f);

            float tap =
                Mathf.Sin(
                    2f * Mathf.PI * (190f * t - 300f * t * t)) *
                Mathf.Exp(-t * 65f);

            float handling =
                filteredNoise * Mathf.Exp(-t * 28f);

            float endFade =
                Mathf.Clamp01((duration - t) / 0.025f);

            samples[i] =
                (tap * 0.35f + handling * 0.45f) *
                attack * endFade;
        }

        AudioClip clip = AudioClip.Create(
            "Soft Object Pickup",
            samples.Length, 1, rate, false);

        clip.SetData(samples, 0);
        return clip;
    }

    public void Dispose()
    {
        foreach (OutlinePart part in parts)
        {
            if (part.outline != null)
                Object.Destroy(part.outline.gameObject);
        }

        parts.Clear();

        if (outlineMaterial != null)
            Object.Destroy(outlineMaterial);

        if (pickupClip != null)
            Object.Destroy(pickupClip);
    }
}