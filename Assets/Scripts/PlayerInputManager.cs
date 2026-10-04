using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerInputManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
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

            wasdJoined = true;
            playerJoinedIndex ++;
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

            arrowsJoined = true;
            playerJoinedIndex ++;
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

                    gamePadJoined = true;
                    playerJoinedIndex ++;
                    break;
                }
            }
        }
    }
}
    