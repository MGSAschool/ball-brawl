using UnityEngine;
using UnityEngine.InputSystem;

// Two-slot power-up storage. Each stored power-up is fired by its own key:
// Dash (LShift), Invulnerability (Q), Super Knockback (E). Each lasts its asset's Duration.
// Speed boosts have no key, so they apply the moment they're picked up.
[RequireComponent(typeof(BrawlPlayer))]
public class PlayerInventory : MonoBehaviour
{
    public const int SlotCount = 2;

    [Header("Current Stored Power-Ups")]
    [SerializeField] private PowerUpModifier[] slots = new PowerUpModifier[SlotCount];

    private BrawlPlayer player;

    private void Awake()
    {
        player = GetComponent<BrawlPlayer>();
        if (slots == null || slots.Length != SlotCount)
        {
            slots = new PowerUpModifier[SlotCount];
        }
    }

    public PowerUpModifier GetSlot(int index) => slots[index];

    public bool TryCollect(PowerUpModifier powerUp)
    {
        if (powerUp == null) return false;

        if (powerUp is SpeedBoostPowerUp)
        {
            player.GrantSpeedBoost(powerUp.ModifyMultiplier(1f), powerUp.Duration);
            return true;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = powerUp;
                return true;
            }
        }

        // Both slots full: leave the pickup in the arena
        return false;
    }

    public void Clear()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = null;
        }
    }

    // --- PlayerInput (Send Messages) callbacks ---
    public void OnDash(InputValue value)
    {
        if (!value.isPressed) return;

        // While a Dash power-up is running, the key dashes freely (cooldown permitting)
        if (player.HasDash) player.Dash();
        else TryUse<DashPowerUp>();
    }

    public void OnInvulnerability(InputValue value)
    {
        if (value.isPressed) TryUse<InvulnerablePowerUp>();
    }

    public void OnSuperKnockback(InputValue value)
    {
        if (value.isPressed) TryUse<SuperKnockbackPowerUp>();
    }

    private void TryUse<T>() where T : PowerUpModifier
    {
        if (!player.ControlsEnabled) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] is T powerUp && Activate(powerUp))
            {
                slots[i] = null;
                return;
            }
        }
    }

    private bool Activate(PowerUpModifier powerUp)
    {
        switch (powerUp)
        {
            case DashPowerUp:
                player.GrantDash(powerUp.Duration);
                player.Dash();
                return true;
            case InvulnerablePowerUp:
                player.GrantInvulnerability(powerUp.Duration);
                return true;
            case SuperKnockbackPowerUp:
                player.GrantSuperKnockback(powerUp.ModifyMultiplier(1f), powerUp.Duration);
                return true;
            default:
                return false;
        }
    }
}
