using System.Collections.Generic;
using UnityEngine;

// Removes the rocky underside of the floating island the stadium sits on, so the sky shows
// through the stadium's open middle whenever arena sections fall away. Only the rendered mesh is
// cut; the collider keeps the original shape. The small floating islands further out keep theirs.
[RequireComponent(typeof(MeshFilter))]
public class IslandUndersideCutout : MonoBehaviour
{
    [SerializeField, Tooltip("Faces whose centre is closer than this to the arena (horizontally) can be removed")]
    private float radius = 90f;
    [SerializeField, Tooltip("Only faces whose centre is below this height are removed, so the stadium walls stay")]
    private float belowHeight = -14f;
    [SerializeField, Range(0f, 1f), Tooltip("How much a face must point downward to count as underside. Kept low so the island's sloped sides go too, since the camera sees them at an angle through the opening")]
    private float downward = 0.05f;

    private Mesh cutMesh;

    private void Awake()
    {
        var filter = GetComponent<MeshFilter>();
        var source = filter.sharedMesh;
        if (source == null || !source.isReadable) return;

        cutMesh = Instantiate(source);
        cutMesh.name = source.name + " (no underside)";
        var vertices = cutMesh.vertices;
        int removed = 0;
        for (int sub = 0; sub < cutMesh.subMeshCount; sub++)
        {
            var triangles = cutMesh.GetTriangles(sub);
            var kept = new List<int>(triangles.Length);
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

    private bool IsUnderside(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 centre = (a + b + c) / 3f;
        if (centre.y > belowHeight) return false;
        if (new Vector2(centre.x, centre.z).magnitude > radius) return false;
        return Vector3.Cross(b - a, c - a).normalized.y < -downward;
    }

    private void OnDestroy()
    {
        if (cutMesh != null) Destroy(cutMesh);
    }
}
