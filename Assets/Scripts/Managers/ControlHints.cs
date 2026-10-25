// Player-facing key names per control scheme, shared by the HUD and in-game hints.
// Keep in sync with PlayerInputSystem.inputactions.
public static class ControlHints
{
    public enum Action { Move, Jump, Dash, Invulnerability, SuperKnockback }

    public static readonly string[] Schemes = { "Keyboard", "Arrows", "Gamepad" };

    public static string SchemeLabel(string scheme)
    {
        switch (scheme)
        {
            case "Keyboard": return "WASD";
            case "Arrows": return "Arrows";
            default: return "Gamepad";
        }
    }

    public static string Key(string scheme, Action action)
    {
        switch (scheme)
        {
            case "Keyboard":
                switch (action)
                {
                    case Action.Move: return "W A S D";
                    case Action.Jump: return "Space";
                    case Action.Dash: return "Left Shift";
                    case Action.Invulnerability: return "Q";
                    default: return "E";
                }
            case "Arrows":
                switch (action)
                {
                    case Action.Move: return "Arrow keys";
                    case Action.Jump: return "Right Ctrl";
                    case Action.Dash: return "Right Shift";
                    case Action.Invulnerability: return ". (period)";
                    default: return "/ (slash)";
                }
            default:
                switch (action)
                {
                    case Action.Move: return "Left stick";
                    case Action.Jump: return "A";
                    case Action.Dash: return "X";
                    case Action.Invulnerability: return "Y";
                    default: return "B";
                }
        }
    }

    // The key that fires a stored power-up, or null for ones that apply on pickup
    public static string KeyFor(string scheme, PowerUpModifier powerUp)
    {
        switch (powerUp)
        {
            case DashPowerUp: return Key(scheme, Action.Dash);
            case InvulnerablePowerUp: return Key(scheme, Action.Invulnerability);
            case SuperKnockbackPowerUp: return Key(scheme, Action.SuperKnockback);
            default: return null;
        }
    }

    public static string PowerUpName(PowerUpModifier powerUp)
    {
        switch (powerUp)
        {
            case DashPowerUp: return "Dash";
            case InvulnerablePowerUp: return "Invulnerability";
            case SuperKnockbackPowerUp: return "Super Knockback";
            case SpeedBoostPowerUp: return "Speed Boost";
            default: return powerUp != null ? powerUp.name : "";
        }
    }

    public static readonly string[] Tips =
    {
        "Knock rivals off the edge. The last player to hit someone gets the point when they fall.",
        "Falling off on your own costs a point, so watch your footing near the edge.",
        "Jump to hop over a player charging at you.",
        "Save Invulnerability for when you're about to be hit near the edge.",
        "Super Knockback lasts 10 seconds. Use it, then go hunting.",
        "A Dash power-up gives 10 seconds of dashing. Tap it again to dash after the cooldown.",
        "Dash back toward the middle if you get knocked close to the edge.",
        "Red, shaking floor is about to drop. Get off it!",
        "Coin Rush coins land near the edges. Big points, big risk.",
        "You can hold 2 power-ups. A full inventory leaves new pickups on the floor.",
        "The center of the arena never falls. It's the safest place to regroup.",
        "Speed Boosts work the moment you grab them."
    };
}
