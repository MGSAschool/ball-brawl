using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// BombaClutch inside the Ball Brawl arena (the "reverse minesweeper"): bombs keep dropping onto
// solid floor and every bomb you set off is +1 point, but the blast throws you too. Blasts set
// off nearby bombs, so chains score for whoever started them. Falling platforms, power-ups and
// knock-off rules stay on (knock-offs score nothing here; falling on your own still costs 1).
// Uses the team's Bomb prefab; spawn pacing follows the original BombaClutchMinigame prototype.
public class BombaClutchMode : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [SerializeField] private FallingPlatforms arena;
    [SerializeField] private GameObject bombPrefab;

    [Header("Spawning")]
    [SerializeField] private float startSpawnInterval = 1.3f;
    [SerializeField] private float endSpawnInterval = 0.55f;
    [SerializeField] private int startMaxBombs = 10;
    [SerializeField] private int endMaxBombs = 20;
    [SerializeField] private float spawnHeight = 1.2f;
    [Tooltip("Fuse and blast multipliers at the very end of the round (they ramp up from 1).")]
    [SerializeField] private float endFuseMultiplier = 0.65f;
    [SerializeField] private float endForceMultiplier = 1.2f;
    [SerializeField] private int pointsPerBomb = 1;

    [Header("Chain reactions")]
    [Tooltip("Bombs within this fraction of the blast radius go off too.")]
    [SerializeField] private float chainRadiusFraction = 0.55f;
    [SerializeField] private float chainDelay = 0.15f;

    private readonly List<Bomb> bombs = new List<Bomb>();
    private readonly Dictionary<BrawlPlayer, int> chains = new Dictionary<BrawlPlayer, int>();
    private bool running;
    private float nextSpawnAt;
    private bool explained;

    private bool IsMode => match != null && match.Mode == GameMode.BombaClutch;
    // 0 at the start of the round, 1 at the end
    private float Progress => match != null && match.Duration > 0f ? 1f - match.TimeRemaining / match.Duration : 0f;

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        if (arena == null) arena = FindAnyObjectByType<FallingPlatforms>();
    }

    private void OnEnable()
    {
        if (match != null) match.StateChanged += OnMatchStateChanged;
        Bomb.Detonated += OnDetonated;
    }

    private void OnDisable()
    {
        if (match != null) match.StateChanged -= OnMatchStateChanged;
        Bomb.Detonated -= OnDetonated;
        ClearBombs();
    }

    private void OnMatchStateChanged(MatchManager.MatchState state)
    {
        running = state == MatchManager.MatchState.Playing && IsMode;
        if (running)
        {
            nextSpawnAt = Time.time + 0.5f;
            explained = false;
            // A few to start with so there's something to go for straight away
            for (int i = 0; i < 4; i++) SpawnBomb();
        }
        else
        {
            ClearBombs();
        }
    }

    private void Update()
    {
        if (!running) return;
        bombs.RemoveAll(b => b == null);

        if (!explained && match.TimeRemaining < match.Duration - 2f)
        {
            explained = true;
            match.Announce("Roll into bombs to set them off: +1 each. Chains count too!");
        }

        if (Time.time < nextSpawnAt) return;
        nextSpawnAt = Time.time + Mathf.Lerp(startSpawnInterval, endSpawnInterval, Progress);
        if (bombs.Count < Mathf.RoundToInt(Mathf.Lerp(startMaxBombs, endMaxBombs, Progress))) SpawnBomb();
    }

    private void SpawnBomb()
    {
        if (bombPrefab == null) return;
        Vector3 point;
        if (arena != null)
        {
            if (!arena.TryGetRandomSolidPoint(0f, out point)) return;
        }
        else
        {
            Vector2 c = Random.insideUnitCircle * 30f;
            point = new Vector3(c.x, 0f, c.y);
        }

        // Don't drop a bomb right on top of a ball
        foreach (var entry in match.Players)
        {
            if (entry.Player != null && !entry.IsRespawning && Vector3.Distance(entry.Player.transform.position, point) < 3f) return;
        }

        var go = Instantiate(bombPrefab, point + Vector3.up * spawnHeight, Quaternion.identity);
        go.name = "Bomb";
        if (go.TryGetComponent<Bomb>(out var bomb))
        {
            float p = Progress;
            bomb.Initialize(null, Mathf.Lerp(1f, endFuseMultiplier, p), Mathf.Lerp(1f, endForceMultiplier, p));
            bombs.Add(bomb);
        }
        go.AddComponent<BombVisual>();
        go.AddComponent<DespawnWhenUnsupported>();
    }

    private void OnDetonated(Bomb bomb, BrawlPlayer by)
    {
        Vfx.Explosion(bomb.transform.position, bomb.ExplosionRadius);
        if (!running || by == null) return;

        // Chain count resets when the blast that started it is over
        chains.TryGetValue(by, out int chain);
        chain++;
        chains[by] = chain;
        if (match.AwardPoints(by, pointsPerBomb))
        {
            var entry = match.EntryFor(by);
            if (chain >= 2 && entry != null) match.Announce($"{entry.Name} CHAIN x{chain}!");
        }

        bool chained = false;
        float reach = bomb.ExplosionRadius * chainRadiusFraction;
        foreach (var other in bombs)
        {
            if (other == null || other == bomb || other.IsPrimed) continue;
            if (Vector3.Distance(other.transform.position, bomb.transform.position) > reach) continue;
            StartCoroutine(PrimeLater(other, by));
            chained = true;
        }
        if (!chained) StartCoroutine(EndChainLater(by));
    }

    private IEnumerator PrimeLater(Bomb bomb, BrawlPlayer by)
    {
        yield return new WaitForSeconds(chainDelay);
        if (bomb != null) bomb.Prime(by);
    }

    private IEnumerator EndChainLater(BrawlPlayer by)
    {
        yield return new WaitForSeconds(chainDelay * 2f);
        chains[by] = 0;
    }

    private void ClearBombs()
    {
        StopAllCoroutines();
        foreach (var bomb in bombs)
        {
            if (bomb != null) Destroy(bomb.gameObject);
        }
        bombs.Clear();
        chains.Clear();
    }
}
