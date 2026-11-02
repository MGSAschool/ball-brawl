using System;
using System.Collections.Generic;
using UnityEngine;

// Connects the shared PowerupInventory (one Use Power Up button fires the oldest stored power-up;
// a third pickup pushes out the oldest) to BrawlPlayer's effects in the Ball Brawl scene, without
// changing PowerupInventory itself. Also lets the Dash key dash while a Dash power-up is running,
// and lets the match clear the inventory between rounds.
[RequireComponent(typeof(BrawlPlayer), typeof(PowerupInventory))]
public class BrawlPowerUps : MonoBehaviour
{
    // Raised with the power-up that was just stored (used for in-game hints)
    public event Action<PowerUpModifier> Collected;
    // Raised after Clear, so HUDs can redraw
    public event Action Cleared;

    public PowerupInventory Inventory { get; private set; }
    public IReadOnlyList<PowerUpModifier> Items => Inventory.powerupInventory;

    private BrawlPlayer player;
    private PlayerInputReader input;
    // Copy of the stored list, so we know which power-up PowerupInventory just used
    private readonly List<PowerUpModifier> known = new List<PowerUpModifier>();
    private bool refunding;

    private void Awake()
    {
        player = GetComponent<BrawlPlayer>();
        Inventory = GetComponent<PowerupInventory>();
        input = GetComponent<PlayerInputReader>();
    }

    private void OnEnable()
    {
        Inventory.OnPowerupPickup += OnPickup;
        Inventory.OnPowerupUse += OnUse;
        if (input != null) input.DashPressed += OnDashPressed;
    }

    private void OnDisable()
    {
        Inventory.OnPowerupPickup -= OnPickup;
        Inventory.OnPowerupUse -= OnUse;
        if (input != null) input.DashPressed -= OnDashPressed;
    }

    public void Clear()
    {
        Inventory.powerupInventory.Clear();
        known.Clear();
        Cleared?.Invoke();
    }

    private void OnPickup()
    {
        Remember();
        if (!refunding && Items.Count > 0) Collected?.Invoke(Items[Items.Count - 1]);
    }

    private void OnUse()
    {
        PowerUpModifier used = known.Count > 0 ? known[0] : null;
        Remember();
        if (used == null) return;

        // Pressed while it can't act (respawning, countdown): give it back instead of wasting it
        if (!player.ControlsEnabled)
        {
            refunding = true;
            Inventory.StorePowerUp(used);
            refunding = false;
            return;
        }

        switch (used)
        {
            case DashPowerUp:
                player.GrantDash(used.Duration);
                player.Dash();
                break;
            case InvulnerablePowerUp:
                player.GrantInvulnerability(used.Duration);
                break;
            case SuperKnockbackPowerUp:
                player.GrantSuperKnockback(used.ModifyMultiplier(1f), used.Duration);
                break;
            case SpeedBoostPowerUp:
                player.GrantSpeedBoost(used.ModifyMultiplier(1f), used.Duration);
                break;
        }
    }

    // While a Dash power-up is running, the Dash key dashes freely (cooldown permitting)
    private void OnDashPressed()
    {
        if (player.ControlsEnabled && player.HasDash) player.Dash();
    }

    private void Remember()
    {
        known.Clear();
        known.AddRange(Items);
    }
}
