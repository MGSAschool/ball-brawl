using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class DashArrow
{
    public static readonly Color ArrowColor = new Color(0.2f, 0.85f, 1f);
    public static readonly Color StreakColor = new Color(0.8f, 0.97f, 1f);

    private const float Half = 0.08f;

    private static Mesh dartMesh;
    private static Material defaultArrow;
    private static Material defaultStreak;

    public static Transform Build(Transform parent, float size, Material arrow = null, Material streak = null)
    {
        if (defaultArrow == null) defaultArrow = ArtUtil.GlowingLit(ArrowColor, 0.9f);
        if (defaultStreak == null) defaultStreak = ArtUtil.GlowingLit(StreakColor, 0.7f);
        if (arrow == null) arrow = defaultArrow;
        if (streak == null) streak = defaultStreak;

        Transform root = new GameObject("Dash Arrow").transform;
        root.SetParent(parent, false);
        root.localScale = Vector3.one * (size / 1.65f);

        Transform shape = new GameObject("Shape").transform;
        shape.SetParent(root, false);
        shape.localPosition = new Vector3(0f, 0f, 0.32f);

        AddDart(shape, Vector3.zero, 1f, arrow);
        AddDart(shape, new Vector3(0f, 0f, -0.42f), 0.72f, arrow);

        AddStreak(shape, new Vector3(-0.2f, 0f, -0.88f), 0.4f, streak);
        AddStreak(shape, new Vector3(0f, 0f, -0.98f), 0.6f, streak);
        AddStreak(shape, new Vector3(0.2f, 0f, -0.88f), 0.4f, streak);
        return root;
    }

    private static void AddDart(Transform parent, Vector3 position, float scale, Material material)
    {
        GameObject go = new GameObject("Dart", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = Vector3.one * scale;
        go.GetComponent<MeshFilter>().sharedMesh = DartMesh();

        MeshRenderer rend = go.GetComponent<MeshRenderer>();
        rend.sharedMaterial = material;
        rend.shadowCastingMode = ShadowCastingMode.Off;
    }

    private static void AddStreak(Transform parent, Vector3 position, float length, Material material)
    {
        Transform streak = ArtUtil.MakeShape(PrimitiveType.Capsule, "Streak", parent, material);
        streak.localPosition = position;
        streak.localRotation = Quaternion.Euler(90f, 0f, 0f);
        streak.localScale = new Vector3(0.13f, length * 0.5f, 0.13f);
    }

    private static Mesh DartMesh()
    {
        if (dartMesh != null) return dartMesh;

        Vector2[] outline =
        {
            new Vector2(0f, 0.5f),
            new Vector2(-0.46f, -0.12f),
            new Vector2(0f, 0.1f),
            new Vector2(0.46f, -0.12f)
        };

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();

        AddCap(outline[0], outline[1], outline[2], vertices, normals, triangles);
        AddCap(outline[0], outline[2], outline[3], vertices, normals, triangles);

        for (int i = 0; i < outline.Length; i++)
        {
            Vector2 a = outline[i];
            Vector2 b = outline[(i + 1) % outline.Length];
            Vector2 edge = b - a;
            Vector3 outward = new Vector3(edge.y, 0f, -edge.x).normalized;
            Vector3[] side = { At(a, Half), At(b, Half), At(b, -Half), At(a, -Half) };
            AddFace(side, outward, vertices, normals, triangles);
        }

        dartMesh = new Mesh();
        dartMesh.name = "Dash Dart";
        dartMesh.SetVertices(vertices);
        dartMesh.SetNormals(normals);
        dartMesh.SetTriangles(triangles, 0);
        dartMesh.RecalculateBounds();
        return dartMesh;
    }

    private static void AddCap(Vector2 a, Vector2 b, Vector2 c, List<Vector3> vertices, List<Vector3> normals, List<int> triangles)
    {
        Vector3[] top = { At(a, Half), At(b, Half), At(c, Half) };
        Vector3[] bottom = { At(a, -Half), At(b, -Half), At(c, -Half) };
        AddFace(top, Vector3.up, vertices, normals, triangles);
        AddFace(bottom, Vector3.down, vertices, normals, triangles);
    }

    private static void AddFace(Vector3[] points, Vector3 outward, List<Vector3> vertices, List<Vector3> normals, List<int> triangles)
    {
        int start = vertices.Count;
        foreach (Vector3 point in points)
        {
            vertices.Add(point);
            normals.Add(outward);
        }

        for (int i = 1; i + 1 < points.Length; i++)
        {
            int b = start + i;
            int c = start + i + 1;
            Vector3 normal = Vector3.Cross(points[i] - points[0], points[i + 1] - points[0]);
            if (Vector3.Dot(normal, outward) < 0f)
            {
                int swap = b;
                b = c;
                c = swap;
            }
            triangles.Add(start);
            triangles.Add(b);
            triangles.Add(c);
        }
    }

    private static Vector3 At(Vector2 point, float y)
    {
        return new Vector3(point.x, y, point.y);
    }
}
