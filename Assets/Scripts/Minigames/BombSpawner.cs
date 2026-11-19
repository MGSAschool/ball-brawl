using System.Collections.Generic;
using UnityEngine;

public class BombSpawner : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [SerializeField] private ArenaFloor arena;
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private float startSpawnInterval = 1.3f;
    [SerializeField] private float endSpawnInterval = 0.55f;
    [SerializeField] private int startMaxBombs = 10;
    [SerializeField] private int endMaxBombs = 20;
    [SerializeField] private float spawnHeight = 1.2f;
    [SerializeField] private float endFuseMultiplier = 0.65f;
    [SerializeField] private float endForceMultiplier = 1.2f;
    [SerializeField] private int bombsAtStart = 4;

    private readonly List<Bomb> bombs = new List<Bomb>();
    private bool spawning;
    private float nextSpawnAt;

    public IReadOnlyList<Bomb> Bombs { get { return bombs; } }

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

    public void StartSpawning()
    {
        spawning = true;
        nextSpawnAt = Time.time + 0.5f;
        for (int i = 0; i < bombsAtStart; i++) SpawnBomb();
    }

    public void StopAndClear()
    {
        spawning = false;
        foreach (Bomb bomb in bombs)
        {
            if (bomb != null) Destroy(bomb.gameObject);
        }
        bombs.Clear();
    }

    private void OnDisable()
    {
        StopAndClear();
    }

    private void Update()
    {
        if (!spawning) return;
        bombs.RemoveAll(bomb => bomb == null);

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
