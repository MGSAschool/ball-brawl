using System.Collections.Generic;
using UnityEngine;

// "Falling Platforms" stage obstacle. At runtime the round arena is rebuilt as a safe
// center disc plus rings of curved sections; during a match random sections crumble
// into the void and later rise back. Put this on the arena object: its renderer and
// collider are only used for size, material and as the edit-time preview.
public class FallingPlatforms : MonoBehaviour
{
    [Header("Layout")]
    [Tooltip("Center disc that never falls. Keep spawn points inside it.")]
    [SerializeField] private float safeRadius = 16f;
    [SerializeField] private int ringCount = 3;
    [Tooltip("Rough arc length of each section, in metres.")]
    [SerializeField] private float targetSectionLength = 14f;
    [SerializeField] private float sectionGap = 0.08f;
    [SerializeField] private float thickness = 1f;

    [Header("Timing")]
    [SerializeField] private Vector2 fallInterval = new Vector2(5f, 9f);
    [SerializeField] private float firstFallDelay = 10f;
    [SerializeField] private int maxDownAtOnce = 3;
    [SerializeField] private float warningDuration = 2f;
    [SerializeField] private float fallDuration = 1.2f;
    [SerializeField] private float goneDuration = 8f;
    [SerializeField] private float riseDuration = 1.5f;
    [SerializeField] private float fallDistance = 25f;

    [Header("Look")]
    [SerializeField] private Color warningColor = new Color(1f, 0.25f, 0.15f);

    [SerializeField] private MatchManager match;

    public Vector3 Center { get; private set; }
    public float TopY { get; private set; }
    public float Radius { get; private set; }
    public IReadOnlyList<ArenaSection> Sections => sections;

    private readonly List<ArenaSection> sections = new List<ArenaSection>();
    private bool active;
    private bool explainedThisMatch;
    private float nextFallTime;

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        BuildSections();
    }

    private void OnEnable()
    {
        if (match != null) match.StateChanged += OnMatchStateChanged;
    }

    private void OnDisable()
    {
        if (match != null) match.StateChanged -= OnMatchStateChanged;
    }

    private void OnMatchStateChanged(MatchManager.MatchState state)
    {
        active = state == MatchManager.MatchState.Playing;
        if (active)
        {
            nextFallTime = Time.time + firstFallDelay;
            explainedThisMatch = false;
        }
        else
        {
            ResetAll();
        }
    }

    private void Update()
    {
        if (!active || Time.time < nextFallTime) return;
        nextFallTime = Time.time + Random.Range(fallInterval.x, fallInterval.y);

        int down = 0;
        var candidates = new List<ArenaSection>();
        foreach (var section in sections)
        {
            if (!section.IsSolid) down++;
            else if (section.CanFall) candidates.Add(section);
        }
        if (down >= maxDownAtOnce || candidates.Count == 0) return;

        candidates[Random.Range(0, candidates.Count)]
            .TriggerFall(warningDuration, fallDuration, goneDuration, riseDuration, fallDistance);

        // Explain the hazard the first time it happens each match
        if (!explainedThisMatch && match != null)
        {
            explainedThisMatch = true;
            match.Announce("Red, shaking floor is about to fall - get off it!");
        }
    }

    public void ResetAll()
    {
        foreach (var section in sections) section.ResetImmediate();
    }

    // A random standing spot on a section that is currently solid, at least minRadius from the center
    public bool TryGetRandomSolidPoint(float minRadius, out Vector3 point)
    {
        var options = sections.FindAll(s => s.IsSolid && s.OuterRadius > minRadius);
        if (options.Count == 0)
        {
            point = Center;
            return false;
        }
        point = options[Random.Range(0, options.Count)].RandomTopPoint(Center, TopY, 1.5f);
        return true;
    }

    // --- Arena construction ---
    private void BuildSections()
    {
        var sourceRenderer = GetComponent<Renderer>();
        var sourceCollider = GetComponent<Collider>();
        if (sourceRenderer == null)
        {
            Debug.LogError("FallingPlatforms needs to sit on the arena object (it reads the arena's renderer).", this);
            enabled = false;
            return;
        }

        Bounds bounds = sourceRenderer.bounds;
        Center = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        TopY = bounds.max.y;
        Radius = Mathf.Min(bounds.extents.x, bounds.extents.z);

        Material material = sourceRenderer.sharedMaterial;
        PhysicsMaterial physicsMaterial = sourceCollider != null ? sourceCollider.sharedMaterial : null;
        Color baseColor = material != null && material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.gray;

        // The original disc is replaced by the sections at runtime
        sourceRenderer.enabled = false;
        if (sourceCollider != null) sourceCollider.enabled = false;

        var root = new GameObject("Arena Sections").transform;
        root.SetParent(transform.parent, false);
        root.position = Center;

        CreateSection(root, "Safe Center", 0f, safeRadius, 0f, Mathf.PI * 2f, false, material, physicsMaterial, baseColor);

        float ringWidth = (Radius - safeRadius) / Mathf.Max(1, ringCount);
        for (int ring = 0; ring < ringCount; ring++)
        {
            float r0 = safeRadius + ring * ringWidth;
            float r1 = r0 + ringWidth;
            float midCircumference = Mathf.PI * (r0 + r1);
            int count = Mathf.Max(3, Mathf.RoundToInt(midCircumference / targetSectionLength));
            // Stagger rings so seams don't line up into long straight cracks
            float offset = ring * 0.5f * (Mathf.PI * 2f / count);

            for (int i = 0; i < count; i++)
            {
                float a0 = offset + i * Mathf.PI * 2f / count;
                float a1 = a0 + Mathf.PI * 2f / count;
                CreateSection(root, $"Ring {ring + 1} Section {i + 1}", r0, r1, a0, a1, true, material, physicsMaterial, baseColor);
            }
        }
    }

    private void CreateSection(Transform root, string name, float r0, float r1, float a0, float a1, bool canFall,
        Material material, PhysicsMaterial physicsMaterial, Color baseColor)
    {
        bool fullCircle = a1 - a0 >= Mathf.PI * 2f - 0.0001f;
        bool outerEdge = r1 >= Radius - 0.01f;

        // Shrink by half the gap on every shared edge so neighbours read as separate pieces
        float half = sectionGap * 0.5f;
        float innerR = r0 > 0f ? r0 + half : 0f;
        float outerR = outerEdge ? r1 : r1 - half;
        float angleInset = fullCircle ? 0f : half / Mathf.Max(innerR, 1f);
        Mesh mesh = BuildSectorMesh(innerR, outerR, a0 + angleInset, a1 - angleInset, thickness, fullCircle);
        mesh.name = name;

        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider), typeof(Rigidbody), typeof(ArenaSection));
        go.transform.SetParent(root, false);
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        var meshCollider = go.GetComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.sharedMaterial = physicsMaterial;

        var section = go.GetComponent<ArenaSection>();
        section.Init(r0, r1, a0, a1, baseColor, warningColor);
        section.CanFall = canFall;
        sections.Add(section);
    }

    // Annular sector slab: top at y = 0, bottom at y = -thickness
    private static Mesh BuildSectorMesh(float r0, float r1, float a0, float a1, float thickness, bool fullCircle)
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        int steps = Mathf.Max(2, Mathf.CeilToInt((a1 - a0) / (5f * Mathf.Deg2Rad)));
        Vector3 Point(float r, float a, float y) => new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);

        void AddQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 outward)
        {
            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0);
            if (normal.sqrMagnitude < 1e-10f)
            {
                normal = Vector3.Cross(v2 - v0, v3 - v0);
            }
            if (Vector3.Dot(normal, outward) < 0f)
            {
                (v1, v3) = (v3, v1);
            }

            int start = vertices.Count;
            foreach (var v in new[] { v0, v1, v2, v3 })
            {
                vertices.Add(v);
                normals.Add(outward.normalized);
                // Planar mapping keeps the texture continuous across sections
                uvs.Add(Mathf.Abs(outward.y) > 0.5f ? new Vector2(v.x, v.z) * 0.1f : new Vector2(v.x + v.z, v.y) * 0.1f);
            }
            triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }

        for (int i = 0; i < steps; i++)
        {
            float ta = Mathf.Lerp(a0, a1, (float)i / steps);
            float tb = Mathf.Lerp(a0, a1, (float)(i + 1) / steps);
            float mid = (ta + tb) * 0.5f;
            Vector3 radial = new Vector3(Mathf.Cos(mid), 0f, Mathf.Sin(mid));

            AddQuad(Point(r0, ta, 0f), Point(r1, ta, 0f), Point(r1, tb, 0f), Point(r0, tb, 0f), Vector3.up);
            AddQuad(Point(r0, ta, -thickness), Point(r1, ta, -thickness), Point(r1, tb, -thickness), Point(r0, tb, -thickness), Vector3.down);
            AddQuad(Point(r1, ta, 0f), Point(r1, tb, 0f), Point(r1, tb, -thickness), Point(r1, ta, -thickness), radial);
            if (r0 > 0f)
            {
                AddQuad(Point(r0, ta, 0f), Point(r0, tb, 0f), Point(r0, tb, -thickness), Point(r0, ta, -thickness), -radial);
            }
        }

        if (!fullCircle)
        {
            Vector3 startTangent = new Vector3(-Mathf.Sin(a0), 0f, Mathf.Cos(a0));
            Vector3 endTangent = new Vector3(-Mathf.Sin(a1), 0f, Mathf.Cos(a1));
            AddQuad(Point(r0, a0, 0f), Point(r1, a0, 0f), Point(r1, a0, -thickness), Point(r0, a0, -thickness), -startTangent);
            AddQuad(Point(r0, a1, 0f), Point(r1, a1, 0f), Point(r1, a1, -thickness), Point(r0, a1, -thickness), endTangent);
        }

        var mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }
}
