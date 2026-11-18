using System.Collections.Generic;
using UnityEngine;

public class CoinRush : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [SerializeField] private ArenaFloor platforms;
    [SerializeField] private Coin coinPrefab;

    [Header("Timing")]
    [SerializeField] private float firstRushDelay = 30f;
    [SerializeField] private Vector2 rushInterval = new Vector2(35f, 50f);
    [SerializeField] private float rushDuration = 10f;
    [SerializeField] private float minMatchTimeLeft = 8f;

    [Header("Coins")]
    [SerializeField] private int coinsPerRush = 15;
    [SerializeField] private float minSpawnRadius = 9f;
    [SerializeField] private float heightAboveFloor = 1.2f;
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private float fallbackRadius = 20f;

    private readonly List<Coin> coins = new List<Coin>();
    private bool matchRunning;
    private float nextRushAt;
    private float rushEndsAt;

    public bool IsActive { get; private set; }
    public int CoinsLeft { get { return coins.Count; } }

    public float TimeRemaining
    {
        get
        {
            if (!IsActive) return 0f;
            return Mathf.Max(0f, rushEndsAt - Time.time);
        }
    }

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        if (platforms == null) platforms = FindAnyObjectByType<ArenaFloor>();
    }

    private void OnEnable()
    {
        if (match != null) match.StateChanged += OnMatchStateChanged;
    }

    private void OnDisable()
    {
        if (match != null) match.StateChanged -= OnMatchStateChanged;
    }

    private void OnMatchStateChanged(MatchState state)
    {
        matchRunning = state == MatchState.Playing && match.Mode == GameMode.Brawl;
        if (matchRunning) nextRushAt = Time.time + firstRushDelay;
        else EndRush(false);
    }

    private void Update()
    {
        coins.RemoveAll(coin => coin == null);

        if (IsActive)
        {
            if (Time.time >= rushEndsAt || coins.Count == 0) EndRush(true);
        }
        else if (matchRunning && Time.time >= nextRushAt && match.TimeRemaining > minMatchTimeLeft)
        {
            StartRush(minSpawnRadius, coinsPerRush, rushDuration);
        }
    }

    public void StartRushNow(string announcement, int coinCount, float duration)
    {
        if (!matchRunning) return;
        if (IsActive) EndRush(false);

        StartRush(0f, coinCount, duration);
        match.Announce(announcement);
    }

    private void StartRush(float minRadius, int coinCount, float duration)
    {
        if (coinPrefab == null) return;

        IsActive = true;
        rushEndsAt = Time.time + duration;
        for (int i = 0; i < coinCount; i++)
        {
            Vector3 floorPoint;
            if (!TryGetSpawnPoint(minRadius, out floorPoint)) continue;

            Coin coin = Instantiate(coinPrefab, floorPoint + Vector3.up * heightAboveFloor, Quaternion.identity, transform);
            if (coin.GetComponent<DespawnWhenUnsupported>() == null) coin.gameObject.AddComponent<DespawnWhenUnsupported>();
            coins.Add(coin);
        }
        match.Announce("COIN RUSH! Get your coins!");
    }

    private void EndRush(bool announce)
    {
        foreach (Coin coin in coins)
        {
            if (coin != null) Destroy(coin.gameObject);
        }
        coins.Clear();

        if (IsActive && announce) match.Announce("Coin Rush over");
        IsActive = false;
        nextRushAt = Time.time + Random.Range(rushInterval.x, rushInterval.y);
    }

    private bool TryGetSpawnPoint(float minRadius, out Vector3 point)
    {
        if (platforms != null) return platforms.TryGetRandomSolidPoint(minRadius, out point);

        Vector3 center = Vector3.zero;
        if (arenaCenter != null) center = arenaCenter.position;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            float angle = Random.value * Mathf.PI * 2f;
            float r = Mathf.Sqrt(Mathf.Lerp(minRadius * minRadius, fallbackRadius * fallbackRadius, Random.value));
            Vector3 above = center + new Vector3(Mathf.Cos(angle) * r, 30f, Mathf.Sin(angle) * r);

            RaycastHit hit;
            if (Physics.Raycast(above, Vector3.down, out hit, 60f, ~0, QueryTriggerInteraction.Ignore) &&
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
