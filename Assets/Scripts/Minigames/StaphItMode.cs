using System.Collections.Generic;
using UnityEngine;

// "STAPH IT!" - a chaotic take on Red Light, Green Light inside the Ball Brawl arena.
// A glowing safe-zone dome sits somewhere on the arena. The light cycles:
//   GREEN  - roam, grab power-ups, bump people around (the dome has just moved)
//   YELLOW - hurry: get inside the dome
//   RED    - anyone outside the dome (even for a moment) is zapped: -1 point and a jolt.
//            A shield blocks the zap. Everyone still inside when red ends scores +1.
// Rounds speed up and the dome shrinks as the match goes on, so the dome gets crowded and
// knocking rivals out of it right before red is the whole game. Falling platforms and power-ups
// stay on; floor under the dome never falls while the dome is there.
// (The team's RedLightGreenLightMinigame is the online, networked version of the idea.)
public class StaphItMode : MonoBehaviour
{
    public enum Signal { Off, Green, Yellow, Red }

    // The running round, if any (the HUD reads this)
    public static StaphItMode Active { get; private set; }

    [SerializeField] private MatchManager match;
    [SerializeField] private FallingPlatforms arena;

    [Header("Timing (start of match -> end of match)")]
    [SerializeField] private Vector2 greenAtStart = new Vector2(5f, 8f);
    [SerializeField] private Vector2 greenAtEnd = new Vector2(2.5f, 4.5f);
    [SerializeField] private float yellowAtStart = 2.2f;
    [SerializeField] private float yellowAtEnd = 1.3f;
    [SerializeField] private float redDuration = 3f;
    [Tooltip("Extra green time on the first round so everyone can read the rules.")]
    [SerializeField] private float firstRoundBonus = 3f;

    [Header("Safe zone")]
    [SerializeField] private float radiusAtStart = 7f;
    [SerializeField] private float radiusAtEnd = 4f;
    [Tooltip("The dome tries to move at least this far each round.")]
    [SerializeField] private float minMoveDistance = 12f;

    [Header("Scoring")]
    [SerializeField] private int survivePoints = 1;
    [SerializeField] private int caughtPenalty = 1;
    [Tooltip("Upward jolt for a zapped ball (kept nearly vertical so it rarely throws anyone off the edge).")]
    [SerializeField] private float zapForce = 14f;

    [Header("Look")]
    [SerializeField] private Color green = new Color(0.3f, 1f, 0.45f);
    [SerializeField] private Color yellow = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color red = new Color(1f, 0.2f, 0.2f);

    public Signal Light { get; private set; } = Signal.Off;
    public float PhaseRemaining => Mathf.Max(0f, phaseEndsAt - Time.time);
    public Vector3 DomeCenter { get; private set; }
    public float DomeRadius { get; private set; }
    public Color SignalColor => Light == Signal.Red ? red : Light == Signal.Yellow ? yellow : green;

    private readonly HashSet<MatchManager.PlayerEntry> caught = new HashSet<MatchManager.PlayerEntry>();
    private readonly List<ArenaSection> locked = new List<ArenaSection>();
    private bool running;
    private bool firstRound;
    private float phaseEndsAt;
    private float domeMovedAt;

    private Transform domeRoot;
    private Transform dome;
    private Transform edge;
    private Transform beam;
    private Material domeMaterial, edgeMaterial, beamMaterial;
    private UnityEngine.Light sun;
    private Color sunColor;

    private bool IsMode => match != null && match.Mode == GameMode.StaphIt;
    private float Progress => match != null && match.Duration > 0f ? Mathf.Clamp01(1f - match.TimeRemaining / match.Duration) : 0f;

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        if (arena == null) arena = FindAnyObjectByType<FallingPlatforms>();
        sun = RenderSettings.sun;
        if (sun == null)
        {
            foreach (var l in FindObjectsByType<UnityEngine.Light>())
            {
                if (l.type == LightType.Directional) { sun = l; break; }
            }
        }
        if (sun != null) sunColor = sun.color;
    }

    private void OnEnable()
    {
        if (match != null) match.StateChanged += OnMatchStateChanged;
    }

    private void OnDisable()
    {
        if (match != null) match.StateChanged -= OnMatchStateChanged;
        Stop();
    }

    private void OnMatchStateChanged(MatchManager.MatchState state)
    {
        if (state == MatchManager.MatchState.Playing && IsMode) Begin();
        else Stop();
    }

    private void Begin()
    {
        running = true;
        Active = this;
        firstRound = true;
        BuildDome();
        EnterGreen();
    }

    private void Stop()
    {
        if (Active == this) Active = null;
        running = false;
        Light = Signal.Off;
        caught.Clear();
        Unlock();
        if (domeRoot != null) domeRoot.gameObject.SetActive(false);
        if (sun != null) sun.color = sunColor;
    }

    private void Update()
    {
        if (!running) return;

        if (Time.time >= phaseEndsAt)
        {
            switch (Light)
            {
                case Signal.Green: EnterYellow(); break;
                case Signal.Yellow: EnterRed(); break;
                case Signal.Red: EndRed(); EnterGreen(); break;
            }
        }

        if (Light == Signal.Red) CheckCaught();
        UpdateLook();
    }

    // --- Light cycle ---
    private void EnterGreen()
    {
        Light = Signal.Green;
        MoveDome();
        float p = Progress;
        float duration = Random.Range(Mathf.Lerp(greenAtStart.x, greenAtEnd.x, p), Mathf.Lerp(greenAtStart.y, greenAtEnd.y, p));
        if (firstRound) duration += firstRoundBonus;
        phaseEndsAt = Time.time + duration;
        Sfx.Play(Sound.LightGreen);
        match.Announce(firstRound ? "STAPH IT! When the light turns RED, be inside the dome!" : "GREEN LIGHT - the dome moved!");
        firstRound = false;
    }

    private void EnterYellow()
    {
        Light = Signal.Yellow;
        phaseEndsAt = Time.time + Mathf.Lerp(yellowAtStart, yellowAtEnd, Progress);
        Sfx.Play(Sound.LightYellow);
        match.Announce("YELLOW - get in the dome!");
    }

    private void EnterRed()
    {
        Light = Signal.Red;
        phaseEndsAt = Time.time + redDuration;
        caught.Clear();
        Sfx.Play(Sound.LightRed);
        Vfx.Shake(0.25f);
        match.Announce("RED LIGHT - STAPH IT!");
    }

    // Everyone who made it through red without being caught scores
    private void EndRed()
    {
        bool anyone = false;
        foreach (var entry in match.Players)
        {
            if (entry.Player == null || entry.IsRespawning || caught.Contains(entry) || !Inside(entry)) continue;
            if (match.AwardPoints(entry.Player, survivePoints))
            {
                anyone = true;
                Vfx.Sparkle(entry.Player.transform.position + Vector3.up * 0.5f, green, 12);
            }
        }
        if (anyone) Sfx.Play(Sound.Safe);
    }

    private void CheckCaught()
    {
        foreach (var entry in match.Players)
        {
            if (entry.Player == null || entry.IsRespawning || caught.Contains(entry) || Inside(entry)) continue;
            Catch(entry);
        }
    }

    private void Catch(MatchManager.PlayerEntry entry)
    {
        caught.Add(entry);
        Vector3 position = entry.Player.transform.position;

        if (entry.Player.IsInvulnerable)
        {
            Sfx.Play(Sound.Blocked, position);
            Vfx.Sparkle(position, new Color(0.5f, 0.9f, 1f));
            match.Announce($"{entry.Name}'s shield blocked the zap!");
            return;
        }

        match.AwardPoints(entry.Player, -caughtPenalty);
        Vfx.Zap(position, red);
        Sfx.Play(Sound.Zap, position);
        Vfx.Shake(0.3f);
        Vector3 away = position - DomeCenter;
        away.y = 0f;
        Vector3 dir = (away.sqrMagnitude > 0.01f ? away.normalized * 0.15f : Vector3.zero) + Vector3.up;
        entry.Player.ApplyKnockback(dir.normalized, zapForce);
        match.Announce($"{entry.Name} got caught outside! -{caughtPenalty}");
    }

    private bool Inside(MatchManager.PlayerEntry entry)
    {
        Vector3 p = entry.Player.transform.position;
        Vector3 flat = new Vector3(p.x - DomeCenter.x, 0f, p.z - DomeCenter.z);
        return flat.magnitude <= DomeRadius && p.y > DomeCenter.y - 1.5f;
    }

    // --- Safe zone placement ---
    private void MoveDome()
    {
        DomeRadius = Mathf.Lerp(radiusAtStart, radiusAtEnd, Progress);
        Vector3 previous = DomeCenter;
        bool hadDome = domeMovedAt > 0f && running;
        Vector3 chosen = arena != null ? arena.Center : Vector3.zero;

        if (arena != null)
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                if (!arena.TryGetRandomSolidPoint(0f, out var candidate)) break;
                // Later attempts accept shorter moves
                if (hadDome && attempt < 20 && Vector3.Distance(candidate, previous) < minMoveDistance) continue;
                if (!FootprintSolid(candidate, DomeRadius)) continue;
                chosen = candidate;
                break;
            }
        }

        Unlock();
        DomeCenter = chosen;
        Lock(chosen, DomeRadius);
        domeMovedAt = Time.time;

        domeRoot.gameObject.SetActive(true);
        domeRoot.position = chosen;
        Vfx.Poof(chosen, green);
    }

    private static readonly Vector2[] Footprint =
    {
        Vector2.zero, new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
        new Vector2(0.7f, 0.7f), new Vector2(-0.7f, 0.7f), new Vector2(0.7f, -0.7f), new Vector2(-0.7f, -0.7f)
    };

    // Solid floor all across the dome (no holes, nothing about to fall)
    private bool FootprintSolid(Vector3 center, float radius)
    {
        foreach (var offset in Footprint)
        {
            var section = SectionBelow(center + new Vector3(offset.x, 0f, offset.y) * radius * 0.85f);
            if (section == null || !section.IsSolid) return false;
        }
        return true;
    }

    private static ArenaSection SectionBelow(Vector3 point)
    {
        if (!Physics.Raycast(point + Vector3.up * 3f, Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore)) return null;
        return hit.collider.GetComponent<ArenaSection>();
    }

    // Floor under the dome can't fall while the dome sits on it
    private void Lock(Vector3 center, float radius)
    {
        const int steps = 6;
        for (int x = -steps; x <= steps; x++)
        {
            for (int z = -steps; z <= steps; z++)
            {
                Vector3 offset = new Vector3(x, 0f, z) / steps * radius;
                if (offset.magnitude > radius) continue;
                var section = SectionBelow(center + offset);
                if (section == null || locked.Contains(section)) continue;
                section.CanFall = false;
                locked.Add(section);
            }
        }
    }

    private void Unlock()
    {
        foreach (var section in locked)
        {
            if (section != null) section.CanFall = true;
        }
        locked.Clear();
    }

    // --- Look ---
    private void BuildDome()
    {
        if (domeRoot != null) return;
        domeRoot = new GameObject("Staph It Safe Zone").transform;

        // A sphere centred on the floor shows as a dome above it
        dome = MakePart(PrimitiveType.Sphere, "Dome", out domeMaterial, ArtUtil.TransparentLit(green, 0.8f));
        dome.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Bright ring marking the edge on the floor
        edge = MakePart(PrimitiveType.Quad, "Edge", out edgeMaterial, new Material(Vfx.Additive()));
        edgeMaterial.mainTexture = Vfx.Ring();
        if (edgeMaterial.HasProperty("_BaseMap")) edgeMaterial.SetTexture("_BaseMap", Vfx.Ring());
        edge.localRotation = Quaternion.Euler(90f, 0f, 0f);
        edge.localPosition = Vector3.up * 0.06f;

        // Light beam so players can spot it from anywhere
        beam = MakePart(PrimitiveType.Cylinder, "Beam", out beamMaterial, new Material(Vfx.Additive()));
    }

    private Transform MakePart(PrimitiveType type, string name, out Material material, Material source)
    {
        var part = GameObject.CreatePrimitive(type).transform;
        Destroy(part.GetComponent<Collider>());
        part.name = name;
        part.SetParent(domeRoot, false);
        material = source;
        var r = part.GetComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return part;
    }

    private void UpdateLook()
    {
        Color c = SignalColor;
        float t = Time.time;
        // Yellow flashes, red throbs
        float pulse = Light == Signal.Yellow ? Mathf.PingPong(t * 6f, 1f) : Light == Signal.Red ? 0.5f + 0.5f * Mathf.Sin(t * 10f) : 0.5f + 0.5f * Mathf.Sin(t * 2f);

        // Pops up with a bounce whenever it moves
        float age = (t - domeMovedAt) / 0.5f;
        float grow = age >= 1f ? 1f : Mathf.Sin(age * Mathf.PI * 0.5f) * (1f + 0.2f * Mathf.Sin(age * Mathf.PI));
        float d = DomeRadius * 2f * Mathf.Max(0.01f, grow);
        dome.localScale = new Vector3(d, d * 0.8f, d);
        // The ring texture's line sits at 85% of its radius
        edge.localScale = Vector3.one * (d / 0.85f);
        beam.localScale = new Vector3(0.7f, 25f, 0.7f) * Mathf.Max(0.01f, grow);
        beam.localPosition = Vector3.up * 25f * grow;

        SetColor(domeMaterial, new Color(c.r, c.g, c.b, Mathf.Lerp(0.12f, 0.24f, pulse)), c * Mathf.Lerp(0.4f, 1.2f, pulse));
        SetColor(edgeMaterial, new Color(c.r, c.g, c.b, Mathf.Lerp(0.6f, 1f, pulse)), Color.black);
        SetColor(beamMaterial, new Color(c.r, c.g, c.b, Mathf.Lerp(0.08f, 0.18f, pulse)), Color.black);

        // Tint the whole arena: amber on yellow, red on red
        if (sun != null)
        {
            Color target = Light == Signal.Red ? Color.Lerp(sunColor, red, 0.3f) : Light == Signal.Yellow ? Color.Lerp(sunColor, yellow, 0.2f) : sunColor;
            sun.color = Color.Lerp(sun.color, target, 1f - Mathf.Exp(-8f * Time.deltaTime));
        }
    }

    private static void SetColor(Material m, Color color, Color emission)
    {
        m.color = color;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_EmissionColor") && emission != Color.black) m.SetColor("_EmissionColor", emission);
    }
}
