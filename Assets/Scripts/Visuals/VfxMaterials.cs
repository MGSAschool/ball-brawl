using UnityEngine;
using UnityEngine.Rendering;

public static class VfxMaterials
{
    private static Material additive;
    private static Material alpha;

    public static Material Additive()
    {
        if (additive == null) additive = LoadOrMake("VFX/ParticleAdditive", true);
        return additive;
    }

    public static Material Alpha()
    {
        if (alpha == null) alpha = LoadOrMake("VFX/ParticleAlpha", false);
        return alpha;
    }

    private static Material LoadOrMake(string path, bool isAdditive)
    {
        Material source = Resources.Load<Material>(path);
        Material material;
        if (source != null) material = new Material(source);
        else material = MakeFallback(isAdditive);

        ArtUtil.SetMaterialTexture(material, TextureMaker.Dot());
        return material;
    }

    private static Material MakeFallback(bool isAdditive)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        if (isAdditive)
        {
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
        }
        else
        {
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        }
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        return material;
    }
}
