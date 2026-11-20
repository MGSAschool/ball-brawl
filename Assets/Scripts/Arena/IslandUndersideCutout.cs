using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class IslandUndersideCutout : MonoBehaviour
{
    [SerializeField] private float radius = 90f;
    [SerializeField] private float belowHeight = -14f;
    [Range(0f, 1f)]
    [SerializeField] private float downward = 0.05f;

    private Mesh cutMesh;

    private void Awake()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        Mesh source = filter.sharedMesh;
        if (source == null || !source.isReadable) return;

        cutMesh = Instantiate(source);
        cutMesh.name = source.name + " (no underside)";
        Vector3[] vertices = cutMesh.vertices;
        int removed = 0;

        for (int sub = 0; sub < cutMesh.subMeshCount; sub++)
        {
            int[] triangles = cutMesh.GetTriangles(sub);
            List<int> kept = new List<int>(triangles.Length);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = transform.TransformPoint(vertices[triangles[i]]);
                Vector3 b = transform.TransformPoint(vertices[triangles[i + 1]]);
                Vector3 c = transform.TransformPoint(vertices[triangles[i + 2]]);
                if (IsUnderside(a, b, c))
                {
                    removed++;
                    continue;
                }
                kept.Add(triangles[i]);
                kept.Add(triangles[i + 1]);
                kept.Add(triangles[i + 2]);
            }
            cutMesh.SetTriangles(kept, sub);
        }

        filter.sharedMesh = cutMesh;
        if (removed == 0) Debug.LogWarning("IslandUndersideCutout: nothing matched; check the radius and height");
    }

    private void OnDestroy()
    {
        if (cutMesh != null) Destroy(cutMesh);
    }

    private bool IsUnderside(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 middle = (a + b + c) / 3f;
        if (middle.y > belowHeight) return false;
        if (new Vector2(middle.x, middle.z).magnitude > radius) return false;

        Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
        return normal.y < -downward;
    }
}
