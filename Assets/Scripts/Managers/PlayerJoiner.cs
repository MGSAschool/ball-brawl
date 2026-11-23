using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MatchManager))]
public class PlayerJoiner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private Color[] playerColors =
    {
        new Color(0.95f, 0.30f, 0.30f),
        new Color(0.30f, 0.55f, 0.95f),
        new Color(0.35f, 0.85f, 0.40f),
        new Color(1.00f, 0.60f, 0.20f)
    };

    private MatchManager match;
    private readonly List<InputDevice> joinedGamepads = new List<InputDevice>();
    private readonly Dictionary<PlayerEntry, List<System.Type>> hintsShown = new Dictionary<PlayerEntry, List<System.Type>>();
    private bool wasdJoined;
    private bool arrowsJoined;

    public int MaxPlayers { get { return maxPlayers; } }

    private void Awake()
    {
        match = GetComponent<MatchManager>();
    }

    private void Update()
    {
        AnnounceLostControllers();

        if (match.IsInMenu) CheckJoinButtons();
        else CheckReconnectButtons();
    }

    private void CheckJoinButtons()
    {
        if (match.Players.Count >= maxPlayers || playerPrefab == null) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            bool arrowsKey = keyboard.rightShiftKey.wasPressedThisFrame || keyboard.numpad0Key.wasPressedThisFrame;
            if (!wasdJoined && keyboard.spaceKey.wasPressedThisFrame)
            {
                wasdJoined = Join("Keyboard", keyboard);
            }
            else if (!arrowsJoined && arrowsKey)
            {
                arrowsJoined = Join("Arrows", keyboard);
            }
        }

        foreach (Gamepad gamepad in Gamepad.all)
        {
            if (match.Players.Count >= maxPlayers) break;
            if (joinedGamepads.Contains(gamepad)) continue;
            if (!gamepad.buttonSouth.wasPressedThisFrame) continue;

            if (TryReconnect(gamepad)) continue;
            if (Join("Gamepad", gamepad)) joinedGamepads.Add(gamepad);
        }
    }

    private void CheckReconnectButtons()
    {
        foreach (Gamepad gamepad in Gamepad.all)
        {
            if (!joinedGamepads.Contains(gamepad) && gamepad.buttonSouth.wasPressedThisFrame)
            {
                TryReconnect(gamepad);
            }
        }
    }

    private void AnnounceLostControllers()
    {
        foreach (PlayerEntry entry in match.Players)
        {
            if (!entry.IsControllerLost)
            {
                entry.ControllerLostAnnounced = false;
                continue;
            }
            if (entry.ControllerLostAnnounced) continue;

            entry.ControllerLostAnnounced = true;
            match.Announce(entry.Name + "'s controller disconnected - reconnect it, or press A on a free controller");
        }
    }

    private bool TryReconnect(Gamepad gamepad)
    {
        PlayerEntry lost = null;
        foreach (PlayerEntry entry in match.Players)
        {
            if (entry.IsControllerLost)
            {
                lost = entry;
                break;
            }
        }
        if (lost == null || lost.Player == null) return false;

        PlayerInput input = lost.Player.GetComponent<PlayerInput>();
        if (input == null) return false;

        input.SwitchCurrentControlScheme("Gamepad", gamepad);
        if (lost.Device != null) joinedGamepads.Remove(lost.Device);
        lost.Device = gamepad;
        lost.ControllerLostAnnounced = false;
        joinedGamepads.Add(gamepad);

        match.Announce(lost.Name + " is back on a new controller");
        Sfx.Play(Sound.Join);
        return true;
    }

    private bool Join(string controlScheme, InputDevice device)
    {
        int index = match.Players.Count;
        PlayerInput input = PlayerInput.Instantiate(playerPrefab, controlScheme: controlScheme, pairWithDevice: device);
        BrawlPlayer player = input.GetComponent<BrawlPlayer>();
        if (player == null)
        {
            Debug.LogError(playerPrefab.name + " needs a BrawlPlayer component to join the match.");
            Destroy(input.gameObject);
            return false;
        }
        if (input.GetComponent<BallPhysics>() == null) input.gameObject.AddComponent<BallPhysics>();

        PlayerEntry entry = new PlayerEntry();
        entry.Player = player;
        entry.PowerUps = input.GetComponent<BrawlPowerUps>();
        entry.Name = "P" + (index + 1);
        entry.Color = ColorForSlot(index);
        entry.ControlScheme = controlScheme;
        entry.Device = device;
        input.gameObject.name = "Player " + (index + 1) + " (" + controlScheme + ")";

        PaintBall(input.gameObject, index, entry.Color);
        TurnOffPlayerCameras(input.gameObject);
        match.AddPlayer(entry);
        ListenForFirstPickups(entry);

        string schemeName = ControlHints.SchemeLabel(controlScheme);
        string powerUpKey = ControlHints.Key(controlScheme, ControlHints.Action.UsePowerUp);
        match.Announce(entry.Name + " joined on " + schemeName + " - power-up: " + powerUpKey);
        Sfx.Play(Sound.Join);
        Vfx.Poof(player.transform.position, entry.Color);
        return true;
    }

    private void ListenForFirstPickups(PlayerEntry entry)
    {
        if (entry.PowerUps == null) return;
        hintsShown[entry] = new List<System.Type>();
        entry.PowerUps.Collected += powerUp => ShowPowerUpHint(entry, powerUp);
    }

    private void ShowPowerUpHint(PlayerEntry entry, PowerUpModifier powerUp)
    {
        List<System.Type> shown = hintsShown[entry];
        System.Type type = powerUp.GetType();
        if (shown.Contains(type)) return;
        shown.Add(type);

        string name = PowerUpLook.DisplayName(powerUp);
        string key = ControlHints.Key(entry.ControlScheme, ControlHints.Action.UsePowerUp);
        match.Announce(entry.Name + ": " + name + " stored - " + key + " uses your oldest power-up");
    }

    private Color ColorForSlot(int index)
    {
        if (playerColors.Length == 0) return Color.white;
        return playerColors[index % playerColors.Length];
    }

    private void PaintBall(GameObject playerObject, int index, Color color)
    {
        BallSkin skin = playerObject.GetComponent<BallSkin>();
        if (skin != null && skin.Apply(index, color)) return;

        foreach (Renderer rend in playerObject.GetComponentsInChildren<Renderer>())
        {
            rend.material.color = color;
        }
    }

    private void TurnOffPlayerCameras(GameObject playerObject)
    {
        Camera main = Camera.main;
        if (main == null || main.transform.IsChildOf(playerObject.transform)) return;

        foreach (Camera cam in playerObject.GetComponentsInChildren<Camera>())
        {
            cam.enabled = false;
        }
        foreach (AudioListener listener in playerObject.GetComponentsInChildren<AudioListener>())
        {
            listener.enabled = false;
        }
    }
}
