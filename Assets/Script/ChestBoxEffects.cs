using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class ChestBoxEffects : MonoBehaviour
{
    [Header("Old wood")]
    public Material woodMaterial;

    [Tooltip("Optional. Leave empty to find the chest renderers.")]
    public Renderer[] woodRenderers;

    public Color darkWood = new Color(0.16f, 0.09f, 0.045f);
    public Color fadedWood = new Color(0.40f, 0.27f, 0.15f);

    [Header("Small triangular fragments")]
    [Range(2, 6)]
    public int stripsPerSide = 4;

    [Range(2, 3)]
    public int rowsPerSide = 2;

    public float fragmentLifetime = 1f;
    public float scatterSpeed = 0.35f;

    [Header("Transparent dark dust")]
    public Shader dustShader;

    [Range(0f, 0.3f)]
    public float dustOpacity = 0.10f;

    [Header("Chest hover")]
    public Color hoverColor = Color.red;
    public float outlineWidth = 0.003f;

    private WoodenBox owner;
    private Renderer[] chestRenderers;
    private Bounds localBounds;

    private Material runtimeWood;
    private Texture2D runtimeWoodTexture;

    private Material outlineMaterial;
    private LineRenderer chestOutline;

    private bool initialized;
    private bool broken;

    private readonly List<Mesh> meshes = new List<Mesh>();
    private readonly List<GameObject> effectObjects =
        new List<GameObject>();

    private readonly List<Material> effectMaterials =
        new List<Material>();

    private static readonly int[] OutlinePath =
    {
        0, 1, 3, 2, 0, 4, 5, 1,
        5, 7, 3, 7, 6, 2, 6, 4
    };

    private void Start()
    {
        Initialize();
    }

    private bool Initialize()
    {
        if (initialized)
            return true;

        owner = GetComponent<WoodenBox>();

        if (owner == null)
        {
            Debug.LogError(
                "ChestBoxEffects needs WoodenBox on the same object.",
                this);
            return false;
        }

        chestRenderers = CollectRenderers();

        if (chestRenderers.Length == 0)
        {
            Debug.LogError("No chest renderers found.", this);
            return false;
        }

        CalculateBounds();

        if (woodMaterial == null)
        {
            BuildWoodMaterial(
                out runtimeWood,
                out runtimeWoodTexture);
        }

        Material wood = CurrentWood();

        if (wood != null)
        {
            foreach (Renderer part in chestRenderers)
                ApplyWood(part, wood);
        }

        initialized = true;
        return true;
    }

    private Renderer[] CollectRenderers()
    {
        if (owner == null)
            owner = GetComponent<WoodenBox>();

        List<Renderer> result = new List<Renderer>();

        Renderer[] candidates =
            woodRenderers != null && woodRenderers.Length > 0
            ? woodRenderers
            : GetComponentsInChildren<Renderer>(true);

        foreach (Renderer part in candidates)
        {
            if (part == null ||
                part is LineRenderer ||
                part is ParticleSystemRenderer)
                continue;

            if (owner != null)
            {
                if (owner.key != null &&
                    part.transform.IsChildOf(owner.key.transform))
                    continue;

                if (owner.keypadRoot != null &&
                    part.transform.IsChildOf(owner.keypadRoot))
                    continue;
            }

            if (part.GetComponentInParent<ChestKeypadButton>() != null)
                continue;

            if (!result.Contains(part))
                result.Add(part);
        }

        return result.ToArray();
    }

    private Material CurrentWood()
    {
        return woodMaterial != null ? woodMaterial : runtimeWood;
    }

    private static bool IsMetal(Material material)
    {
        if (material == null)
            return false;

        string name = material.name.ToLowerInvariant();

        return name.Contains("metal") ||
               name.Contains("iron") ||
               name.Contains("steel") ||
               name.Contains("brass");
    }

    private static void ApplyWood(Renderer part, Material wood)
    {
        Material[] materials = part.sharedMaterials;

        for (int i = 0; i < materials.Length; i++)
        {
            if (!IsMetal(materials[i]))
                materials[i] = wood;
        }

        part.sharedMaterials = materials;
    }

    private void CalculateBounds()
    {
        bool found = false;

        foreach (Renderer part in chestRenderers)
        {
            Bounds bounds = part.localBounds;

            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = bounds.center +
                    Vector3.Scale(bounds.extents, new Vector3(
                        (i & 1) == 0 ? -1f : 1f,
                        (i & 2) == 0 ? -1f : 1f,
                        (i & 4) == 0 ? -1f : 1f));

                Vector3 point = transform.InverseTransformPoint(
                    part.transform.TransformPoint(corner));

                if (!found)
                {
                    localBounds = new Bounds(point, Vector3.zero);
                    found = true;
                }
                else
                {
                    localBounds.Encapsulate(point);
                }
            }
        }
    }

    private void BuildWoodMaterial(
        out Material material,
        out Texture2D texture)
    {
        material = null;
        texture = null;

        Shader shader =
            Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Standard");

        if (shader == null)
            return;

        const int width = 128;
        const int height = 256;

        texture = new Texture2D(
            width, height, TextureFormat.RGBA32, true);

        texture.name = "Old Chest Wood Grain";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float warp =
                    Mathf.PerlinNoise(x * 0.018f, y * 0.012f);

                float grain = Mathf.PerlinNoise(
                    x * 0.35f + warp * 8f,
                    y * 0.012f);

                float wear =
                    Mathf.PerlinNoise(x * 0.045f, y * 0.045f);

                float streak = Mathf.Pow(
                    Mathf.Abs(Mathf.Sin(x * 0.75f + warp * 10f)),
                    12f);

                float value = Mathf.Clamp01(
                    grain * 0.65f +
                    wear * 0.35f -
                    streak * 0.22f);

                pixels[y * width + x] =
                    Color.Lerp(darkWood, fadedWood, value);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        material = new Material(shader);
        material.name = "Old Chest Wood";
        material.mainTexture = texture;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.07f);

        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", 0.07f);

        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);
    }

    [ContextMenu("Apply Old Wood Permanently")]
    private void ApplyOldWoodPermanently()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            Debug.LogWarning("Stop Play Mode first.", this);
            return;
        }

        owner = GetComponent<WoodenBox>();

        if (owner == null)
        {
            Debug.LogError("WoodenBox component is missing.", this);
            return;
        }

        Renderer[] parts = CollectRenderers();

        if (parts.Length == 0)
        {
            Debug.LogError("No chest renderers found.", this);
            return;
        }

        BuildWoodMaterial(
            out Material savedMaterial,
            out Texture2D savedTexture);

        if (savedMaterial == null)
            return;

        if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            UnityEditor.AssetDatabase.CreateFolder(
                "Assets", "Materials");
        }

        string texturePath =
            UnityEditor.AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Materials/ChestOldWoodGrain.asset");

        UnityEditor.AssetDatabase.CreateAsset(
            savedTexture, texturePath);

        string materialPath =
            UnityEditor.AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Materials/ChestOldWood.mat");

        UnityEditor.AssetDatabase.CreateAsset(
            savedMaterial, materialPath);

        UnityEditor.Undo.RecordObject(this, "Apply Old Chest Wood");
        woodMaterial = savedMaterial;

        foreach (Renderer part in parts)
        {
            UnityEditor.Undo.RecordObject(part, "Apply Old Chest Wood");

            ApplyWood(part, savedMaterial);

            UnityEditor.EditorUtility.SetDirty(part);

            UnityEditor.PrefabUtility
                .RecordPrefabInstancePropertyModifications(part);
        }

        UnityEditor.EditorUtility.SetDirty(this);

        UnityEditor.PrefabUtility
            .RecordPrefabInstancePropertyModifications(this);

        UnityEditor.SceneManagement.EditorSceneManager
            .MarkSceneDirty(gameObject.scene);

        UnityEditor.AssetDatabase.SaveAssets();

        Debug.Log(
            "Wood saved and applied. Save the scene with Ctrl+S.",
            this);
#endif
    }

    private void Update()
    {
        if (!initialized || broken || owner.playerCamera == null)
            return;

        bool hovered = false;

        if (!ChestPuzzleTools.Inspecting &&
            ChestPuzzleTools.Raycast(
                owner.playerCamera,
                owner.interactionDistance,
                out RaycastHit hit))
        {
            hovered =
                hit.transform.IsChildOf(transform) ||
                (owner.keypadRoot != null &&
                 hit.transform.IsChildOf(owner.keypadRoot));
        }

        ShowOutline(hovered);
    }

    private void ShowOutline(bool visible)
    {
        if (chestOutline == null && visible)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Unlit");

            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            if (shader == null)
                return;

            outlineMaterial = new Material(shader);
            outlineMaterial.mainTexture = Texture2D.whiteTexture;

            GameObject border = new GameObject("Chest Red Hover");
            border.transform.SetParent(transform, false);

            chestOutline = border.AddComponent<LineRenderer>();
            chestOutline.sharedMaterial = outlineMaterial;
            chestOutline.positionCount = OutlinePath.Length;
            chestOutline.useWorldSpace = true;
            chestOutline.loop = false;
            chestOutline.numCornerVertices = 2;
            chestOutline.shadowCastingMode = ShadowCastingMode.Off;
            chestOutline.receiveShadows = false;
        }

        if (chestOutline == null)
            return;

        chestOutline.enabled = visible;

        if (!visible)
            return;

        if (outlineMaterial.HasProperty("_BaseColor"))
            outlineMaterial.SetColor("_BaseColor", hoverColor);

        if (outlineMaterial.HasProperty("_Color"))
            outlineMaterial.SetColor("_Color", hoverColor);

        chestOutline.widthMultiplier =
            Mathf.Max(outlineWidth, 0.0005f);

        Vector3 min = localBounds.min;
        Vector3 max = localBounds.max;

        for (int i = 0; i < OutlinePath.Length; i++)
        {
            int corner = OutlinePath[i];

            Vector3 point = new Vector3(
                (corner & 1) == 0 ? min.x : max.x,
                (corner & 2) == 0 ? min.y : max.y,
                (corner & 4) == 0 ? min.z : max.z);

            point = localBounds.center +
                (point - localBounds.center) * 1.025f;

            chestOutline.SetPosition(
                i, transform.TransformPoint(point));
        }
    }

    // The argument is retained for compatibility with WoodenBox.
    // Break audio is played by WoodenBox using the assigned recording.
    public void Shatter(bool playFallbackSound = false)
    {
        if (!Initialize() || broken)
            return;

        broken = true;
        ShowOutline(false);

        Vector3 center = transform.TransformPoint(localBounds.center);

        GameObject root = new GameObject("Chest Wood Fragments");
        effectObjects.Add(root);

        List<Collider> fragmentColliders = new List<Collider>();

        Vector3 min = localBounds.min;
        Vector3 max = localBounds.max;
        Vector3 size = localBounds.size;

        AddPanel(root.transform, fragmentColliders, center,
            min, Vector3.right * size.x, Vector3.up * size.y);

        AddPanel(root.transform, fragmentColliders, center,
            new Vector3(min.x, min.y, max.z),
            Vector3.right * size.x, Vector3.up * size.y);

        AddPanel(root.transform, fragmentColliders, center,
            min, Vector3.forward * size.z, Vector3.up * size.y);

        AddPanel(root.transform, fragmentColliders, center,
            new Vector3(max.x, min.y, min.z),
            Vector3.forward * size.z, Vector3.up * size.y);

        AddPanel(root.transform, fragmentColliders, center,
            min, Vector3.right * size.x, Vector3.forward * size.z);

        AddPanel(root.transform, fragmentColliders, center,
            new Vector3(min.x, max.y, min.z),
            Vector3.right * size.x, Vector3.forward * size.z);

        Transform player = ChestPuzzleTools.Player(owner.playerCamera);

        Collider[] playerColliders = player != null
            ? player.GetComponentsInChildren<Collider>(true)
            : new Collider[0];

        Collider[] keyColliders = owner.key != null
            ? owner.key.GetComponentsInChildren<Collider>(true)
            : new Collider[0];

        for (int i = 0; i < fragmentColliders.Count; i++)
        {
            Collider fragment = fragmentColliders[i];

            foreach (Collider playerCol in playerColliders)
                Physics.IgnoreCollision(fragment, playerCol);

            foreach (Collider keyCol in keyColliders)
                Physics.IgnoreCollision(fragment, keyCol);

            for (int j = 0; j < i; j++)
                Physics.IgnoreCollision(fragment, fragmentColliders[j]);
        }

        // No forced two-second minimum.
        float lifetime = Mathf.Max(fragmentLifetime, 0.2f);
        Destroy(root, lifetime);

        foreach (Mesh mesh in meshes)
        {
            if (mesh != null)
                Destroy(mesh, lifetime + 0.05f);
        }

        CreateDust(center);
    }

    private void AddPanel(
        Transform parent,
        List<Collider> colliders,
        Vector3 center,
        Vector3 origin,
        Vector3 across,
        Vector3 along)
    {
        int columns = Mathf.Clamp(stripsPerSide, 2, 6);
        int rows = Mathf.Clamp(rowsPerSide, 2, 3);

        float[] rowCuts = RandomCuts(rows);
        float[] lower = RandomCuts(columns);

        for (int row = 0; row < rows; row++)
        {
            float[] upper = RandomCuts(columns);

            Vector3 start = origin + along * rowCuts[row];
            Vector3 height =
                along * (rowCuts[row + 1] - rowCuts[row]);

            for (int i = 0; i < columns; i++)
            {
                Vector3 a = start + across * lower[i];
                Vector3 b = start + across * lower[i + 1];
                Vector3 c = start + across * upper[i + 1] + height;
                Vector3 d = start + across * upper[i] + height;

                if (Random.value > 0.5f)
                {
                    AddSplinter(parent, colliders, center, a, b, c);
                    AddSplinter(parent, colliders, center, a, c, d);
                }
                else
                {
                    AddSplinter(parent, colliders, center, a, b, d);
                    AddSplinter(parent, colliders, center, b, c, d);
                }
            }

            lower = upper;
        }
    }

    private float[] RandomCuts(int count)
    {
        float[] cuts = new float[count + 1];
        cuts[0] = 0f;
        cuts[count] = 1f;

        for (int i = 1; i < count; i++)
            cuts[i] = (i + Random.Range(-0.32f, 0.32f)) / count;

        return cuts;
    }

    private void AddSplinter(
        Transform parent,
        List<Collider> colliders,
        Vector3 explosionCenter,
        Vector3 localA,
        Vector3 localB,
        Vector3 localC)
    {
        Vector3 a = transform.TransformPoint(localA);
        Vector3 b = transform.TransformPoint(localB);
        Vector3 c = transform.TransformPoint(localC);

        Vector3 cross = Vector3.Cross(b - a, c - a);

        if (cross.sqrMagnitude < 0.000000001f)
            return;

        Vector3 center = (a + b + c) / 3f;

        float worldSize = Mathf.Max(
            transform.TransformVector(
                Vector3.right * localBounds.size.x).magnitude,
            transform.TransformVector(
                Vector3.up * localBounds.size.y).magnitude,
            transform.TransformVector(
                Vector3.forward * localBounds.size.z).magnitude);

        Vector3 thickness =
            cross.normalized * Mathf.Max(worldSize * 0.004f, 0.0007f);

        a -= center;
        b -= center;
        c -= center;

        Mesh mesh = new Mesh();
        mesh.name = "Small Triangular Wood";

        mesh.vertices = new Vector3[]
        {
            a - thickness, b - thickness, c - thickness,
            a + thickness, b + thickness, c + thickness
        };

        mesh.triangles = new int[]
        {
            0, 2, 1,
            3, 4, 5,
            0, 1, 4, 0, 4, 3,
            1, 2, 5, 1, 5, 4,
            2, 0, 3, 2, 3, 5
        };

        mesh.uv = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(0.25f, 0f),
            new Vector2(0.05f, 0.6f),
            new Vector2(0f, 0f),
            new Vector2(0.25f, 0f),
            new Vector2(0.05f, 0.6f)
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        meshes.Add(mesh);

        GameObject piece = new GameObject("Wood Splinter");
        piece.transform.SetParent(parent, false);
        piece.transform.position = center;

        piece.AddComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer renderer = piece.AddComponent<MeshRenderer>();

        // Use the exact same Material asset as the chest.
        renderer.sharedMaterial = CurrentWood();
        renderer.shadowCastingMode = ShadowCastingMode.Off;

        MeshCollider col = piece.AddComponent<MeshCollider>();
        col.sharedMesh = mesh;
        col.convex = true;
        colliders.Add(col);

        Rigidbody body = piece.AddComponent<Rigidbody>();
        body.mass = Random.Range(0.015f, 0.045f);
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        Vector3 outward = center - explosionCenter;
        outward.y *= 0.3f;

        float speed = Mathf.Max(scatterSpeed, 0f);

        body.linearVelocity =
            outward.normalized * Random.Range(speed * 0.3f, speed) +
            Vector3.up * Random.Range(0.08f, 0.25f);

        body.angularVelocity =
            Random.insideUnitSphere * Random.Range(3f, 7f);
    }

    private void CreateDust(Vector3 position)
    {
        Shader shader = dustShader != null
            ? dustShader
            : Shader.Find("BuriedOutside/ChestSoftDust");

        if (shader == null)
        {
            Debug.LogWarning(
                "Assign ChestSoftDust.shader to Dust Shader.",
                this);
            return;
        }

        Material material = new Material(shader);

        if (material.HasProperty("_Color"))
        {
            material.SetColor(
                "_Color",
                new Color(0.025f, 0.02f, 0.018f, 1f));
        }

        effectMaterials.Add(material);

        GameObject dust = new GameObject("Soft Dark Chest Dust");
        dust.transform.position = position;
        effectObjects.Add(dust);

        ParticleSystem particles = dust.AddComponent<ParticleSystem>();

        particles.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear);

        float size = Mathf.Max(
            transform.TransformVector(
                Vector3.right * localBounds.size.x).magnitude,
            0.1f);

        var main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.2f;

        main.startLifetime =
            new ParticleSystem.MinMaxCurve(0.65f, 1.1f);

        main.startSpeed =
            new ParticleSystem.MinMaxCurve(size * 0.03f, size * 0.1f);

        main.startSize =
            new ParticleSystem.MinMaxCurve(size * 0.12f, size * 0.27f);

        main.startRotation =
            new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 28;

        var emission = particles.emission;
        emission.enabled = false;

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(
            size * 0.65f, size * 0.3f, size * 0.45f);

        var color = particles.colorOverLifetime;
        color.enabled = true;

        Gradient gradient = new Gradient();

        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(dustOpacity, 0.12f),
                new GradientAlphaKey(0f, 1f)
            });

        color.color = gradient;

        var growth = particles.sizeOverLifetime;
        growth.enabled = true;
        growth.size = new ParticleSystem.MinMaxCurve(
            1f, AnimationCurve.Linear(0f, 0.65f, 1f, 1.7f));

        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(size * 0.08f);

        ParticleSystemRenderer renderer =
            particles.GetComponent<ParticleSystemRenderer>();

        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        particles.Play();
        particles.Emit(22);

        Destroy(dust, 1.5f);
        Destroy(material, 1.6f);
    }

    private void OnDisable()
    {
        if (chestOutline != null)
            chestOutline.enabled = false;
    }

    private void OnDestroy()
    {
        foreach (GameObject effect in effectObjects)
        {
            if (effect != null)
                Destroy(effect);
        }

        foreach (Mesh mesh in meshes)
        {
            if (mesh != null)
                Destroy(mesh);
        }

        foreach (Material material in effectMaterials)
        {
            if (material != null)
                Destroy(material);
        }

        if (outlineMaterial != null)
            Destroy(outlineMaterial);

        if (runtimeWood != null)
            Destroy(runtimeWood);

        if (runtimeWoodTexture != null)
            Destroy(runtimeWoodTexture);
    }
}