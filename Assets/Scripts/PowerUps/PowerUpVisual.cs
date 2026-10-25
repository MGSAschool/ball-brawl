using System.Collections.Generic;
using UnityEngine;

// Gives each power-up pickup its own shape and colour so players can tell them apart at a glance:
//   Dash = double arrow, Invulnerability = shield bubble with a halo,
//   Super Knockback = spiky star, Speed Boost = wheel.
// The level (1-3) makes it bigger, glow brighter and adds one orbiting spark per level.
// Added by PowerUpSpawner; replaces the pickup's own mesh with the shape.
[RequireComponent(typeof(PowerUpPickUp))]
public class PowerUpVisual : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField] private float bobHeight = 0.2f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float sparkOrbitSpeed = 200f;

    private readonly List<Material> materials = new List<Material>();
    private Transform body;
    private Transform sparks;
    private float bobOffset;

    public static Color ColorFor(PowerUpModifier powerUp)
    {
        switch (powerUp)
        {
            case DashPowerUp: return new Color(0.2f, 0.85f, 1f);
            case InvulnerablePowerUp: return new Color(1f, 0.85f, 0.25f);
            case SuperKnockbackPowerUp: return new Color(1f, 0.32f, 0.2f);
            case SpeedBoostPowerUp: return new Color(0.4f, 1f, 0.45f);
            default: return Color.white;
        }
    }

    private void Start()
    {
        var powerUp = GetComponent<PowerUpPickUp>().powerUp;
        if (powerUp == null) return;

        var ownRenderer = GetComponent<MeshRenderer>();
        Material template = ownRenderer != null ? ownRenderer.sharedMaterial : null;
        if (ownRenderer != null) ownRenderer.enabled = false;

        int level = Mathf.Clamp(powerUp.Level, 1, 3);
        Color color = ColorFor(powerUp);
        Material shapeMaterial = CreateMaterial(template, color, 0.25f + 0.35f * level);
        Material sparkMaterial = CreateMaterial(template, Color.white, 1.5f);

        body = new GameObject("Shape").transform;
        body.SetParent(transform, false);
        body.localScale = Vector3.one * (1.2f + 0.3f * (level - 1));
        bobOffset = Random.value * Mathf.PI * 2f;
        BuildShape(powerUp, shapeMaterial);

        // One spark per level circling the shape
        sparks = new GameObject("Level Sparks").transform;
        sparks.SetParent(body, false);
        for (int i = 0; i < level; i++)
        {
            float a = i * Mathf.PI * 2f / level;
            Part(PrimitiveType.Sphere, sparks, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.75f, Vector3.zero, Vector3.one * 0.14f, sparkMaterial);
        }
    }

    private void Update()
    {
        if (body == null) return;
        body.localPosition = Vector3.up * (Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobHeight);
        sparks.Rotate(Vector3.up, sparkOrbitSpeed * Time.deltaTime, Space.Self);
    }

    private void OnDestroy()
    {
        foreach (var m in materials) Destroy(m);
    }

    private void BuildShape(PowerUpModifier powerUp, Material material)
    {
        switch (powerUp)
        {
            case DashPowerUp:
                // Two chevrons pointing forward: >>
                for (int i = 0; i < 2; i++)
                {
                    float z = 0.2f - i * 0.32f;
                    Part(PrimitiveType.Cube, body, new Vector3(0.13f, 0f, z), new Vector3(0f, -45f, 0f), new Vector3(0.14f, 0.18f, 0.42f), material);
                    Part(PrimitiveType.Cube, body, new Vector3(-0.13f, 0f, z), new Vector3(0f, 45f, 0f), new Vector3(0.14f, 0.18f, 0.42f), material);
                }
                break;

            case InvulnerablePowerUp:
                Part(PrimitiveType.Sphere, body, Vector3.zero, Vector3.zero, Vector3.one * 0.6f, material);
                Part(PrimitiveType.Cylinder, body, Vector3.zero, new Vector3(15f, 0f, 0f), new Vector3(0.95f, 0.02f, 0.95f), material);
                break;

            case SuperKnockbackPowerUp:
                // Three overlapping cubes at different angles make a spiky star
                Part(PrimitiveType.Cube, body, Vector3.zero, Vector3.zero, Vector3.one * 0.45f, material);
                Part(PrimitiveType.Cube, body, Vector3.zero, new Vector3(45f, 45f, 0f), Vector3.one * 0.45f, material);
                Part(PrimitiveType.Cube, body, Vector3.zero, new Vector3(0f, 45f, 45f), Vector3.one * 0.45f, material);
                break;

            case SpeedBoostPowerUp:
                // Upright wheel with a hub
                Part(PrimitiveType.Cylinder, body, Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(0.7f, 0.08f, 0.7f), material);
                Part(PrimitiveType.Cylinder, body, Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(0.25f, 0.16f, 0.25f), material);
                break;

            default:
                Part(PrimitiveType.Cube, body, Vector3.zero, Vector3.zero, Vector3.one * 0.5f, material);
                break;
        }
    }

    private Material CreateMaterial(Material template, Color color, float glow)
    {
        // Solid lit material; the pickup's own material can be see-through
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        var material = lit != null ? new Material(lit) : new Material(template);
        material.color = color;
        if (material.HasProperty(BaseColorId)) material.SetColor(BaseColorId, color);
        if (material.HasProperty(EmissionColorId))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor(EmissionColorId, color * glow);
        }
        materials.Add(material);
        return material;
    }

    private static void Part(PrimitiveType type, Transform parent, Vector3 position, Vector3 euler, Vector3 scale, Material material)
    {
        var part = GameObject.CreatePrimitive(type);
        // Only the pickup's own trigger should collide
        DestroyImmediate(part.GetComponent<Collider>());
        part.name = type.ToString();
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.Euler(euler);
        part.transform.localScale = scale;
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
    }
}
