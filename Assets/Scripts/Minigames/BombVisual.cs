using UnityEngine;

// Dresses a spawned Bomb with the bomb model from GameArt: drops in with a bounce, then bobs,
// wobbles and pulses a warning glow under it so it reads as dangerous. Added by BombaClutchMode.
[RequireComponent(typeof(Bomb))]
public class BombVisual : MonoBehaviour
{
    [SerializeField] private float size = 1.8f;
    [SerializeField] private Color glowColor = new Color(1f, 0.35f, 0.15f, 0.5f);

    private Transform model;
    private Transform glow;
    private Material glowMaterial;
    private float spawnTime;
    private float phase;

    private void Start()
    {
        spawnTime = Time.time;
        phase = Random.value * 10f;

        var art = GameArt.Instance;
        if (art != null && art.bombModel != null)
        {
            // The prefab's sphere stays as the trigger; only its look is replaced
            if (TryGetComponent<MeshRenderer>(out var sphere)) sphere.enabled = false;
            model = ArtUtil.FitModel(art.bombModel, transform, size, "Bomb Model");
            model.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        // Pulsing danger ring on the floor
        glow = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
        Destroy(glow.GetComponent<Collider>());
        glow.name = "Danger Glow";
        glow.SetParent(transform, false);
        glow.rotation = Quaternion.Euler(90f, 0f, 0f);
        glowMaterial = new Material(Vfx.Additive());
        glowMaterial.color = glowColor;
        if (glowMaterial.HasProperty("_BaseColor")) glowMaterial.SetColor("_BaseColor", glowColor);
        var r = glow.GetComponent<MeshRenderer>();
        r.sharedMaterial = glowMaterial;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private void Update()
    {
        float age = Time.time - spawnTime;
        float t = Time.time + phase;

        if (model != null)
        {
            // Pop in with a little overshoot, then idle bob and wobble
            float pop = age < 0.35f ? Mathf.Sin(age / 0.35f * Mathf.PI * 0.5f) * (1f + 0.25f * Mathf.Sin(age / 0.35f * Mathf.PI)) : 1f;
            model.localScale = Vector3.one * Mathf.Max(0.01f, pop);
            model.position = transform.position + Vector3.up * (Mathf.Sin(t * 3f) * 0.12f);
            model.rotation = Quaternion.Euler(Mathf.Sin(t * 4f) * 8f, model.eulerAngles.y + 30f * Time.deltaTime, Mathf.Cos(t * 3.3f) * 8f);
        }

        if (glow != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 5f);
            glow.position = new Vector3(transform.position.x, transform.position.y - 1.1f, transform.position.z);
            glow.localScale = Vector3.one * Mathf.Lerp(3f, 4f, pulse) / Mathf.Max(0.01f, transform.lossyScale.x);
            var c = glowColor;
            c.a *= Mathf.Lerp(0.4f, 1f, pulse);
            glowMaterial.color = c;
            if (glowMaterial.HasProperty("_BaseColor")) glowMaterial.SetColor("_BaseColor", c);
        }
    }

    private void OnDestroy()
    {
        if (glowMaterial != null) Destroy(glowMaterial);
    }
}
