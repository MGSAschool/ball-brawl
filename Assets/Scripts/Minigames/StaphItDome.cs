using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(StaphItMode))]
public class StaphItDome : MonoBehaviour
{
    private StaphItMode mode;
    private Transform root;
    private Transform dome;
    private Transform edge;
    private Transform beam;
    private Material domeMaterial;
    private Material edgeMaterial;
    private Material beamMaterial;
    private Light sun;
    private Color sunColor;
    private bool tintingSun;
    private float lastMovedAt = -1f;

    private void Awake()
    {
        mode = GetComponent<StaphItMode>();
        sun = FindSun();
        if (sun != null) sunColor = sun.color;
    }

    private void LateUpdate()
    {
        bool running = StaphItMode.Active == mode && mode.Zone.IsPlaced;
        if (!running)
        {
            if (root != null) root.gameObject.SetActive(false);
            RestoreSun();
            return;
        }

        if (root == null) BuildDome();
        root.gameObject.SetActive(true);

        StaphItSafeZone zone = mode.Zone;
        if (zone.MovedAt != lastMovedAt)
        {
            lastMovedAt = zone.MovedAt;
            root.position = zone.Center;
            Vfx.Poof(zone.Center, mode.Green);
        }

        UpdateLook(zone);
        TintSun();
    }

    private void UpdateLook(StaphItSafeZone zone)
    {
        Color c = mode.SignalColor;
        float t = Time.time;
        float pulse = 0.5f + 0.5f * Mathf.Sin(t * 2f);
        if (mode.Light == StaphItMode.Signal.Yellow) pulse = Mathf.PingPong(t * 6f, 1f);
        if (mode.Light == StaphItMode.Signal.Red) pulse = 0.5f + 0.5f * Mathf.Sin(t * 10f);

        float age = (t - zone.MovedAt) / 0.5f;
        float grow = 1f;
        if (age < 1f) grow = Mathf.Sin(age * Mathf.PI * 0.5f) * (1f + 0.2f * Mathf.Sin(age * Mathf.PI));
        float size = zone.Radius * 2f * Mathf.Max(0.01f, grow);

        dome.localScale = new Vector3(size, size * 0.8f, size);
        edge.localScale = Vector3.one * (size / 0.85f);
        beam.localScale = new Vector3(0.7f, 25f, 0.7f) * Mathf.Max(0.01f, grow);
        beam.localPosition = Vector3.up * 25f * grow;

        SetColor(domeMaterial, new Color(c.r, c.g, c.b, Mathf.Lerp(0.12f, 0.24f, pulse)), c * Mathf.Lerp(0.4f, 1.2f, pulse));
        SetColor(edgeMaterial, new Color(c.r, c.g, c.b, Mathf.Lerp(0.6f, 1f, pulse)), Color.black);
        SetColor(beamMaterial, new Color(c.r, c.g, c.b, Mathf.Lerp(0.08f, 0.18f, pulse)), Color.black);
    }

    private void TintSun()
    {
        if (sun == null) return;
        tintingSun = true;

        Color target = sunColor;
        if (mode.Light == StaphItMode.Signal.Red) target = Color.Lerp(sunColor, mode.Red, 0.3f);
        if (mode.Light == StaphItMode.Signal.Yellow) target = Color.Lerp(sunColor, mode.Yellow, 0.2f);
        sun.color = Color.Lerp(sun.color, target, 1f - Mathf.Exp(-8f * Time.deltaTime));
    }

    private void RestoreSun()
    {
        if (!tintingSun || sun == null) return;
        sun.color = sunColor;
        tintingSun = false;
    }

    private void BuildDome()
    {
        root = new GameObject("Staph It Safe Zone").transform;

        domeMaterial = ArtUtil.TransparentLit(mode.Green, 0.8f);
        dome = ArtUtil.MakeShape(PrimitiveType.Sphere, "Dome", root, domeMaterial);

        edgeMaterial = new Material(VfxMaterials.Additive());
        ArtUtil.SetMaterialTexture(edgeMaterial, TextureMaker.Ring());
        edge = ArtUtil.MakeShape(PrimitiveType.Quad, "Edge", root, edgeMaterial);
        edge.localRotation = Quaternion.Euler(90f, 0f, 0f);
        edge.localPosition = Vector3.up * 0.06f;

        beamMaterial = new Material(VfxMaterials.Additive());
        beam = ArtUtil.MakeShape(PrimitiveType.Cylinder, "Beam", root, beamMaterial);
    }

    private void SetColor(Material material, Color color, Color emission)
    {
        ArtUtil.SetMaterialColor(material, color);
        if (material.HasProperty("_EmissionColor") && emission != Color.black) material.SetColor("_EmissionColor", emission);
    }

    private Light FindSun()
    {
        if (RenderSettings.sun != null) return RenderSettings.sun;
        foreach (Light light in FindObjectsByType<Light>())
        {
            if (light.type == LightType.Directional) return light;
        }
        return null;
    }
}
