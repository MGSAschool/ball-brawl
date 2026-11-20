using System.Collections.Generic;
using UnityEngine;

public class ArenaFloor : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private float centerRadius = 7f;
    [SerializeField] private float middleDiscRadius = 2.5f;
    [SerializeField] private int ringCount = 3;
    [SerializeField] private float targetSectionLength = 6f;
    [SerializeField] private float sectionGap = 0.08f;
    [SerializeField] private float thickness = 1f;

    [Header("Look")]
    [SerializeField] private Color warningColor = new Color(1f, 0.25f, 0.15f);
    [SerializeField] private Color[] ringColors =
    {
        new Color(0.96f, 0.91f, 0.78f),
        new Color(0.91f, 0.73f, 0.36f),
        new Color(0.55f, 0.76f, 0.32f),
        new Color(0.45f, 0.65f, 0.26f),
        new Color(0.85f, 0.70f, 0.48f)
    };
    [Range(0f, 0.5f)]
    [SerializeField] private float alternateShade = 0.12f;

    private readonly List<ArenaSection> sections = new List<ArenaSection>();
    private Material floorMaterial;
    private PhysicsMaterial floorPhysics;
    private Color fallbackColor = Color.gray;

    public Vector3 Center { get; private set; }
    public float TopY { get; private set; }
    public float Radius { get; private set; }
    public float Thickness { get { return thickness; } }
    public PhysicsMaterial FloorPhysics { get { return floorPhysics; } }
    public int RingColorCount { get { return ringColors.Length; } }
    public IReadOnlyList<ArenaSection> Sections { get { return sections; } }

    private void Awake()
    {
        Renderer sourceRenderer = GetComponent<Renderer>();
        if (sourceRenderer == null)
        {
            Debug.LogError("ArenaFloor needs to sit on the arena object (it reads the arena's renderer).", this);
            enabled = false;
            return;
        }

        ReadArenaShape(sourceRenderer);
        BuildRings();
    }

    public int CountSolid()
    {
        int count = 0;
        foreach (ArenaSection section in sections)
        {
            if (section.IsSolid) count++;
        }
        return count;
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

    public Color FloorColor(int ringIndex, int sectionIndex)
    {
        if (ringColors == null || ringColors.Length == 0) return fallbackColor;

        Color color = ringColors[Mathf.Min(ringIndex, ringColors.Length - 1)];
        if (sectionIndex % 2 == 1)
        {
            float keep = 1f - alternateShade;
            color = new Color(color.r * keep, color.g * keep, color.b * keep, color.a);
        }
        return color;
    }

    public int PiecesInRing(float innerRadius, float outerRadius, float pieceLength)
    {
        float middleCircumference = Mathf.PI * (innerRadius + outerRadius);
        int count = Mathf.Max(3, Mathf.RoundToInt(middleCircumference / pieceLength));
        if (alternateShade > 0f && count % 2 == 1) count++;
        return count;
    }

    public ArenaSection MakeSection(Transform parent, string name, float r0, float r1, float a0, float a1, Color color, bool isOuterEdge)
    {
        bool fullCircle = a1 - a0 >= Mathf.PI * 2f - 0.0001f;
        float half = sectionGap * 0.5f;
        float innerRadius = 0f;
        if (r0 > 0f) innerRadius = r0 + half;
        float outerRadius = r1 - half;
        if (isOuterEdge) outerRadius = r1;
        float angleInset = 0f;
        if (!fullCircle) angleInset = half / Mathf.Max(innerRadius, 1f);

        Mesh mesh = SectorMesh.Build(innerRadius, outerRadius, a0 + angleInset, a1 - angleInset, thickness, fullCircle);
        mesh.name = name;

        GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider), typeof(Rigidbody), typeof(SectionPaint), typeof(ArenaSection));
        go.transform.SetParent(parent, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;
        MeshCollider meshCollider = go.GetComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.sharedMaterial = floorPhysics;

        ArenaSection section = go.GetComponent<ArenaSection>();
        section.Init(r0, r1, a0, a1, color, warningColor);
        return section;
    }

    private void ReadArenaShape(Renderer sourceRenderer)
    {
        Collider sourceCollider = GetComponent<Collider>();
        Bounds bounds = sourceRenderer.bounds;
        Center = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        TopY = bounds.max.y;
        Radius = Mathf.Min(bounds.extents.x, bounds.extents.z);

        floorMaterial = sourceRenderer.sharedMaterial;
        if (sourceCollider != null) floorPhysics = sourceCollider.sharedMaterial;
        if (floorMaterial != null && floorMaterial.HasProperty("_BaseColor")) fallbackColor = floorMaterial.GetColor("_BaseColor");

        sourceRenderer.enabled = false;
        if (sourceCollider != null) sourceCollider.enabled = false;
    }

    private void BuildRings()
    {
        Transform root = new GameObject("Arena Sections").transform;
        root.SetParent(transform.parent, false);
        root.position = Center;

        sections.Add(MakeSection(root, "Middle Disc", 0f, middleDiscRadius, 0f, Mathf.PI * 2f, FloorColor(0, 0), false));

        List<Vector2> rings = new List<Vector2>();
        rings.Add(new Vector2(middleDiscRadius, centerRadius));
        float ringWidth = (Radius - centerRadius) / Mathf.Max(1, ringCount);
        for (int ring = 0; ring < ringCount; ring++)
        {
            rings.Add(new Vector2(centerRadius + ring * ringWidth, centerRadius + (ring + 1) * ringWidth));
        }

        for (int ring = 0; ring < rings.Count; ring++)
        {
            float r0 = rings[ring].x;
            float r1 = rings[ring].y;
            int count = PiecesInRing(r0, r1, targetSectionLength);
            float pieceAngle = Mathf.PI * 2f / count;
            float offset = ring * 0.5f * pieceAngle;
            bool outerEdge = r1 >= Radius - 0.01f;

            string ringName = "Ring " + ring;
            if (ring == 0) ringName = "Middle Ring";

            for (int i = 0; i < count; i++)
            {
                float a0 = offset + i * pieceAngle;
                string name = ringName + " Section " + (i + 1);
                sections.Add(MakeSection(root, name, r0, r1, a0, a0 + pieceAngle, FloorColor(ring + 1, i), outerEdge));
            }
        }
    }
}
