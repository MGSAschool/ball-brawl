using System.Collections.Generic;
using UnityEngine;

public class StaphItMode : MonoBehaviour
{
    public enum Signal { Off, Green, Yellow, Red }

    public static StaphItMode Active { get; private set; }

    private static readonly Vector2[] FootprintChecks =
    {
        Vector2.zero, new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
        new Vector2(0.7f, 0.7f), new Vector2(-0.7f, 0.7f), new Vector2(0.7f, -0.7f), new Vector2(-0.7f, -0.7f)
    };

    [SerializeField] private MatchManager match;
    [SerializeField] private ArenaFloor arena;

    [Header("Timing (start of match -> end of match)")]
    [SerializeField] private Vector2 greenAtStart = new Vector2(5f, 8f);
    [SerializeField] private Vector2 greenAtEnd = new Vector2(2.5f, 4.5f);
    [SerializeField] private float yellowAtStart = 2.2f;
    [SerializeField] private float yellowAtEnd = 1.3f;
    [SerializeField] private float redDuration = 3f;
    [SerializeField] private float firstRoundBonus = 3f;

    [Header("Scoring")]
    [SerializeField] private int survivePoints = 1;
    [SerializeField] private int caughtPenalty = 1;
    [SerializeField] private float zapForce = 14f;

    [Header("Safe Zone")]
    [SerializeField] private float radiusAtStart = 7f;
    [SerializeField] private float radiusAtEnd = 4f;
    [SerializeField] private float minMoveDistance = 12f;

    [Header("Look")]
    [SerializeField] private Color green = new Color(0.3f, 1f, 0.45f);
    [SerializeField] private Color yellow = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color red = new Color(1f, 0.2f, 0.2f);

    private readonly List<PlayerEntry> caught = new List<PlayerEntry>();
    private bool running;
    private bool firstRound;
    private float phaseEndsAt;

    private readonly List<ArenaSection> lockedSections = new List<ArenaSection>();
    private bool zonePlaced;
    private Vector3 zoneCenter;
    private float zoneRadius;
    private float zoneMovedAt;

    private Transform domeRoot;
    private Transform dome;
    private Transform domeEdge;
    private Transform domeBeam;
    private Material domeMaterial;
    private Material edgeMaterial;
    private Material beamMaterial;
    private Light sun;
    private Color sunColor;
    private bool tintingSun;
    private float lastMovedAt = -1f;

    public Signal Light { get; private set; } = Signal.Off;
    public Color Green { get { return green; } }
    public Color Yellow { get { return yellow; } }
    public Color Red { get { return red; } }

    public float PhaseRemaining
    {
        get { return Mathf.Max(0f, phaseEndsAt - Time.time); }
    }

    public Color SignalColor
    {
        get
        {
            if (Light == Signal.Red) return red;
            if (Light == Signal.Yellow) return yellow;
            return green;
        }
    }

    private float Progress
    {
        get
        {
            if (match == null || match.Duration <= 0f) return 0f;
            return Mathf.Clamp01(1f - match.TimeRemaining / match.Duration);
        }
    }

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        if (arena == null) arena = FindAnyObjectByType<ArenaFloor>();
        sun = FindSun();
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

    private void OnMatchStateChanged(MatchState state)
    {
        if (state == MatchState.Playing && match.Mode == GameMode.StaphIt) Begin();
        else Stop();
    }

    private void Begin()
    {
        running = true;
        Active = this;
        firstRound = true;
        EnterGreen();
    }

    private void Stop()
    {
        if (Active == this) Active = null;
        running = false;
        Light = Signal.Off;
        caught.Clear();
        ReleaseZone();
    }

    private void Update()
    {
        if (!running) return;

        if (Time.time >= phaseEndsAt)
        {
            if (Light == Signal.Green)
            {
                EnterYellow();
            }
            else if (Light == Signal.Yellow)
            {
                EnterRed();
            }
            else if (Light == Signal.Red)
            {
                ScoreSurvivors();
                EnterGreen();
            }
        }

        if (Light == Signal.Red) CatchPlayersOutside();
    }

    private void EnterGreen()
    {
        Light = Signal.Green;
        MoveZoneToNewSpot(Progress);

        float shortest = Mathf.Lerp(greenAtStart.x, greenAtEnd.x, Progress);
        float longest = Mathf.Lerp(greenAtStart.y, greenAtEnd.y, Progress);
        float duration = Random.Range(shortest, longest);
        if (firstRound) duration += firstRoundBonus;
        phaseEndsAt = Time.time + duration;

        Sfx.Play(Sound.LightGreen);
        if (firstRound) match.Announce("STAPH IT! When the light turns RED, be inside the dome!");
        else match.Announce("GREEN LIGHT - the dome moved!");
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

    private void ScoreSurvivors()
    {
        bool anyoneScored = false;
        foreach (PlayerEntry entry in match.Players)
        {
            if (!IsInPlay(entry) || caught.Contains(entry)) continue;
            if (!ZoneContains(entry.Player.transform.position)) continue;

            if (match.AwardPoints(entry.Player, survivePoints))
            {
                anyoneScored = true;
                Vfx.Sparkle(entry.Player.transform.position + Vector3.up * 0.5f, green, 12);
            }
        }
        if (anyoneScored) Sfx.Play(Sound.Safe);
    }

    private void CatchPlayersOutside()
    {
        foreach (PlayerEntry entry in match.Players)
        {
            if (!IsInPlay(entry) || caught.Contains(entry)) continue;
            if (ZoneContains(entry.Player.transform.position)) continue;
            Catch(entry);
        }
    }

    private bool IsInPlay(PlayerEntry entry)
    {
        return entry.Player != null && !entry.IsRespawning;
    }

    private void Catch(PlayerEntry entry)
    {
        caught.Add(entry);
        Vector3 position = entry.Player.transform.position;

        if (entry.Player.IsInvulnerable)
        {
            Sfx.Play(Sound.Blocked, position);
            Vfx.Sparkle(position, new Color(0.5f, 0.9f, 1f));
            match.Announce(entry.Name + "'s shield blocked the zap!");
            return;
        }

        match.AwardPoints(entry.Player, -caughtPenalty);
        Vfx.Zap(position, red);
        Sfx.Play(Sound.Zap, position);
        Vfx.Shake(0.3f);

        Vector3 away = position - zoneCenter;
        away.y = 0f;
        Vector3 direction = Vector3.up;
        if (away.sqrMagnitude > 0.01f) direction += away.normalized * 0.15f;
        entry.Player.ApplyKnockback(direction.normalized, zapForce);

        match.Announce(entry.Name + " got caught outside! -" + caughtPenalty);
    }

    private void MoveZoneToNewSpot(float progress)
    {
        zoneRadius = Mathf.Lerp(radiusAtStart, radiusAtEnd, progress);
        Vector3 chosen = Vector3.zero;
        if (arena != null) chosen = PickZoneSpot();

        ReleaseZone();
        zoneCenter = chosen;
        LockFloorUnder(chosen, zoneRadius);
        zoneMovedAt = Time.time;
        zonePlaced = true;
    }

    private bool ZoneContains(Vector3 position)
    {
        Vector3 flat = new Vector3(position.x - zoneCenter.x, 0f, position.z - zoneCenter.z);
        return flat.magnitude <= zoneRadius && position.y > zoneCenter.y - 1.5f;
    }

    private void ReleaseZone()
    {
        foreach (ArenaSection section in lockedSections)
        {
            if (section != null) section.CanFall = true;
        }
        lockedSections.Clear();
        zonePlaced = false;
    }

    private Vector3 PickZoneSpot()
    {
        Vector3 previous = zoneCenter;
        bool hadSpot = zonePlaced;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector3 candidate;
            if (!arena.TryGetRandomSolidPoint(0f, out candidate)) break;

            bool tooClose = Vector3.Distance(candidate, previous) < minMoveDistance;
            if (hadSpot && attempt < 20 && tooClose) continue;
            if (!FloorIsSolidUnder(candidate, zoneRadius)) continue;
            return candidate;
        }
        return arena.Center;
    }

    private bool FloorIsSolidUnder(Vector3 center, float radius)
    {
        foreach (Vector2 offset in FootprintChecks)
        {
            Vector3 point = center + new Vector3(offset.x, 0f, offset.y) * radius * 0.85f;
            ArenaSection section = SectionBelow(point);
            if (section == null || !section.IsSolid) return false;
        }
        return true;
    }

    private void LockFloorUnder(Vector3 center, float radius)
    {
        int steps = 6;
        for (int x = -steps; x <= steps; x++)
        {
            for (int z = -steps; z <= steps; z++)
            {
                Vector3 offset = new Vector3(x, 0f, z) / steps * radius;
                if (offset.magnitude > radius) continue;

                ArenaSection section = SectionBelow(center + offset);
                if (section == null || lockedSections.Contains(section)) continue;
                section.CanFall = false;
                lockedSections.Add(section);
            }
        }
    }

    private ArenaSection SectionBelow(Vector3 point)
    {
        RaycastHit hit;
        if (!Physics.Raycast(point + Vector3.up * 3f, Vector3.down, out hit, 8f, ~0, QueryTriggerInteraction.Ignore)) return null;
        return hit.collider.GetComponent<ArenaSection>();
    }

    private void LateUpdate()
    {
        bool showDome = Active == this && zonePlaced;
        if (!showDome)
        {
            if (domeRoot != null) domeRoot.gameObject.SetActive(false);
            RestoreSun();
            return;
        }

        if (domeRoot == null) BuildDome();
        domeRoot.gameObject.SetActive(true);

        if (zoneMovedAt != lastMovedAt)
        {
            lastMovedAt = zoneMovedAt;
            domeRoot.position = zoneCenter;
            Vfx.Poof(zoneCenter, green);
        }

        UpdateDomeLook();
        TintSun();
    }

    private void UpdateDomeLook()
    {
        Color c = SignalColor;
        float t = Time.time;
        float pulse = 0.5f + 0.5f * Mathf.Sin(t * 2f);
        if (Light == Signal.Yellow) pulse = Mathf.PingPong(t * 6f, 1f);
        if (Light == Signal.Red) pulse = 0.5f + 0.5f * Mathf.Sin(t * 10f);

        float age = (t - zoneMovedAt) / 0.5f;
        float grow = 1f;
        if (age < 1f) grow = Mathf.Sin(age * Mathf.PI * 0.5f) * (1f + 0.2f * Mathf.Sin(age * Mathf.PI));
        float size = zoneRadius * 2f * Mathf.Max(0.01f, grow);

        dome.localScale = new Vector3(size, size * 0.8f, size);
        domeEdge.localScale = Vector3.one * (size / 0.85f);
        domeBeam.localScale = new Vector3(0.7f, 25f, 0.7f) * Mathf.Max(0.01f, grow);
        domeBeam.localPosition = Vector3.up * 25f * grow;

        SetColor(domeMaterial, new Color(c.r, c.g, c.b, Mathf.Lerp(0.12f, 0.24f, pulse)), c * Mathf.Lerp(0.4f, 1.2f, pulse));
        SetColor(edgeMaterial, new Color(c.r, c.g, c.b, Mathf.Lerp(0.6f, 1f, pulse)), Color.black);
        SetColor(beamMaterial, new Color(c.r, c.g, c.b, Mathf.Lerp(0.08f, 0.18f, pulse)), Color.black);
    }

    private void TintSun()
    {
        if (sun == null) return;
        tintingSun = true;

        Color target = sunColor;
        if (Light == Signal.Red) target = Color.Lerp(sunColor, red, 0.3f);
        if (Light == Signal.Yellow) target = Color.Lerp(sunColor, yellow, 0.2f);
        sun.color = Color.Lerp(sun.color, target, 1f - Mathf.Exp(-8f * Time.deltaTime));
    }

    private void RestoreSun()
    {
        if (!tintingSun || sun == null) return;
        sun.color = sunColor;
        tintingSun = false;
    }

    private void BuildDome()
    {
        domeRoot = new GameObject("Staph It Safe Zone").transform;

        domeMaterial = ArtUtil.TransparentLit(green, 0.8f);
        dome = ArtUtil.MakeShape(PrimitiveType.Sphere, "Dome", domeRoot, domeMaterial);

        edgeMaterial = new Material(VfxMaterials.Additive());
        ArtUtil.SetMaterialTexture(edgeMaterial, TextureMaker.Ring());
        domeEdge = ArtUtil.MakeShape(PrimitiveType.Quad, "Edge", domeRoot, edgeMaterial);
        domeEdge.localRotation = Quaternion.Euler(90f, 0f, 0f);
        domeEdge.localPosition = Vector3.up * 0.06f;

        beamMaterial = new Material(VfxMaterials.Additive());
        domeBeam = ArtUtil.MakeShape(PrimitiveType.Cylinder, "Beam", domeRoot, beamMaterial);
    }

    private void SetColor(Material material, Color color, Color emission)
    {
        ArtUtil.SetMaterialColor(material, color);
        if (material.HasProperty("_EmissionColor") && emission != Color.black) material.SetColor("_EmissionColor", emission);
    }

    private Light FindSun()
    {
        if (RenderSettings.sun != null) return RenderSettings.sun;
        foreach (Light light in FindObjectsByType<Light>())
        {
            if (light.type == LightType.Directional) return light;
        }
        return null;
    }
}
