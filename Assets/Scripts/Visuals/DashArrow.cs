using UnityEngine;

// The Dash power-up's look, built in code: a chunky double arrow (like a fast-forward sign) with
// three speed streaks trailing behind it. It lies flat and points along +Z, about 1 unit long.
// Used for the pickup (PowerUpVisual) and the prop over a ball while Dash is running (BallSkin).
public static class DashArrow
{
    public static readonly Color ArrowColor = new Color(0.2f, 0.85f, 1f);
    public static readonly Color StreakColor = new Color(0.8f, 0.97f, 1f);

    private static Mesh dartMesh;
    private static Material defaultArrow, defaultStreak;

    // Adds the arrow under `parent`; `size` is its length in the parent's units
    public static Transform Build(Transform parent, float size, Material arrow = null, Material streak = null)
    {
        if (arrow == null || streak == null) EnsureDefaults();
        arrow = arrow != null ? arrow : defaultArrow;
        streak = streak != null ? streak : defaultStreak;

        var root = new GameObject("Dash Arrow").transform;
        root.SetParent(parent, false);
        root.localScale = Vector3.one * (size / 1.65f);
        // Shifted so the whole shape (tip at +0.5, streaks to about -1.15) is centred
        var shape = new GameObject("Shape").transform;
        shape.SetParent(root, false);
        shape.localPosition = new Vector3(0f, 0f, 0.32f);

        // Front dart, then a smaller one tucked behind it
        Dart(shape, Vector3.zero, 1f, arrow);
        Dart(shape, new Vector3(0f, 0f, -0.42f), 0.72f, arrow);

        // Speed streaks, the middle one longest
        Streak(shape, new Vector3(-0.2f, 0f, -0.88f), 0.4f, streak);
        Streak(shape, new Vector3(0f, 0f, -0.98f), 0.6f, streak);
        Streak(shape, new Vector3(0.2f, 0f, -0.88f), 0.4f, streak);
        return root;
    }

    private static void Dart(Transform parent, Vector3 position, float scale, Material material)
    {
        var go = new GameObject("Dart", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = Vector3.one * scale;
        go.GetComponent<MeshFilter>().sharedMesh = DartMesh();
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private static void Streak(Transform parent, Vector3 position, float length, Material material)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.name = "Streak";
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        // Capsules stand along Y; lay it down along Z
        part.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        part.transform.localScale = new Vector3(0.13f, length * 0.5f, 0.13f);
        var r = part.GetComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // Arrowhead with a notch at the back, extruded to a slab
    private static Mesh DartMesh()
    {
        if (dartMesh != null) return dartMesh;

        // Outline counter-clockwise seen from above (x right, z forward)
        Vector2[] outline =
        {
            new Vector2(0f, 0.5f),      // tip
            new Vector2(-0.46f, -0.12f), // left wing
            new Vector2(0f, 0.1f),      // notch
            new Vector2(0.46f, -0.12f)  // right wing
        };
        int[] caps = { 0, 1, 2, 0, 2, 3 };
        const float half = 0.08f;

        var vertices = new System.Collections.Generic.List<Vector3>();
        var normals = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();

        void Face(Vector3[] points, Vector3 outward)
        {
            int start = vertices.Count;
            foreach (var p in points)
            {
                vertices.Add(p);
                normals.Add(outward);
            }
            for (int i = 1; i + 1 < points.Length; i++)
            {
                int b = start + i, c = start + i + 1;
                // Keep the front side facing outward
                if (Vector3.Dot(Vector3.Cross(points[i] - points[0], points[i + 1] - points[0]), outward) < 0f) (b, c) = (c, b);
                triangles.Add(start);
                triangles.Add(b);
                triangles.Add(c);
            }
        }

        Vector3 At(Vector2 p, float y) => new Vector3(p.x, y, p.y);
        for (int t = 0; t < caps.Length; t += 3)
        {
            Vector2 a = outline[caps[t]], b = outline[caps[t + 1]], c = outline[caps[t + 2]];
            Face(new[] { At(a, half), At(b, half), At(c, half) }, Vector3.up);
            Face(new[] { At(a, -half), At(b, -half), At(c, -half) }, Vector3.down);
        }
        for (int i = 0; i < outline.Length; i++)
        {
            Vector2 a = outline[i], b = outline[(i + 1) % outline.Length];
            Vector2 d = b - a;
            // Outward is to the right of each edge for a counter-clockwise outline
            Vector3 outward = new Vector3(d.y, 0f, -d.x).normalized;
            Face(new[] { At(a, half), At(b, half), At(b, -half), At(a, -half) }, outward);
        }

        dartMesh = new Mesh { name = "Dash Dart" };
        dartMesh.SetVertices(vertices);
        dartMesh.SetNormals(normals);
        dartMesh.SetTriangles(triangles, 0);
        dartMesh.RecalculateBounds();
        return dartMesh;
    }

    private static void EnsureDefaults()
    {
        if (defaultArrow == null) defaultArrow = Glowing(ArrowColor, 0.9f);
        if (defaultStreak == null) defaultStreak = Glowing(StreakColor, 0.7f);
    }

    public static Material Glowing(Color color, float glow)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", color);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * glow);
        return material;
    }
}
