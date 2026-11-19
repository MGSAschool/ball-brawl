using TMPro;
using UnityEngine;

public class HUDBottomText : HUDPanel
{
    private TextMeshProUGUI promptText;
    private TextMeshProUGUI controlsText;

    protected override void BuildPanel()
    {
        Vector2 bottomCenter = new Vector2(0.5f, 0f);
        promptText = UIKit.NewAutoText(Root, "Prompt", 18f, 34f, TextAlignmentOptions.Bottom, 0f);
        UIKit.Place(promptText.rectTransform, new Vector2(0.25f, 0f), new Vector2(0.75f, 0f), bottomCenter, new Vector2(0f, 64f), new Vector2(0f, 90f));

        controlsText = UIKit.NewAutoText(Root, "Controls", 12f, 22f, TextAlignmentOptions.Bottom, 0f);
        controlsText.color = new Color(0.9f, 0.88f, 1f);
        UIKit.Place(controlsText.rectTransform, new Vector2(0.19f, 0f), new Vector2(0.81f, 0f), bottomCenter, new Vector2(0f, 14f), new Vector2(0f, 48f));
    }

    private void Update()
    {
        if (hud == null || Match == null) return;

        if (hud.GuideOpen)
        {
            promptText.text = "";
            controlsText.text = "";
            return;
        }

        promptText.text = NewsLine() + Prompt();
        if (hud.InMenu) controlsText.text = MenuControlsLine();
        else controlsText.text = MatchControlsLine();
    }

    private string NewsLine()
    {
        bool over = Match.State == MatchState.MatchOver;
        if (!hud.InMenu || over || !hud.ShowAnnouncement) return "";
        return UIKit.Colored(Match.Announcement, HUDStyle.GoldHex) + "\n";
    }

    private string Prompt()
    {
        if (!hud.InMenu) return "";

        bool lobby = Match.State == MatchState.Lobby;
        string text = "";
        if (!lobby && !LobbyFull)
        {
            text += "<size=80%>Join:  WASD " + ControlHints.Cap("Space") + "   Arrows " + ControlHints.Cap("Right Shift") +
                    "   Gamepad " + ControlHints.Cap("A") + "</size>\n";
        }
        if (!lobby)
        {
            text += "Mode: " + UIKit.Colored(GameModeInfo.Name(Match.Mode), HUDStyle.GoldHex) + "  <size=80%>(1 / 2 / 3 or LB / RB to change)</size>\n";
        }

        if (Match.CanStart)
        {
            string action = "rematch";
            if (lobby) action = "start";
            return text + "Press " + ControlHints.Cap("Enter") + " / " + ControlHints.Cap("Start") + " to " + action;
        }
        if (Match.Players.Count < Match.MinPlayersToStart)
        {
            return text + "Need " + Match.MinPlayersToStart + "+ players to start";
        }
        return text.TrimEnd('\n');
    }

    private string MenuControlsLine()
    {
        string modeKeys = ControlHints.Cap("1") + " " + ControlHints.Cap("2") + " " + ControlHints.Cap("3") + " / " +
                          ControlHints.Cap("LB") + " " + ControlHints.Cap("RB");
        string guideKeys = ControlHints.Cap("H") + " / " + ControlHints.Cap("Select");
        return "Mode " + modeKeys + "      How to Play " + guideKeys + "      Leave " + ControlHints.Cap("Esc");
    }

    private string MatchControlsLine()
    {
        string line = "";
        foreach (string scheme in ControlHints.Schemes)
        {
            if (!SchemeJoined(scheme)) continue;
            if (line.Length > 0) line += "        ";

            line += UIKit.Colored(ControlHints.SchemeLabel(scheme).ToUpper(), ControlHints.SchemeColor(scheme)) + "  ";
            line += "Jump " + ControlHints.CapFor(scheme, ControlHints.Action.Jump);
            line += "  Power-up " + ControlHints.CapFor(scheme, ControlHints.Action.UsePowerUp);
            line += "  Dash " + ControlHints.CapFor(scheme, ControlHints.Action.Dash);
        }
        return line;
    }
}
