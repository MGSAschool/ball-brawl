using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(StaphItSafeZone))]
public class StaphItMode : MonoBehaviour
{
    public enum Signal { Off, Green, Yellow, Red }

    public static StaphItMode Active { get; private set; }

    [SerializeField] private MatchManager match;

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

    [Header("Look")]
    [SerializeField] private Color green = new Color(0.3f, 1f, 0.45f);
    [SerializeField] private Color yellow = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color red = new Color(1f, 0.2f, 0.2f);

    private readonly List<PlayerEntry> caught = new List<PlayerEntry>();
    private StaphItSafeZone zone;
    private bool running;
    private bool firstRound;
    private float phaseEndsAt;

    public Signal Light { get; private set; } = Signal.Off;
    public StaphItSafeZone Zone { get { return zone; } }
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
        zone = GetComponent<StaphItSafeZone>();
        if (zone == null) zone = gameObject.AddComponent<StaphItSafeZone>();
        if (match == null) match = FindAnyObjectByType<MatchManager>();
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
        zone.Release();
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
        zone.MoveToNewSpot(Progress);

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
            if (!zone.Contains(entry.Player.transform.position)) continue;

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
            if (zone.Contains(entry.Player.transform.position)) continue;
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

        Vector3 away = position - zone.Center;
        away.y = 0f;
        Vector3 direction = Vector3.up;
        if (away.sqrMagnitude > 0.01f) direction += away.normalized * 0.15f;
        entry.Player.ApplyKnockback(direction.normalized, zapForce);

        match.Announce(entry.Name + " got caught outside! -" + caughtPenalty);
    }
}
