using System;
using System.Collections;
using UnityEngine;
public class SpawnManager : MonoBehaviour
{
    [SerializeField] private PowerUpModifier[] powerUps;
    [SerializeField] private PowerUpPickUp pickupPrefab;
    [SerializeField] private float arenaRadius = 30f;
    private float spawnInterval = 10f;

    void Start()
    {
        StartCoroutine(SpawnPowerup());
    }
    private PowerUpModifier GetRandomPowerUp()
    {
        float totalWeight = 0f;

        foreach (var powerUp in powerUps)
        {
            totalWeight += powerUp.Weight;
        }

        float randomValue = UnityEngine.Random.Range(0f, totalWeight);

        foreach (var powerUp in powerUps)
        {
            randomValue -= powerUp.Weight;

            if (randomValue <= 0f)
            {
                return powerUp;
            }
        }

        return powerUps[^1];
    }

    private void Spawn()
    {
        PowerUpModifier powerup = GetRandomPowerUp();
        Vector2 randomPos = UnityEngine.Random.insideUnitCircle * arenaRadius; 
        Vector3 spawnInsideCircle = new(randomPos.x, 2, randomPos.y);
        PowerUpPickUp pickup = Instantiate(pickupPrefab, spawnInsideCircle, Quaternion.identity);
        pickup.SetupPowerUp(powerup);
    }

    IEnumerator SpawnPowerup()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            Spawn();
        }
    }
}
