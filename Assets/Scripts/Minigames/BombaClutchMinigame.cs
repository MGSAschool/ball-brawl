using System.Collections;
using UnityEngine;
using TMPro; // Optional: Remove if using standard UI or custom HUD

public class BombaClutchMinigame : MonoBehaviour
{
    [Header("Timer & Scoring")]
    [SerializeField] private float gameDuration = 60f;
    private float currentTimer;
    private int totalScore = 0;
    private bool isGameActive = false;

    [Header("Spawn Settings")]
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private float spawnInterval = 1.2f;
    [SerializeField] private int maxActiveBombs = 12;
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private float arenaRadius = 8f; // Sized to match your Cylinder floor
    [SerializeField] private float spawnHeight = 0.5f;

    [Header("UI (Optional)")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI scoreText;

    private int activeBombCount = 0;

    private void Start()
    {
        StartMinigame();
    }

    public void StartMinigame()
    {
        currentTimer = gameDuration;
        totalScore = 0;
        isGameActive = true;
        UpdateUI();

        StartCoroutine(SpawnRoutine());
    }

    private void Update()
    {
        if (!isGameActive) return;

        currentTimer -= Time.deltaTime;
        if (currentTimer <= 0f)
        {
            currentTimer = 0f;
            EndMinigame();
        }

        UpdateUI();
    }

    private IEnumerator SpawnRoutine()
    {
        while (isGameActive)
        {
            if (activeBombCount < maxActiveBombs)
            {
                SpawnBomb();
            }
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnBomb()
    {
        // Random point on circular platform
        Vector2 randomCircle = Random.insideUnitCircle * arenaRadius;
        Vector3 spawnPosition = arenaCenter != null ? arenaCenter.position : Vector3.zero;
        spawnPosition += new Vector3(randomCircle.x, spawnHeight, randomCircle.y);

        GameObject bombObj = Instantiate(bombPrefab, spawnPosition, Quaternion.identity);
        activeBombCount++;

        if (bombObj.TryGetComponent<Bomb>(out var bomb))
        {
            bomb.Initialize(this);
        }
    }

    public void AddScore(int amount)
    {
        if (!isGameActive) return;
        totalScore += amount;
        activeBombCount = Mathf.Max(0, activeBombCount - 1);
        UpdateUI();
    }

    private void EndMinigame()
    {
        isGameActive = false;
        StopAllCoroutines();
        Debug.Log($"Bomba Clutch Over! Final Score: {totalScore}");
    }

    private void UpdateUI()
    {
        if (timerText != null) timerText.text = $"Time: {Mathf.CeilToInt(currentTimer)}s";
        if (scoreText != null) scoreText.text = $"Score: {totalScore}";
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = arenaCenter != null ? arenaCenter.position : transform.position;
        Gizmos.DrawWireSphere(center, arenaRadius);
    }

    public void PlayerFell(BrawlPlayer player)
{
    Debug.Log($"{player.name} fell off the platform!");

    EndMinigame();
}
}