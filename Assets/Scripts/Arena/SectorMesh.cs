using System.Collections.Generic;
using UnityEngine;

public static class SectorMesh
{
    private static readonly List<Vector3> vertices = new List<Vector3>();
    private static readonly List<Vector3> normals = new List<Vector3>();
    private static readonly List<Vector2> uvs = new List<Vector2>();
    private static readonly List<int> triangles = new List<int>();

    public static Mesh Build(float innerRadius, float outerRadius, float startAngle, float endAngle, float thickness, bool fullCircle)
    {
        vertices.Clear();
        normals.Clear();
        uvs.Clear();
        triangles.Clear();

        float r0 = innerRadius;
        float r1 = outerRadius;
        float bottom = -thickness;
        int steps = Mathf.Max(2, Mathf.CeilToInt((endAngle - startAngle) / (5f * Mathf.Deg2Rad)));

        for (int i = 0; i < steps; i++)
        {
            float a = Mathf.Lerp(startAngle, endAngle, (float)i / steps);
            float b = Mathf.Lerp(startAngle, endAngle, (float)(i + 1) / steps);
            float middle = (a + b) * 0.5f;
            Vector3 outward = new Vector3(Mathf.Cos(middle), 0f, Mathf.Sin(middle));

            AddQuad(Point(r0, a, 0f), Point(r1, a, 0f), Point(r1, b, 0f), Point(r0, b, 0f), Vector3.up);
            AddQuad(Point(r0, a, bottom), Point(r1, a, bottom), Point(r1, b, bottom), Point(r0, b, bottom), Vector3.down);
            AddQuad(Point(r1, a, 0f), Point(r1, b, 0f), Point(r1, b, bottom), Point(r1, a, bottom), outward);
            if (r0 > 0f)
            {
                AddQuad(Point(r0, a, 0f), Point(r0, b, 0f), Point(r0, b, bottom), Point(r0, a, bottom), -outward);
            }
        }

        if (!fullCircle)
        {
            Vector3 startSide = new Vector3(-Mathf.Sin(startAngle), 0f, Mathf.Cos(startAngle));
            Vector3 endSide = new Vector3(-Mathf.Sin(endAngle), 0f, Mathf.Cos(endAngle));
            AddQuad(Point(r0, startAngle, 0f), Point(r1, startAngle, 0f), Point(r1, startAngle, bottom), Point(r0, startAngle, bottom), -startSide);
            AddQuad(Point(r0, endAngle, 0f), Point(r1, endAngle, 0f), Point(r1, endAngle, bottom), Point(r0, endAngle, bottom), endSide);
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Vector3 Point(float radius, float angle, float y)
    {
        return new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
    }

    private static void AddQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 outward)
    {
        Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0);
        if (normal.sqrMagnitude < 1e-10f) normal = Vector3.Cross(v2 - v0, v3 - v0);
        if (Vector3.Dot(normal, outward) < 0f)
        {
            Vector3 swap = v1;
            v1 = v3;
            v3 = swap;
        }

        int start = vertices.Count;
        AddVertex(v0, outward);
        AddVertex(v1, outward);
        AddVertex(v2, outward);
        AddVertex(v3, outward);

        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 3);
    }

    private static void AddVertex(Vector3 position, Vector3 outward)
    {
        vertices.Add(position);
        normals.Add(outward.normalized);

        bool flat = Mathf.Abs(outward.y) > 0.5f;
        if (flat) uvs.Add(new Vector2(position.x, position.z) * 0.1f);
        else uvs.Add(new Vector2(position.x + position.z, position.y) * 0.1f);
    }
}
