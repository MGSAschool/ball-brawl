using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(PowerUpPickUp))]
public class PowerUpVisual : MonoBehaviour
{
    private const float StackScale = 1.1f;

    [SerializeField] private float bobHeight = 0.2f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float spinSpeed = 60f;

    private readonly List<Material> materials = new List<Material>();
    private Transform body;
    private float bobOffset;

    private void Start()
    {
        PowerUpModifier powerUp = GetComponent<PowerUpPickUp>().powerUp;
        if (powerUp == null) return;

        MeshRenderer ownRenderer = GetComponent<MeshRenderer>();
        if (ownRenderer != null) ownRenderer.enabled = false;

        int level = Mathf.Clamp(powerUp.Level, 1, 3);
        Material material = MakeMaterial(PowerUpLook.ColorFor(powerUp), 0.25f + 0.35f * level);

        body = new GameObject("Shape").transform;
        body.SetParent(transform, false);
        bobOffset = Random.value * Mathf.PI * 2f;

        float spacing = 0.62f * StackScale;
        for (int i = 0; i < level; i++)
        {
            Transform copy = new GameObject("Level " + (i + 1)).transform;
            copy.SetParent(body, false);
            copy.localPosition = Vector3.up * ((i - (level - 1) * 0.5f) * spacing);
            copy.localScale = Vector3.one * StackScale;
            copy.localRotation = Quaternion.Euler(0f, i * 25f, 0f);
            BuildShape(powerUp, copy, material);
        }
    }

    private void Update()
    {
        if (body == null) return;
        body.localPosition = Vector3.up * (Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobHeight);
        body.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
    }

    private void OnDestroy()
    {
        foreach (Material material in materials)
        {
            Destroy(material);
        }
    }

    private void BuildShape(PowerUpModifier powerUp, Transform parent, Material material)
    {
        GameObject model = PowerUpLook.ModelFor(powerUp);
        if (model != null)
        {
            Transform fitted = ArtUtil.FitModel(model, parent, 0.95f * parent.lossyScale.x);
            foreach (Renderer rend in fitted.GetComponentsInChildren<Renderer>())
            {
                rend.shadowCastingMode = ShadowCastingMode.Off;
            }
            return;
        }

        if (powerUp is DashPowerUp)
        {
            Transform arrow = DashArrow.Build(parent, 1.8f, material, MakeMaterial(DashArrow.StreakColor, 0.7f));
            arrow.localRotation = Quaternion.Euler(-35f, 0f, 0f);
        }
        else if (powerUp is InvulnerablePowerUp)
        {
            AddPart(PrimitiveType.Sphere, parent, Vector3.zero, Vector3.one * 0.6f, material);
            AddPart(PrimitiveType.Cylinder, parent, new Vector3(15f, 0f, 0f), new Vector3(0.95f, 0.02f, 0.95f), material);
        }
        else if (powerUp is SuperKnockbackPowerUp)
        {
            AddPart(PrimitiveType.Cube, parent, Vector3.zero, Vector3.one * 0.45f, material);
            AddPart(PrimitiveType.Cube, parent, new Vector3(45f, 45f, 0f), Vector3.one * 0.45f, material);
            AddPart(PrimitiveType.Cube, parent, new Vector3(0f, 45f, 45f), Vector3.one * 0.45f, material);
        }
        else if (powerUp is SpeedBoostPowerUp)
        {
            AddPart(PrimitiveType.Cylinder, parent, new Vector3(0f, 0f, 90f), new Vector3(0.7f, 0.08f, 0.7f), material);
            AddPart(PrimitiveType.Cylinder, parent, new Vector3(0f, 0f, 90f), new Vector3(0.25f, 0.16f, 0.25f), material);
        }
        else
        {
            AddPart(PrimitiveType.Cube, parent, Vector3.zero, Vector3.one * 0.5f, material);
        }
    }

    private Material MakeMaterial(Color color, float glow)
    {
        Material material = ArtUtil.GlowingLit(color, glow);
        materials.Add(material);
        return material;
    }

    private void AddPart(PrimitiveType type, Transform parent, Vector3 rotation, Vector3 scale, Material material)
    {
        Transform part = ArtUtil.MakeShape(type, type.ToString(), parent, material);
        part.localRotation = Quaternion.Euler(rotation);
        part.localScale = scale;
        part.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
    }
}
