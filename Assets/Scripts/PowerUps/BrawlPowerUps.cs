using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BrawlPlayer), typeof(PowerupInventory))]
public class BrawlPowerUps : MonoBehaviour
{
    public event Action<PowerUpModifier> Collected;
    public event Action Cleared;

    private BrawlPlayer player;
    private PlayerInputReader input;
    private readonly List<PowerUpModifier> lastKnownItems = new List<PowerUpModifier>();
    private bool givingBack;

    public PowerupInventory Inventory { get; private set; }
    public IReadOnlyList<PowerUpModifier> Items { get { return Inventory.powerupInventory; } }

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
        lastKnownItems.Clear();
        if (Cleared != null) Cleared();
    }

    private void OnPickup()
    {
        RememberItems();
        if (givingBack || Items.Count == 0) return;

        PowerUpModifier item = Items[Items.Count - 1];
        Sfx.Play(Sound.Pickup, transform.position);
        Vfx.Sparkle(transform.position, PowerUpLook.ColorFor(item));
        if (Collected != null) Collected(item);
    }

    private void OnUse()
    {
        PowerUpModifier used = null;
        if (lastKnownItems.Count > 0) used = lastKnownItems[0];
        RememberItems();
        if (used == null) return;

        if (!player.ControlsEnabled)
        {
            givingBack = true;
            Inventory.StorePowerUp(used);
            givingBack = false;
            return;
        }

        Vector3 position = transform.position;
        Vfx.Poof(position, PowerUpLook.ColorFor(used));

        if (used is DashPowerUp)
        {
            player.GrantDash(used.Duration);
            DashWithEffects();
        }
        else if (used is InvulnerablePowerUp)
        {
            player.GrantInvulnerability(used.Duration);
            Sfx.Play(Sound.UseShield, position);
        }
        else if (used is SuperKnockbackPowerUp)
        {
            player.GrantSuperKnockback(used.ModifyMultiplier(1f), used.Duration);
            Sfx.Play(Sound.UsePunch, position);
        }
        else if (used is SpeedBoostPowerUp)
        {
            player.GrantSpeedBoost(used.ModifyMultiplier(1f), used.Duration);
            Sfx.Play(Sound.UseSpeed, position);
        }
    }

    private void OnDashPressed()
    {
        if (player.ControlsEnabled && player.HasDash) DashWithEffects();
    }

    private void DashWithEffects()
    {
        if (!player.Dash()) return;
        Sfx.Play(Sound.Dash, transform.position);
        Vfx.Dust(transform.position + Vector3.down * 0.4f, 0.6f);
    }

    private void RememberItems()
    {
        lastKnownItems.Clear();
        lastKnownItems.AddRange(Items);
    }
}
