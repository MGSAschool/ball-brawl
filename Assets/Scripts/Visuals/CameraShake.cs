using UnityEngine;

[DefaultExecutionOrder(1000)]
public class CameraShake : MonoBehaviour
{
    [SerializeField] private float maxOffset = 0.8f;
    [SerializeField] private float maxRoll = 2f;
    [SerializeField] private float frequency = 22f;
    [SerializeField] private float recovery = 1.8f;

    private float trauma;
    private Vector3 appliedOffset;
    private Quaternion appliedRoll = Quaternion.identity;

    public void Add(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }

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

        float power = trauma * trauma;
        float t = Time.time * frequency;
        float sideways = Mathf.PerlinNoise(t, 0f) * 2f - 1f;
        float upDown = Mathf.PerlinNoise(0f, t) * 2f - 1f;
        float roll = Mathf.PerlinNoise(t, t) * 2f - 1f;

        appliedOffset = (transform.right * sideways + transform.up * upDown) * (maxOffset * power);
        appliedRoll = Quaternion.Euler(0f, 0f, roll * maxRoll * power);
        transform.position += appliedOffset;
        transform.rotation *= appliedRoll;
    }
}
