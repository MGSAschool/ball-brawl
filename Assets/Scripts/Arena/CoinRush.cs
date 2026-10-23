using System.Collections.Generic;
using UnityEngine;

// "Coin Rush (Get Your Coins)" stage event. A few times per match coins scatter across
// the arena, mostly toward the risky outer rings, for a short window. Each coin scores.
public class CoinRush : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [Tooltip("Optional: when set, coins only land on sections that are still solid.")]
    [SerializeField] private FallingPlatforms platforms;
    [SerializeField] private Coin coinPrefab;

    [Header("Timing")]
    [SerializeField] private float firstRushDelay = 30f;
    [SerializeField] private Vector2 rushInterval = new Vector2(35f, 50f);
    [SerializeField] private float rushDuration = 10f;
    [Tooltip("No new rush starts when less than this much match time is left.")]
    [SerializeField] private float minMatchTimeLeft = 8f;

    [Header("Coins")]
    [SerializeField] private int coinsPerRush = 15;
    [Tooltip("Coins land at least this far from the center, pushing players toward the edges.")]
    [SerializeField] private float minSpawnRadius = 18f;
    [SerializeField] private float heightAboveFloor = 1.2f;
    [Tooltip("Used without FallingPlatforms: center and radius of the area coins can land in.")]
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private float fallbackRadius = 45f;

    public bool IsActive { get; private set; }
    public float TimeRemaining => IsActive ? Mathf.Max(0f, rushEndsAt - Time.time) : 0f;
    public int CoinsLeft => coins.Count;

    private readonly List<Coin> coins = new List<Coin>();
    private bool matchRunning;
    private float nextRushAt;
    private float rushEndsAt;

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
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
        matchRunning = state == MatchManager.MatchState.Playing;
        if (matchRunning)
        {
            nextRushAt = Time.time + firstRushDelay;
        }
        else
        {
            EndRush(false);
        }
    }

    private void Update()
    {
        coins.RemoveAll(c => c == null);

        if (IsActive && (Time.time >= rushEndsAt || coins.Count == 0))
        {
            EndRush(true);
        }
        else if (!IsActive && matchRunning && Time.time >= nextRushAt && match.TimeRemaining > minMatchTimeLeft)
        {
            StartRush();
        }
    }

    private void StartRush()
    {
        if (coinPrefab == null) return;

        IsActive = true;
        rushEndsAt = Time.time + rushDuration;
        for (int i = 0; i < coinsPerRush; i++)
        {
            if (TryGetSpawnPoint(out Vector3 floorPoint))
            {
                var coin = Instantiate(coinPrefab, floorPoint + Vector3.up * heightAboveFloor, Quaternion.identity, transform);
                if (coin.GetComponent<DespawnWhenUnsupported>() == null) coin.gameObject.AddComponent<DespawnWhenUnsupported>();
                coins.Add(coin);
            }
        }
        match.Announce("COIN RUSH! Get your coins!");
    }

    private void EndRush(bool announce)
    {
        foreach (var coin in coins)
        {
            if (coin != null) Destroy(coin.gameObject);
        }
        coins.Clear();

        if (IsActive && announce) match.Announce("Coin Rush over");
        IsActive = false;
        nextRushAt = Time.time + Random.Range(rushInterval.x, rushInterval.y);
    }

    private bool TryGetSpawnPoint(out Vector3 point)
    {
        if (platforms != null)
        {
            return platforms.TryGetRandomSolidPoint(minSpawnRadius, out point);
        }

        // No segmented arena: pick a spot in the ring and drop a ray onto whatever floor is there
        Vector3 center = arenaCenter != null ? arenaCenter.position : Vector3.zero;
        for (int attempt = 0; attempt < 10; attempt++)
        {
            float angle = Random.value * Mathf.PI * 2f;
            float r = Mathf.Sqrt(Mathf.Lerp(minSpawnRadius * minSpawnRadius, fallbackRadius * fallbackRadius, Random.value));
            Vector3 above = center + new Vector3(Mathf.Cos(angle) * r, 30f, Mathf.Sin(angle) * r);
            if (Physics.Raycast(above, Vector3.down, out var hit, 60f, ~0, QueryTriggerInteraction.Ignore) &&
                hit.collider.GetComponentInParent<BrawlPlayer>() == null)
            {
                point = hit.point;
                return true;
            }
        }
        point = center;
        return false;
    }
}
