using UnityEngine;

public static class ControlHints
{
    public enum Action { Move, Jump, UsePowerUp, Dash }

    public static readonly string[] Schemes = { "Keyboard", "Arrows", "Gamepad" };

    public static string SchemeLabel(string scheme)
    {
        if (scheme == "Keyboard") return "WASD";
        if (scheme == "Arrows") return "Arrows";
        return "Gamepad";
    }

    public static Color SchemeColor(string scheme)
    {
        if (scheme == "Keyboard") return new Color(1f, 0.84f, 0.3f);
        if (scheme == "Arrows") return new Color(1f, 0.45f, 0.78f);
        return new Color(0.36f, 0.88f, 0.92f);
    }

    public static string JoinKey(string scheme)
    {
        if (scheme == "Keyboard") return "Space";
        if (scheme == "Arrows") return "Right Shift";
        return "A";
    }

    public static string Key(string scheme, Action action)
    {
        if (scheme == "Keyboard")
        {
            if (action == Action.Move) return "W A S D";
            if (action == Action.Jump) return "Space";
            if (action == Action.UsePowerUp) return "E";
            return "Left Shift";
        }
        if (scheme == "Arrows")
        {
            if (action == Action.Move) return "Arrow keys";
            if (action == Action.Jump) return "Right Ctrl";
            if (action == Action.UsePowerUp) return "/ (slash)";
            return "Right Shift";
        }
        if (action == Action.Move) return "Stick / D-pad";
        if (action == Action.Jump) return "A";
        if (action == Action.UsePowerUp) return "B";
        return "X";
    }

    public static string Cap(string key)
    {
        string tint = "#FFFFFF";
        if (key == "A") tint = "#5BD16A";
        if (key == "B") tint = "#FF6B5E";
        if (key == "X") tint = "#4FA8FF";
        if (key == "Y") tint = "#FFD84D";
        return "<mark=#FFFFFF2E padding=14,14,6,6><color=" + tint + ">" + key + "</color></mark>";
    }

    public static string CapFor(string scheme, Action action)
    {
        return Cap(Key(scheme, action));
    }
}
