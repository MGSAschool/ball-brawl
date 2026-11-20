using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Bomb))]
public class BombVisual : MonoBehaviour
{
    [SerializeField] private float size = 1.8f;
    [SerializeField] private Color glowColor = new Color(1f, 0.35f, 0.15f, 0.5f);

    private Transform model;
    private Transform glow;
    private Material glowMaterial;
    private float spawnTime;
    private float randomOffset;

    private void Start()
    {
        spawnTime = Time.time;
        randomOffset = Random.value * 10f;

        GameArt art = GameArt.Instance;
        if (art != null && art.bombModel != null)
        {
            MeshRenderer sphere = GetComponent<MeshRenderer>();
            if (sphere != null) sphere.enabled = false;
            model = ArtUtil.FitModel(art.bombModel, transform, size, "Bomb Model");
            model.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        glowMaterial = new Material(VfxMaterials.Additive());
        ArtUtil.SetMaterialColor(glowMaterial, glowColor);
        glow = ArtUtil.MakeShape(PrimitiveType.Quad, "Danger Glow", transform, glowMaterial);
        glow.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void Update()
    {
        float age = Time.time - spawnTime;
        float t = Time.time + randomOffset;

        if (model != null)
        {
            float pop = 1f;
            if (age < 0.35f) pop = Mathf.Sin(age / 0.35f * Mathf.PI * 0.5f) * (1f + 0.25f * Mathf.Sin(age / 0.35f * Mathf.PI));
            model.localScale = Vector3.one * Mathf.Max(0.01f, pop);
            model.position = transform.position + Vector3.up * (Mathf.Sin(t * 3f) * 0.12f);

            float spin = model.eulerAngles.y + 30f * Time.deltaTime;
            model.rotation = Quaternion.Euler(Mathf.Sin(t * 4f) * 8f, spin, Mathf.Cos(t * 3.3f) * 8f);
        }

        if (glow != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 5f);
            Vector3 position = transform.position;
            glow.position = new Vector3(position.x, position.y - 1.1f, position.z);
            glow.localScale = Vector3.one * Mathf.Lerp(3f, 4f, pulse) / Mathf.Max(0.01f, transform.lossyScale.x);

            Color color = glowColor;
            color.a *= Mathf.Lerp(0.4f, 1f, pulse);
            ArtUtil.SetMaterialColor(glowMaterial, color);
        }
    }

    private void OnDestroy()
    {
        if (glowMaterial != null) Destroy(glowMaterial);
    }
}
