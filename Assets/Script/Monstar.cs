using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Monster : MonoBehaviour
{
    [Header("Existing references")]
    public GameManager gameManager;
    public Transform corridorRestartPoint;

    [Header("Flashlight")]
    public Light flashlightLight;
    public float exposureSeconds = 0.25f;
    public float defeatDistance = 12f;
    public LayerMask obstructionLayers = ~0;

    [Header("Death sound - seconds 6 to 10")]
    public AudioClip deathSound;

    [Range(0f, 1f)]
    public float deathVolume = 0.3f;

    public float deathSoundStart = 6f;
    public float deathSoundEnd = 10f;

    [Header("Disappearance timing")]
    public float smokeBeforeHiding = 0.9f;
    public float victoryDelayAfterHiding = 1.6f;

    [Header("Smoke")]
    public Color smokeColor =
        new Color(0.06f, 0.055f, 0.05f, 0.45f);

    [Range(20, 250)]
    public int smokeParticleCount = 150;

    public float smokeMinimumLifetime = 2.5f;
    public float smokeMaximumLifetime = 4.5f;

    private bool defeated;
    private float exposure;

    private Renderer[] modelRenderers;
    private Transform playerRoot;

    private void Awake()
    {
        modelRenderers =
            GetComponentsInChildren<Renderer>(true);
    }

    private void Start()
    {
        if (gameManager == null)
        {
            gameManager =
                Object.FindFirstObjectByType<GameManager>();
        }

        PlayerMovement player =
            Object.FindFirstObjectByType<PlayerMovement>();

        if (player != null)
            playerRoot = player.transform;

        if (flashlightLight == null)
        {
            Debug.LogWarning(
                "Monster: assign the flashlight Spot Light.",
                this
            );
        }
    }

    private void Update()
    {
        if (defeated || Time.timeScale <= 0f)
            return;

        if (IsFlashlightOnMonster())
        {
            exposure += Time.deltaTime;

            if (exposure >= Mathf.Max(0.05f, exposureSeconds))
                DefeatMonster();
        }
        else
        {
            exposure = 0f;
        }
    }

    private Bounds GetMonsterBounds()
    {
        Bounds bounds = new Bounds(
            transform.position + Vector3.up,
            new Vector3(0.8f, 2f, 0.8f)
        );

        bool found = false;

        foreach (Renderer part in modelRenderers)
        {
            if (part == null ||
                !part.enabled ||
                !part.gameObject.activeInHierarchy)
                continue;

            if (!(part is MeshRenderer) &&
                !(part is SkinnedMeshRenderer))
                continue;

            if (!found)
            {
                bounds = part.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(part.bounds);
            }
        }

        return bounds;
    }

    private bool IsFlashlightOnMonster()
    {
        if (flashlightLight == null ||
            !flashlightLight.isActiveAndEnabled ||
            flashlightLight.intensity <= 0.01f)
            return false;

        if (gameManager == null || !gameManager.hasFlashlight)
            return false;

        if (flashlightLight.type != LightType.Spot)
            return false;

        Bounds bounds = GetMonsterBounds();

        Vector3 center = bounds.center;
        float offset = bounds.extents.y * 0.55f;

        return IsPointLit(center) ||
               IsPointLit(center + Vector3.up * offset) ||
               IsPointLit(center - Vector3.up * offset);
    }

    private bool IsPointLit(Vector3 target)
    {
        Vector3 origin = flashlightLight.transform.position;
        Vector3 difference = target - origin;
        float distance = difference.magnitude;

        float allowedDistance = Mathf.Min(
            Mathf.Max(0f, defeatDistance),
            flashlightLight.range
        );

        if (distance > allowedDistance)
            return false;

        if (distance < 0.01f)
            return true;

        Vector3 direction = difference / distance;

        float angle = Vector3.Angle(
            flashlightLight.transform.forward,
            direction
        );

        if (angle > flashlightLight.spotAngle * 0.5f)
            return false;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction,
            distance,
            obstructionLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (RaycastHit hit in hits)
        {
            Transform hitTransform = hit.collider.transform;

            if (hitTransform.IsChildOf(transform))
                continue;

            if (playerRoot != null &&
                hitTransform.IsChildOf(playerRoot))
                continue;

            FlashlightPickup flashlight =
                hit.collider.GetComponentInParent<FlashlightPickup>();

            if (flashlight != null && flashlight.IsHeld)
                continue;

            // Walls and other solid objects block the beam.
            return false;
        }

        return true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (defeated)
            return;

        PlayerMovement player =
            collision.collider.GetComponentInParent<PlayerMovement>();

        if (player == null)
            return;

        if (corridorRestartPoint == null)
        {
            Debug.LogWarning(
                "Corridor Restart Point is not assigned.",
                this
            );
            return;
        }

        exposure = 0f;

        Rigidbody playerRb = player.GetComponent<Rigidbody>();

        if (playerRb != null)
        {
            if (!playerRb.isKinematic)
            {
                playerRb.linearVelocity = Vector3.zero;
                playerRb.angularVelocity = Vector3.zero;
            }

            playerRb.position = corridorRestartPoint.position;
            playerRb.rotation = corridorRestartPoint.rotation;
        }
        else
        {
            player.transform.SetPositionAndRotation(
                corridorRestartPoint.position,
                corridorRestartPoint.rotation
            );
        }
    }

    public void DefeatMonster()
    {
        if (defeated)
            return;

        defeated = true;
        StartCoroutine(DisappearSequence());
    }

    private IEnumerator DisappearSequence()
    {
        Bounds bounds = GetMonsterBounds();

        MonsterChase chase = GetComponent<MonsterChase>();

        if (chase != null)
            chase.enabled = false;

        MonsterChaseSound chaseSound =
            GetComponent<MonsterChaseSound>();

        if (chaseSound != null)
            chaseSound.StopChaseSound();

        Rigidbody body = GetComponent<Rigidbody>();

        if (body != null && !body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }

        foreach (Collider col in
                 GetComponentsInChildren<Collider>(true))
        {
            col.enabled = false;
        }

        foreach (Animator animator in
                 GetComponentsInChildren<Animator>(true))
        {
            animator.enabled = false;
        }

        PlayDeathSound(bounds.center);
        CreateSmoke(bounds);

        // Smoke builds around the monster first.
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, smokeBeforeHiding)
        );

        foreach (Renderer part in modelRenderers)
        {
            if (part != null)
                part.enabled = false;
        }

        RoomAmbienceManager ambience =
            Object.FindFirstObjectByType<RoomAmbienceManager>();

        if (ambience != null)
            ambience.OnMonsterDefeated();

        // Keep the result panel from hiding the effect immediately.
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, victoryDelayAfterHiding)
        );

        gameObject.SetActive(false);

        // if (gameManager != null)
        //     gameManager.WinGame();
        yield return new WaitForSecondsRealtime(
    Mathf.Max(0f, victoryDelayAfterHiding)
);

gameObject.SetActive(false);
    }

    private void PlayDeathSound(Vector3 position)
    {
        if (deathSound == null)
            return;

        float start = Mathf.Max(0f, deathSoundStart);
        float end = Mathf.Min(deathSoundEnd, deathSound.length);

        if (end <= start)
        {
            Debug.LogWarning(
                "Monster death sound: check start/end seconds.",
                this
            );
            return;
        }

        GameObject soundObject =
            new GameObject("Monster Death Sound");

        soundObject.transform.position = position;

        AudioSource source =
            soundObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.clip = deathSound;
        source.pitch = 1f;
        source.volume = Mathf.Clamp01(deathVolume);

        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 2f;
        source.maxDistance = 18f;
        source.dopplerLevel = 0f;

        source.timeSamples = Mathf.Clamp(
            Mathf.RoundToInt(start * deathSound.frequency),
            0,
            deathSound.samples - 1
        );

        double scheduledStart = AudioSettings.dspTime + 0.05;
        float duration = end - start;

        source.PlayScheduled(scheduledStart);
        source.SetScheduledEndTime(
            scheduledStart + duration
        );

        MonsterSmokeLifetime cleanup =
            soundObject.AddComponent<MonsterSmokeLifetime>();

        cleanup.Initialize(null, null, duration + 0.5f);
    }

    private void CreateSmoke(Bounds bounds)
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Particles/Unlit");

        if (shader == null || !shader.isSupported)
        {
            Debug.LogWarning(
                "Monster smoke: URP Particles/Unlit shader unavailable.",
                this
            );
            return;
        }

        const int resolution = 64;

        Texture2D texture = new Texture2D(
            resolution,
            resolution,
            TextureFormat.RGBA32,
            false
        );

        texture.name = "Monster Soft Smoke";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float px = (x + 0.5f) / resolution * 2f - 1f;
                float py = (y + 0.5f) / resolution * 2f - 1f;

                float radial = Mathf.Clamp01(
                    1f - px * px - py * py
                );

                float noise = Mathf.PerlinNoise(
                    x * 0.11f,
                    y * 0.11f
                );

                float alpha =
                    radial * radial *
                    Mathf.Lerp(0.4f, 1f, noise);

                pixels[y * resolution + x] =
                    new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);

        Material material = new Material(shader);
        material.name = "Monster Smoke Material";

        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);

        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_Cull", (float)CullMode.Off);

        material.SetFloat(
            "_SrcBlend",
            (float)BlendMode.SrcAlpha
        );

        material.SetFloat(
            "_DstBlend",
            (float)BlendMode.OneMinusSrcAlpha
        );

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.DisableKeyword("_ALPHAMODULATE_ON");

        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;

        GameObject effect =
            new GameObject("Monster Disappearance Smoke");

        effect.transform.position = bounds.center;

        ParticleSystem particles =
            effect.AddComponent<ParticleSystem>();

        particles.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        float bodyHeight = Mathf.Max(0.5f, bounds.size.y);

        float minimumLifetime =
            Mathf.Max(0.2f, smokeMinimumLifetime);

        float maximumLifetime =
            Mathf.Max(minimumLifetime, smokeMaximumLifetime);

        var main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.2f;

        main.startLifetime =
            new ParticleSystem.MinMaxCurve(
                minimumLifetime,
                maximumLifetime
            );

        main.startSpeed =
            new ParticleSystem.MinMaxCurve(0.08f, 0.3f);

        main.startSize = new ParticleSystem.MinMaxCurve(
            bodyHeight * 0.25f,
            bodyHeight * 0.55f
        );

        main.startRotation =
            new ParticleSystem.MinMaxCurve(
                0f,
                Mathf.PI * 2f
            );

        main.startColor = smokeColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.useUnscaledTime = true;

        int count = Mathf.Clamp(smokeParticleCount, 20, 250);
        main.maxParticles = count + 10;

        var emission = particles.emission;
        emission.enabled = false;

        var shape = particles.shape;
        shape.enabled = false;

        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;

        velocity.x =
            new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);

        velocity.y =
            new ParticleSystem.MinMaxCurve(0.15f, 0.4f);

        velocity.z =
            new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);

        var sizeOverLife = particles.sizeOverLifetime;
        sizeOverLife.enabled = true;

        sizeOverLife.size = new ParticleSystem.MinMaxCurve(
            1f,
            AnimationCurve.Linear(0f, 0.65f, 1f, 1.6f)
        );

        Gradient fade = new Gradient();

        fade.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.1f),
                new GradientAlphaKey(0.8f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        var colorOverLife = particles.colorOverLifetime;
        colorOverLife.enabled = true;

        colorOverLife.color =
            new ParticleSystem.MinMaxGradient(fade);

        ParticleSystemRenderer particleRenderer =
            particles.GetComponent<ParticleSystemRenderer>();

        particleRenderer.sharedMaterial = material;
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;

        MonsterSmokeLifetime cleanup =
            effect.AddComponent<MonsterSmokeLifetime>();

        cleanup.Initialize(
            material,
            texture,
            maximumLifetime + 1f
        );

        particles.Play();

        for (int i = 0; i < count; i++)
        {
            ParticleSystem.EmitParams particle =
                new ParticleSystem.EmitParams();

            particle.position = bounds.center + new Vector3(
                Random.Range(-bounds.extents.x, bounds.extents.x),
                Random.Range(-bounds.extents.y, bounds.extents.y),
                Random.Range(-bounds.extents.z, bounds.extents.z)
            );

            particles.Emit(particle, 1);
        }
    }
}

// Keep this class in Monster.cs only.
public class MonsterSmokeLifetime : MonoBehaviour
{
    private Material smokeMaterial;
    private Texture2D smokeTexture;
    private float remaining = 6f;

    public void Initialize(
        Material material,
        Texture2D texture,
        float lifetime = 6f)
    {
        smokeMaterial = material;
        smokeTexture = texture;
        remaining = Mathf.Max(0.1f, lifetime);
    }

    private void Update()
    {
        remaining -= Time.unscaledDeltaTime;

        if (remaining <= 0f)
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (smokeMaterial != null)
            Destroy(smokeMaterial);

        if (smokeTexture != null)
            Destroy(smokeTexture);
    }
}