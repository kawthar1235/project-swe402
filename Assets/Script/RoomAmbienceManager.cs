using System.Collections.Generic;
using UnityEngine;

public class RoomAmbienceManager : MonoBehaviour
{
    public enum Area
    {
        FirstRoom,
        Corridor,
        TVRoom
    }

    [Header("Background clips")]
    public AudioClip firstRoomSound;
    public AudioClip corridorSound;
    public AudioClip tvRoomSound;

    [Header("Volumes")]
    [Range(0f, 1f)]
    public float firstRoomVolume = 0.18f;

    [Range(0f, 1f)]
    public float corridorVolume = 0.18f;

    [Range(0f, 1f)]
    public float tvRoomVolume = 0.2f;

    [Range(0f, 1f)]
    public float masterVolume = 1f;

    [Header("Transitions")]
    public float fadeSeconds = 2f;
    public Area initialArea = Area.FirstRoom;

    private readonly AudioSource[] sources =
        new AudioSource[3];

    private readonly float[] weights = new float[3];
    private readonly float[] fromWeights = new float[3];
    private readonly float[] targetWeights = new float[3];

    private readonly List<RoomAmbienceZone> zones =
        new List<RoomAmbienceZone>();

    private Area currentArea;
    private double transitionStart;
    private float transitionDuration;

    private bool hasEnteredZone;
    private bool tvChallengeStarted;
    private bool monsterDefeated;

    private void Awake()
    {
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source =
                gameObject.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.spatialBlend = 0f;

            // All backgrounds loop, including the TV challenge.
            source.loop = true;
            source.volume = 0f;

            sources[i] = source;
        }

        sources[(int)Area.FirstRoom].clip = firstRoomSound;
        sources[(int)Area.Corridor].clip = corridorSound;
        sources[(int)Area.TVRoom].clip = tvRoomSound;
    }

    private void OnEnable()
    {
        tvChallengeStarted = false;
        monsterDefeated = false;
        hasEnteredZone = false;

        for (int i = 0; i < sources.Length; i++)
        {
            weights[i] = 0f;
            fromWeights[i] = 0f;
            targetWeights[i] = 0f;

            AudioSource source = sources[i];

            if (source == null)
                continue;

            source.Stop();
            source.volume = 0f;
            source.loop = true;

            // Keep the two normal backgrounds ready.
            if (i != (int)Area.TVRoom && source.clip != null)
                source.Play();
        }

        BeginTransition(initialArea, 0f);
    }

    public void RegisterZone(RoomAmbienceZone zone)
    {
        if (zone == null)
            return;

        hasEnteredZone = true;

        if (!zones.Contains(zone))
            zones.Add(zone);
    }

    public void UnregisterZone(RoomAmbienceZone zone)
    {
        zones.Remove(zone);
    }

    private Area GetPlayerArea()
    {
        Area result = hasEnteredZone
            ? Area.Corridor
            : initialArea;

        int priority = int.MinValue;

        foreach (RoomAmbienceZone zone in zones)
        {
            if (zone == null ||
                !zone.isActiveAndEnabled ||
                !zone.IsPlayerInside)
                continue;

            if (zone.priority >= priority)
            {
                priority = zone.priority;
                result = zone.area;
            }
        }

        return result;
    }

    // Starts automatically on the first visit to the TV room.
    // Can also be called by the gameplay challenge script.
    public void BeginTVChallenge()
    {
        if (tvChallengeStarted || monsterDefeated)
            return;

        tvChallengeStarted = true;

        AudioSource source = sources[(int)Area.TVRoom];

        if (source != null && source.clip != null)
        {
            source.loop = true;
            source.Play();
        }
    }

    // Call this ONLY when the monster is defeated.
    public void OnMonsterDefeated()
    {
        if (monsterDefeated)
            return;

        monsterDefeated = true;
        tvChallengeStarted = false;

        Area playerArea = GetPlayerArea();

        Area desired = playerArea == Area.FirstRoom
            ? Area.FirstRoom
            : Area.Corridor;

        BeginTransition(desired, fadeSeconds);
    }

    private void Update()
    {
        Area playerArea = GetPlayerArea();

        if (playerArea == Area.TVRoom &&
            !tvChallengeStarted &&
            !monsterDefeated)
        {
            BeginTVChallenge();
        }

        Area desired;

        if (playerArea == Area.FirstRoom)
        {
            // The first room keeps its own background.
            desired = Area.FirstRoom;
        }
        else if (tvChallengeStarted && !monsterDefeated)
        {
            // TV background takes over BOTH TV room and corridor.
            desired = Area.TVRoom;
        }
        else
        {
            // After victory, both areas use corridor ambience.
            desired = Area.Corridor;
        }

        if (desired != currentArea)
            BeginTransition(desired, fadeSeconds);

        float t = transitionDuration <= 0f
            ? 1f
            : Mathf.Clamp01(
                (float)(
                    AudioSettings.dspTime - transitionStart
                ) / transitionDuration
            );

        t = Mathf.SmoothStep(0f, 1f, t);

        for (int i = 0; i < sources.Length; i++)
        {
            weights[i] = Mathf.Lerp(
                fromWeights[i],
                targetWeights[i],
                t
            );

            float areaVolume = i == (int)Area.FirstRoom
                ? firstRoomVolume
                : i == (int)Area.Corridor
                    ? corridorVolume
                    : tvRoomVolume;

            if (sources[i] != null)
            {
                sources[i].volume =
                    weights[i] *
                    Mathf.Clamp01(areaVolume) *
                    Mathf.Clamp01(masterVolume);
            }
        }

        // Stop the TV clip after its victory fade has finished.
        AudioSource tvSource = sources[(int)Area.TVRoom];

        if (monsterDefeated &&
            t >= 1f &&
            weights[(int)Area.TVRoom] <= 0.0001f &&
            tvSource != null &&
            tvSource.isPlaying)
        {
            tvSource.Stop();
        }
    }

    private void BeginTransition(Area area, float duration)
    {
        currentArea = area;
        transitionStart = AudioSettings.dspTime;
        transitionDuration = Mathf.Max(0f, duration);

        for (int i = 0; i < sources.Length; i++)
        {
            fromWeights[i] = weights[i];
            targetWeights[i] = i == (int)area ? 1f : 0f;
        }
    }

    private void OnDisable()
    {
        foreach (AudioSource source in sources)
        {
            if (source != null)
                source.Stop();
        }
    }

    private void OnDestroy()
    {
        foreach (AudioSource source in sources)
        {
            if (source != null)
                Destroy(source);
        }
    }
}