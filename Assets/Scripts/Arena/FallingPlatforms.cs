using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// "Falling Platforms" stage obstacle. At runtime the round arena is rebuilt as a small middle
// disc plus rings of curved sections; every piece, the middle included, can fall.
// Falls speed up every `speedUpEvery` seconds. A match runs in three phases (timer counts down):
//   Normal    - groups of sections crumble into the void and later rise back.
//   Permanent - from `permanentFallsAt` left, anything that falls stays gone.
//   Island    - at `islandAppearsAt` left, a cluster of pads rises beside the arena across a
//               jumpable gap, respawns move onto it, and the old arena keeps crumbling until it
//               is gone `arenaGoneBy` seconds before the end. Island pads take turns freezing
//               into slippery ice (flashing blue first, like the red fall warning).
// Put this on the arena object: its renderer and collider are only used for size,
// material and as the edit-time preview.
public class FallingPlatforms : MonoBehaviour
{
    private enum Phase { Normal, Permanent, Island }

    [Header("Layout")]
    [Tooltip("Radius of the middle area. It is split into a small disc plus a ring of pieces, and all of it can fall.")]
    [FormerlySerializedAs("safeRadius")]
    [SerializeField] private float centerRadius = 7f;
    [SerializeField] private float middleDiscRadius = 2.5f;
    [SerializeField] private int ringCount = 3;
    [Tooltip("Rough arc length of each section, in metres.")]
    [SerializeField] private float targetSectionLength = 6f;
    [SerializeField] private float sectionGap = 0.08f;
    [SerializeField] private float thickness = 1f;

    [Header("Falling")]
    [Tooltip("How many sections crumble together each time (min, max).")]
    [SerializeField] private Vector2Int piecesPerFall = new Vector2Int(2, 3);
    [SerializeField] private Vector2 fallInterval = new Vector2(4f, 7f);
    [SerializeField] private float firstFallDelay = 8f;
    [Tooltip("Normal phase only: cap on sections that are down at the same time (grows as falls speed up).")]
    [SerializeField] private int maxDownAtOnce = 6;
    [SerializeField] private float warningDuration = 2f;
    [SerializeField] private float fallDuration = 1.2f;
    [SerializeField] private float goneDuration = 8f;
    [SerializeField] private float riseDuration = 1.5f;
    [SerializeField] private float fallDistance = 25f;

    [Header("Speed Up")]
    [Tooltip("Every this many seconds of match time, platforms fall faster.")]
    [SerializeField] private float speedUpEvery = 30f;
    [Tooltip("Time between falls is multiplied by this at each speed-up.")]
    [SerializeField] private float intervalMultiplier = 0.8f;
    [Tooltip("Warning and drop time are multiplied by this at each speed-up.")]
    [SerializeField] private float timingMultiplier = 0.9f;
    [SerializeField] private float minWarningDuration = 0.9f;
    [Tooltip("Extra sections allowed down at once per speed-up.")]
    [SerializeField] private int extraDownPerSpeedUp = 2;

    [Header("Final Minute (seconds left on the match timer)")]
    [SerializeField] private float permanentFallsAt = 80f;
    [SerializeField] private float islandAppearsAt = 60f;
    [Tooltip("The old arena is fully gone this many seconds before the end.")]
    [SerializeField] private float arenaGoneBy = 10f;
    [SerializeField] private float finalPhaseMaxInterval = 4f;

    [Header("Island")]
    [Tooltip("Middle pad of the island, where respawns land.")]
    [SerializeField] private float hubRadius = 3.5f;
    [Tooltip("Inner and outer radius of the ring of pads around the hub. The space between them and the hub is a jump.")]
    [SerializeField] private Vector2 padRing = new Vector2(5.5f, 8.5f);
    [SerializeField] private int padCount = 6;
    [Tooltip("Angle each pad covers; the rest of each slice is a gap to jump.")]
    [SerializeField] private float padArcDegrees = 40f;
    [Tooltip("Every other pad sits this much higher (jump height is about 1.2m).")]
    [SerializeField] private float raisedPadHeight = 0.6f;
    [Tooltip("Gap between the arena edge and the nearest pad. ~3m needs a jump; a running jump clears about 6m.")]
    [SerializeField] private float islandGap = 3f;
    [SerializeField] private float islandRiseDuration = 3f;
    [Tooltip("Pick a different side of the arena for the island every match.")]
    [SerializeField] private bool randomizeIslandSide = true;
    [Tooltip("Island direction in degrees when not randomized (90 = far side from the default camera).")]
    [SerializeField] private float islandAngle = 90f;

    [Header("Island Freezing")]
    [SerializeField] private Vector2 freezeInterval = new Vector2(4f, 7f);
    [SerializeField] private Vector2Int padsPerFreeze = new Vector2Int(1, 2);
    [SerializeField] private float firstFreezeDelay = 5f;
    [SerializeField] private float freezeWarningDuration = 2f;
    [SerializeField] private float frozenDuration = 5f;

    [Header("Look")]
    [SerializeField] private Color warningColor = new Color(1f, 0.25f, 0.15f);
    [SerializeField] private Color iceColor = new Color(0.55f, 0.85f, 1f);

    [SerializeField] private MatchManager match;

    public Vector3 Center { get; private set; }
    public float TopY { get; private set; }
    public float Radius { get; private set; }
    public IReadOnlyList<ArenaSection> Sections => sections;

    private readonly List<ArenaSection> sections = new List<ArenaSection>();
    private readonly List<ArenaSection> islandPads = new List<ArenaSection>();
    private readonly List<Vector3> islandPadOffsets = new List<Vector3>();
    private Transform islandRoot;
    private Transform[] islandSpawns;
    private PhysicsMaterial iceMaterial;
    private bool active;
    private bool explainedFalls;
    private bool explainedIce;
    private float matchStartTime;
    private float nextFallTime;
    private float nextFreezeTime;
    private int speedLevel;
    private Phase phase = Phase.Normal;

    private float IntervalScale => Mathf.Pow(intervalMultiplier, speedLevel);
    private float CurrentWarning => Mathf.Max(minWarningDuration, warningDuration * Mathf.Pow(timingMultiplier, speedLevel));
    private float CurrentFall => fallDuration * Mathf.Pow(timingMultiplier, speedLevel);

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        BuildSections();
    }

    private void OnEnable()
    {
        if (match == null) return;
        match.StateChanged += OnMatchStateChanged;
        match.SpawnPointCheck = IsSafeToSpawn;
        match.SpawnFallback = FindFallbackSpawn;
    }

    private void OnDisable()
    {
        if (match == null) return;
        match.StateChanged -= OnMatchStateChanged;
        match.SpawnPointCheck = null;
        match.SpawnFallback = null;
    }

    private void OnMatchStateChanged(MatchManager.MatchState state)
    {
        active = state == MatchManager.MatchState.Playing;
        if (active)
        {
            matchStartTime = Time.time;
            nextFallTime = Time.time + firstFallDelay;
            speedLevel = 0;
            explainedFalls = false;
            explainedIce = false;
        }
        else
        {
            ResetAll();
        }
    }

    private void Update()
    {
        if (!active || match == null) return;

        int level = Mathf.FloorToInt((Time.time - matchStartTime) / Mathf.Max(1f, speedUpEvery));
        float timeLeft = match.TimeRemaining;
        if (level > speedLevel)
        {
            speedLevel = level;
            // Once the island is coming, its own message matters more
            if (phase != Phase.Island && timeLeft > islandAppearsAt + 1f) match.Announce("The floor is crumbling faster!");
        }

        if (phase == Phase.Normal && timeLeft <= permanentFallsAt) EnterPermanentPhase();
        if (phase == Phase.Permanent && timeLeft <= islandAppearsAt) RaiseIsland();

        if (phase == Phase.Island && Time.time >= nextFreezeTime)
        {
            nextFreezeTime = Time.time + Random.Range(freezeInterval.x, freezeInterval.y);
            FreezePads();
        }

        if (Time.time < nextFallTime) return;
        nextFallTime = Time.time + NextInterval(timeLeft);
        DropGroup();
    }

    private void EnterPermanentPhase()
    {
        phase = Phase.Permanent;
        // Anything already down, or falling from now on, never comes back
        foreach (var section in sections) section.StayDown = true;
        match.Announce("Fallen floor won't come back now!");
    }

    private void RaiseIsland()
    {
        phase = Phase.Island;
        foreach (var pad in islandPads) pad.Emerge(islandRiseDuration, fallDistance);
        match.SetSpawnOverride(islandSpawns);
        nextFreezeTime = Time.time + islandRiseDuration + firstFreezeDelay;

        // Ice needs a helper on each ball; it leaves the player's own movement code alone
        foreach (var entry in match.Players)
        {
            if (entry.Player != null && !entry.Player.TryGetComponent<IceSlide>(out _)) entry.Player.gameObject.AddComponent<IceSlide>();
        }
        match.Announce("NEW PLATFORM! Jump to it before the arena falls!");
    }

    private float NextInterval(float timeLeft)
    {
        if (phase != Phase.Island) return Random.Range(fallInterval.x, fallInterval.y) * IntervalScale;

        // Final phase: pace the drops so the old arena is gone `arenaGoneBy` seconds before the end
        int solid = sections.FindAll(s => s.IsSolid).Count;
        float averageGroup = (piecesPerFall.x + piecesPerFall.y) * 0.5f;
        int groupsLeft = Mathf.Max(1, Mathf.CeilToInt(solid / Mathf.Max(1f, averageGroup)));
        float timeForDrops = timeLeft - arenaGoneBy - CurrentWarning;
        return Mathf.Clamp(timeForDrops / groupsLeft, 0.5f, finalPhaseMaxInterval * IntervalScale);
    }

    private void DropGroup()
    {
        int down = 0;
        var candidates = new List<ArenaSection>();
        foreach (var section in sections)
        {
            if (!section.IsSolid) { down++; continue; }
            if (section.CanFall) candidates.Add(section);
        }
        if (candidates.Count == 0) return;

        int count = Random.Range(piecesPerFall.x, piecesPerFall.y + 1);
        if (phase == Phase.Normal)
        {
            int room = maxDownAtOnce + speedLevel * extraDownPerSpeedUp - down;
            if (room < piecesPerFall.x) return;
            count = Mathf.Min(count, room);
        }
        count = Mathf.Min(count, candidates.Count);

        for (int i = 0; i < count; i++)
        {
            int pick = Random.Range(0, candidates.Count);
            candidates[pick].TriggerFall(CurrentWarning, CurrentFall, goneDuration, riseDuration, fallDistance);
            candidates.RemoveAt(pick);
        }

        // Explain the hazard the first time it happens each match
        if (!explainedFalls)
        {
            explainedFalls = true;
            match.Announce("Red, shaking floor is about to fall - get off it!");
        }
    }

    private void FreezePads()
    {
        var candidates = islandPads.FindAll(p => p.IsSolid && p.Freeze == ArenaSection.FreezeState.None);
        int count = Mathf.Min(Random.Range(padsPerFreeze.x, padsPerFreeze.y + 1), candidates.Count);
        for (int i = 0; i < count; i++)
        {
            int pick = Random.Range(0, candidates.Count);
            candidates[pick].TriggerFreeze(freezeWarningDuration, frozenDuration, iceColor, iceMaterial);
            candidates.RemoveAt(pick);
        }

        if (count > 0 && !explainedIce)
        {
            explainedIce = true;
            match.Announce("Blue, flashing floor freezes into slippery ice!");
        }
    }

    public void ResetAll()
    {
        phase = Phase.Normal;
        speedLevel = 0;
        foreach (var section in sections) section.ResetImmediate();
        if (match != null) match.ClearSpawnOverride();
        if (islandRoot != null)
        {
            PlaceIsland(randomizeIslandSide ? Random.Range(0f, 360f) : islandAngle);
            foreach (var pad in islandPads) pad.HideImmediate();
        }
    }

    private void PlaceIsland(float angleDegrees)
    {
        float distance = Radius + islandGap + padRing.y;
        float a = angleDegrees * Mathf.Deg2Rad;
        islandRoot.position = Center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * distance;
        // Turn the island so pad 1 (local +X) faces the arena; Unity's yaw turns +X toward -Z
        islandRoot.rotation = Quaternion.Euler(0f, -(angleDegrees + 180f), 0f);
        for (int i = 0; i < islandPads.Count; i++)
        {
            islandPads[i].SetHome(islandRoot.TransformPoint(islandPadOffsets[i]));
        }
    }

    // --- Spawning (used by MatchManager when a player respawns) ---
    private bool IsSafeToSpawn(Vector3 point)
    {
        if (!Physics.Raycast(point, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore)) return false;
        return !hit.collider.TryGetComponent<ArenaSection>(out var section) || section.IsSolid;
    }

    private Vector3? FindFallbackSpawn()
    {
        // Island spawns can fail the floor check while the island is still rising; it'll be there in time
        if (phase == Phase.Island) return islandSpawns[Random.Range(0, islandSpawns.Length)].position;
        if (TryGetRandomSolidPoint(0f, out var point)) return point + Vector3.up * 1.4f;
        return null;
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

        iceMaterial = new PhysicsMaterial("Ice")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounciness = physicsMaterial != null ? physicsMaterial.bounciness : 0f,
            bounceCombine = physicsMaterial != null ? physicsMaterial.bounceCombine : PhysicsMaterialCombine.Average
        };

        // The original disc is replaced by the sections at runtime
        sourceRenderer.enabled = false;
        if (sourceCollider != null) sourceCollider.enabled = false;

        var root = new GameObject("Arena Sections").transform;
        root.SetParent(transform.parent, false);
        root.position = Center;

        CreateSection(root, "Middle Disc", 0f, middleDiscRadius, 0f, Mathf.PI * 2f, material, physicsMaterial, baseColor);

        // The rest of the middle area, then the outer rings, all cut into similar-sized pieces
        var rings = new List<Vector2> { new Vector2(middleDiscRadius, centerRadius) };
        float ringWidth = (Radius - centerRadius) / Mathf.Max(1, ringCount);
        for (int ring = 0; ring < ringCount; ring++)
        {
            rings.Add(new Vector2(centerRadius + ring * ringWidth, centerRadius + (ring + 1) * ringWidth));
        }

        for (int ring = 0; ring < rings.Count; ring++)
        {
            float r0 = rings[ring].x;
            float r1 = rings[ring].y;
            float midCircumference = Mathf.PI * (r0 + r1);
            int count = Mathf.Max(3, Mathf.RoundToInt(midCircumference / targetSectionLength));
            // Stagger rings so seams don't line up into long straight cracks
            float offset = ring * 0.5f * (Mathf.PI * 2f / count);
            string ringName = ring == 0 ? "Middle Ring" : $"Ring {ring}";

            for (int i = 0; i < count; i++)
            {
                float a0 = offset + i * Mathf.PI * 2f / count;
                float a1 = a0 + Mathf.PI * 2f / count;
                CreateSection(root, $"{ringName} Section {i + 1}", r0, r1, a0, a1, material, physicsMaterial, baseColor);
            }
        }

        BuildIsland(material, physicsMaterial, baseColor);
        PlaceIsland(randomizeIslandSide ? Random.Range(0f, 360f) : islandAngle);
        foreach (var pad in islandPads) pad.HideImmediate();
    }

    // Late-match island: a hub pad ringed by smaller curved pads with gaps to jump, some raised
    private void BuildIsland(Material material, PhysicsMaterial physicsMaterial, Color baseColor)
    {
        islandRoot = new GameObject("Island").transform;
        islandRoot.SetParent(transform.parent, false);
        islandRoot.position = Center;

        AddIslandPad("Island Hub", 0f, hubRadius, 0f, Mathf.PI * 2f, 0f, material, physicsMaterial, baseColor);

        float arc = Mathf.Clamp(padArcDegrees, 5f, 360f / Mathf.Max(1, padCount)) * Mathf.Deg2Rad;
        for (int i = 0; i < padCount; i++)
        {
            // Pad 1 is centered on local +X, which PlaceIsland turns toward the arena
            float mid = i * Mathf.PI * 2f / padCount;
            float height = i % 2 == 1 ? raisedPadHeight : 0f;
            AddIslandPad($"Island Pad {i + 1}", padRing.x, padRing.y, mid - arc * 0.5f, mid + arc * 0.5f, height,
                material, physicsMaterial, baseColor);
        }

        islandSpawns = new Transform[4];
        for (int i = 0; i < islandSpawns.Length; i++)
        {
            float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
            var spawn = new GameObject($"Island Spawn {i + 1}").transform;
            spawn.SetParent(islandRoot, false);
            // Half a ball radius above the hub, like the arena spawn points
            spawn.localPosition = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (hubRadius * 0.55f) + Vector3.up * 1.4f;
            islandSpawns[i] = spawn;
        }
    }

    private void AddIslandPad(string name, float r0, float r1, float a0, float a1, float height,
        Material material, PhysicsMaterial physicsMaterial, Color baseColor)
    {
        var pad = CreateSection(islandRoot, name, r0, r1, a0, a1, material, physicsMaterial, baseColor, addToArena: false);
        pad.CanFall = false;
        Vector3 offset = Vector3.up * height;
        pad.transform.localPosition = offset;
        islandPads.Add(pad);
        islandPadOffsets.Add(offset);
    }

    private ArenaSection CreateSection(Transform root, string name, float r0, float r1, float a0, float a1,
        Material material, PhysicsMaterial physicsMaterial, Color baseColor, bool addToArena = true)
    {
        bool fullCircle = a1 - a0 >= Mathf.PI * 2f - 0.0001f;
        bool outerEdge = addToArena && r1 >= Radius - 0.01f;

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
        if (addToArena) sections.Add(section);
        return section;
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
