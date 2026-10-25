using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawnManager : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    private int playerCount = 0;

    // PlayerInputManager automatically broadcasts this when a player joins
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            int spawnIndex = playerCount % spawnPoints.Length;
            playerInput.transform.position = spawnPoints[spawnIndex].position;
            playerInput.transform.rotation = spawnPoints[spawnIndex].rotation;
        }

        // Register with minigame
        BrawlPlayer brawlPlayer = playerInput.GetComponent<BrawlPlayer>();
        BombaClutchMinigame minigame = FindAnyObjectByType<BombaClutchMinigame>();
        if (brawlPlayer != null && minigame != null)
        {
            minigame.RegisterPlayer(brawlPlayer);
        }

        playerCount++;
    }
}