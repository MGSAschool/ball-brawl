using System.Collections.Generic;
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

    public override void Tick()
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

public class HUDPlayerCards : HUDPanel
{
    private const float FrameHeight = 165f;
    private const float StripHeight = 46f;
    private const float Gap = 6f;

    private static readonly Rect FrameArtArea = Rect.MinMaxRect(0.121f, 0.204f, 0.867f, 0.781f);
    private static readonly Rect CoinsNumber = Rect.MinMaxRect(0.508f, 0.315f, 0.604f, 0.407f);
    private static readonly Rect PointsNumber = Rect.MinMaxRect(0.669f, 0.315f, 0.833f, 0.407f);

    private static readonly Rect[] Slots =
    {
        Rect.MinMaxRect(0.185f, 0.333f, 0.393f, 0.685f),
        Rect.MinMaxRect(0.440f, 0.433f, 0.583f, 0.676f),
        Rect.MinMaxRect(0.615f, 0.433f, 0.758f, 0.676f)
    };

    private static readonly Color[] LevelColors =
    {
        new Color(0.36f, 0.82f, 0.42f),
        new Color(1f, 0.62f, 0.2f),
        new Color(0.93f, 0.25f, 0.3f)
    };

    private class PlayerCard
    {
        public TextMeshProUGUI Points;
        public TextMeshProUGUI Coins;
        public TextMeshProUGUI Status;
        public GameObject Strip;
        public PowerupInventoryUI Box;
        public Image[] Badges = new Image[0];
        public TextMeshProUGUI[] BadgeNumbers = new TextMeshProUGUI[0];
    }

    private readonly Dictionary<PlayerEntry, PlayerCard> cards = new Dictionary<PlayerEntry, PlayerCard>();

    protected override void BuildPanel()
    {
    }

    public override void Tick()
    {
        if (hud == null || Match == null) return;

        for (int i = 0; i < Match.Players.Count; i++)
        {
            PlayerEntry entry = Match.Players[i];
            PlayerCard card;
            if (!cards.TryGetValue(entry, out card))
            {
                card = MakeCard(entry, i);
                cards.Add(entry, card);
            }

            card.Points.text = entry.Score.ToString();
            card.Coins.text = entry.Coins.ToString();
            card.Status.text = StatusText(entry);
            card.Strip.SetActive(card.Status.text.Length > 0);
            RefreshLevels(entry, card);
        }
    }

    private string StatusText(PlayerEntry entry)
    {
        if (entry.IsControllerLost) return UIKit.Colored("NO CONTROLLER", HUDStyle.BadHex) + " - press " + ControlHints.Cap("A");
        if (entry.IsRespawning) return UIKit.Colored("back in " + Mathf.CeilToInt(entry.RespawnAt - Time.time), "aaaaaa");

        string status = "";
        if (entry.Player.IsInvulnerable) status += UIKit.Colored("SHIELD", "7fd7ff") + " ";
        if (entry.Player.HasSuperKnockback) status += UIKit.Colored("PUNCH", "ff9f43") + " ";
        if (entry.Player.HasSpeedBoost) status += UIKit.Colored("SPEED", HUDStyle.GoodHex) + " ";
        if (entry.Player.HasDash) status += UIKit.Colored("DASH", "ffe066") + " ";
        return status.TrimEnd();
    }

    private void RefreshLevels(PlayerEntry entry, PlayerCard card)
    {
        if (entry.PowerUps == null || card.Box == null) return;

        IReadOnlyList<PowerUpModifier> items = entry.PowerUps.Items;
        bool iconsOutOfDate = false;
        for (int s = 0; s < card.Badges.Length; s++)
        {
            Image icon = card.Box.icons[s];
            bool hasItem = s < items.Count && items[s] != null;
            int level = 0;
            if (hasItem && icon.enabled && icon.sprite != null) level = Mathf.Clamp(items[s].Level, 1, 3);
            ShowLevel(card.Badges[s], card.BadgeNumbers[s], level);

            if (icon.enabled != s < items.Count) iconsOutOfDate = true;
        }

        if (iconsOutOfDate) card.Box.SendMessage("RefreshUI", SendMessageOptions.DontRequireReceiver);
    }

    private void ShowLevel(Image badge, TextMeshProUGUI number, int level)
    {
        bool show = level > 0;
        badge.enabled = show;
        number.enabled = show;
        if (!show) return;

        badge.color = LevelColors[level - 1];
        number.text = level.ToString();
    }

    private PlayerCard MakeCard(PlayerEntry entry, int index)
    {
        Vector2 corner = new Vector2(index % 2, 1f);
        if (index >= 2) corner.y = 0f;
        bool top = corner.y > 0.5f;

        bool framed = Art != null && Art.inventoryFrame != null;
        float width = 300f;
        if (framed)
        {
            float artWidth = Art.inventoryFrame.rect.width * FrameArtArea.width;
            float artHeight = Art.inventoryFrame.rect.height * FrameArtArea.height;
            width = FrameHeight * artWidth / artHeight;
        }
        float height = FrameHeight + Gap + StripHeight;

        float margin = hud.CornerMargin;
        Vector2 offset = new Vector2(margin, -margin);
        if (corner.x > 0.5f) offset.x = -margin;
        if (!top) offset.y = margin;

        RectTransform root = UIKit.NewRect(entry.Name + " Card", Root);
        UIKit.PlaceAt(root, corner, offset, new Vector2(width, height));

        float stripPart = StripHeight / height;
        float framePart = (StripHeight + Gap) / height;
        Rect frameRect = Rect.MinMaxRect(0f, 0f, 1f, 1f - framePart);
        Rect stripRect = Rect.MinMaxRect(0f, 1f - stripPart, 1f, 1f);
        if (top)
        {
            frameRect = Rect.MinMaxRect(0f, framePart, 1f, 1f);
            stripRect = Rect.MinMaxRect(0f, 0f, 1f, stripPart);
        }

        Image frame = UIKit.NewImage("Inventory", root);
        if (framed)
        {
            frame.sprite = Art.inventoryFrame;
            UIKit.StretchToArt(frame.rectTransform, frameRect, FrameArtArea);
        }
        else
        {
            UIKit.Stretch(frame.rectTransform, frameRect);
            frame.color = HUDStyle.Panel;
        }

        PlayerCard card = new PlayerCard();
        MakePortrait(entry, frame.rectTransform);
        MakeCounters(card, frame.rectTransform);
        MakeStatsStrip(entry, card, root, stripRect);
        if (hud.PowerUpBoxPrefab != null && entry.PowerUps != null) AttachPowerUpBox(entry, index, frame.rectTransform, card);
        return card;
    }

    private void MakeCounters(PlayerCard card, RectTransform frame)
    {
        card.Coins = UIKit.NewAutoText(frame, "Coins", 10f, 30f, TextAlignmentOptions.Left, 0f);
        UIKit.Stretch(card.Coins.rectTransform, CoinsNumber);

        card.Points = UIKit.NewAutoText(frame, "Points", 10f, 30f, TextAlignmentOptions.Left, 0f);
        UIKit.Stretch(card.Points.rectTransform, PointsNumber);
    }

    private void MakePortrait(PlayerEntry entry, RectTransform frame)
    {
        Image portrait = UIKit.NewImage("Portrait", frame);
        UIKit.Stretch(portrait.rectTransform, UIKit.Inset(Slots[0], 0.12f));
        portrait.sprite = TextureMaker.Circle();
        portrait.color = entry.Color;
        portrait.preserveAspect = true;

        TextMeshProUGUI label = UIKit.NewAutoText(portrait.rectTransform, "Name", 14f, 40f, TextAlignmentOptions.Center, 0f);
        label.text = entry.Name;
    }

    private void MakeStatsStrip(PlayerEntry entry, PlayerCard card, RectTransform root, Rect stripRect)
    {
        Image strip = UIKit.NewImage("Stats", root);
        UIKit.Stretch(strip.rectTransform, stripRect);
        strip.color = HUDStyle.Panel;
        UIKit.AddOutline(strip.gameObject, entry.Color, 3f);

        card.Strip = strip.gameObject;

        card.Status = UIKit.NewAutoText(strip.rectTransform, "Status", 10f, 22f, TextAlignmentOptions.Center, 0f);
        UIKit.Stretch(card.Status.rectTransform, Rect.MinMaxRect(0.03f, 0.1f, 0.97f, 0.9f));
    }

    private void AttachPowerUpBox(PlayerEntry entry, int index, RectTransform frame, PlayerCard card)
    {
        PowerupInventoryUI box = Object.Instantiate(hud.PowerUpBoxPrefab, frame, false);
        box.name = entry.Name + " Power-Ups";
        box.Initialize(index + 1, entry.PowerUps.Inventory);
        entry.PowerUps.Cleared += () => box.SendMessage("RefreshUI", SendMessageOptions.DontRequireReceiver);
        card.Box = box;

        int slotCount = box.icons.Length;
        card.Badges = new Image[slotCount];
        card.BadgeNumbers = new TextMeshProUGUI[slotCount];
        for (int s = 0; s < slotCount; s++)
        {
            Rect slot = UIKit.Inset(Slots[Mathf.Min(s + 1, Slots.Length - 1)], 0.1f);

            Image icon = box.icons[s];
            icon.rectTransform.SetParent(frame, false);
            icon.preserveAspect = true;
            icon.enabled = false;
            UIKit.Stretch(icon.rectTransform, slot);

            Image badge = UIKit.NewImage("Level", frame);
            badge.sprite = TextureMaker.Circle();
            badge.preserveAspect = true;
            UIKit.Stretch(badge.rectTransform, Rect.MinMaxRect(
                slot.xMax - slot.width * 0.42f, slot.yMin - slot.height * 0.06f,
                slot.xMax + slot.width * 0.06f, slot.yMin + slot.height * 0.42f));
            UIKit.AddOutline(badge.gameObject, new Color(0.1f, 0.05f, 0.15f, 0.9f), 2f);

            TextMeshProUGUI number = UIKit.NewAutoText(badge.rectTransform, "Number", 10f, 30f, TextAlignmentOptions.Center, 0f);
            number.outlineWidth = 0.25f;
            number.outlineColor = new Color32(30, 15, 40, 255);

            card.Badges[s] = badge;
            card.BadgeNumbers[s] = number;
            ShowLevel(badge, number, 0);
        }

        foreach (Graphic graphic in box.GetComponentsInChildren<Graphic>(true))
        {
            if (System.Array.IndexOf(box.icons, graphic) < 0) graphic.enabled = false;
        }
    }
}

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

    public override void Tick()
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

public class HUDSignalTint : HUDPanel
{
    private Image tint;

    protected override void BuildPanel()
    {
        tint = UIKit.NewImage("Signal Tint", Root);
        tint.rectTransform.SetAsFirstSibling();
        UIKit.Stretch(tint.rectTransform, Rect.MinMaxRect(0f, 0f, 1f, 1f));
        tint.sprite = TextureMaker.Vignette();
        tint.color = Color.clear;
    }

    public override void Tick()
    {
        if (hud == null) return;

        StaphItMode staph = StaphItMode.Active;
        Color target = Color.clear;
        if (staph != null && staph.Light == StaphItMode.Signal.Red)
        {
            float alpha = Mathf.Lerp(0.18f, 0.32f, 0.5f + 0.5f * Mathf.Sin(Time.time * 10f));
            target = new Color(1f, 0.1f, 0.1f, alpha);
        }
        else if (staph != null && staph.Light == StaphItMode.Signal.Yellow)
        {
            target = new Color(1f, 0.8f, 0.1f, Mathf.PingPong(Time.time * 3f, 0.22f));
        }

        tint.color = Color.Lerp(tint.color, target, 1f - Mathf.Exp(-12f * Time.deltaTime));
    }
}
