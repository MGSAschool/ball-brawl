using UnityEngine;
using UnityEngine.Rendering;

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
