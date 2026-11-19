using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDTopBar : HUDPanel
{
    private static readonly Rect TitleNormal = Rect.MinMaxRect(0.1f, 0.5f, 0.9f, 0.74f);
    private static readonly Rect TitleResults = Rect.MinMaxRect(0.1f, 0.74f, 0.9f, 0.88f);

    private GameObject timerPanel;
    private TextMeshProUGUI timerText;
    private GameObject eventPanel;
    private Image eventBackground;
    private Image eventIcon;
    private TextMeshProUGUI eventText;
    private TextMeshProUGUI feedText;
    private TextMeshProUGUI titleText;

    protected override void BuildPanel()
    {
        Vector2 topCenter = new Vector2(0.5f, 1f);
        timerPanel = UIKit.NewPanel("Timer Panel", Root, topCenter, topCenter, topCenter, new Vector2(0f, -24f), new Vector2(320f, 96f));
        UIKit.AddOutline(timerPanel, HUDStyle.Trim, 4f);
        timerText = UIKit.NewAutoText(timerPanel.transform, "Timer", 28f, 68f, TextAlignmentOptions.Center, 8f);
        timerText.color = HUDStyle.Gold;

        eventPanel = UIKit.NewPanel("Event Panel", Root, new Vector2(0.385f, 1f), new Vector2(0.615f, 1f), topCenter, new Vector2(0f, -132f), new Vector2(0f, 60f));
        UIKit.AddOutline(eventPanel, HUDStyle.Trim, 4f);
        eventBackground = eventPanel.GetComponent<Image>();
        Sprite coin = null;
        if (Art != null) coin = Art.coinIcon;
        eventIcon = UIKit.NewIcon(eventPanel.transform, coin, Rect.MinMaxRect(0.01f, 0.08f, 0.1f, 0.92f));
        eventText = UIKit.NewAutoText(eventPanel.transform, "Event", 16f, 34f, TextAlignmentOptions.Center, 8f);
        eventText.rectTransform.offsetMin = new Vector2(48f, 8f);

        feedText = UIKit.NewAutoText(Root, "Feed", 16f, 34f, TextAlignmentOptions.Center, 0f);
        UIKit.Place(feedText.rectTransform, new Vector2(0.385f, 1f), new Vector2(0.615f, 1f), topCenter, new Vector2(0f, -200f), new Vector2(0f, 56f));

        titleText = UIKit.NewAutoText(Root, "Title", 40f, 150f, TextAlignmentOptions.Center, 0f);
        UIKit.Stretch(titleText.rectTransform, TitleNormal);
        titleText.color = HUDStyle.Gold;
        titleText.outlineWidth = 0.28f;
    }

    private void Update()
    {
        if (hud == null || Match == null) return;
        bool over = Match.State == MatchState.MatchOver;

        timerPanel.SetActive(Match.State != MatchState.Lobby);
        timerText.text = TimerLabel();

        string eventLabel = EventLabel();
        eventPanel.SetActive(eventLabel.Length > 0);
        eventText.text = eventLabel;
        eventIcon.enabled = eventIcon.sprite != null && CoinRushRunning();
        UpdateEventColor();

        bool showFeed = hud.ShowAnnouncement && !over && !hud.InMenu;
        if (showFeed) feedText.text = Match.Announcement;
        else feedText.text = "";

        if (hud.GuideOpen) titleText.text = "";
        else titleText.text = Title();
        if (over) UIKit.Stretch(titleText.rectTransform, TitleResults);
        else UIKit.Stretch(titleText.rectTransform, TitleNormal);
    }

    private bool CoinRushRunning()
    {
        return hud.CoinRush != null && hud.CoinRush.IsActive;
    }

    private string TimerLabel()
    {
        if (Match.State == MatchState.Lobby) return "LOBBY";
        if (Match.State == MatchState.Countdown) return "GET READY";
        if (Match.State == MatchState.MatchOver) return "TIME!";

        string time = UIKit.FormatTime(Match.TimeRemaining);
        if (Match.Mode == GameMode.Brawl) return time;
        return "<size=40%>" + GameModeInfo.Name(Match.Mode) + "</size>\n" + time;
    }

    private string Title()
    {
        if (Match.State == MatchState.Countdown)
        {
            return Mathf.CeilToInt(Match.CountdownRemaining).ToString();
        }
        if (Match.State == MatchState.Playing)
        {
            if (Match.TimeRemaining <= 10f) return Mathf.CeilToInt(Match.TimeRemaining).ToString();
            return "";
        }
        if (Match.State == MatchState.MatchOver)
        {
            if (Match.Winners.Count == 1)
            {
                PlayerEntry winner = Match.Winners[0];
                return UIKit.Colored(winner.Name, winner.Color) + " WINS!";
            }
            return "DRAW!";
        }
        return "";
    }

    private string EventLabel()
    {
        StaphItMode staph = StaphItMode.Active;
        if (staph != null && staph.Light != StaphItMode.Signal.Off)
        {
            string seconds = "  <size=80%>" + Mathf.CeilToInt(staph.PhaseRemaining) + "s</size>";
            if (staph.Light == StaphItMode.Signal.Red) return "RED LIGHT - STAPH IT!" + seconds;
            if (staph.Light == StaphItMode.Signal.Yellow) return "YELLOW - GET IN THE DOME!" + seconds;
            return "GREEN LIGHT" + seconds;
        }

        if (Match.Mode == GameMode.BombaClutch && Match.State == MatchState.Playing)
        {
            return "BOMBA CLUTCH  <size=80%>set off bombs: +1 each</size>";
        }

        if (CoinRushRunning())
        {
            CoinRush rush = hud.CoinRush;
            return UIKit.Colored("COIN RUSH", HUDStyle.Coin) + "  " + Mathf.CeilToInt(rush.TimeRemaining) + "s  -  " + rush.CoinsLeft + " coins left";
        }
        return "";
    }

    private void UpdateEventColor()
    {
        StaphItMode staph = StaphItMode.Active;
        if (staph == null || staph.Light == StaphItMode.Signal.Off)
        {
            eventBackground.color = HUDStyle.Panel;
            eventPanel.transform.localScale = Vector3.one;
            return;
        }

        Color c = staph.SignalColor;
        eventBackground.color = new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.55f, 0.95f);

        float swell = 1f;
        if (staph.Light == StaphItMode.Signal.Red) swell = 1.15f + 0.05f * Mathf.Sin(Time.time * 10f);
        eventPanel.transform.localScale = Vector3.one * swell;
    }
}
