using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Light))]
[DisallowMultipleComponent]
public class HorrorLightFlicker : MonoBehaviour
{
    [Header("Time between flicker bursts")]
    public float minimumDelay = 4f;
    public float maximumDelay = 9f;

    [Header("Flashes per burst")]
    [Range(1, 6)]
    public int minimumFlashes = 2;

    [Range(1, 6)]
    public int maximumFlashes = 4;

    [Header("Flash duration")]
    public float minimumFlashTime = 0.08f;
    public float maximumFlashTime = 0.18f;

    private Light targetLight;

    private float originalIntensity;
    private bool originallyEnabled;

    private void OnEnable()
    {
        targetLight = GetComponent<Light>();

        originalIntensity = targetLight.intensity;
        originallyEnabled = targetLight.enabled;

        if (originallyEnabled)
            StartCoroutine(Flicker());
    }

    private IEnumerator Flicker()
    {
        // Different start time for each light.
        yield return new WaitForSeconds(
            Random.Range(0.2f, 1.5f));

        while (true)
        {
            int minimum = Mathf.Clamp(minimumFlashes, 1, 6);
            int maximum = Mathf.Clamp(maximumFlashes, minimum, 6);

            int flashes = Random.Range(minimum, maximum + 1);

            float shortest = Mathf.Max(0.03f, minimumFlashTime);
            float longest = Mathf.Max(shortest, maximumFlashTime);

            for (int i = 0; i < flashes; i++)
            {
                targetLight.enabled = false;

                yield return new WaitForSeconds(
                    Random.Range(shortest, longest));

                targetLight.enabled = true;

                targetLight.intensity =
                    originalIntensity * Random.Range(0.7f, 1.1f);

                yield return new WaitForSeconds(
                    Random.Range(shortest, longest));
            }

            targetLight.enabled = true;
            targetLight.intensity = originalIntensity;

            float delayMin = Mathf.Max(0.2f, minimumDelay);
            float delayMax = Mathf.Max(delayMin, maximumDelay);

            yield return new WaitForSeconds(
                Random.Range(delayMin, delayMax));
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        if (targetLight != null)
        {
            targetLight.intensity = originalIntensity;
            targetLight.enabled = originallyEnabled;
        }
    }
}