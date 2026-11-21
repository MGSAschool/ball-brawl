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
        Material template = Resources.Load<Material>("VFX/TransparentLit");
        Material material;
        if (template != null)
        {
            material = new Material(template);
        }
        else
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_EMISSION");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        material.SetColor("_BaseColor", color);
        material.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * glow);
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

public static class TextureMaker
{
    private static Sprite circle;
    private static Sprite downArrow;
    private static Sprite rightArrow;
    private static Sprite vignette;
    private static Texture2D floorRing;
    private static Texture2D dot;
    private static Texture2D ring;

    public static Sprite Circle()
    {
        if (circle != null) return circle;

        int size = 128;
        Texture2D texture = NewTexture(size, false);
        Color32[] pixels = new Color32[size * size];
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                float alpha = Mathf.Clamp01(radius - distance);
                pixels[y * size + x] = White(alpha);
            }
        }
        circle = ToSprite(texture, pixels);
        return circle;
    }

    public static Sprite DownArrow()
    {
        if (downArrow != null) return downArrow;

        int size = 64;
        Texture2D texture = NewTexture(size, false);
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float v = (y + 0.5f) / size;
                float u = Mathf.Abs((x + 0.5f) / size - 0.5f) * 2f;
                float alpha = Mathf.Clamp01((v - u) * size * 0.5f);
                pixels[y * size + x] = White(alpha);
            }
        }
        downArrow = ToSprite(texture, pixels);
        return downArrow;
    }

    public static Sprite RightArrow()
    {
        if (rightArrow != null) return rightArrow;

        int size = 128;
        Texture2D texture = NewTexture(size, false);
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size;
                float v = Mathf.Abs((y + 0.5f) / size - 0.5f) * 2f;
                float alpha = Mathf.Clamp01(((1f - u) - v) * size * 0.5f);
                pixels[y * size + x] = White(alpha);
            }
        }
        rightArrow = ToSprite(texture, pixels);
        return rightArrow;
    }

    public static Sprite Vignette()
    {
        if (vignette != null) return vignette;

        int size = 64;
        Texture2D texture = NewTexture(size, false);
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float r = Mathf.Clamp01(DistanceFromCenter(x, y, size) / 1.414f);
                pixels[y * size + x] = White(Mathf.SmoothStep(0.35f, 1f, r));
            }
        }
        vignette = ToSprite(texture, pixels);
        return vignette;
    }

    public static Texture2D FloorRing()
    {
        if (floorRing != null) return floorRing;

        int size = 128;
        floorRing = NewTexture(size, true);
        Color32[] pixels = new Color32[size * size];
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius)) / radius;
                float outerEdge = Mathf.Clamp01((1f - d) * size * 0.5f);
                float band = Mathf.Clamp01((d - 0.72f) * size * 0.5f);
                pixels[y * size + x] = White(outerEdge * Mathf.Max(band, 0.18f));
            }
        }
        floorRing.SetPixels32(pixels);
        floorRing.Apply();
        return floorRing;
    }

    public static Texture2D Dot()
    {
        if (dot != null) return dot;

        int size = 64;
        dot = NewTexture(size, false);
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float r = DistanceFromCenter(x, y, size);
                pixels[y * size + x] = White(Mathf.Clamp01((1f - r) * 3.5f));
            }
        }
        dot.SetPixels32(pixels);
        dot.Apply();
        return dot;
    }

    public static Texture2D Ring()
    {
        if (ring != null) return ring;

        int size = 64;
        ring = NewTexture(size, false);
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float r = DistanceFromCenter(x, y, size);
                float band = Mathf.Clamp01(1f - Mathf.Abs(r - 0.85f) / 0.12f);
                float edge = Mathf.Clamp01((1f - r) * 20f);
                pixels[y * size + x] = White(band * edge);
            }
        }
        ring.SetPixels32(pixels);
        ring.Apply();
        return ring;
    }

    private static float DistanceFromCenter(int x, int y, int size)
    {
        float dx = (x + 0.5f) / size * 2f - 1f;
        float dy = (y + 0.5f) / size * 2f - 1f;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    private static Color32 White(float alpha)
    {
        return new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha) * 255f));
    }

    private static Texture2D NewTexture(int size, bool mipmaps)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, mipmaps);
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    private static Sprite ToSprite(Texture2D texture, Color32[] pixels)
    {
        texture.SetPixels32(pixels);
        texture.Apply();
        Rect rect = new Rect(0, 0, texture.width, texture.height);
        return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
    }
}
