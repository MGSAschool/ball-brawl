using UnityEngine;

public static class GameModeInfo
{
    public const int Count = 3;

    public static string Name(GameMode mode)
    {
        if (mode == GameMode.StaphIt) return "STAPH IT";
        if (mode == GameMode.BombaClutch) return "BOMBA CLUTCH";
        return "BRAWL";
    }

    public static string CardTitle(GameMode mode)
    {
        if (mode == GameMode.StaphIt) return "STAPH IT!";
        if (mode == GameMode.BombaClutch) return "BOMBA CLUTCH";
        return "BRAWL";
    }

    public static string Blurb(GameMode mode)
    {
        if (mode == GameMode.StaphIt) return "Red light? Be in the dome!";
        if (mode == GameMode.BombaClutch) return "Set off bombs, survive the blasts";
        return "Knock everyone off the arena";
    }

    public static Color CardColor(GameMode mode)
    {
        if (mode == GameMode.StaphIt) return new Color(1f, 0.3f, 0.35f);
        if (mode == GameMode.BombaClutch) return new Color(0.55f, 0.45f, 1f);
        return new Color(1f, 0.55f, 0.2f);
    }

    public static GameMode Step(GameMode mode, int step)
    {
        int index = ((int)mode + step + Count) % Count;
        return (GameMode)index;
    }
}
