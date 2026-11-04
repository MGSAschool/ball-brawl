// Player-facing key names per control scheme, shared by the HUD and in-game hints.
// Keep in sync with PlayerInputSystem.inputactions.
public static class ControlHints
{
    public enum Action { Move, Jump, UsePowerUp, Dash }

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
                    case Action.UsePowerUp: return "E";
                    default: return "Left Shift";
                }
            case "Arrows":
                switch (action)
                {
                    case Action.Move: return "Arrow keys";
                    case Action.Jump: return "Right Ctrl";
                    case Action.UsePowerUp: return "/ (slash)";
                    default: return "Right Shift";
                }
            default:
                switch (action)
                {
                    case Action.Move: return "Left stick";
                    case Action.Jump: return "A";
                    case Action.UsePowerUp: return "B";
                    default: return "X";
                }
        }
    }

    public static string PowerUpName(PowerUpModifier powerUp)
    {
        switch (powerUp)
        {
            case DashPowerUp: return "Dash " + LevelNumeral(powerUp);
            case InvulnerablePowerUp: return "Invulnerability " + LevelNumeral(powerUp);
            case SuperKnockbackPowerUp: return "Super Knockback " + LevelNumeral(powerUp);
            case SpeedBoostPowerUp: return "Speed Boost " + LevelNumeral(powerUp);
            default: return powerUp != null ? powerUp.name : "";
        }
    }

    public static string LevelNumeral(PowerUpModifier powerUp)
    {
        switch (powerUp != null ? powerUp.Level : 1)
        {
            case 3: return "III";
            case 2: return "II";
            default: return "I";
        }
    }

    public static readonly string[] Tips =
    {
        "Knock rivals off the edge. The last player to hit someone gets the point when they fall.",
        "Falling off on your own costs a point, so watch your footing near the edge.",
        "Jump to hop over a player charging at you.",
        "Save Invulnerability for when you're about to be hit near the edge.",
        "Super Knockback lasts 10 seconds. Level III hits three times as hard as level I.",
        "Using a Dash power-up gives you a window of dashing (longer at higher levels). Press Dash to dash again after the cooldown.",
        "Power-ups come in levels I to III. Bigger, brighter pickups with more sparks around them are stronger.",
        "Dash back toward the middle if you get knocked close to the edge.",
        "Red, shaking floor is about to drop. Get off it!",
        "Coin Rush coins land near the edges. Big points, big risk.",
        "You hold 2 power-ups. Use fires the left (oldest) one, and a third pickup pushes the oldest out.",
        "Plan your order: grab the power-up you want to use next first.",
        "Even the middle of the arena can fall, and the floor crumbles faster every 30 seconds.",
        "At 1:00 a new platform rises. Jump across before the old arena is gone.",
        "Blue, flashing floor on the new platform freezes into slippery ice. Steer gently!",
        "STAPH IT!: knock rivals out of the dome just before the light turns red.",
        "STAPH IT!: a shield blocks the red-light zap, so save one for when you can't make it to the dome.",
        "BOMBA CLUTCH: bombs set off nearby bombs. Line up a chain for a big score.",
        "BOMBA CLUTCH: with a shield up, blasts can't throw you, so set off as many bombs as you can.",
        "Lost track of your ball? An arrow at the screen edge points to it."
    };
}
