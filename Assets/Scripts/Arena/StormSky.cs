using UnityEngine;

public class StormSky : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float stormSunDim = 0.45f;

    private Light sun;
    private float normalIntensity;
    private bool dimming;
    private float dimStart;
    private float dimDuration;
    private float flashUntil;

    public void StartDimming(float duration)
    {
        sun = RenderSettings.sun;
        if (sun != null) normalIntensity = sun.intensity;
        dimming = true;
        dimStart = Time.time;
        dimDuration = duration;
    }

    public void FlashAndClear()
    {
        dimming = false;
        flashUntil = Time.time + 0.15f;
    }

    public void Restore()
    {
        if (sun != null) sun.intensity = normalIntensity;
        sun = null;
        dimming = false;
    }

    private void Update()
    {
        if (sun == null) return;

        if (Time.time < flashUntil)
        {
            sun.intensity = normalIntensity * 3f;
            return;
        }

        float target = normalIntensity;
        if (dimming)
        {
            float t = Mathf.Clamp01((Time.time - dimStart) / dimDuration);
            target = normalIntensity * Mathf.Lerp(1f, stormSunDim, t);
        }

        sun.intensity = Mathf.MoveTowards(sun.intensity, target, normalIntensity * 2f * Time.deltaTime);
        if (!dimming && Mathf.Approximately(sun.intensity, normalIntensity)) sun = null;
    }
}
