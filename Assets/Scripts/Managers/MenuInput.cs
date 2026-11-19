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
        UnityEngine.Application.Quit();
#endif
    }
}
