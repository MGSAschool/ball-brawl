using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Prefabs to Spawn")]
    [Tooltip("Assign ready-made power-up prefabs directly here, OR use the weighted modifier system below.")]
    [SerializeField] private GameObject[] powerUpPrefabs;

    [Header("Weighted Modifiers (Alternative)")]
    [SerializeField] private PowerUpModifier[] powerUps;
    [SerializeField] private GameObject pickupPrefab;

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
            activePowerUps.RemoveAll(item => item == null);

            if (activePowerUps.Count < maxPowerUpsInArena)
            {
                SpawnPowerUp();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnPowerUp()
    {
        Vector2 randomCircle = Random.insideUnitCircle * arenaRadius;
        Vector3 origin = arenaCenter != null ? arenaCenter.position : Vector3.zero;
        Vector3 spawnPos = origin + new Vector3(randomCircle.x, spawnHeight, randomCircle.y);

        // Mode 1: Spawn from designated full prefabs
        if (powerUpPrefabs != null && powerUpPrefabs.Length > 0)
        {
            int index = Random.Range(0, powerUpPrefabs.Length);
            GameObject chosenPrefab = powerUpPrefabs[index];
            if (chosenPrefab != null)
            {
                GameObject spawned = Instantiate(chosenPrefab, spawnPos, Quaternion.identity);
                activePowerUps.Add(spawned);
                return;
            }
        }

        // Mode 2: Spawn base pickup prefab configured with weighted modifier
        if (pickupPrefab != null && powerUps != null && powerUps.Length > 0)
        {
            PowerUpModifier modifier = GetWeightPowerUp();
            if (modifier != null)
            {
                GameObject spawned = Instantiate(pickupPrefab, spawnPos, Quaternion.identity);
                if (spawned.TryGetComponent<PowerUpPickUp>(out var pickup))
                {
                    pickup.SetupPowerUp(modifier);
                }
                activePowerUps.Add(spawned);
            }
        }
    }

    private PowerUpModifier GetWeightPowerUp()
    {
        float totalWeight = 0f;
        foreach (var p in powerUps)
        {
            if (p != null) totalWeight += p.Weight;
        }

        float randomWeight = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var p in powerUps)
        {
            if (p == null) continue;
            currentWeight += p.Weight;
            if (randomWeight <= currentWeight)
            {
                return p;
            }
        }

        return powerUps.Length > 0 ? powerUps[0] : null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = arenaCenter != null ? arenaCenter.position : transform.position;
        Gizmos.DrawWireSphere(center, arenaRadius);
    }
}