using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum GameState { Starting, InProgress, SuddenDeath, GameOver }

public class BombaClutchMinigame : MonoBehaviour
{
    [Header("Match Settings")]
    [SerializeField] private float matchDuration = 60f;
    [SerializeField] private int fallPenalty = 50;

    [Header("Spawning Settings")]
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private float arenaRadius = 38f;
    [SerializeField] private float spawnHeight = 1.2f;
    [SerializeField] private float baseSpawnInterval = 1.2f;
    [SerializeField] private int maxActiveBombs = 25;

    [Header("HUD References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI p1ScoreText;
    [SerializeField] private TextMeshProUGUI p2ScoreText;
    [SerializeField] private TextMeshProUGUI announcementText;

    private float timer;
    private GameState state = GameState.Starting;
    private readonly Dictionary<BrawlPlayer, int> playerScores = new Dictionary<BrawlPlayer, int>();
    private readonly List<BrawlPlayer> activePlayers = new List<BrawlPlayer>();
    private int spawnedBombCount = 0;

    private void Start()
    {
        StartCoroutine(MatchFlowRoutine());
    }

    private IEnumerator MatchFlowRoutine()
    {
        // 1. Countdown Phase
        state = GameState.Starting;
        timer = matchDuration;

        if (announcementText != null)
        {
            announcementText.gameObject.SetActive(true);
            for (int i = 3; i > 0; i--)
            {
                announcementText.text = i.ToString();
                yield return new WaitForSeconds(1f);
            }
            announcementText.text = "BOMBA CLUTCH!";
            yield return new WaitForSeconds(1f);
            announcementText.gameObject.SetActive(false);
        }

        // 2. Main Match Loop
        state = GameState.InProgress;
        StartCoroutine(SpawnBombsRoutine());

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            UpdateHUD();
            yield return null;
        }

        timer = 0f;
        UpdateHUD();

        // 3. Game Over / Sudden Death Check
        EndMatch();
    }

    private IEnumerator SpawnBombsRoutine()
    {
        while (state == GameState.InProgress || state == GameState.SuddenDeath)
        {
            if (spawnedBombCount < maxActiveBombs)
            {
                SpawnBomb();
            }

            // Ramp up frenzy: bombs spawn faster as time gets lower
            float progress = 1f - (timer / matchDuration);
            float currentInterval = Mathf.Lerp(baseSpawnInterval, baseSpawnInterval * 0.45f, progress);
            yield return new WaitForSeconds(currentInterval);
        }
    }

    private void SpawnBomb()
    {
        Vector2 circle = Random.insideUnitCircle * arenaRadius;
        Vector3 origin = arenaCenter != null ? arenaCenter.position : Vector3.zero;
        Vector3 spawnPos = origin + new Vector3(circle.x, spawnHeight, circle.y);

        GameObject bombObj = Instantiate(bombPrefab, spawnPos, Quaternion.identity);
        spawnedBombCount++;

        if (bombObj.TryGetComponent<Bomb>(out var bomb))
        {
            // Faster fuses and bigger blasts toward match end
            float progress = 1f - (timer / matchDuration);
            float fuseMod = Mathf.Lerp(1f, 0.65f, progress);
            float forceMod = Mathf.Lerp(1f, 1.35f, progress);
            bomb.Initialize(this, fuseMod, forceMod);
        }
    }

    public void RegisterPlayer(BrawlPlayer player)
    {
        if (!playerScores.ContainsKey(player))
        {
            playerScores[player] = 0;
            activePlayers.Add(player);
            UpdateHUD();
        }
    }

    public void AwardScore(BrawlPlayer player, int amount)
    {
        if (state != GameState.InProgress && state != GameState.SuddenDeath) return;

        RegisterPlayer(player);
        playerScores[player] += amount;
        spawnedBombCount = Mathf.Max(0, spawnedBombCount - 1);
        UpdateHUD();
    }

    public void PlayerFell(BrawlPlayer player)
    {
        RegisterPlayer(player);
        playerScores[player] = Mathf.Max(0, playerScores[player] - fallPenalty);
        UpdateHUD();
    }

    private void EndMatch()
    {
        state = GameState.GameOver;
        StopAllCoroutines();

        // Calculate winner
        BrawlPlayer winner = null;
        int topScore = -1;
        bool isTie = false;

        foreach (var kvp in playerScores)
        {
            if (kvp.Value > topScore)
            {
                topScore = kvp.Value;
                winner = kvp.Key;
                isTie = false;
            }
            else if (kvp.Value == topScore)
            {
                isTie = true;
            }
        }

        if (announcementText != null)
        {
            announcementText.gameObject.SetActive(true);
            if (isTie)
            {
                announcementText.text = "DRAW!";
            }
            else if (winner != null)
            {
                announcementText.text = $"{winner.name.ToUpper()} WINS!\n{topScore} PTS";
            }
            else
            {
                announcementText.text = "TIME'S UP!";
            }
        }
    }

    private void UpdateHUD()
    {
        if (timerText != null)
        {
            timerText.text = $"TIME: {Mathf.CeilToInt(timer)}s";
        }

        if (activePlayers.Count > 0 && p1ScoreText != null)
        {
            p1ScoreText.text = $"P1: {playerScores[activePlayers[0]]}";
        }

        if (activePlayers.Count > 1 && p2ScoreText != null)
        {
            p2ScoreText.text = $"P2: {playerScores[activePlayers[1]]}";
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origin = arenaCenter != null ? arenaCenter.position : transform.position;
        Gizmos.DrawWireSphere(origin, arenaRadius);
    }
}