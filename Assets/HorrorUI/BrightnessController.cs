
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BrightnessController : MonoBehaviour
{
    public Volume globalVolume;

    private ColorAdjustments colorAdjustments;

    void Start()
    {
        if (globalVolume == null)
        {
            Debug.LogError("Global Volume is not assigned!");
            return;
        }

        if (globalVolume.profile.TryGet(out colorAdjustments))
        {
            float brightness =
                PlayerPrefs.GetFloat("Brightness", 0.5f);

            ApplyBrightness(brightness);
        }
    }

    public void ApplyBrightness(float value)
    {
        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.overrideState = true;

            colorAdjustments.postExposure.value =
                Mathf.Lerp(-1f, 1f, Mathf.Clamp01(value));
        }
    }
}
