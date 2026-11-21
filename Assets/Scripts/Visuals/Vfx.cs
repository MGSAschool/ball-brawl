using UnityEngine;
using UnityEngine.Rendering;

public static class Vfx
{
    public static void Hit(Vector3 position, Color color, float strength)
    {
        strength = Mathf.Clamp01(strength);
        int count = Mathf.RoundToInt(Mathf.Lerp(8, 26, strength));
        Vector2 speed = new Vector2(6f, 14f) * Mathf.Lerp(0.7f, 1.3f, strength);
        ParticleKit.Burst(position, Color.white, color, count, speed, new Vector2(0.3f, 0.6f), new Vector2(0.25f, 0.45f), 1.5f, stretch: true);
        ParticleKit.Shockwave(position, color, Mathf.Lerp(2.5f, 5f, strength), 0.3f);
        ParticleKit.Flash(position, color, Mathf.Lerp(1.5f, 3f, strength));
    }

    public static void Dust(Vector3 position, float strength)
    {
        strength = Mathf.Clamp01(strength);
        Color dust = new Color(0.85f, 0.82f, 0.78f, 0.55f);
        int count = Mathf.RoundToInt(Mathf.Lerp(5, 14, strength));
        Vector2 speed = new Vector2(1.5f, 4f) * Mathf.Lerp(0.8f, 1.4f, strength);
        ParticleKit.Burst(position, dust, ParticleKit.Clear(dust), count, speed, new Vector2(0.8f, 1.4f), new Vector2(0.4f, 0.7f), -0.2f, flat: true, grow: 2.2f);
    }

    public static void Sparkle(Vector3 position, Color color, int count = 18)
    {
        ParticleKit.Burst(position, Color.white, color, count, new Vector2(2f, 6f), new Vector2(0.25f, 0.5f), new Vector2(0.5f, 0.9f), -0.6f);
        ParticleKit.Shockwave(position, color, 2.5f, 0.35f);
    }

    public static void Poof(Vector3 position, Color color)
    {
        Color start = Color.Lerp(color, Color.white, 0.5f);
        ParticleKit.Burst(position, start, new Color(1f, 1f, 1f, 0f), 14, new Vector2(2f, 5f), new Vector2(0.6f, 1.1f), new Vector2(0.4f, 0.7f), -0.3f, grow: 1.8f);
        ParticleKit.Shockwave(position, color, 4f, 0.4f);
    }

    public static void Explosion(Vector3 position, float radius)
    {
        float scale = radius / 8f;
        ParticleKit.Flash(position, new Color(1f, 0.85f, 0.5f), radius * 1.2f);
        ParticleKit.Burst(position, new Color(1f, 0.9f, 0.4f), new Color(0.9f, 0.15f, 0.05f, 0f), 26,
            new Vector2(3f, 9f) * scale, new Vector2(2f, 4f) * scale, new Vector2(0.35f, 0.7f), -0.4f, grow: 1.8f);
        ParticleKit.Burst(position + Vector3.up * 0.5f, new Color(0.25f, 0.22f, 0.22f, 0.7f), new Color(0.3f, 0.3f, 0.3f, 0f), 16,
            new Vector2(1f, 3f), new Vector2(1.5f, 3f) * scale, new Vector2(0.9f, 1.6f), -0.5f, grow: 2.2f);
        ParticleKit.Burst(position, Color.white, new Color(1f, 0.6f, 0.1f), 40,
            new Vector2(10f, 22f), new Vector2(0.3f, 0.6f), new Vector2(0.4f, 0.9f), 2f, stretch: true);
        ParticleKit.Shockwave(position + Vector3.up * 0.2f, new Color(1f, 0.6f, 0.2f), radius * 2f, 0.45f);
        Shake(0.6f);
    }

    public static void Zap(Vector3 position, Color color)
    {
        DrawLightningBolt(position, color);
        ParticleKit.Flash(position, color, 4f);
        ParticleKit.Burst(position, Color.white, color, 30, new Vector2(6f, 14f), new Vector2(0.3f, 0.6f), new Vector2(0.25f, 0.5f), 1f, stretch: true);
        ParticleKit.Shockwave(position, color, 5f, 0.3f);
    }

    public static void KnockOutFlare(Vector3 position, Color color)
    {
        ParticleSystem ps = ParticleKit.Make("KO Flare", position, VfxMaterials.Alpha(), TextureMaker.Dot(), 1.2f);
        ParticleSystem.MainModule main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(12f, 26f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        ParticleKit.ShootUpward(ps, 8f);
        ParticleKit.Fade(ps, Color.white, ParticleKit.Clear(color));
        ps.Emit(50);
    }

    public static void Confetti(Vector3 position)
    {
        ParticleSystem pinkAndBlue = MakeConfetti(position, new Color(1f, 0.3f, 0.4f), new Color(0.3f, 0.7f, 1f));
        ParticleSystem.RotationOverLifetimeModule spin = pinkAndBlue.rotationOverLifetime;
        spin.enabled = true;
        spin.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
        ParticleSystem.NoiseModule noise = pinkAndBlue.noise;
        noise.enabled = true;
        noise.strength = 1.5f;
        noise.frequency = 0.6f;
        pinkAndBlue.Emit(160);

        ParticleSystem goldAndGreen = MakeConfetti(position, new Color(1f, 0.85f, 0.2f), new Color(0.4f, 1f, 0.5f));
        goldAndGreen.Emit(120);
    }

    public static void Shake(float amount)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        CameraShake shake = cam.GetComponent<CameraShake>();
        if (shake == null) shake = cam.gameObject.AddComponent<CameraShake>();
        shake.Add(amount);
    }

    private static ParticleSystem MakeConfetti(Vector3 position, Color colorA, Color colorB)
    {
        ParticleSystem ps = ParticleKit.Make("Confetti", position, VfxMaterials.Alpha(), null, 5f);
        ParticleSystem.MainModule main = ps.main;
        ParticleSystem.MinMaxGradient colors = new ParticleSystem.MinMaxGradient(colorA, colorB);
        colors.mode = ParticleSystemGradientMode.TwoColors;
        main.startColor = colors;
        main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 22f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0.6f;
        ParticleKit.ShootUpward(ps, 35f);
        return ps;
    }

    private static void DrawLightningBolt(Vector3 position, Color color)
    {
        GameObject go = new GameObject("Zap");
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.material = VfxMaterials.Alpha();
        line.textureMode = LineTextureMode.Stretch;
        line.widthMultiplier = 0.6f;
        line.startColor = Color.white;
        line.endColor = color;

        int points = 12;
        line.positionCount = points;
        Vector3 top = position + Vector3.up * 16f;
        for (int i = 0; i < points; i++)
        {
            float t = i / (points - 1f);
            Vector3 jitter = Vector3.zero;
            if (i > 0 && i < points - 1) jitter = new Vector3(Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f));
            line.SetPosition(i, Vector3.Lerp(top, position, t) + jitter);
        }
        Object.Destroy(go, 0.22f);
    }
}

public static class ParticleKit
{
    public static ParticleSystem Make(string name, Vector3 position, Material material, Texture2D texture, float life)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;
        main.startColor = Color.white;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;

        ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = material;
        rend.shadowCastingMode = ShadowCastingMode.Off;
        rend.receiveShadows = false;
        if (texture != null && texture != TextureMaker.Dot())
        {
            Material copy = new Material(material);
            ArtUtil.SetMaterialTexture(copy, texture);
            rend.material = copy;
        }

        Object.Destroy(go, life + 0.5f);
        return ps;
    }

    public static void Burst(Vector3 position, Color from, Color to, int count, Vector2 speed, Vector2 size,
        Vector2 lifetime, float gravity, bool stretch = false, bool flat = false, float grow = 0f)
    {
        ParticleSystem ps = Make("Burst", position, VfxMaterials.Alpha(), TextureMaker.Dot(), lifetime.y + 0.1f);
        ParticleSystem.MainModule main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
        main.gravityModifier = gravity;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        if (flat)
        {
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;
            shape.rotation = new Vector3(90f, 0f, 0f);
        }
        else
        {
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;
        }

        Fade(ps, from, to);

        ParticleSystem.LimitVelocityOverLifetimeModule drag = ps.limitVelocityOverLifetime;
        drag.enabled = true;
        drag.drag = 2f;
        drag.multiplyDragByParticleSize = false;

        ParticleSystem.SizeOverLifetimeModule sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        if (grow > 0f) sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, grow));
        else sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        if (stretch)
        {
            ParticleSystemRenderer rend = ps.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Stretch;
            rend.velocityScale = 0.05f;
            rend.lengthScale = 1.5f;
        }

        ps.Emit(count);
    }

    public static void Shockwave(Vector3 position, Color color, float size, float lifetime)
    {
        ParticleSystem ps = Make("Shockwave", position + Vector3.up * 0.05f, VfxMaterials.Alpha(), TextureMaker.Ring(), lifetime + 0.05f);
        ParticleSystem.MainModule main = ps.main;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startLifetime = lifetime;

        ParticleSystemRenderer rend = ps.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.HorizontalBillboard;

        ParticleSystem.SizeOverLifetimeModule sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.2f, 1f, 1f));

        Fade(ps, color, Clear(color));
        ps.Emit(1);
    }

    public static void Flash(Vector3 position, Color color, float size)
    {
        ParticleSystem ps = Make("Flash", position, VfxMaterials.Additive(), TextureMaker.Dot(), 0.2f);
        ParticleSystem.MainModule main = ps.main;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startLifetime = 0.12f;

        Color light = Color.Lerp(color, Color.white, 0.6f);
        Fade(ps, light, light);
        ps.Emit(1);
    }

    public static void Fade(ParticleSystem ps, Color from, Color to)
    {
        GradientColorKey[] colors = { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) };
        GradientAlphaKey[] alphas =
        {
            new GradientAlphaKey(from.a, 0f),
            new GradientAlphaKey(from.a * 0.8f, 0.5f),
            new GradientAlphaKey(0f, 1f)
        };
        Gradient gradient = new Gradient();
        gradient.SetKeys(colors, alphas);

        ParticleSystem.ColorOverLifetimeModule colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        colorOverLife.color = gradient;
    }

    public static void ShootUpward(ParticleSystem ps, float coneAngle)
    {
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = coneAngle;
        shape.rotation = new Vector3(-90f, 0f, 0f);
    }

    public static Color Clear(Color color)
    {
        return new Color(color.r, color.g, color.b, 0f);
    }
}

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
