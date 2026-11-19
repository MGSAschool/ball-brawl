using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerEntry
{
    public BrawlPlayer Player;
    public BrawlPowerUps PowerUps;
    public string Name;
    public Color Color;
    public string ControlScheme;
    public InputDevice Device;

    public int Score;
    public int Knockouts;
    public int Falls;
    public int Coins;

    public bool IsRespawning;
    public float RespawnAt;
    public bool ControllerLostAnnounced;

    public bool IsControllerLost
    {
        get { return ControlScheme == "Gamepad" && (Device == null || !Device.added); }
    }

    public void ResetStats()
    {
        Score = 0;
        Knockouts = 0;
        Falls = 0;
        Coins = 0;
        IsRespawning = false;
    }
}
