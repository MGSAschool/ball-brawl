using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerInputManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    private List<PlayerInput> players = new List<PlayerInput>();
    bool wasdJoined = false;
    bool arrowsJoined = false;
    bool gamePadJoined = false;

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
            player.name = $"Player {playerJoinedIndex}";
        }
    }

    private void JoinArrows()
    {
        if(arrowsJoined) return;
        if(Keyboard.current.numpad0Key.wasPressedThisFrame)
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
            player.name = $"Player {playerJoinedIndex}";
        }
    }

    private void JoinGamepad()
    {
        if (!gamePadJoined)
        {
            foreach(var gamePad in Gamepad.all)
            {
                if (gamePad.buttonSouth.wasPressedThisFrame)
                {
                    var player = PlayerInput.Instantiate(playerPrefab, 
                    controlScheme: "Gamepad", 
                    pairWithDevice: gamePad);

                    if(spawnPoints.Length > 2)
                    {
                        player.transform.position = spawnPoints[playerJoinedIndex].position;
                    }

                    players.Add(player);
                    gamePadJoined = true;
                    playerJoinedIndex ++;
                    player.name = $"Player {playerJoinedIndex}";
                    break;
                }
            }
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
}
    