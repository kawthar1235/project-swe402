using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class ReverseAudioPuzzle : MonoBehaviour
{
    public AudioClip originalClip;

    private AudioSource audioSource;
    private AudioClip reversedClip;

    public bool IsPlaying =>
        audioSource != null && audioSource.isPlaying;

    public float Volume
    {
        get => audioSource != null ? audioSource.volume : 1f;

        set
        {
            if (audioSource != null)
                audioSource.volume = Mathf.Clamp01(value);
        }
    }

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.Stop();
    }

    IEnumerator Start()
    {
        if (originalClip == null)
        {
            Debug.LogError("Assign Original Clip.", this);
            yield break;
        }

        if (originalClip.loadState == AudioDataLoadState.Unloaded)
            originalClip.LoadAudioData();

        while (originalClip.loadState == AudioDataLoadState.Loading)
            yield return null;

        if (originalClip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogError("Could not load the audio.", this);
            yield break;
        }

        int channels = originalClip.channels;
        int samples = originalClip.samples;

        float[] originalData = new float[samples * channels];

        if (!originalClip.GetData(originalData, 0))
        {
            Debug.LogError(
                "Set Load Type to Decompress On Load.",
                this
            );
            yield break;
        }

        float[] reversedData = new float[originalData.Length];

        for (int i = 0; i < samples; i++)
        {
            for (int c = 0; c < channels; c++)
            {
                reversedData[i * channels + c] =
                    originalData[(samples - 1 - i) * channels + c];
            }
        }

        reversedClip = AudioClip.Create(
            "SecretCode_Backwards",
            samples,
            channels,
            originalClip.frequency,
            false
        );

        reversedClip.SetData(reversedData, 0);
    }

    public bool PlayNormal()
    {
        return PlayClip(reversedClip);
    }

    public bool PlayReversed()
    {
        return PlayClip(originalClip);
    }

    private bool PlayClip(AudioClip clip)
    {
        if (audioSource == null ||
            reversedClip == null ||
            clip == null)
        {
            Debug.LogWarning("Audio is not ready. Check Original Clip.");
            return false;
        }

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();

        return true;
    }

    public void StopAudio()
    {
        if (audioSource != null)
            audioSource.Stop();
    }

    void OnDisable()
    {
        StopAudio();
    }

    void OnDestroy()
    {
        if (reversedClip != null)
            Destroy(reversedClip);
    }
}