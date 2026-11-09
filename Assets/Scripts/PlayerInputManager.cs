using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerInputManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject HUDPrefab;
    private List<PlayerInput> players = new List<PlayerInput>();
    bool wasdJoined = false;
    bool arrowsJoined = false;
    // Every gamepad can join as its own player, alongside WASD and Arrows
    private readonly HashSet<Gamepad> joinedGamepads = new HashSet<Gamepad>();

    int playerJoinedIndex = 0;
    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current != null)
        {
            JoinWASD();
            JoinArrows();
        }
        
        JoinGamepad();
        CheckWinner();
    }
    private void JoinWASD()
    {
        if(wasdJoined) return;
        if(Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            var player = PlayerInput.Instantiate(playerPrefab, 
            controlScheme: "Keyboard", 
            pairWithDevice: Keyboard.current);

            if(spawnPoints.Length > playerJoinedIndex)
            {
                player.transform.position = spawnPoints[playerJoinedIndex].position;
            }

            players.Add(player);
            wasdJoined = true;
            playerJoinedIndex ++;
            CreateHUD(player);
            player.name = $"Player {playerJoinedIndex}";
        }
    }

    private void JoinArrows()
    {
        if(arrowsJoined) return;
        if(Keyboard.current.rightShiftKey.wasPressedThisFrame || Keyboard.current.numpad0Key.wasPressedThisFrame)
        {
            var player = PlayerInput.Instantiate(playerPrefab, 
            controlScheme: "Arrows", 
            pairWithDevice: Keyboard.current);

            if(spawnPoints.Length > playerJoinedIndex)
            {
                player.transform.position = spawnPoints[playerJoinedIndex].position;
            }

            players.Add(player);
            arrowsJoined = true;
            playerJoinedIndex ++;
            CreateHUD(player);
            player.name = $"Player {playerJoinedIndex}";
        }
    }

    private void JoinGamepad()
    {
        foreach(var gamePad in Gamepad.all)
        {
            if (joinedGamepads.Contains(gamePad) || !gamePad.buttonSouth.wasPressedThisFrame) continue;

            var player = PlayerInput.Instantiate(playerPrefab,
            controlScheme: "Gamepad",
            pairWithDevice: gamePad);

            if(spawnPoints.Length > playerJoinedIndex)
            {
                player.transform.position = spawnPoints[playerJoinedIndex].position;
            }

            players.Add(player);
            joinedGamepads.Add(gamePad);
            playerJoinedIndex ++;
            CreateHUD(player);
            player.name = $"Player {playerJoinedIndex}";
        }
    }

    private void CheckWinner()
    {
        PlayerController player = null;
        int aliveCount = 0;

        foreach(PlayerInput p in players)
        {
            PlayerController playerController = p.GetComponent<PlayerController>();

            if (playerController.IsAlive)
            {
                aliveCount ++;
                player = playerController;
            }
        }
        if(aliveCount == 1 && players.Count > 1)
        {
            Debug.Log(player.name + " Wins!");
        }
    }
    private void CreateHUD(PlayerInput player)
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        GameObject hud = Instantiate(HUDPrefab, canvas.transform, false);
        
        PowerupInventory inventory = player.GetComponent<PowerupInventory>();

        PowerupInventoryUI inventoryUI = hud.GetComponent<PowerupInventoryUI>();

        inventoryUI.Initialize(playerJoinedIndex, inventory);
    }
}
    