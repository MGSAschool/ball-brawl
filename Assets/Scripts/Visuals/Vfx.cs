using UnityEngine;

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
