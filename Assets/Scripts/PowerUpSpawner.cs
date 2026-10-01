using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Prefabs to Spawn")]
    [SerializeField] private GameObject[] powerUpPrefabs;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private int maxPowerUpsInArena = 6;
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private float arenaRadius = 35f;
    [SerializeField] private float spawnHeight = 1.2f;

    private readonly List<GameObject> activePowerUps = new List<GameObject>();

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            // Clean up destroyed/collected items from tracking list
            activePowerUps.RemoveAll(item => item == null);

            if (activePowerUps.Count < maxPowerUpsInArena && powerUpPrefabs.Length > 0)
            {
                SpawnRandomPowerUp();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnRandomPowerUp()
    {
        // Pick random power-up prefab
        int index = Random.Range(0, powerUpPrefabs.Length);
        GameObject chosenPrefab = powerUpPrefabs[index];
        if (chosenPrefab == null) return;

        // Calculate circular spawn point on arena floor
        Vector2 randomCircle = Random.insideUnitCircle * arenaRadius;
        Vector3 origin = arenaCenter != null ? arenaCenter.position : Vector3.zero;
        Vector3 spawnPos = origin + new Vector3(randomCircle.x, spawnHeight, randomCircle.y);

        GameObject spawned = Instantiate(chosenPrefab, spawnPos, Quaternion.identity);
        activePowerUps.Add(spawned);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = arenaCenter != null ? arenaCenter.position : transform.position;
        Gizmos.DrawWireSphere(center, arenaRadius);
    }
}