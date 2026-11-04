using UnityEngine;

// One-shot particle effects built in code: hit sparks, landing dust, pickup sparkles, explosions,
// zaps, confetti. Each call makes a short-lived particle system that deletes itself.
// Materials come from Resources/VFX so their shaders are included in builds.
public static class Vfx
{
    private static Material additive, alpha;
    private static Texture2D dot, ring;

    // --- Effects ---
    public static void Hit(Vector3 position, Color color, float strength)
    {
        strength = Mathf.Clamp01(strength);
        Burst(position, Alpha(), Dot(), Color.white, color, Mathf.RoundToInt(Mathf.Lerp(8, 26, strength)),
            new Vector2(6f, 14f) * Mathf.Lerp(0.7f, 1.3f, strength), new Vector2(0.3f, 0.6f), new Vector2(0.25f, 0.45f), 1.5f, stretch: true);
        Shockwave(position, color, Mathf.Lerp(2.5f, 5f, strength), 0.3f);
        Flash(position, color, Mathf.Lerp(1.5f, 3f, strength));
    }

    public static void Dust(Vector3 position, float strength)
    {
        strength = Mathf.Clamp01(strength);
        var color = new Color(0.85f, 0.82f, 0.78f, 0.55f);
        Burst(position, Alpha(), Dot(), color, new Color(color.r, color.g, color.b, 0f), Mathf.RoundToInt(Mathf.Lerp(5, 14, strength)),
            new Vector2(1.5f, 4f) * Mathf.Lerp(0.8f, 1.4f, strength), new Vector2(0.8f, 1.4f), new Vector2(0.4f, 0.7f), -0.2f,
            flat: true, grow: 2.2f);
    }

    public static void Sparkle(Vector3 position, Color color, int count = 18)
    {
        Burst(position, Alpha(), Dot(), Color.white, color, count,
            new Vector2(2f, 6f), new Vector2(0.25f, 0.5f), new Vector2(0.5f, 0.9f), -0.6f);
        Shockwave(position, color, 2.5f, 0.35f);
    }

    public static void Poof(Vector3 position, Color color)
    {
        Burst(position, Alpha(), Dot(), Color.Lerp(color, Color.white, 0.5f), new Color(1f, 1f, 1f, 0f), 14,
            new Vector2(2f, 5f), new Vector2(0.6f, 1.1f), new Vector2(0.4f, 0.7f), -0.3f, grow: 1.8f);
        Shockwave(position, color, 4f, 0.4f);
    }

    public static void Explosion(Vector3 position, float radius)
    {
        Flash(position, new Color(1f, 0.85f, 0.5f), radius * 1.2f);
        // Fireballs: hot yellow to deep red, swelling as they fly out
        Burst(position, Alpha(), Dot(), new Color(1f, 0.9f, 0.4f), new Color(0.9f, 0.15f, 0.05f, 0f), 26,
            new Vector2(3f, 9f) * (radius / 8f), new Vector2(2f, 4f) * (radius / 8f), new Vector2(0.35f, 0.7f), -0.4f, grow: 1.8f);
        // Smoke rising after
        Burst(position + Vector3.up * 0.5f, Alpha(), Dot(), new Color(0.25f, 0.22f, 0.22f, 0.7f), new Color(0.3f, 0.3f, 0.3f, 0f), 16,
            new Vector2(1f, 3f), new Vector2(1.5f, 3f) * (radius / 8f), new Vector2(0.9f, 1.6f), -0.5f, grow: 2.2f);
        Burst(position, Alpha(), Dot(), Color.white, new Color(1f, 0.6f, 0.1f), 40,
            new Vector2(10f, 22f), new Vector2(0.3f, 0.6f), new Vector2(0.4f, 0.9f), 2f, stretch: true);
        Shockwave(position + Vector3.up * 0.2f, new Color(1f, 0.6f, 0.2f), radius * 2f, 0.45f);
        Shake(0.6f);
    }

    // Lightning strike from the sky (Staph It! catches)
    public static void Zap(Vector3 position, Color color)
    {
        var go = new GameObject("Zap");
        var line = go.AddComponent<LineRenderer>();
        line.material = Alpha();
        line.textureMode = LineTextureMode.Stretch;
        line.widthMultiplier = 0.6f;
        line.startColor = Color.white;
        line.endColor = color;
        const int points = 12;
        line.positionCount = points;
        Vector3 top = position + Vector3.up * 16f;
        for (int i = 0; i < points; i++)
        {
            float t = i / (points - 1f);
            Vector3 jitter = i == 0 || i == points - 1 ? Vector3.zero : new Vector3(Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f));
            line.SetPosition(i, Vector3.Lerp(top, position, t) + jitter);
        }
        Object.Destroy(go, 0.22f);

        Flash(position, color, 4f);
        Burst(position, Alpha(), Dot(), Color.white, color, 30, new Vector2(6f, 14f), new Vector2(0.3f, 0.6f), new Vector2(0.25f, 0.5f), 1f, stretch: true);
        Shockwave(position, color, 5f, 0.3f);
    }

    // Upward flare where a ball fell out of the arena
    public static void KnockOutFlare(Vector3 position, Color color)
    {
        var ps = Make("KO Flare", position, Alpha(), Dot(), 1.2f);
        var main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(12f, 26f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 8f;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        Fade(ps, Color.white, new Color(color.r, color.g, color.b, 0f));
        ps.Emit(50);
    }

    public static void Confetti(Vector3 position)
    {
        var ps = Make("Confetti", position, Alpha(), null, 5f);
        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.3f, 0.4f), new Color(0.3f, 0.7f, 1f)) { mode = ParticleSystemGradientMode.TwoColors };
        main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 22f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = 0.6f;
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 1.5f;
        noise.frequency = 0.6f;
        ps.Emit(160);
        // A second pass in gold and green for more colours
        var gold = Make("Confetti", position, Alpha(), null, 5f);
        var gm = gold.main;
        gm.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.2f), new Color(0.4f, 1f, 0.5f));
        gm.startSpeed = main.startSpeed;
        gm.startSize = main.startSize;
        gm.startLifetime = main.startLifetime;
        gm.startRotation = main.startRotation;
        gm.gravityModifier = 0.6f;
        var gs = gold.shape;
        gs.enabled = true;
        gs.shapeType = ParticleSystemShapeType.Cone;
        gs.angle = 35f;
        gs.rotation = new Vector3(-90f, 0f, 0f);
        gold.Emit(120);
    }

    public static void Shake(float amount)
    {
        var cam = Camera.main;
        if (cam == null) return;
        if (!cam.TryGetComponent<CameraShake>(out var shake)) shake = cam.gameObject.AddComponent<CameraShake>();
        shake.Add(amount);
    }

    // --- Building blocks ---
    private static void Burst(Vector3 position, Material material, Texture2D texture, Color from, Color to, int count,
        Vector2 speed, Vector2 size, Vector2 lifetime, float gravity, bool stretch = false, bool flat = false, float grow = 0f)
    {
        var ps = Make("Burst", position, material, texture, lifetime.y + 0.1f);
        var main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
        main.gravityModifier = gravity;

        var shape = ps.shape;
        shape.enabled = true;
        if (flat)
        {
            // Spread along the ground
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
        var drag = ps.limitVelocityOverLifetime;
        drag.enabled = true;
        drag.drag = 2f;
        drag.multiplyDragByParticleSize = false;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = grow > 0f
            ? new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, grow))
            : new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        if (stretch)
        {
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.05f;
            r.lengthScale = 1.5f;
        }
        ps.Emit(count);
    }

    // Flat expanding ring on the ground
    private static void Shockwave(Vector3 position, Color color, float size, float lifetime)
    {
        var ps = Make("Shockwave", position + Vector3.up * 0.05f, Alpha(), Ring(), lifetime + 0.05f);
        var main = ps.main;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startLifetime = lifetime;
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.2f, 1f, 1f));
        Fade(ps, color, new Color(color.r, color.g, color.b, 0f));
        ps.Emit(1);
    }

    private static void Flash(Vector3 position, Color color, float size)
    {
        var ps = Make("Flash", position, Additive(), Dot(), 0.2f);
        var main = ps.main;
        main.startSpeed = 0f;
        main.startSize = size;
        main.startLifetime = 0.12f;
        Color light = Color.Lerp(color, Color.white, 0.6f);
        Fade(ps, light, light);
        ps.Emit(1);
    }

    private static ParticleSystem Make(string name, Vector3 position, Material material, Texture2D texture, float life)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;
        var emission = ps.emission;
        emission.enabled = false;
        var shape = ps.shape;
        shape.enabled = false;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        if (texture != null && texture != Dot())
        {
            // Rings use their own texture; give them their own material copy
            var m = new Material(material) { mainTexture = texture };
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", texture);
            r.material = m;
        }
        Object.Destroy(go, life + 0.5f);
        return ps;
    }

    private static void Fade(ParticleSystem ps, Color from, Color to)
    {
        var col = ps.colorOverLifetime;
        col.enabled = true;
        // Colour runs from `from` to `to`; every effect fades out at the end
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
            new[] { new GradientAlphaKey(from.a, 0f), new GradientAlphaKey(from.a * 0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var main = ps.main;
        main.startColor = Color.white;
    }

    public static Material Additive()
    {
        if (additive == null)
        {
            var source = Resources.Load<Material>("VFX/ParticleAdditive");
            additive = source != null ? new Material(source) : FallbackMaterial(true);
            SetTexture(additive, Dot());
        }
        return additive;
    }

    public static Material Alpha()
    {
        if (alpha == null)
        {
            var source = Resources.Load<Material>("VFX/ParticleAlpha");
            alpha = source != null ? new Material(source) : FallbackMaterial(false);
            SetTexture(alpha, Dot());
        }
        return alpha;
    }

    private static void SetTexture(Material m, Texture2D t)
    {
        m.mainTexture = t;
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", t);
    }

    private static Material FallbackMaterial(bool add)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default"));
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", add ? 2f : 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", add ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return m;
    }

    // Round particle
    public static Texture2D Dot()
    {
        if (dot != null) return dot;
        // Solid cartoon blob with a soft rim; reads on the light arena floor
        dot = RadialTexture(r => Mathf.Clamp01((1f - r) * 3.5f));
        return dot;
    }

    public static Texture2D Ring()
    {
        if (ring != null) return ring;
        ring = RadialTexture(r => Mathf.Clamp01(1f - Mathf.Abs(r - 0.85f) / 0.12f) * Mathf.Clamp01((1f - r) * 20f));
        return ring;
    }

    private static Texture2D RadialTexture(System.Func<float, float> alphaAt)
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                byte a = (byte)(Mathf.Clamp01(alphaAt(Mathf.Sqrt(dx * dx + dy * dy))) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }
}
