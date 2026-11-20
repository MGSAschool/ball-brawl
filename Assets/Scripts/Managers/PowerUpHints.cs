using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MatchManager))]
public class PowerUpHints : MonoBehaviour
{
    private MatchManager match;
    private readonly Dictionary<PlayerEntry, List<System.Type>> alreadyShown = new Dictionary<PlayerEntry, List<System.Type>>();

    private void Awake()
    {
        match = GetComponent<MatchManager>();
    }

    private void OnEnable()
    {
        match.PlayerJoined += OnPlayerJoined;
    }

    private void OnDisable()
    {
        match.PlayerJoined -= OnPlayerJoined;
    }

    private void OnPlayerJoined(PlayerEntry entry)
    {
        if (entry.PowerUps == null) return;
        alreadyShown[entry] = new List<System.Type>();
        entry.PowerUps.Collected += powerUp => ShowHint(entry, powerUp);
    }

    private void ShowHint(PlayerEntry entry, PowerUpModifier powerUp)
    {
        List<System.Type> shown = alreadyShown[entry];
        System.Type type = powerUp.GetType();
        if (shown.Contains(type)) return;
        shown.Add(type);

        string name = PowerUpLook.DisplayName(powerUp);
        string key = ControlHints.Key(entry.ControlScheme, ControlHints.Action.UsePowerUp);
        match.Announce(entry.Name + ": " + name + " stored - " + key + " uses your oldest power-up");
    }
}
