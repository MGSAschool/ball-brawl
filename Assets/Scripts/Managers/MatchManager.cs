using System;
using System.Collections.Generic;
using UnityEngine;

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance { get; private set; }

    public event Action<MatchState> StateChanged;
    public event Action<GameMode> ModeChanged;
    public event Action<PlayerEntry> PlayerJoined;

    [Header("Match Rules")]
    [SerializeField] private int minPlayersToStart = 2;
    [SerializeField] private float matchDuration = 180f;
    [SerializeField] private float minigameDuration = 120f;
    [SerializeField] private float countdownDuration = 3f;
    [SerializeField] private float knockoutCreditWindow = 5f;
    [SerializeField] private int selfKnockoutPenalty = 1;
    [SerializeField] private float resultsLockDuration = 2f;

    [Header("Arena")]
    [SerializeField] private PowerUpSpawner powerUpSpawner;

    private readonly List<PlayerEntry> players = new List<PlayerEntry>();
    private readonly List<PlayerEntry> winners = new List<PlayerEntry>();
    private PlayerSpawner spawner;
    private float matchEndedAt;

    public MatchState State { get; private set; } = MatchState.Lobby;
    public GameMode Mode { get; private set; } = GameMode.Brawl;
    public float TimeRemaining { get; private set; }
    public float CountdownRemaining { get; private set; }
    public string Announcement { get; private set; }
    public float AnnouncementTime { get; private set; } = float.NegativeInfinity;

    public IReadOnlyList<PlayerEntry> Players { get { return players; } }
    public IReadOnlyList<PlayerEntry> Winners { get { return winners; } }
    public int MinPlayersToStart { get { return minPlayersToStart; } }
    public float Duration
    {
        get
        {
            if (Mode == GameMode.Brawl) return matchDuration;
            return minigameDuration;
        }
    }

    public bool IsInMenu
    {
        get { return State == MatchState.Lobby || State == MatchState.MatchOver; }
    }

    public bool CanStart
    {
        get
        {
            if (players.Count < minPlayersToStart) return false;
            if (State == MatchState.Lobby) return true;
            return State == MatchState.MatchOver && Time.time - matchEndedAt >= resultsLockDuration;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        spawner = GetComponent<PlayerSpawner>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (powerUpSpawner != null) powerUpSpawner.enabled = true;
        Announce("Join in, then press ENTER / START to brawl!");
    }

    private void Update()
    {
        if (IsInMenu && CanStart && MenuInput.StartPressed())
        {
            StartCountdown();
        }

        if (State == MatchState.Countdown)
        {
            UpdateCountdown();
        }
        else if (State == MatchState.Playing)
        {
            TimeRemaining -= Time.deltaTime;
            if (TimeRemaining <= 0f) EndMatch();
        }
    }

    public void AddPlayer(PlayerEntry entry)
    {
        players.Add(entry);
        spawner.PlaceAtSpawn(entry, spawner.GetSpawnPosition(players.Count - 1));
        if (PlayerJoined != null) PlayerJoined(entry);
    }

    public bool TryStartMatch()
    {
        if (!CanStart) return false;
        StartCountdown();
        return true;
    }

    public bool SetMode(GameMode mode)
    {
        if (!IsInMenu) return false;
        if (mode == Mode) return true;

        Mode = mode;
        if (ModeChanged != null) ModeChanged(mode);
        Sfx.Play(Sound.Select);
        return true;
    }

    public void Announce(string message)
    {
        Announcement = message;
        AnnouncementTime = Time.time;
    }

    public PlayerEntry EntryFor(BrawlPlayer player)
    {
        if (player == null) return null;
        foreach (PlayerEntry entry in players)
        {
            if (entry.Player == player) return entry;
        }
        return null;
    }

    public bool AwardCoins(BrawlPlayer player, int amount)
    {
        PlayerEntry entry = EntryFor(player);
        if (entry == null || State != MatchState.Playing || entry.IsRespawning) return false;

        entry.Coins += amount;
        entry.Score += amount;
        Sfx.Play(Sound.Coin, player.transform.position);
        Vfx.Sparkle(player.transform.position, new Color(1f, 0.84f, 0.3f), 10);
        return true;
    }

    public bool AwardPoints(BrawlPlayer player, int amount)
    {
        PlayerEntry entry = EntryFor(player);
        if (entry == null || State != MatchState.Playing) return false;

        entry.Score += amount;
        return true;
    }

    public void HandlePlayerFell(BrawlPlayer player)
    {
        PlayerEntry entry = EntryFor(player);
        if (entry == null || entry.IsRespawning) return;

        Vector3 position = player.transform.position;
        Sfx.Play(Sound.Fall, position, 0.8f);
        Vfx.KnockOutFlare(new Vector3(position.x, Mathf.Max(position.y, -3f), position.z), entry.Color);

        if (State == MatchState.Playing) ScoreFall(entry);
        spawner.StartRespawn(entry);
    }

    private void ScoreFall(PlayerEntry entry)
    {
        entry.Falls++;
        PlayerEntry attacker = EntryFor(entry.Player.LastAttacker);
        bool hitRecently = Time.time - entry.Player.LastHitTime <= knockoutCreditWindow;

        if (attacker != null && hitRecently)
        {
            attacker.Knockouts++;
            if (Mode != GameMode.BombaClutch)
            {
                attacker.Score++;
                Sfx.Play(Sound.KnockOff, 0.8f);
            }
            Announce(attacker.Name + " knocked off " + entry.Name + "!");
        }
        else
        {
            entry.Score -= selfKnockoutPenalty;
            Announce(entry.Name + " fell off!");
        }
    }

    private void UpdateCountdown()
    {
        int secondsBefore = Mathf.CeilToInt(CountdownRemaining);
        CountdownRemaining -= Time.deltaTime;

        if (CountdownRemaining <= 0f)
        {
            BeginMatch();
        }
        else if (Mathf.CeilToInt(CountdownRemaining) < secondsBefore)
        {
            Sfx.Play(Sound.Countdown);
        }
    }

    private void StartCountdown()
    {
        SetState(MatchState.Countdown);
        CountdownRemaining = countdownDuration;
        winners.Clear();

        if (powerUpSpawner != null)
        {
            powerUpSpawner.enabled = false;
            powerUpSpawner.ClearSpawned();
        }

        for (int i = 0; i < players.Count; i++)
        {
            PlayerEntry entry = players[i];
            entry.ResetStats();
            if (entry.PowerUps != null) entry.PowerUps.Clear();
            spawner.PlaceAtSpawn(entry, spawner.GetSpawnPosition(i));
        }

        Announce("Get ready...");
        Sfx.Play(Sound.Countdown);
    }

    private void BeginMatch()
    {
        TimeRemaining = Duration;
        SetState(MatchState.Playing);

        foreach (PlayerEntry entry in players)
        {
            if (!entry.IsRespawning) entry.Player.ControlsEnabled = true;
        }
        if (powerUpSpawner != null) powerUpSpawner.enabled = true;

        Announce(GameModeInfo.Name(Mode) + "!");
        Sfx.Play(Sound.Go);
    }

    private void EndMatch()
    {
        SetState(MatchState.MatchOver);
        TimeRemaining = 0f;
        matchEndedAt = Time.time;
        if (powerUpSpawner != null) powerUpSpawner.enabled = false;

        foreach (PlayerEntry entry in players)
        {
            entry.Player.ControlsEnabled = false;
        }

        PickWinners();
        if (winners.Count == 1) Announce(winners[0].Name + " wins!");
        else Announce("It's a draw!");

        Sfx.Play(Sound.Whistle);
        Sfx.Play(Sound.Win, 0.9f);
        foreach (PlayerEntry winner in winners)
        {
            if (winner.Player != null && !winner.IsRespawning) Vfx.Confetti(winner.Player.transform.position);
        }
    }

    private void PickWinners()
    {
        winners.Clear();
        if (players.Count == 0) return;

        int best = int.MinValue;
        foreach (PlayerEntry entry in players)
        {
            if (entry.Score > best) best = entry.Score;
        }
        foreach (PlayerEntry entry in players)
        {
            if (entry.Score == best) winners.Add(entry);
        }
    }

    private void SetState(MatchState newState)
    {
        State = newState;
        if (StateChanged != null) StateChanged(newState);
    }
}
