using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

// Core Ball Brawl loop: Lobby (join + free roam) -> Countdown -> timed match -> Results.
// Knocking an opponent off the platform scores for whoever hit them last.
public class MatchManager : MonoBehaviour
{
    public enum MatchState { Lobby, Countdown, Playing, MatchOver }

    public class PlayerEntry
    {
        public BrawlPlayer Player;
        public PlayerInventory Inventory;
        public string Name;
        public Color Color;
        public int Score;
        public int Knockouts;
        public int Falls;
        public int Coins;
        public bool IsRespawning;
        public float RespawnAt;
        public string ControlScheme;
        // Power-up types this player has already been shown a "press X" hint for
        public readonly HashSet<System.Type> HintedPowerUps = new HashSet<System.Type>();
    }

    public static MatchManager Instance { get; private set; }

    // Fired after every state change; arena events use it to start, stop and reset
    public event Action<MatchState> StateChanged;

    [Header("Players")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private int minPlayersToStart = 2;
    [SerializeField] private Color[] playerColors =
    {
        new Color(0.95f, 0.30f, 0.30f),
        new Color(0.30f, 0.55f, 0.95f),
        new Color(0.35f, 0.85f, 0.40f),
        new Color(0.98f, 0.80f, 0.25f)
    };

    [Header("Match Rules")]
    [SerializeField] private float matchDuration = 180f;
    [SerializeField] private float countdownDuration = 3f;
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private float spawnProtection = 1.5f;
    [Tooltip("A fall counts as a knock-off if the player was hit within this many seconds.")]
    [SerializeField] private float knockoutCreditWindow = 5f;
    [Tooltip("Points lost for falling off without being hit.")]
    [SerializeField] private int selfKnockoutPenalty = 1;
    [Tooltip("Safety net: players below this height count as fallen even if they miss the DeathZone.")]
    [SerializeField] private float fallHeight = -15f;
    [Tooltip("Results stay up at least this long before a rematch can be started.")]
    [SerializeField] private float resultsLockDuration = 2f;

    [Header("Arena")]
    [SerializeField] private PowerUpSpawner powerUpSpawner;

    public MatchState State { get; private set; } = MatchState.Lobby;
    public float TimeRemaining { get; private set; }
    public float CountdownRemaining { get; private set; }
    public IReadOnlyList<PlayerEntry> Players => players;
    public IReadOnlyList<PlayerEntry> Winners => winners;
    public int MinPlayersToStart => minPlayersToStart;
    public string Announcement { get; private set; }
    public float AnnouncementTime { get; private set; } = float.NegativeInfinity;
    public bool CanStart => players.Count >= minPlayersToStart &&
        (State == MatchState.Lobby || (State == MatchState.MatchOver && Time.time - matchEndedAt >= resultsLockDuration));

    private readonly List<PlayerEntry> players = new List<PlayerEntry>();
    private readonly List<PlayerEntry> winners = new List<PlayerEntry>();
    private readonly HashSet<InputDevice> joinedGamepads = new HashSet<InputDevice>();
    private bool wasdJoined;
    private bool arrowsJoined;
    private float matchEndedAt;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (powerUpSpawner != null) powerUpSpawner.enabled = false;
        Announce("Join in, then press ENTER / START to brawl!");
    }

    private void Update()
    {
        if (State == MatchState.Lobby || State == MatchState.MatchOver)
        {
            HandleJoining();
            if (CanStart && StartPressed()) StartCountdown();
        }

        switch (State)
        {
            case MatchState.Countdown:
                CountdownRemaining -= Time.deltaTime;
                if (CountdownRemaining <= 0f) BeginMatch();
                break;

            case MatchState.Playing:
                TimeRemaining -= Time.deltaTime;
                if (TimeRemaining <= 0f) EndMatch();
                break;
        }

        UpdatePlayers();
    }

    // --- Joining ---
    private void HandleJoining()
    {
        if (players.Count >= maxPlayers || playerPrefab == null) return;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (!wasdJoined && keyboard.spaceKey.wasPressedThisFrame)
            {
                wasdJoined = Join("Keyboard", keyboard);
            }
            else if (!arrowsJoined && (keyboard.rightShiftKey.wasPressedThisFrame || keyboard.numpad0Key.wasPressedThisFrame))
            {
                arrowsJoined = Join("Arrows", keyboard);
            }
        }

        foreach (var gamepad in Gamepad.all)
        {
            if (players.Count >= maxPlayers) break;
            if (!joinedGamepads.Contains(gamepad) && gamepad.buttonSouth.wasPressedThisFrame && Join("Gamepad", gamepad))
            {
                joinedGamepads.Add(gamepad);
            }
        }
    }

    private bool Join(string controlScheme, InputDevice device)
    {
        int index = players.Count;
        var input = PlayerInput.Instantiate(playerPrefab, controlScheme: controlScheme, pairWithDevice: device);
        if (!input.TryGetComponent<BrawlPlayer>(out var player))
        {
            Debug.LogError($"{playerPrefab.name} needs a BrawlPlayer component to join the match.");
            Destroy(input.gameObject);
            return false;
        }

        var entry = new PlayerEntry
        {
            Player = player,
            Inventory = input.GetComponent<PlayerInventory>(),
            Name = $"P{index + 1}",
            Color = playerColors.Length > 0 ? playerColors[index % playerColors.Length] : Color.white,
            ControlScheme = controlScheme
        };
        input.gameObject.name = $"Player {index + 1} ({controlScheme})";
        players.Add(entry);
        if (entry.Inventory != null) entry.Inventory.Collected += powerUp => ShowPowerUpHint(entry, powerUp);

        foreach (var rend in input.GetComponentsInChildren<Renderer>())
        {
            rend.material.color = entry.Color;
        }

        DisablePlayerCameras(input.gameObject);
        PlaceAtSpawn(entry, GetSpawnPosition(index));
        Announce($"{entry.Name} joined ({controlScheme})");
        return true;
    }

    // First time each player grabs a power-up type, tell them which of *their* keys uses it
    private void ShowPowerUpHint(PlayerEntry entry, PowerUpModifier powerUp)
    {
        if (!entry.HintedPowerUps.Add(powerUp.GetType())) return;

        string name = ControlHints.PowerUpName(powerUp);
        string key = ControlHints.KeyFor(entry.ControlScheme, powerUp);
        Announce(key != null
            ? $"{entry.Name}: {name} stored - press {key} to use it"
            : $"{entry.Name}: {name}!");
    }

    // The arena uses one shared camera, so per-player cameras on the prefab are switched off
    private static void DisablePlayerCameras(GameObject playerObject)
    {
        Camera main = Camera.main;
        if (main == null || main.transform.IsChildOf(playerObject.transform)) return;

        foreach (var cam in playerObject.GetComponentsInChildren<Camera>()) cam.enabled = false;
        foreach (var listener in playerObject.GetComponentsInChildren<AudioListener>()) listener.enabled = false;
    }

    private static bool StartPressed()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            return true;

        foreach (var gamepad in Gamepad.all)
        {
            if (gamepad.startButton.wasPressedThisFrame) return true;
        }
        return false;
    }

    // --- Match flow ---
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
            var entry = players[i];
            entry.Score = 0;
            entry.Knockouts = 0;
            entry.Falls = 0;
            entry.Coins = 0;
            entry.IsRespawning = false;
            if (entry.Inventory != null) entry.Inventory.Clear();
            PlaceAtSpawn(entry, GetSpawnPosition(i));
        }
        Announce("Get ready...");
    }

    private void BeginMatch()
    {
        SetState(MatchState.Playing);
        TimeRemaining = matchDuration;
        foreach (var entry in players)
        {
            if (!entry.IsRespawning) entry.Player.ControlsEnabled = true;
        }
        if (powerUpSpawner != null) powerUpSpawner.enabled = true;
        Announce("BRAWL!");
    }

    private void EndMatch()
    {
        SetState(MatchState.MatchOver);
        TimeRemaining = 0f;
        matchEndedAt = Time.time;
        if (powerUpSpawner != null) powerUpSpawner.enabled = false;

        foreach (var entry in players) entry.Player.ControlsEnabled = false;

        winners.Clear();
        if (players.Count > 0)
        {
            int best = players.Max(p => p.Score);
            winners.AddRange(players.Where(p => p.Score == best));
        }

        Announce(winners.Count == 1 ? $"{winners[0].Name} wins!" : "It's a draw!");
    }

    private void SetState(MatchState newState)
    {
        State = newState;
        StateChanged?.Invoke(newState);
    }

    // --- Scoring from arena events ---
    public bool AwardCoins(BrawlPlayer player, int amount)
    {
        var entry = FindEntry(player);
        if (entry == null || State != MatchState.Playing || entry.IsRespawning) return false;

        entry.Coins += amount;
        entry.Score += amount;
        return true;
    }

    // --- Knock-offs & respawning ---
    public void HandlePlayerFell(BrawlPlayer player)
    {
        var entry = FindEntry(player);
        if (entry == null || entry.IsRespawning) return;

        if (State == MatchState.Playing)
        {
            entry.Falls++;
            var attacker = FindEntry(player.LastAttacker);
            bool credited = attacker != null && Time.time - player.LastHitTime <= knockoutCreditWindow;

            if (credited)
            {
                attacker.Score++;
                attacker.Knockouts++;
                Announce($"{attacker.Name} knocked off {entry.Name}!");
            }
            else
            {
                entry.Score -= selfKnockoutPenalty;
                Announce($"{entry.Name} fell off!");
            }
        }

        Hide(entry);
        entry.IsRespawning = true;
        entry.RespawnAt = Time.time + respawnDelay;
    }

    private void UpdatePlayers()
    {
        foreach (var entry in players)
        {
            if (entry.Player == null) continue;

            if (entry.IsRespawning)
            {
                if (Time.time >= entry.RespawnAt)
                {
                    entry.IsRespawning = false;
                    PlaceAtSpawn(entry, GetSafestSpawnPosition(entry));
                    if (State == MatchState.Playing) entry.Player.GrantInvulnerability(spawnProtection);
                }
            }
            else if (entry.Player.transform.position.y < fallHeight)
            {
                HandlePlayerFell(entry.Player);
            }
        }
    }

    private void Hide(PlayerEntry entry)
    {
        var rb = entry.Player.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        entry.Player.ControlsEnabled = false;
        SetVisible(entry, false);
    }

    private void PlaceAtSpawn(PlayerEntry entry, Vector3 position)
    {
        var rb = entry.Player.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.position = position;
        entry.Player.transform.position = position;
        entry.Player.ResetState();
        SetVisible(entry, true);
        entry.Player.ControlsEnabled = State == MatchState.Lobby || State == MatchState.Playing;
    }

    private static void SetVisible(PlayerEntry entry, bool visible)
    {
        foreach (var rend in entry.Player.GetComponentsInChildren<Renderer>()) rend.enabled = visible;
        foreach (var col in entry.Player.GetComponentsInChildren<Collider>()) col.enabled = visible;
    }

    // --- Spawn points ---
    // Arena events can move spawning elsewhere for a while (e.g. onto the late-match island)
    private Transform[] spawnOverride;

    // Arena events can rule out respawn spots (e.g. over fallen floor) and offer a spot when none are left
    public Func<Vector3, bool> SpawnPointCheck { get; set; }
    public Func<Vector3?> SpawnFallback { get; set; }

    public void SetSpawnOverride(Transform[] points) => spawnOverride = points != null && points.Length > 0 ? points : null;
    public void ClearSpawnOverride() => spawnOverride = null;

    private Transform[] ActiveSpawnPoints => spawnOverride ?? spawnPoints;

    private Vector3 GetSpawnPosition(int index)
    {
        var points = ActiveSpawnPoints;
        if (points != null && points.Length > 0)
        {
            return points[index % points.Length].position;
        }

        // No spawn points assigned: spread players in a ring around the manager
        float angle = index * Mathf.PI * 2f / Mathf.Max(1, maxPlayers);
        return transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 8f + Vector3.up * 2f;
    }

    // Respawn at the spawn point farthest from everyone still on the platform
    private Vector3 GetSafestSpawnPosition(PlayerEntry respawning)
    {
        var points = ActiveSpawnPoints;
        int count = points != null && points.Length > 0 ? points.Length : maxPlayers;
        Vector3 best = GetSpawnPosition(0);
        float bestDistance = float.NegativeInfinity;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            Vector3 candidate = GetSpawnPosition(i);
            if (SpawnPointCheck != null && !SpawnPointCheck(candidate)) continue;
            found = true;
            float nearest = float.PositiveInfinity;
            foreach (var other in players)
            {
                if (other == respawning || other.IsRespawning || other.Player == null) continue;
                nearest = Mathf.Min(nearest, Vector3.Distance(candidate, other.Player.transform.position));
            }
            if (nearest > bestDistance)
            {
                bestDistance = nearest;
                best = candidate;
            }
        }

        if (!found && SpawnFallback != null && SpawnFallback() is Vector3 fallback) return fallback;
        return best;
    }

    private PlayerEntry FindEntry(BrawlPlayer player)
    {
        if (player == null) return null;
        return players.FirstOrDefault(p => p.Player == player);
    }

    public void Announce(string message)
    {
        Announcement = message;
        AnnouncementTime = Time.time;
    }
}
