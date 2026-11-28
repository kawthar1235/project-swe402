using UnityEngine;

[DisallowMultipleComponent]
public class MonsterChaseSound : MonoBehaviour
{
    [Header("Monster sound")]
    public AudioClip monsterSound;

    public float startSeconds = 33f;
    public float endSeconds = 230f;
    public bool loopSegment = true;

    [Range(0f, 1f)]
    public float volume = 0.15f;

    [Header("3D distance")]
    public float minimumDistance = 1f;
    public float maximumDistance = 8f;

    [Header("Wall blocking")]
    public Camera playerCamera;
    public Transform playerRoot;

    [Tooltip("Layers containing walls, doors and other solid obstacles.")]
    public LayerMask obstructionLayers = ~0;

    [Tooltip("How quickly the sound returns after the monster is uncovered.")]
    public float fadeInSeconds = 0.4f;

    private readonly AudioSource[] speakers = new AudioSource[2];

    private Renderer[] visuals;
    private MonsterChase chase;

    private bool armed;
    private bool playing;
    private bool loadRequested;

    private float audibleAmount;

    private int startSample;
    private double segmentDuration;
    private double nextStart;
    private int nextSpeaker;

    private void Awake()
    {
        chase = GetComponent<MonsterChase>();
        visuals = GetComponentsInChildren<Renderer>(true);

        ResolveReferences();

        for (int i = 0; i < speakers.Length; i++)
        {
            AudioSource source =
                gameObject.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.dopplerLevel = 0f;
            source.pitch = 1f;
            source.volume = 0f;

            speakers[i] = source;
        }
    }

    private void ResolveReferences()
    {
        playerCamera =
            ChestPuzzleTools.ResolveCamera(playerCamera);

        if (playerRoot == null && playerCamera != null)
            playerRoot = ChestPuzzleTools.Player(playerCamera);
    }

    public void BeginChaseSound()
    {
        // Called by the room exit trigger.
        if (armed)
            return;

        audibleAmount = 0f;
        armed = true;
    }

    public void StopChaseSound()
    {
        armed = false;
        audibleAmount = 0f;
        StopSources();
    }

    private bool TryGetModelBounds(out Bounds bounds)
    {
        bounds = default;
        bool found = false;

        foreach (Renderer part in visuals)
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

        return found;
    }

    private bool HasClearPath(Vector3 target)
    {
        if (playerCamera == null)
            return false;

        Vector3 origin = playerCamera.transform.position;
        Vector3 difference = target - origin;
        float distance = difference.magnitude;

        if (distance < 0.01f)
            return true;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            difference / distance,
            distance,
            obstructionLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (RaycastHit hit in hits)
        {
            Transform hitTransform = hit.collider.transform;

            // Ignore the monster's own colliders.
            if (hitTransform.IsChildOf(transform))
                continue;

            // Ignore the player's body and held objects.
            if (playerRoot != null &&
                hitTransform.IsChildOf(playerRoot))
                continue;

            if (hitTransform.IsChildOf(playerCamera.transform))
                continue;

            // A wall, closed door or another solid object blocks sound.
            return false;
        }

        return true;
    }

    private bool CanHearMonster(Bounds bounds)
    {
        // Check several body heights so a small obstacle
        // does not silence the entire monster.
        Vector3 center = bounds.center;
        float offset = bounds.extents.y * 0.65f;

        return HasClearPath(center) ||
               HasClearPath(center + Vector3.up * offset) ||
               HasClearPath(center - Vector3.up * offset);
    }

    private void ApplyAudioSettings(float gain)
    {
        foreach (AudioSource source in speakers)
        {
            if (source == null)
                continue;

            source.volume = Mathf.Clamp01(volume) * gain;

            source.minDistance =
                Mathf.Max(0.1f, minimumDistance);

            source.maxDistance = Mathf.Max(
                source.minDistance + 0.1f,
                maximumDistance
            );
        }
    }

    private void Update()
    {
        if (!armed)
        {
            ApplyAudioSettings(0f);
            return;
        }

        if (playerCamera == null)
            ResolveReferences();

        if (chase != null && !chase.enabled)
        {
            audibleAmount = 0f;
            ApplyAudioSettings(0f);
            StopSources();
            return;
        }

        if (!TryGetModelBounds(out Bounds bounds))
        {
            audibleAmount = 0f;
            ApplyAudioSettings(0f);
            StopSources();
            return;
        }

        bool uncovered = CanHearMonster(bounds);

        if (!uncovered)
        {
            // Immediately silence sound through a wall.
            audibleAmount = 0f;
        }
        else
        {
            audibleAmount = Mathf.MoveTowards(
                audibleAmount,
                1f,
                Time.unscaledDeltaTime /
                Mathf.Max(0.01f, fadeInSeconds)
            );
        }

        ApplyAudioSettings(audibleAmount);

        if (monsterSound == null)
            return;

        if (monsterSound.loadState == AudioDataLoadState.Failed)
        {
            Debug.LogError(
                "Monster audio failed to load.",
                this
            );

            StopChaseSound();
            return;
        }

        if (monsterSound.loadState != AudioDataLoadState.Loaded)
        {
            if (!loadRequested)
            {
                loadRequested = true;
                monsterSound.LoadAudioData();
            }

            return;
        }

        if (!playing)
        {
            // Wait until uncovered before starting the clip.
            if (uncovered)
                StartSegment();

            return;
        }

        double now = AudioSettings.dspTime;

        if (!loopSegment)
        {
            if (now >= nextStart)
                StopChaseSound();

            return;
        }

        if (now >= nextStart - 0.2)
        {
            double scheduledTime =
                System.Math.Max(nextStart, now + 0.03);

            ScheduleSegment(nextSpeaker, scheduledTime);

            nextSpeaker = 1 - nextSpeaker;
            nextStart = scheduledTime + segmentDuration;
        }
    }

    private void StartSegment()
    {
        float start = Mathf.Max(0f, startSeconds);
        float end = Mathf.Min(endSeconds, monsterSound.length);

        if (end <= start)
        {
            Debug.LogWarning(
                "Monster sound: check Start Seconds and End Seconds.",
                this
            );

            StopChaseSound();
            return;
        }

        startSample = Mathf.Clamp(
            Mathf.RoundToInt(start * monsterSound.frequency),
            0,
            monsterSound.samples - 1
        );

        int endSample = Mathf.Clamp(
            Mathf.RoundToInt(end * monsterSound.frequency),
            startSample + 1,
            monsterSound.samples
        );

        segmentDuration =
            (endSample - startSample) /
            (double)monsterSound.frequency;

        double firstStart = AudioSettings.dspTime + 0.05;

        ScheduleSegment(0, firstStart);

        nextSpeaker = 1;
        nextStart = firstStart + segmentDuration;
        playing = true;
    }

    private void ScheduleSegment(int index, double startTime)
    {
        AudioSource source = speakers[index];

        source.Stop();
        source.clip = monsterSound;
        source.pitch = 1f;
        source.timeSamples = startSample;

        source.PlayScheduled(startTime);
        source.SetScheduledEndTime(
            startTime + segmentDuration
        );
    }

    private void StopSources()
    {
        foreach (AudioSource source in speakers)
        {
            if (source != null)
            {
                source.Stop();
                source.volume = 0f;
            }
        }

        playing = false;
    }

    private void OnDisable()
    {
        StopChaseSound();
    }

    private void OnDestroy()
    {
        foreach (AudioSource source in speakers)
        {
            if (source != null)
                Destroy(source);
        }
    }
}