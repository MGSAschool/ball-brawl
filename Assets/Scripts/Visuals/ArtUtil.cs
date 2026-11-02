using UnityEngine;

// Helpers for placing imported art at runtime
public static class ArtUtil
{
    private static Sprite circle;

    // Instantiates a model under `parent`, scaled so its largest side is `size` metres and
    // centred on the parent. Returns the wrapper, which can be rotated freely around that centre.
    public static Transform FitModel(GameObject prefab, Transform parent, float size, string name = null)
    {
        var wrapper = new GameObject(name ?? prefab.name).transform;
        wrapper.SetParent(parent, false);

        var model = Object.Instantiate(prefab, wrapper);
        model.name = prefab.name;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;

        // Exported helper curves come in as empty meshes; they only skew the bounds
        foreach (var r in model.GetComponentsInChildren<Renderer>())
        {
            if (r.bounds.size.sqrMagnitude < 1e-8f) r.enabled = false;
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
        var bounds = new Bounds(go.transform.position, Vector3.zero);
        bool any = false;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled) continue;
            if (!any) bounds = r.bounds;
            else bounds.Encapsulate(r.bounds);
            any = true;
        }
        return bounds;
    }

    // See-through, lit URP material (used for the shield bubble)
    public static Material TransparentLit(Color color, float glow = 0f)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        material.SetColor("_BaseColor", color);
        if (glow > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * glow);
        }
        return material;
    }

    // Soft-edged white circle, tinted by UI Images (player badges and portraits)
    public static Sprite Circle()
    {
        if (circle != null) return circle;
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(r - d) * 255f));
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        circle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return circle;
    }
}
