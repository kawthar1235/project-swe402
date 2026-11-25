
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("Sliders")]
    public Slider volumeSlider;
    public Slider brightnessSlider;
    public Slider speedSlider;

    void Start()
    {
        volumeSlider.value =
            PlayerPrefs.GetFloat("Volume", 1f);

        brightnessSlider.value =
            PlayerPrefs.GetFloat("Brightness", 0.5f);

        speedSlider.value =
            PlayerPrefs.GetFloat("MovementSpeed", 0.5f);

        AudioListener.volume = volumeSlider.value;
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;

        PlayerPrefs.SetFloat("Volume", value);
        PlayerPrefs.Save();
    }

    public void SetBrightness(float value)
    {
        PlayerPrefs.SetFloat("Brightness", value);
        PlayerPrefs.Save();
    }

    public void SetMovementSpeed(float value)
    {
        PlayerPrefs.SetFloat("MovementSpeed", value);
        PlayerPrefs.Save();
    }
}
