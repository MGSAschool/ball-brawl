using UnityEngine;

// Short camera shake for big hits and explosions. Sits on top of ArenaCamera's framing: the
// offset is taken back off before ArenaCamera moves (Update) and added again after (LateUpdate),
// so its smoothing never sees it. Added to the camera by Vfx.Shake when first needed.
[DefaultExecutionOrder(1000)]
public class CameraShake : MonoBehaviour
{
    [SerializeField] private float maxOffset = 0.8f;
    [SerializeField] private float maxRoll = 2f;
    [SerializeField] private float frequency = 22f;
    [Tooltip("How much shake fades per second.")]
    [SerializeField] private float recovery = 1.8f;

    private float trauma;
    private Vector3 appliedOffset;
    private Quaternion appliedRoll = Quaternion.identity;

    public void Add(float amount) => trauma = Mathf.Clamp01(trauma + amount);

    private void Update()
    {
        transform.position -= appliedOffset;
        transform.rotation *= Quaternion.Inverse(appliedRoll);
        appliedOffset = Vector3.zero;
        appliedRoll = Quaternion.identity;
    }

    private void LateUpdate()
    {
        if (trauma <= 0f) return;
        trauma = Mathf.Max(0f, trauma - recovery * Time.deltaTime);

        // Squared so small shakes stay subtle and big ones kick
        float power = trauma * trauma;
        float t = Time.time * frequency;
        appliedOffset = (transform.right * (Mathf.PerlinNoise(t, 0f) * 2f - 1f) +
                         transform.up * (Mathf.PerlinNoise(0f, t) * 2f - 1f)) * (maxOffset * power);
        appliedRoll = Quaternion.Euler(0f, 0f, (Mathf.PerlinNoise(t, t) * 2f - 1f) * maxRoll * power);
        transform.position += appliedOffset;
        transform.rotation *= appliedRoll;
    }
}
