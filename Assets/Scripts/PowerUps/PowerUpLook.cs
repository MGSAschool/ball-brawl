using UnityEngine;

public static class PowerUpLook
{
    public static Color ColorFor(PowerUpModifier powerUp)
    {
        if (powerUp is DashPowerUp) return new Color(0.2f, 0.85f, 1f);
        if (powerUp is InvulnerablePowerUp) return new Color(1f, 0.85f, 0.25f);
        if (powerUp is SuperKnockbackPowerUp) return new Color(1f, 0.32f, 0.2f);
        if (powerUp is SpeedBoostPowerUp) return new Color(0.4f, 1f, 0.45f);
        return Color.white;
    }

    public static GameObject ModelFor(PowerUpModifier powerUp)
    {
        GameArt art = GameArt.Instance;
        if (art == null) return null;

        if (powerUp is SpeedBoostPowerUp) return art.speedBoostModel;
        if (powerUp is InvulnerablePowerUp) return art.invulnerabilityModel;
        if (powerUp is SuperKnockbackPowerUp) return art.superKnockbackModel;
        return null;
    }

    public static string DisplayName(PowerUpModifier powerUp)
    {
        if (powerUp == null) return "";
        if (powerUp is DashPowerUp) return "Dash " + LevelNumeral(powerUp);
        if (powerUp is InvulnerablePowerUp) return "Invulnerability " + LevelNumeral(powerUp);
        if (powerUp is SuperKnockbackPowerUp) return "Super Knockback " + LevelNumeral(powerUp);
        if (powerUp is SpeedBoostPowerUp) return "Speed Boost " + LevelNumeral(powerUp);
        return powerUp.name;
    }

    public static string LevelNumeral(PowerUpModifier powerUp)
    {
        int level = 1;
        if (powerUp != null) level = powerUp.Level;

        if (level == 3) return "III";
        if (level == 2) return "II";
        return "I";
    }
}
