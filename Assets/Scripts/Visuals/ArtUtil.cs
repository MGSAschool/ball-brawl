using UnityEngine;
using UnityEngine.Rendering;

public static class ArtUtil
{
    public static Transform FitModel(GameObject prefab, Transform parent, float size, string name = null)
    {
        string wrapperName = name;
        if (wrapperName == null) wrapperName = prefab.name;

        Transform wrapper = new GameObject(wrapperName).transform;
        wrapper.SetParent(parent, false);

        GameObject model = Object.Instantiate(prefab, wrapper);
        model.name = prefab.name;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;

        foreach (Renderer rend in model.GetComponentsInChildren<Renderer>())
        {
            if (rend.bounds.size.sqrMagnitude < 1e-8f) rend.enabled = false;
        }

        Bounds bounds = WorldBounds(model);
        float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (largest > 1e-5f) model.transform.localScale *= size / largest;

        bounds = WorldBounds(model);
        model.transform.position += wrapper.position - bounds.center;
        return wrapper;
    }

    public static Bounds WorldBounds(GameObject go)
    {
        Bounds bounds = new Bounds(go.transform.position, Vector3.zero);
        bool foundAny = false;
        foreach (Renderer rend in go.GetComponentsInChildren<Renderer>())
        {
            if (!rend.enabled) continue;
            if (!foundAny) bounds = rend.bounds;
            else bounds.Encapsulate(rend.bounds);
            foundAny = true;
        }
        return bounds;
    }

    public static Material TransparentLit(Color color, float glow = 0f)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetColor("_BaseColor", color);
        if (glow > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * glow);
        }
        return material;
    }

    public static Material GlowingLit(Color color, float glow)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        material.SetColor("_BaseColor", color);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * glow);
        return material;
    }

    public static void SetMaterialColor(Material material, Color color)
    {
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
    }

    public static void SetMaterialTexture(Material material, Texture texture)
    {
        material.mainTexture = texture;
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
    }

    public static Transform MakeShape(PrimitiveType type, string name, Transform parent, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.name = name;
        part.transform.SetParent(parent, false);

        MeshRenderer rend = part.GetComponent<MeshRenderer>();
        rend.sharedMaterial = material;
        rend.shadowCastingMode = ShadowCastingMode.Off;
        return part.transform;
    }
}
