using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// "Falling Platforms" stage obstacle. At runtime the round arena is rebuilt as a small middle
// disc plus rings of curved sections; every piece, the middle included, can fall.
// Falls speed up every `speedUpEvery` seconds. A Brawl match runs in phases (timer counts down):
//   Normal    - groups of sections crumble into the void and later rise back.
//   Permanent - from `permanentFallsAt` left, anything that falls stays gone.
//   Island    - at `islandAppearsAt` left, a smaller copy of the arena (same rings and colours, on
//               a floating rock) rises beside it across a jumpable gap and respawns move onto it.
//               The old arena keeps crumbling down to about `ruinsFraction` of its pieces.
//               Island pieces take turns freezing into slippery ice (flashing blue first).
//   Storm     - at `stormAt` (+ the warning) left, the old arena glows, crackles and the sky
//               darkens while a countdown runs. Then lightning strikes everyone still over the old
//               arena: they respawn on the island at no cost to their score. A shield blocks it.
//   Ruins     - the old arena stops falling and stays exactly as the storm left it, so players
//               can jump back. A bonus coin rush lands on the ruins to make it worth the risk.
// Put this on the arena object: its renderer and collider are only used for size,
// material and as the edit-time preview.
public class FallingPlatforms : MonoBehaviour
{
    private enum Phase { Normal, Permanent, Island, Storm, Ruins }

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
    [SerializeField] private float finalPhaseMaxInterval = 4f;

    [Header("Storm")]
    [Tooltip("Seconds left on the timer when lightning strikes the old arena.")]
    [SerializeField] private float stormAt = 40f;
    [Tooltip("Countdown before the strike, so players can get across.")]
    [SerializeField] private float stormWarning = 5f;
    [Tooltip("Share of the old arena's pieces still standing when the storm hits. They stay as the ruins.")]
    [Range(0f, 1f)]
    [SerializeField] private float ruinsFraction = 0.35f;
    [SerializeField] private Color stormColor = new Color(0.55f, 0.35f, 1f);
    [Tooltip("How dark the sun gets during the countdown (1 = no change).")]
    [Range(0f, 1f)]
    [SerializeField] private float stormSunDim = 0.45f;
    [SerializeField] private CoinRush coinRush;
    [Tooltip("Bonus coins scattered on the ruins after the strike.")]
    [SerializeField] private int ruinsCoins = 12;
    [SerializeField] private float ruinsCoinDuration = 15f;

    [Header("Island")]
    [Tooltip("The island is a smaller copy of the arena: a middle disc plus rings of pieces in the same colours.")]
    [SerializeField] private float islandRadius = 8f;
    [SerializeField] private float islandDiscRadius = 2.5f;
    [SerializeField] private int islandRingCount = 3;
    [Tooltip("Rough arc length of each island piece, in metres.")]
    [SerializeField] private float islandSectionLength = 4.5f;
    [Tooltip("Gap between the arena edge and the island. ~3m needs a jump; a running jump clears about 6m.")]
    [SerializeField] private float islandGap = 3f;
    [SerializeField] private float islandRiseDuration = 3f;
    [Tooltip("Pick a different side of the arena for the island every match.")]
    [SerializeField] private bool randomizeIslandSide = true;
    [Tooltip("Island direction in degrees when not randomized (90 = far side from the default camera).")]
    [SerializeField] private float islandAngle = 90f;
    [Tooltip("Optional rock the island sits on (e.g. one of the stadium's floating islands). It is scaled to fit under the island and is only for looks.")]
    [SerializeField] private Mesh islandModel;
    [SerializeField] private Material islandModelMaterial;

    [Header("Island Freezing")]
    [SerializeField] private Vector2 freezeInterval = new Vector2(4f, 7f);
    [Tooltip("Island pieces that freeze together (min, max).")]
    [SerializeField] private Vector2Int padsPerFreeze = new Vector2Int(3, 5);
    [SerializeField] private float firstFreezeDelay = 5f;
    [SerializeField] private float freezeWarningDuration = 2f;
    [SerializeField] private float frozenDuration = 5f;

    [Header("Look")]
    [SerializeField] private Color warningColor = new Color(1f, 0.25f, 0.15f);
    [SerializeField] private Color iceColor = new Color(0.55f, 0.85f, 1f);
    [Tooltip("Floor colours from the middle out: middle disc, middle ring, then each outer ring (the last colour repeats if there are more rings). Empty uses the arena material's colour.")]
    [SerializeField] private Color[] ringColors =
    {
        new Color(0.96f, 0.91f, 0.78f), // middle disc - cream
        new Color(0.91f, 0.73f, 0.36f), // middle ring - gold
        new Color(0.55f, 0.76f, 0.32f), // ring 1 - grass
        new Color(0.45f, 0.65f, 0.26f), // ring 2 - darker grass
        new Color(0.85f, 0.70f, 0.48f)  // outer ring - tan track
    };
    [Tooltip("Every other section in a ring is darkened by this much so each platform reads as its own piece.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float alternateShade = 0.12f;

    [SerializeField] private MatchManager match;

    public Vector3 Center { get; private set; }
    public float TopY { get; private set; }
    public float Radius { get; private set; }
    public IReadOnlyList<ArenaSection> Sections => sections;

    private readonly List<ArenaSection> sections = new List<ArenaSection>();
    private readonly List<ArenaSection> islandPads = new List<ArenaSection>();
    private readonly List<Vector3> islandPadOffsets = new List<Vector3>();
    private Transform islandRoot;
    private float stormStart;
    private int lastStormCount;
    private float treasureAt;
    private Light sun;
    private float sunIntensity;
    private float flashUntil;
    private Transform[] islandSpawns;
    // How far the island reaches from its center toward the arena
    private float islandReach;
    private PhysicsMaterial iceMaterial;
    private bool active;
    // Permanent falls and the island are the Brawl finale; minigames keep the normal crumbling
    private bool finalPhases = true;
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

    private bool IslandUp => phase >= Phase.Island;

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        if (coinRush == null) coinRush = FindAnyObjectByType<CoinRush>();
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
            finalPhases = match.Mode == GameMode.Brawl;
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

        if (finalPhases && phase == Phase.Normal && timeLeft <= permanentFallsAt) EnterPermanentPhase();
        if (finalPhases && phase == Phase.Permanent && timeLeft <= islandAppearsAt) RaiseIsland();
        if (finalPhases && phase == Phase.Island && timeLeft <= stormAt + stormWarning) BeginStorm();

        if (IslandUp && Time.time >= nextFreezeTime)
        {
            nextFreezeTime = Time.time + Random.Range(freezeInterval.x, freezeInterval.y);
            FreezePads();
        }

        UpdateSun();
        if (phase == Phase.Storm)
        {
            UpdateStorm();
            return;
        }
        if (phase == Phase.Ruins)
        {
            // The ruins stay as they are; a moment after the strike, treasure lands on them
            if (treasureAt > 0f && Time.time >= treasureAt)
            {
                treasureAt = 0f;
                if (coinRush != null)
                {
                    coinRush.StartRushNow("TREASURE ON THE RUINS! Jump back for coins!", ruinsCoins,
                        Mathf.Min(ruinsCoinDuration, Mathf.Max(1f, timeLeft - 1f)));
                }
            }
            return;
        }

        if (Time.time < nextFallTime) return;
        nextFallTime = Time.time + NextInterval(timeLeft);
        DropGroup();
    }

    // --- Storm ---
    private void BeginStorm()
    {
        phase = Phase.Storm;
        stormStart = Time.time;
        lastStormCount = -1;
        sun = RenderSettings.sun;
        if (sun != null) sunIntensity = sun.intensity;
        Sfx.Play(Sound.Rumble, Center, 1f, 0.6f);
    }

    private void UpdateStorm()
    {
        float elapsed = Time.time - stormStart;
        float t = Mathf.Clamp01(elapsed / stormWarning);

        // The countdown, once a second
        int count = Mathf.CeilToInt(stormWarning - elapsed);
        if (count != lastStormCount && count > 0)
        {
            lastStormCount = count;
            match.Announce($"STORM IN {count}! Get to the new platform!");
            Sfx.Play(Sound.Countdown, 0.7f, 0.8f);
            Sfx.Play(Sound.Rumble, Center, 0.5f + 0.4f * t, 0.6f);
            Vfx.Shake(0.15f + 0.3f * t);
        }

        // The old floor throbs storm-purple, faster and stronger as the strike gets closer
        float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * Mathf.Lerp(4f, 16f, t));
        float glow = Mathf.Lerp(0.45f, 0.85f, t) * Mathf.Lerp(0.65f, 1f, pulse);
        foreach (var section in sections) section.Tint(stormColor, glow);

        // Stray lightning crackles across it
        if (Random.value < Time.deltaTime * Mathf.Lerp(1.5f, 6f, t) && TryGetRandomSolidPoint(0f, out var spot))
        {
            Vfx.Zap(spot, stormColor);
            Sfx.Play(Sound.Zap, spot, 0.35f, Random.Range(0.8f, 1.2f));
        }

        if (elapsed >= stormWarning) StrikeOldArena();
    }

    private void StrikeOldArena()
    {
        phase = Phase.Ruins;
        flashUntil = Time.time + 0.15f;
        Vfx.Shake(1f);
        Sfx.Play(Sound.Explosion, Center, 1f, 0.7f);
        foreach (var section in sections) section.Tint(stormColor, 0f);

        int struck = 0;
        string blocked = null;
        foreach (var entry in match.Players)
        {
            if (entry.Player == null || entry.IsRespawning || !IsOverOldArena(entry.Player.transform.position)) continue;
            Vector3 at = entry.Player.transform.position;
            Vfx.Zap(at, stormColor);
            if (entry.Player.IsInvulnerable)
            {
                // A shield shrugs the lightning off, like the Staph It! zap
                Sfx.Play(Sound.Blocked, at, 1f);
                blocked = entry.Name;
                continue;
            }
            Sfx.Play(Sound.Zap, at, 1f);
            Vfx.Explosion(at, 3f);
            match.KnockOutWithoutPenalty(entry.Player);
            struck++;
        }

        if (blocked != null) match.Announce($"{blocked}'s shield blocked the storm!");
        else if (struck > 0) match.Announce(struck == 1 ? "The storm struck a player! The old arena is now ruins." : $"The storm struck {struck} players! The old arena is now ruins.");
        else match.Announce("The storm passed! The old arena is now ruins.");
        treasureAt = Time.time + 3f;
    }

    // From the storm on, the area the camera should keep in view: the new platform and the
    // middle of the old arena (players' own spread pulls in the rest)
    public bool TryGetFinaleView(out Bounds view)
    {
        view = default;
        if (phase < Phase.Storm || islandRoot == null) return false;
        view = new Bounds(islandRoot.position, Vector3.one * islandRadius * 2f);
        view.Encapsulate(new Bounds(Center, Vector3.one * Radius));
        return true;
    }

    // Anywhere above the old arena's footprint, standing or mid-jump
    private bool IsOverOldArena(Vector3 position)
    {
        Vector3 offset = position - Center;
        offset.y = 0f;
        return offset.magnitude <= Radius + 0.75f;
    }

    // The sky darkens through the countdown, flashes with the strike, then clears up
    private void UpdateSun()
    {
        if (sun == null) return;
        float target = sunIntensity;
        if (phase == Phase.Storm) target = sunIntensity * Mathf.Lerp(1f, stormSunDim, Mathf.Clamp01((Time.time - stormStart) / stormWarning));
        if (Time.time < flashUntil)
        {
            sun.intensity = sunIntensity * 3f;
            return;
        }
        sun.intensity = Mathf.MoveTowards(sun.intensity, target, sunIntensity * 2f * Time.deltaTime);
        if (phase != Phase.Storm && Mathf.Approximately(sun.intensity, sunIntensity)) sun = null;
    }

    private void RestoreSun()
    {
        if (sun != null) sun.intensity = sunIntensity;
        sun = null;
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
        match.Announce("NEW PLATFORM! Get over there before the storm hits!");
    }

    // Pieces of the old arena that should still be standing when the storm hits
    private int RuinsToKeep => Mathf.CeilToInt(sections.Count * ruinsFraction);

    private float NextInterval(float timeLeft)
    {
        if (phase != Phase.Island) return Random.Range(fallInterval.x, fallInterval.y) * IntervalScale;

        // Island phase: pace the drops so the arena is down to the ruins just as the storm comes
        int extra = sections.FindAll(s => s.IsSolid).Count - RuinsToKeep;
        if (extra <= 0) return float.PositiveInfinity;
        float averageGroup = (piecesPerFall.x + piecesPerFall.y) * 0.5f;
        int groupsLeft = Mathf.Max(1, Mathf.CeilToInt(extra / Mathf.Max(1f, averageGroup)));
        float timeForDrops = timeLeft - stormAt - stormWarning - CurrentWarning;
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
        else if (phase == Phase.Island)
        {
            // Never eat into the ruins
            count = Mathf.Min(count, candidates.Count - RuinsToKeep);
            if (count <= 0) return;
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
        treasureAt = 0f;
        RestoreSun();
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
        float distance = Radius + islandGap + islandReach;
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
        if (IslandUp) return islandSpawns[Random.Range(0, islandSpawns.Length)].position;
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

        CreateSection(root, "Middle Disc", 0f, middleDiscRadius, 0f, Mathf.PI * 2f, material, physicsMaterial, FloorColor(0, 0, baseColor));

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
            // Even counts let the light/dark pattern alternate all the way round
            if (alternateShade > 0f && count % 2 == 1) count++;
            // Stagger rings so seams don't line up into long straight cracks
            float offset = ring * 0.5f * (Mathf.PI * 2f / count);
            string ringName = ring == 0 ? "Middle Ring" : $"Ring {ring}";

            for (int i = 0; i < count; i++)
            {
                float a0 = offset + i * Mathf.PI * 2f / count;
                float a1 = a0 + Mathf.PI * 2f / count;
                CreateSection(root, $"{ringName} Section {i + 1}", r0, r1, a0, a1, material, physicsMaterial, FloorColor(ring + 1, i, baseColor));
            }
        }

        BuildIsland(material, physicsMaterial, baseColor);
        PlaceIsland(randomizeIslandSide ? Random.Range(0f, 360f) : islandAngle);
        foreach (var pad in islandPads) pad.HideImmediate();
    }

    // Colour for a section: its ring's colour, with every other section in the ring a shade darker
    private Color FloorColor(int ringIndex, int sectionIndex, Color fallback)
    {
        if (ringColors == null || ringColors.Length == 0) return fallback;
        Color color = ringColors[Mathf.Min(ringIndex, ringColors.Length - 1)];
        if (sectionIndex % 2 == 1)
        {
            float keep = 1f - alternateShade;
            color = new Color(color.r * keep, color.g * keep, color.b * keep, color.a);
        }
        return color;
    }

    // Late-match island: a smaller copy of the arena (middle disc plus rings of pieces in the same
    // colours and light/dark pattern), optionally sitting on a floating rock
    private void BuildIsland(Material material, PhysicsMaterial physicsMaterial, Color baseColor)
    {
        islandRoot = new GameObject("Island").transform;
        islandRoot.SetParent(transform.parent, false);
        islandRoot.position = Center;
        islandReach = islandRadius;

        var middle = AddIslandPad("Island Middle", 0f, islandDiscRadius, 0f, Mathf.PI * 2f,
            material, physicsMaterial, FloorColor(0, 0, baseColor));

        int rings = Mathf.Max(1, islandRingCount);
        float ringWidth = (islandRadius - islandDiscRadius) / rings;
        int colorCount = ringColors != null ? ringColors.Length : 0;
        for (int ring = 0; ring < rings; ring++)
        {
            float r0 = islandDiscRadius + ring * ringWidth;
            float r1 = r0 + ringWidth;
            int count = Mathf.Max(3, Mathf.RoundToInt(Mathf.PI * (r0 + r1) / Mathf.Max(1f, islandSectionLength)));
            if (alternateShade > 0f && count % 2 == 1) count++;
            float offset = ring * 0.5f * (Mathf.PI * 2f / count);
            // Same colours as the arena from the middle out, with its tan track always on the rim
            int colorIndex = ring == rings - 1 && colorCount > 0 ? colorCount - 1 : ring + 1;
            for (int i = 0; i < count; i++)
            {
                float a0 = offset + i * Mathf.PI * 2f / count;
                AddIslandPad($"Island Ring {ring + 1} Piece {i + 1}", r0, r1, a0, a0 + Mathf.PI * 2f / count,
                    material, physicsMaterial, FloorColor(colorIndex, i, baseColor));
            }
        }

        // The rock rides along with the middle piece, so it rises with the island
        if (islandModel != null) AddRockBase(middle.transform);

        islandSpawns = new Transform[4];
        for (int i = 0; i < islandSpawns.Length; i++)
        {
            float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
            var spawn = new GameObject($"Island Spawn {i + 1}").transform;
            spawn.SetParent(islandRoot, false);
            // On the first ring, half a ball radius up, like the arena spawn points
            float r = islandDiscRadius + ringWidth * 0.5f;
            spawn.localPosition = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r + Vector3.up * 1.4f;
            islandSpawns[i] = spawn;
        }
    }

    // Scales the rock model to sit just under the island, a little wider so its grass shows as a rim
    private void AddRockBase(Transform parent)
    {
        var rock = new GameObject("Rock Base", typeof(MeshFilter), typeof(MeshRenderer)).transform;
        rock.SetParent(parent, false);
        rock.GetComponent<MeshFilter>().sharedMesh = islandModel;
        rock.GetComponent<MeshRenderer>().sharedMaterial = islandModelMaterial;

        Bounds bounds = islandModel.bounds;
        float reach = Mathf.Max(0.01f, Mathf.Max(bounds.extents.x, bounds.extents.z));
        float scale = islandRadius * 1.12f / reach;
        rock.localScale = Vector3.one * scale;
        // Top of the rock tucked under the floor pieces
        rock.localPosition = new Vector3(-bounds.center.x * scale, -thickness * 0.6f - bounds.max.y * scale, -bounds.center.z * scale);
    }

    private ArenaSection AddIslandPad(string name, float r0, float r1, float a0, float a1,
        Material material, PhysicsMaterial physicsMaterial, Color color)
    {
        var pad = CreateSection(islandRoot, name, r0, r1, a0, a1, material, physicsMaterial, color, addToArena: false);
        pad.CanFall = false;
        islandPads.Add(pad);
        islandPadOffsets.Add(Vector3.zero);
        return pad;
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
