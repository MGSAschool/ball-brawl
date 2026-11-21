using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombaClutchMode : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [SerializeField] private ArenaFloor arena;
    [SerializeField] private int pointsPerBomb = 1;

    [Header("Chain Reactions")]
    [SerializeField] private float chainRadiusFraction = 0.55f;
    [SerializeField] private float chainDelay = 0.15f;

    [Header("Bomb Spawning (start of match -> end of match)")]
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private float startSpawnInterval = 1.3f;
    [SerializeField] private float endSpawnInterval = 0.55f;
    [SerializeField] private int startMaxBombs = 10;
    [SerializeField] private int endMaxBombs = 20;
    [SerializeField] private float spawnHeight = 1.2f;
    [SerializeField] private float endFuseMultiplier = 0.65f;
    [SerializeField] private float endForceMultiplier = 1.2f;
    [SerializeField] private int bombsAtStart = 4;

    private readonly Dictionary<BrawlPlayer, int> chains = new Dictionary<BrawlPlayer, int>();
    private readonly List<Bomb> bombs = new List<Bomb>();
    private bool running;
    private bool explained;
    private float nextSpawnAt;

    private float Progress
    {
        get
        {
            if (match == null || match.Duration <= 0f) return 0f;
            return 1f - match.TimeRemaining / match.Duration;
        }
    }

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        if (arena == null) arena = FindAnyObjectByType<ArenaFloor>();
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
        StopRound();
    }

    private void OnMatchStateChanged(MatchState state)
    {
        running = state == MatchState.Playing && match.Mode == GameMode.BombaClutch;
        if (running)
        {
            explained = false;
            StartSpawning();
        }
        else
        {
            StopRound();
        }
    }

    private void Update()
    {
        if (!running) return;

        if (!explained && match.TimeRemaining < match.Duration - 2f)
        {
            explained = true;
            match.Announce("Roll into bombs to set them off: +1 each. Chains count too!");
        }

        UpdateSpawning();
    }

    private void OnDetonated(Bomb bomb, BrawlPlayer setOffBy)
    {
        Vfx.Explosion(bomb.transform.position, bomb.ExplosionRadius);
        if (!running || setOffBy == null) return;

        int chain = 0;
        chains.TryGetValue(setOffBy, out chain);
        chain++;
        chains[setOffBy] = chain;

        if (match.AwardPoints(setOffBy, pointsPerBomb))
        {
            PlayerEntry entry = match.EntryFor(setOffBy);
            if (chain >= 2 && entry != null) match.Announce(entry.Name + " CHAIN x" + chain + "!");
        }

        bool startedChain = false;
        float reach = bomb.ExplosionRadius * chainRadiusFraction;
        foreach (Bomb other in bombs)
        {
            if (other == null || other == bomb || other.IsPrimed) continue;
            if (Vector3.Distance(other.transform.position, bomb.transform.position) > reach) continue;

            StartCoroutine(PrimeLater(other, setOffBy));
            startedChain = true;
        }
        if (!startedChain) StartCoroutine(EndChainLater(setOffBy));
    }

    private IEnumerator PrimeLater(Bomb bomb, BrawlPlayer setOffBy)
    {
        yield return new WaitForSeconds(chainDelay);
        if (bomb != null) bomb.Prime(setOffBy);
    }

    private IEnumerator EndChainLater(BrawlPlayer setOffBy)
    {
        yield return new WaitForSeconds(chainDelay * 2f);
        chains[setOffBy] = 0;
    }

    private void StopRound()
    {
        StopAllCoroutines();
        chains.Clear();
        ClearBombs();
    }

    private void StartSpawning()
    {
        nextSpawnAt = Time.time + 0.5f;
        for (int i = 0; i < bombsAtStart; i++) SpawnBomb();
    }

    private void ClearBombs()
    {
        foreach (Bomb bomb in bombs)
        {
            if (bomb != null) Destroy(bomb.gameObject);
        }
        bombs.Clear();
    }

    private void UpdateSpawning()
    {
        for (int i = bombs.Count - 1; i >= 0; i--)
        {
            if (bombs[i] == null) bombs.RemoveAt(i);
        }

        if (Time.time < nextSpawnAt) return;
        nextSpawnAt = Time.time + Mathf.Lerp(startSpawnInterval, endSpawnInterval, Progress);

        int maxBombs = Mathf.RoundToInt(Mathf.Lerp(startMaxBombs, endMaxBombs, Progress));
        if (bombs.Count < maxBombs) SpawnBomb();
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
            Vector2 random = Random.insideUnitCircle * 30f;
            point = new Vector3(random.x, 0f, random.y);
        }

        if (IsTooCloseToAPlayer(point)) return;

        GameObject go = Instantiate(bombPrefab, point + Vector3.up * spawnHeight, Quaternion.identity);
        go.name = "Bomb";
        Bomb bomb = go.GetComponent<Bomb>();
        if (bomb != null)
        {
            float p = Progress;
            bomb.Initialize(null, Mathf.Lerp(1f, endFuseMultiplier, p), Mathf.Lerp(1f, endForceMultiplier, p));
            bombs.Add(bomb);
        }
        go.AddComponent<BombVisual>();
        go.AddComponent<DespawnWhenUnsupported>();
    }

    private bool IsTooCloseToAPlayer(Vector3 point)
    {
        foreach (PlayerEntry entry in match.Players)
        {
            if (entry.Player == null || entry.IsRespawning) continue;
            if (Vector3.Distance(entry.Player.transform.position, point) < 3f) return true;
        }
        return false;
    }
}
