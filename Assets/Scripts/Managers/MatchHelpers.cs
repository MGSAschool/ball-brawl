using UnityEngine;
using UnityEngine.InputSystem;

public static class MenuInput
{
    public static bool StartPressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) return true;
        }

        foreach (Gamepad gamepad in Gamepad.all)
        {
            if (gamepad.startButton.wasPressedThisFrame) return true;
        }
        return false;
    }

    public static bool GuidePressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.hKey.wasPressedThisFrame) return true;

        foreach (Gamepad gamepad in Gamepad.all)
        {
            if (gamepad.selectButton.wasPressedThisFrame) return true;
        }
        return false;
    }

    public static bool LeavePressed()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
    }

    public static bool TryGetModeKey(out GameMode mode)
    {
        mode = GameMode.Brawl;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return false;

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
        {
            mode = GameMode.Brawl;
            return true;
        }
        if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
        {
            mode = GameMode.StaphIt;
            return true;
        }
        if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
        {
            mode = GameMode.BombaClutch;
            return true;
        }
        return false;
    }

    public static int ModeStep()
    {
        foreach (Gamepad gamepad in Gamepad.all)
        {
            if (gamepad.rightShoulder.wasPressedThisFrame) return 1;
            if (gamepad.leftShoulder.wasPressedThisFrame) return -1;
        }
        return 0;
    }

    public static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

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

public static class GameTips
{
    public static readonly string[] All =
    {
        "Knock rivals off the edge. The last player to hit someone gets the point when they fall.",
        "Falling off on your own costs a point, so watch your footing near the edge.",
        "Jump to hop over a player charging at you.",
        "Save Invulnerability for when you're about to be hit near the edge.",
        "Super Knockback lasts 10 seconds. Level III hits three times as hard as level I.",
        "Using a Dash power-up gives you a window of dashing (longer at higher levels). Press Dash to dash again after the cooldown.",
        "Power-ups come in levels 1 to 3: the more icons stacked on a pickup, the stronger it is.",
        "Dash back toward the middle if you get knocked close to the edge.",
        "Red, shaking floor is about to drop. Get off it!",
        "Coin Rush coins land near the edges. Big points, big risk.",
        "You hold 2 power-ups. Use fires the left (oldest) one, and a third pickup pushes the oldest out.",
        "Plan your order: grab the power-up you want to use next first.",
        "Even the middle of the arena can fall, and the floor crumbles faster every 30 seconds.",
        "In the final minute of a Brawl, fallen floor never comes back.",
        "STAPH IT!: knock rivals out of the dome just before the light turns red.",
        "STAPH IT!: a shield blocks the red-light zap, so save one for when you can't make it to the dome.",
        "BOMBA CLUTCH: bombs set off nearby bombs. Line up a chain for a big score.",
        "BOMBA CLUTCH: with a shield up, blasts can't throw you, so set off as many bombs as you can.",
        "Lost track of your ball? An arrow at the screen edge points to it."
    };
}
