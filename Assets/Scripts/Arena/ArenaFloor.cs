using System.Collections.Generic;
using UnityEngine;

public class ArenaFloor : MonoBehaviour
{
    private static readonly int GltfColorId = Shader.PropertyToID("baseColorFactor");
    private static readonly int UrpColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField] private Color warningColor = new Color(1f, 0.25f, 0.15f);

    private readonly List<ArenaSection> sections = new List<ArenaSection>();
    private PhysicsMaterial floorPhysics;

    public Vector3 Center { get; private set; }
    public float TopY { get; private set; }
    public float Radius { get; private set; }
    public IReadOnlyList<ArenaSection> Sections { get { return sections; } }

    private void Awake()
    {
        Renderer sourceRenderer = GetComponent<Renderer>();
        if (sourceRenderer == null)
        {
            Debug.LogError("ArenaFloor needs to sit on the arena object (it reads the arena's size from its renderer).", this);
            enabled = false;
            return;
        }
        ReadArenaShape(sourceRenderer);

        GameArt art = GameArt.Instance;
        if (art == null || art.arenaModel == null)
        {
            Debug.LogError("ArenaFloor: drag Models/Arena.glb into the Arena Model slot of Resources/GameArt.asset.", this);
            return;
        }
        BuildFromModel(art.arenaModel);
    }

    public bool TryGetRandomSolidPoint(float minRadius, out Vector3 point)
    {
        List<ArenaSection> options = new List<ArenaSection>();
        foreach (ArenaSection section in sections)
        {
            if (section.IsSolid && section.OuterRadius > minRadius) options.Add(section);
        }

        if (options.Count == 0)
        {
            point = Center;
            return false;
        }

        ArenaSection picked = options[Random.Range(0, options.Count)];
        point = picked.RandomTopPoint(Center, TopY, 1.5f);
        return true;
    }

    private void ReadArenaShape(Renderer sourceRenderer)
    {
        Collider sourceCollider = GetComponent<Collider>();
        Bounds bounds = sourceRenderer.bounds;
        Center = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        TopY = bounds.max.y;
        Radius = Mathf.Min(bounds.extents.x, bounds.extents.z);
        if (sourceCollider != null) floorPhysics = sourceCollider.sharedMaterial;

        sourceRenderer.enabled = false;
        if (sourceCollider != null) sourceCollider.enabled = false;
    }

    private void BuildFromModel(GameObject modelPrefab)
    {
        GameObject model = Instantiate(modelPrefab, Vector3.zero, Quaternion.identity, transform.parent);
        model.name = "Arena Model";

        float modelRadius = 0f;
        float modelTop = float.NegativeInfinity;
        MeshRenderer[] pieces = model.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer piece in pieces)
        {
            Bounds b = piece.bounds;
            modelRadius = Mathf.Max(modelRadius, Mathf.Abs(b.min.x), Mathf.Abs(b.max.x), Mathf.Abs(b.min.z), Mathf.Abs(b.max.z));
            modelTop = Mathf.Max(modelTop, b.max.y);
        }
        if (pieces.Length == 0 || modelRadius <= 0f)
        {
            Debug.LogError("ArenaFloor: the arena model has no mesh pieces.", this);
            return;
        }

        float scale = Radius / modelRadius;
        model.transform.localScale = model.transform.localScale * scale;
        model.transform.position = new Vector3(Center.x, TopY - modelTop * scale, Center.z);

        foreach (MeshRenderer piece in pieces)
        {
            ArenaSection section = MakeSection(piece);
            if (section != null) sections.Add(section);
        }
    }

    private ArenaSection MakeSection(MeshRenderer piece)
    {
        MeshFilter filter = piece.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null) return null;

        GameObject go = piece.gameObject;
        MeshCollider meshCollider = go.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = filter.sharedMesh;
        meshCollider.sharedMaterial = floorPhysics;
        go.AddComponent<Rigidbody>();
        ArenaSection section = go.AddComponent<ArenaSection>();

        float innerRadius;
        float outerRadius;
        float startAngle;
        float endAngle;
        MeasurePiece(filter, out innerRadius, out outerRadius, out startAngle, out endAngle);
        section.Init(innerRadius, outerRadius, startAngle, endAngle, PieceColor(piece), warningColor);
        return section;
    }

    private void MeasurePiece(MeshFilter filter, out float innerRadius, out float outerRadius, out float startAngle, out float endAngle)
    {
        Vector3[] vertices = filter.sharedMesh.vertices;
        Transform piece = filter.transform;

        Vector3 middle = piece.GetComponent<Renderer>().bounds.center - Center;
        middle.y = 0f;
        float middleAngle = Mathf.Atan2(middle.z, middle.x);

        innerRadius = float.PositiveInfinity;
        outerRadius = 0f;
        float lowest = 0f;
        float highest = 0f;
        foreach (Vector3 vertex in vertices)
        {
            Vector3 offset = piece.TransformPoint(vertex) - Center;
            offset.y = 0f;
            float distance = offset.magnitude;
            innerRadius = Mathf.Min(innerRadius, distance);
            outerRadius = Mathf.Max(outerRadius, distance);

            if (distance < 0.01f) continue;
            float turn = Mathf.DeltaAngle(middleAngle * Mathf.Rad2Deg, Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            lowest = Mathf.Min(lowest, turn);
            highest = Mathf.Max(highest, turn);
        }

        bool isMiddleDisc = middle.magnitude < Radius * 0.05f;
        if (isMiddleDisc)
        {
            innerRadius = 0f;
            startAngle = 0f;
            endAngle = Mathf.PI * 2f;
        }
        else
        {
            startAngle = middleAngle + lowest;
            endAngle = middleAngle + highest;
        }
    }

    private Color PieceColor(MeshRenderer piece)
    {
        Material material = piece.sharedMaterial;
        if (material == null) return Color.white;
        if (material.HasProperty(GltfColorId)) return material.GetColor(GltfColorId);
        if (material.HasProperty(UrpColorId)) return material.GetColor(UrpColorId);
        return material.color;
    }
}
