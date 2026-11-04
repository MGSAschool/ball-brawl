using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Builds the match HUD at runtime, dressed with the GameArt UI sprites and font:
//   corners - one card per player (P1 top left, P2 top right, P3 bottom left, P4 bottom right):
//             the inventory frame with their colour and 2 stored power-ups (icons come from the
//             shared PowerupHUD prefab, so its logic stays in charge), plus points, coins and
//             active effects
//   centre  - lobby: logo, menu buttons (Play / How to Play / Leave), game mode cards
//             (Brawl / Staph It! / BombaClutch: click, 1-2-3 or gamepad LB/RB) and one
//             colour-coded join card per control scheme; match: timer, event banner (Coin Rush,
//             Staph It! lights with a screen tint), knock-off feed, countdown; results: winner
//             title and name plates in the scoreboard frame, menu below.
//             The How to Play card opens on request (button, H or gamepad Select), menus only
//   bottom  - start / rematch prompt and the controls line
// Resolution independent: the layout is designed at 1920x1080 and scaled in "Expand" mode,
// so the whole layout is always on screen whatever the monitor size or shape. Text is
// TextMeshPro (sharp at any resolution) and auto-sizes to fit its box instead of clipping.
public class MatchHUD : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [SerializeField] private CoinRush coinRush;
    [SerializeField] private float announcementDuration = 3f;
    [SerializeField] private float tipInterval = 6f;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [Tooltip("Per-player power-up box (PowerupHUD prefab). Its icons are shown in each player's corner card.")]
    [SerializeField] private PowerupInventoryUI powerUpBoxPrefab;
    [SerializeField] private float cornerMargin = 24f;
    [Tooltip("Opacity of the lobby menu and join cards while a ball is behind them.")]
    [SerializeField] private float behindAlpha = 0.3f;

    // Palette taken from the UI art: deep purple frames, orange trim, golden lettering
    private static readonly Color PanelColor = new Color(0.20f, 0.14f, 0.38f, 0.92f);
    private static readonly Color OutlineColor = new Color(0.17f, 0.10f, 0.29f);
    private static readonly Color TrimColor = new Color(0.95f, 0.52f, 0.16f);
    private static readonly Color GoldColor = new Color(1f, 0.82f, 0.25f);
    private static readonly Color CoinColor = new Color(1f, 0.84f, 0.3f);

    // Where the content sits inside each frame sprite (fractions of the cropped sprite)
    private static readonly Rect ScoreboardInner = Rect.MinMaxRect(0.13f, 0.12f, 0.87f, 0.79f);
    private static readonly Rect GuideInner = Rect.MinMaxRect(0.15f, 0.11f, 0.87f, 0.80f);
    private static readonly Rect[] InventorySlots =
    {
        Rect.MinMaxRect(0.076f, 0.28f, 0.327f, 0.767f),
        Rect.MinMaxRect(0.373f, 0.28f, 0.625f, 0.767f),
        Rect.MinMaxRect(0.670f, 0.28f, 0.922f, 0.767f)
    };
    private static readonly Rect PlateSquare = Rect.MinMaxRect(0.10f, 0.19f, 0.40f, 0.82f);
    private static readonly Rect PlateName = Rect.MinMaxRect(0.45f, 0.56f, 0.90f, 0.84f);
    private static readonly Rect PlateBar = Rect.MinMaxRect(0.45f, 0.26f, 0.90f, 0.545f);

    // Player card: inventory frame plus a stats strip on the side facing the screen centre
    private const float CardFrameHeight = 165f;
    private const float CardStripHeight = 56f;
    private const float CardGap = 8f;

    // Join cards, one per control scheme, each with its own colour (keys match MatchManager)
    private static readonly JoinScheme[] JoinSchemes =
    {
        new JoinScheme("Keyboard", "WASD", "SPACE", new Color(1f, 0.84f, 0.3f)),
        new JoinScheme("Arrows", "ARROWS", "RIGHT SHIFT", new Color(1f, 0.45f, 0.78f)),
        new JoinScheme("Gamepad", "GAMEPAD", "A", new Color(0.36f, 0.88f, 0.92f))
    };

    private readonly struct JoinScheme
    {
        public readonly string Scheme, Label, JoinKey;
        public readonly Color Color;

        public JoinScheme(string scheme, string label, string joinKey, Color color)
        {
            Scheme = scheme;
            Label = label;
            JoinKey = joinKey;
            Color = color;
        }
    }

    // Mode cards in the lobby, in GameMode order
    private static readonly ModeInfo[] Modes =
    {
        new ModeInfo(GameMode.Brawl, "BRAWL", "Knock everyone off the arena", new Color(1f, 0.55f, 0.2f)),
        new ModeInfo(GameMode.StaphIt, "STAPH IT!", "Red light? Be in the dome!", new Color(1f, 0.3f, 0.35f)),
        new ModeInfo(GameMode.BombaClutch, "BOMBA CLUTCH", "Set off bombs, survive the blasts", new Color(0.55f, 0.45f, 1f))
    };

    private readonly struct ModeInfo
    {
        public readonly GameMode Mode;
        public readonly string Title, Blurb;
        public readonly Color Color;

        public ModeInfo(GameMode mode, string title, string blurb, Color color)
        {
            Mode = mode;
            Title = title;
            Blurb = blurb;
            Color = color;
        }
    }

    private class ModeCard
    {
        public RectTransform Root;
        public Image Background;
        public Outline Outline;
        public TextMeshProUGUI Text;
    }

    private class PlayerCard
    {
        public TextMeshProUGUI Points, Coins, Status;
        public TextMeshProUGUI[] Badges = new TextMeshProUGUI[0];
        public PowerupInventoryUI Box;
    }

    private readonly Dictionary<MatchManager.PlayerEntry, PlayerCard> cards = new Dictionary<MatchManager.PlayerEntry, PlayerCard>();
    private readonly List<TextMeshProUGUI> joinTexts = new List<TextMeshProUGUI>();
    private readonly List<ModeCard> modeCards = new List<ModeCard>();
    private GameObject modePanel;
    private Image signalTint;

    private GameArt art;
    private RectTransform safeArea;
    private Rect appliedSafeArea;
    private GameObject timerPanel;
    private GameObject eventPanel;
    private Image eventIcon;
    private GameObject logo;
    private GameObject menu;
    private GameObject joinPanel;
    private Image playImage;
    private TextMeshProUGUI playLabel;
    private GameObject resultsPanel;
    private RectTransform resultsGrid;
    private bool resultsBuilt;
    private TextMeshProUGUI timerText;
    private TextMeshProUGUI eventText;
    private TextMeshProUGUI feedText;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI resultsText;
    private TextMeshProUGUI promptText;
    private TextMeshProUGUI controlsText;
    private GameObject guidePanel;
    private TextMeshProUGUI guideText;
    private bool guideOpen;
    private int tipOffset;
    private readonly StringBuilder sb = new StringBuilder();

    private bool InMenu => match.State == MatchManager.MatchState.Lobby || match.State == MatchManager.MatchState.MatchOver;

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        if (coinRush == null) coinRush = FindAnyObjectByType<CoinRush>();
        art = GameArt.Instance;
        tipOffset = Random.Range(0, ControlHints.Tips.Length);
        EnsureEventSystem();
        BuildCanvas();
    }

    private void OnEnable()
    {
        if (match != null) match.StateChanged += OnMatchStateChanged;
    }

    private void OnDisable()
    {
        if (match != null) match.StateChanged -= OnMatchStateChanged;
    }

    // The guide gets out of the way when a match starts; players can reopen it with H / Select
    private void OnMatchStateChanged(MatchManager.MatchState state)
    {
        if (state == MatchManager.MatchState.Countdown)
        {
            guideOpen = false;
            resultsBuilt = false;
        }
    }

    private void Update()
    {
        ApplySafeArea();
        if (match == null) return;

        // How to Play is only for the menus: during a match it would cover the arena for everyone,
        // and only keyboard players can reach H, so it stays shut until the match is over
        if (!InMenu) guideOpen = false;
        else if (GuideTogglePressed()) ToggleGuide();
        if (InMenu && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Leave();
        if (InMenu) HandleModeKeys();

        bool lobby = match.State == MatchManager.MatchState.Lobby;
        bool over = match.State == MatchManager.MatchState.MatchOver;

        guidePanel.SetActive(guideOpen);
        if (guideOpen) guideText.text = Guide();

        logo.SetActive(lobby);
        timerPanel.SetActive(!lobby);
        timerText.text = TimerLabel();

        // The guide covers the centre, so the menu steps aside while it is open
        menu.SetActive(InMenu && !guideOpen);
        if (menu.activeSelf)
        {
            PlaceMenu(over);
            playImage.color = match.CanStart ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.8f);
            playLabel.text = lobby ? "PLAY  <size=70%>(Enter)" : "REMATCH  <size=70%>(Enter)";
        }
        joinPanel.SetActive(lobby && !guideOpen);
        if (joinPanel.activeSelf) UpdateJoinCards();
        modePanel.SetActive(lobby && !guideOpen);
        if (modePanel.activeSelf) UpdateModeCards();
        FadeWhenCovering(menu);
        FadeWhenCovering(joinPanel);
        FadeWhenCovering(modePanel);
        UpdateSignal();

        UpdatePlayerCards();

        string eventLabel = EventLabel();
        eventPanel.SetActive(eventLabel.Length > 0);
        // The coin icon belongs to Coin Rush only
        eventIcon.enabled = eventIcon.sprite != null && coinRush != null && coinRush.IsActive;
        eventText.text = eventLabel;

        // In the menus the logo and buttons fill the top and centre, so news goes above the prompt instead
        bool showFeed = Time.time - match.AnnouncementTime < announcementDuration && !over;
        feedText.text = showFeed && !InMenu ? match.Announcement : "";
        string news = showFeed && InMenu ? $"<color=#FFD64D>{match.Announcement}</color>\n" : "";

        titleText.text = guideOpen ? "" : Title();
        Anchor(titleText.rectTransform, over ? Rect.MinMaxRect(0.1f, 0.74f, 0.9f, 0.88f) : Rect.MinMaxRect(0.1f, 0.5f, 0.9f, 0.74f));
        bool showResults = over && !guideOpen;
        resultsPanel.SetActive(showResults);
        if (showResults)
        {
            if (!resultsBuilt) BuildResults();
            resultsText.text = $"<color=#FFD64D>TIP:</color> {CurrentTip()}";
        }

        promptText.text = news + Prompt();
        if (guideOpen)
            controlsText.text = "Press <b>H</b> / <b>Select</b> or click the card to close How to Play";
        else if (InMenu)
            controlsText.text = "<b>1 / 2 / 3</b> or <b>LB / RB:</b> pick mode     <b>H</b> / <b>Select:</b> How to Play     <b>Esc:</b> Leave";
        else
            controlsText.text = "<b>WASD:</b> Space Jump | E Use power-up | LShift Dash     " +
                                "<b>Arrows:</b> RCtrl | / | RShift     " +
                                "<b>Gamepad:</b> A | B | X";
    }

    // 1 / 2 / 3 pick a mode directly; gamepad shoulders step through them
    private void HandleModeKeys()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) match.SetMode(GameMode.Brawl);
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) match.SetMode(GameMode.StaphIt);
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) match.SetMode(GameMode.BombaClutch);
        }
        foreach (var gamepad in Gamepad.all)
        {
            int step = gamepad.rightShoulder.wasPressedThisFrame ? 1 : gamepad.leftShoulder.wasPressedThisFrame ? -1 : 0;
            if (step == 0) continue;
            int count = Modes.Length;
            match.SetMode((GameMode)(((int)match.Mode + step + count) % count));
            break;
        }
    }

    private static bool GuideTogglePressed()
    {
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame) return true;
        foreach (var gamepad in Gamepad.all)
        {
            if (gamepad.selectButton.wasPressedThisFrame) return true;
        }
        return false;
    }

    // --- Menu buttons ---
    private void Play()
    {
        Sfx.Play(Sound.Click);
        if (!match.TryStartMatch())
        {
            match.Announce($"Need {match.MinPlayersToStart}+ players to start - join with SPACE, RSHIFT or A");
        }
    }

    private void ToggleGuide()
    {
        if (!InMenu) return;
        guideOpen = !guideOpen;
        Sfx.Play(Sound.Click);
    }

    private static void Leave()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Lobby: the middle of the screen. Results: a smaller row under the scoreboard
    private void PlaceMenu(bool results)
    {
        var rect = (RectTransform)menu.transform;
        if (results)
        {
            Place(rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 160f), new Vector2(820f, 200f));
            rect.localScale = Vector3.one * 0.7f;
        }
        else
        {
            Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(820f, 200f));
            rect.localScale = Vector3.one;
        }
    }

    // Players warm up in the middle of the arena, right under the lobby menu, so a panel turns
    // see-through while any ball is behind it (it stays clickable)
    private void FadeWhenCovering(GameObject panel)
    {
        if (!panel.activeSelf) return;
        var rect = (RectTransform)panel.transform;
        bool covering = false;
        Camera cam = Camera.main;
        if (cam != null)
        {
            foreach (var p in match.Players)
            {
                if (p.Player == null || p.IsRespawning) continue;
                Vector3 screen = cam.WorldToScreenPoint(p.Player.transform.position);
                if (screen.z > 0f && RectTransformUtility.RectangleContainsScreenPoint(rect, screen, null))
                {
                    covering = true;
                    break;
                }
            }
        }
        if (!panel.TryGetComponent<CanvasGroup>(out var group)) group = panel.AddComponent<CanvasGroup>();
        group.alpha = Mathf.MoveTowards(group.alpha, covering ? behindAlpha : 1f, Time.unscaledDeltaTime * 4f);
    }

    private string CurrentTip() =>
        ControlHints.Tips[(tipOffset + (int)(Time.unscaledTime / tipInterval)) % ControlHints.Tips.Length];

    // How to Play card (the frame art carries the title): goal, scoring, controls table and a tip
    private string Guide()
    {
        sb.Clear();
        sb.Append($"<align=center><color=#FFD64D>{MatchManager.ModeName(match.Mode)}:</color> ");
        switch (match.Mode)
        {
            case GameMode.StaphIt:
                sb.Append("When the light turns <color=#ff6b6b>RED</color>, be inside the glowing dome. It moves every round and shrinks.\n");
                sb.Append("<color=#9cff7a>+1</color> inside when red ends     <color=#ff6b6b>-1</color> caught outside (a shield blocks it)     <color=#9cff7a>+1</color> knock-off</align>\n\n");
                break;
            case GameMode.BombaClutch:
                sb.Append("Roll into bombs to set them off - but the blast throws you too. Blasts chain into nearby bombs.\n");
                sb.Append("<color=#9cff7a>+1</color> every bomb you set off     <color=#ff6b6b>-1</color> falling off on your own</align>\n\n");
                break;
            default:
                sb.Append("Knock rivals off the arena. Most points when time runs out wins.\n");
                sb.Append("<color=#9cff7a>+1</color> knock-off     <color=#FFD64D>+1</color> coin     <color=#ff6b6b>-1</color> falling off on your own</align>\n\n");
                break;
        }

        sb.Append("<align=left><color=#FFD64D>ACTION<pos=34%>WASD<pos=56%>ARROWS<pos=79%>GAMEPAD</color>\n");
        GuideRow("Move", ControlHints.Action.Move);
        GuideRow("Jump", ControlHints.Action.Jump);
        GuideRow("Use power-up", ControlHints.Action.UsePowerUp);
        GuideRow("Dash *", ControlHints.Action.Dash);
        sb.Append("</align><align=center><size=85%><color=#d8d0ff>Hold 2 power-ups (your corner card). Use fires the left one; a 3rd pickup pushes the oldest out.\n" +
                  "* While a Dash power-up is running. Levels I-III: bigger, brighter pickups are stronger.</color></size>\n\n");

        sb.Append($"<color=#FFD64D>TIP:</color> {CurrentTip()}</align>");
        return sb.ToString();
    }

    private void GuideRow(string label, ControlHints.Action action)
    {
        sb.Append(label)
          .Append("<pos=34%>").Append(ControlHints.Key("Keyboard", action))
          .Append("<pos=56%>").Append(ControlHints.Key("Arrows", action))
          .Append("<pos=79%>").Append(ControlHints.Key("Gamepad", action))
          .Append('\n');
    }

    // --- Content ---
    private string TimerLabel()
    {
        switch (match.State)
        {
            case MatchManager.MatchState.Lobby: return "LOBBY";
            case MatchManager.MatchState.Countdown: return "GET READY";
            case MatchManager.MatchState.Playing:
                return match.Mode == GameMode.Brawl
                    ? FormatTime(match.TimeRemaining)
                    : $"<size=40%>{MatchManager.ModeName(match.Mode)}</size>\n{FormatTime(match.TimeRemaining)}";
            default: return "TIME!";
        }
    }

    private string Title()
    {
        switch (match.State)
        {
            case MatchManager.MatchState.Countdown:
                return Mathf.CeilToInt(match.CountdownRemaining).ToString();
            case MatchManager.MatchState.Playing:
                return match.TimeRemaining <= 10f ? Mathf.CeilToInt(match.TimeRemaining).ToString() : "";
            case MatchManager.MatchState.MatchOver:
                var winners = match.Winners;
                if (winners.Count == 1) return $"<color=#{Hex(winners[0].Color)}>{winners[0].Name}</color> WINS!";
                return "DRAW!";
            default:
                return "";
        }
    }

    private string EventLabel()
    {
        var staph = StaphItMode.Active;
        if (staph != null && staph.Light != StaphItMode.Signal.Off)
        {
            int left = Mathf.CeilToInt(staph.PhaseRemaining);
            switch (staph.Light)
            {
                case StaphItMode.Signal.Red: return $"RED LIGHT - STAPH IT!  <size=80%>{left}s</size>";
                case StaphItMode.Signal.Yellow: return $"YELLOW - GET IN THE DOME!  <size=80%>{left}s</size>";
                default: return $"GREEN LIGHT  <size=80%>{left}s</size>";
            }
        }
        if (match.Mode == GameMode.BombaClutch && match.State == MatchManager.MatchState.Playing)
            return "BOMBA CLUTCH  <size=80%>set off bombs: +1 each</size>";
        if (coinRush != null && coinRush.IsActive)
        {
            return $"<color=#{Hex(CoinColor)}>COIN RUSH</color>  {Mathf.CeilToInt(coinRush.TimeRemaining)}s  -  {coinRush.CoinsLeft} coins left";
        }
        return "";
    }

    // The lobby has its join cards; after a match the join keys go in the prompt
    private string Prompt()
    {
        if (!InMenu) return "";

        string join;
        if (match.State != MatchManager.MatchState.Lobby)
            join = "Join: <b>SPACE</b> (WASD)   <b>RSHIFT</b> (Arrows)   <b>A</b> (Gamepad)\n";
        else if (!LobbyFull && SchemeJoined("Gamepad"))
            // The gamepad card is gone after the first pad, but more pads can still join
            join = $"More controllers: press <color=#{Hex(JoinSchemes[2].Color)}>A</color> to join\n";
        else
            join = "";
        // On the results screen the mode cards are hidden, so say which mode a rematch plays
        if (match.State == MatchManager.MatchState.MatchOver)
            join += $"Mode: <color=#FFD64D>{MatchManager.ModeName(match.Mode)}</color>  <size=80%>(1 / 2 / 3 or LB / RB to change)</size>\n";
        if (match.CanStart)
            return $"{join}Press <b>ENTER</b> / <b>START</b> to {(match.State == MatchManager.MatchState.Lobby ? "start" : "rematch")}";
        if (match.Players.Count < match.MinPlayersToStart)
            return $"{join}Need {match.MinPlayersToStart}+ players to start";
        return join.TrimEnd('\n');
    }

    private bool LobbyFull => match.Players.Count >= 4;

    private bool SchemeJoined(string scheme) => match.Players.Any(p => p.ControlScheme == scheme);

    // --- Join cards ---
    // A card disappears once someone joins with its controls; the rest close up in the centre
    private void UpdateJoinCards()
    {
        var open = new List<int>();
        for (int i = 0; i < JoinSchemes.Length && i < joinTexts.Count; i++)
        {
            bool show = !LobbyFull && !SchemeJoined(JoinSchemes[i].Scheme);
            joinTexts[i].transform.parent.gameObject.SetActive(show);
            if (show) open.Add(i);
        }

        // Cards keep their width; the row stays centred however many are left
        float width = 1f / JoinSchemes.Length;
        float start = 0.5f - open.Count * width * 0.5f;
        for (int n = 0; n < open.Count; n++)
        {
            int i = open[n];
            var scheme = JoinSchemes[i];
            Anchor((RectTransform)joinTexts[i].transform.parent,
                Rect.MinMaxRect(start + n * width + 0.012f, 0f, start + (n + 1) * width - 0.012f, 1f));

            sb.Clear();
            sb.Append($"Press <color=#{Hex(scheme.Color)}>{scheme.JoinKey}</color> to join");
            sb.Append("\n<size=70%><color=#d8d0ff>")
              .Append("Jump ").Append(ControlHints.Key(scheme.Scheme, ControlHints.Action.Jump))
              .Append("  |  Use ").Append(ControlHints.Key(scheme.Scheme, ControlHints.Action.UsePowerUp))
              .Append("  |  Dash ").Append(ControlHints.Key(scheme.Scheme, ControlHints.Action.Dash))
              .Append("</color></size>");
            joinTexts[i].text = sb.ToString();
        }
    }

    // --- Player cards ---
    private void UpdatePlayerCards()
    {
        for (int i = 0; i < match.Players.Count; i++)
        {
            var p = match.Players[i];
            if (!cards.TryGetValue(p, out var card))
            {
                card = CreatePlayerCard(p, i);
                cards.Add(p, card);
            }

            card.Points.text = p.Score.ToString();
            card.Coins.text = p.Coins.ToString();
            card.Status.text = Status(p);

            // Level badge on each filled slot (slot 1 is the oldest, used next)
            if (p.PowerUps == null || card.Box == null) continue;
            var items = p.PowerUps.Items;
            bool stale = false;
            for (int s = 0; s < card.Badges.Length; s++)
            {
                card.Badges[s].text = s < items.Count ? ControlHints.LevelNumeral(items[s]) : "";
                stale |= card.Box.icons[s].enabled != s < items.Count;
            }
            // A pickup grabbed on the frame the player spawned lands before the box's own Start
            // hides its icons, so redraw if they ever disagree with the inventory
            if (stale) card.Box.SendMessage("RefreshUI", SendMessageOptions.DontRequireReceiver);
        }
    }

    private static string Status(MatchManager.PlayerEntry p)
    {
        if (p.IsRespawning) return $"<color=#aaaaaa>back in {Mathf.CeilToInt(p.RespawnAt - Time.time)}</color>";
        var parts = new List<string>();
        if (p.Player.IsInvulnerable) parts.Add("<color=#7fd7ff>SHIELD</color>");
        if (p.Player.HasSuperKnockback) parts.Add("<color=#ff9f43>PUNCH</color>");
        if (p.Player.HasSpeedBoost) parts.Add("<color=#9cff7a>SPEED</color>");
        if (p.Player.HasDash) parts.Add("<color=#ffe066>DASH</color>");
        return string.Join(" ", parts);
    }

    private PlayerCard CreatePlayerCard(MatchManager.PlayerEntry entry, int index)
    {
        // P1 top left, P2 top right, P3 bottom left, P4 bottom right
        var corner = new Vector2(index % 2, index < 2 ? 1f : 0f);
        bool top = corner.y > 0.5f;
        bool framed = art != null && art.inventoryFrame != null;
        float width = framed ? CardFrameHeight * art.inventoryFrame.rect.width / art.inventoryFrame.rect.height : 300f;
        float height = CardFrameHeight + CardGap + CardStripHeight;

        var root = new GameObject($"{entry.Name} Card", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(safeArea, false);
        Place(root, corner, corner, corner,
            new Vector2(corner.x < 0.5f ? cornerMargin : -cornerMargin, top ? -cornerMargin : cornerMargin), new Vector2(width, height));

        // Frame against the screen edge, stats strip on the inner side
        float split = CardStripHeight / height;
        float frameStart = (CardStripHeight + CardGap) / height;
        var frameRect = top ? Rect.MinMaxRect(0f, frameStart, 1f, 1f) : Rect.MinMaxRect(0f, 0f, 1f, 1f - frameStart);
        var stripRect = top ? Rect.MinMaxRect(0f, 0f, 1f, split) : Rect.MinMaxRect(0f, 1f - split, 1f, 1f);

        var frame = new GameObject("Inventory", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        frame.rectTransform.SetParent(root, false);
        Anchor(frame.rectTransform, frameRect);
        frame.raycastTarget = false;
        if (framed) frame.sprite = art.inventoryFrame;
        else frame.color = PanelColor;

        // Slot 1: whose card this is
        var portrait = new GameObject("Portrait", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        portrait.rectTransform.SetParent(frame.rectTransform, false);
        Anchor(portrait.rectTransform, Inset(InventorySlots[0], 0.12f));
        portrait.sprite = ArtUtil.Circle();
        portrait.color = entry.Color;
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        var label = CreateText(portrait.rectTransform, "Name", 14f, 40f, TextAlignmentOptions.Center, 0f);
        label.text = entry.Name;

        // Points, coins and active effects
        var strip = new GameObject("Stats", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        strip.rectTransform.SetParent(root, false);
        Anchor(strip.rectTransform, stripRect);
        strip.color = PanelColor;
        strip.raycastTarget = false;
        var stripOutline = strip.gameObject.AddComponent<Outline>();
        stripOutline.effectColor = entry.Color;
        stripOutline.effectDistance = new Vector2(3f, -3f);

        var card = new PlayerCard
        {
            Points = CreateText(strip.rectTransform, "Points", 14f, 34f, TextAlignmentOptions.Left, 0f),
            Coins = CreateText(strip.rectTransform, "Coins", 14f, 34f, TextAlignmentOptions.Left, 0f),
            Status = CreateText(strip.rectTransform, "Status", 10f, 22f, TextAlignmentOptions.Right, 0f)
        };
        AddIcon(strip.rectTransform, art != null ? art.pointsIcon : null, Rect.MinMaxRect(0.03f, 0.12f, 0.13f, 0.88f));
        Anchor(card.Points.rectTransform, Rect.MinMaxRect(0.14f, 0.1f, 0.28f, 0.9f));
        AddIcon(strip.rectTransform, art != null ? art.coinIcon : null, Rect.MinMaxRect(0.28f, 0.12f, 0.38f, 0.88f));
        Anchor(card.Coins.rectTransform, Rect.MinMaxRect(0.39f, 0.1f, 0.5f, 0.9f));
        Anchor(card.Status.rectTransform, Rect.MinMaxRect(0.5f, 0.1f, 0.97f, 0.9f));

        if (powerUpBoxPrefab != null && entry.PowerUps != null) card.Badges = AttachPowerUpIcons(entry, index, frame.rectTransform, out card.Box);
        return card;
    }

    // Slots 2-3: the PowerupHUD prefab keeps filling its icons; we only move them into the frame
    private TextMeshProUGUI[] AttachPowerUpIcons(MatchManager.PlayerEntry entry, int index, RectTransform frame, out PowerupInventoryUI box)
    {
        var ui = Instantiate(powerUpBoxPrefab, frame, false);
        box = ui;
        ui.name = $"{entry.Name} Power-Ups";
        ui.Initialize(index + 1, entry.PowerUps.Inventory);
        // Redraw the icons when the match empties the inventory
        entry.PowerUps.Cleared += () => ui.SendMessage("RefreshUI", SendMessageOptions.DontRequireReceiver);

        var badges = new TextMeshProUGUI[ui.icons.Length];
        for (int s = 0; s < ui.icons.Length; s++)
        {
            var icon = ui.icons[s];
            icon.rectTransform.SetParent(frame, false);
            Anchor(icon.rectTransform, Inset(InventorySlots[Mathf.Min(s + 1, InventorySlots.Length - 1)], 0.1f));
            icon.preserveAspect = true;
            icon.enabled = false;

            badges[s] = CreateText(icon.rectTransform, "Level", 18f, 34f, TextAlignmentOptions.BottomRight, 0f);
            Place(badges[s].rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(6f, -6f), Vector2.zero);
            badges[s].color = GoldColor;
        }
        // Hide the prefab's own frame; its script stays alive to update the icons
        foreach (var graphic in ui.GetComponentsInChildren<Graphic>(true)) graphic.enabled = false;
        return badges;
    }

    // --- Results ---
    private void BuildResults()
    {
        resultsBuilt = true;
        for (int i = resultsGrid.childCount - 1; i >= 0; i--) Destroy(resultsGrid.GetChild(i).gameObject);

        var ranked = match.Players.OrderByDescending(p => p.Score).ToList();
        int columns = ranked.Count > 2 ? 2 : 1;
        int rows = Mathf.CeilToInt(ranked.Count / (float)columns);
        for (int i = 0; i < ranked.Count; i++)
        {
            var p = ranked[i];
            int col = i % columns, row = i / columns;
            var cell = Rect.MinMaxRect(col / (float)columns, 1f - (row + 1) / (float)Mathf.Max(1, rows),
                (col + 1) / (float)columns, 1f - row / (float)Mathf.Max(1, rows));
            CreatePlate(p, i + 1, Inset(cell, 0.03f));
        }
    }

    private void CreatePlate(MatchManager.PlayerEntry p, int rank, Rect cell)
    {
        var slot = new GameObject($"{p.Name} Slot", typeof(RectTransform)).GetComponent<RectTransform>();
        slot.SetParent(resultsGrid, false);
        Anchor(slot, cell);

        var plate = new GameObject($"{p.Name} Plate", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        plate.rectTransform.SetParent(slot, false);
        plate.raycastTarget = false;
        if (art != null && art.namePlate != null)
        {
            plate.sprite = art.namePlate;
            // Keep the plate's own shape so the parts placed on it line up with the art
            var fitter = plate.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = art.namePlate.rect.width / art.namePlate.rect.height;
        }
        else
        {
            Anchor(plate.rectTransform, Rect.MinMaxRect(0f, 0f, 1f, 1f));
            plate.color = PanelColor;
        }

        var badge = new GameObject("Rank", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        badge.rectTransform.SetParent(plate.rectTransform, false);
        Anchor(badge.rectTransform, Inset(PlateSquare, 0.1f));
        badge.sprite = ArtUtil.Circle();
        badge.color = p.Color;
        badge.preserveAspect = true;
        badge.raycastTarget = false;
        var rankText = CreateText(badge.rectTransform, "Rank", 14f, 60f, TextAlignmentOptions.Center, 0f);
        rankText.text = rank switch { 1 => "1st", 2 => "2nd", 3 => "3rd", _ => $"{rank}th" };

        var name = CreateText(plate.rectTransform, "Name", 12f, 40f, TextAlignmentOptions.Left, 0f);
        Anchor(name.rectTransform, PlateName);
        name.text = $"<color=#{Hex(p.Color)}>{p.Name}</color>  <size=70%>{p.Knockouts} KO</size>";

        AddIcon(plate.rectTransform, art != null ? art.pointsIcon : null, Rect.MinMaxRect(PlateBar.xMin + 0.02f, PlateBar.yMin + 0.03f, PlateBar.xMin + 0.12f, PlateBar.yMax - 0.03f));
        var points = CreateText(plate.rectTransform, "Points", 12f, 40f, TextAlignmentOptions.Left, 0f);
        Anchor(points.rectTransform, Rect.MinMaxRect(PlateBar.xMin + 0.14f, PlateBar.yMin, PlateBar.xMax - 0.02f, PlateBar.yMax));
        points.text = $"{p.Score} pts";
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
        return $"{total / 60}:{total % 60:00}";
    }

    private static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

    // --- Layout ---
    private void ApplySafeArea()
    {
        Rect area = Screen.safeArea;
        if (area == appliedSafeArea || Screen.width <= 0 || Screen.height <= 0) return;
        appliedSafeArea = area;

        safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
    }

    // Menu buttons need an EventSystem; the scene doesn't have one
    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private void BuildCanvas()
    {
        var canvasGO = new GameObject("MatchHUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        // Expand: the full reference layout always fits; extra width/height becomes margin
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        safeArea = new GameObject("Safe Area", typeof(RectTransform)).GetComponent<RectTransform>();
        safeArea.SetParent(canvasGO.transform, false);
        ApplySafeArea();

        // Lobby logo, top centre
        logo = new GameObject("Logo", typeof(RectTransform), typeof(Image));
        logo.transform.SetParent(safeArea, false);
        var logoImage = logo.GetComponent<Image>();
        logoImage.raycastTarget = false;
        if (art != null && art.logo != null)
        {
            logoImage.sprite = art.logo;
            logoImage.preserveAspect = true;
        }
        else
        {
            logoImage.enabled = false;
        }
        Place((RectTransform)logo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(460f, 257f));

        // Top-centre timer, event banner and feed
        timerPanel = CreatePanel("Timer Panel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -24f), new Vector2(320f, 96f));
        AddTrim(timerPanel);
        timerText = CreateText(timerPanel.transform, "Timer", 28f, 68f, TextAlignmentOptions.Center, 8f);
        timerText.color = GoldColor;

        eventPanel = CreatePanel("Event Panel", new Vector2(0.385f, 1f), new Vector2(0.615f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -132f), new Vector2(0f, 60f));
        AddTrim(eventPanel);
        eventIcon = AddIcon(eventPanel.transform, art != null ? art.coinIcon : null, Rect.MinMaxRect(0.01f, 0.08f, 0.1f, 0.92f));
        eventText = CreateText(eventPanel.transform, "Event", 16f, 34f, TextAlignmentOptions.Center, 8f);
        eventText.rectTransform.offsetMin = new Vector2(48f, 8f);

        feedText = CreateText(safeArea, "Feed", 16f, 34f, TextAlignmentOptions.Center, 0f);
        Place(feedText.rectTransform, new Vector2(0.385f, 1f), new Vector2(0.615f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(0f, 56f));

        // Centre countdown / winner title (moved up over the scoreboard on the results screen)
        titleText = CreateText(safeArea, "Title", 40f, 150f, TextAlignmentOptions.Center, 0f);
        Anchor(titleText.rectTransform, Rect.MinMaxRect(0.1f, 0.5f, 0.9f, 0.74f));
        titleText.color = GoldColor;
        titleText.outlineWidth = 0.28f;

        BuildResultsPanel();
        BuildMenu();
        BuildModePanel();
        BuildJoinPanel();
        BuildSignalTint();

        // How to Play card, centred under the logo; clicking it closes it
        guidePanel = CreatePanel("Guide Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -45f), new Vector2(1000f, 630f));
        var guideImage = guidePanel.GetComponent<Image>();
        if (art != null && art.howToPlayFrame != null)
        {
            guideImage.sprite = art.howToPlayFrame;
            guideImage.color = Color.white;
        }
        guideImage.raycastTarget = true;
        var guideButton = guidePanel.AddComponent<Button>();
        guideButton.transition = Selectable.Transition.None;
        guideButton.onClick.AddListener(ToggleGuide);
        guideText = CreateText(guidePanel.transform, "Guide", 12f, 30f, TextAlignmentOptions.TopLeft, 0f);
        Anchor(guideText.rectTransform, guideImage.sprite != null ? GuideInner : Rect.MinMaxRect(0.03f, 0.04f, 0.97f, 0.96f));
        guidePanel.SetActive(false);

        // Bottom prompt and controls, between the bottom player cards
        promptText = CreateText(safeArea, "Prompt", 18f, 34f, TextAlignmentOptions.Bottom, 0f);
        Place(promptText.rectTransform, new Vector2(0.25f, 0f), new Vector2(0.75f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(0f, 90f));

        controlsText = CreateText(safeArea, "Controls", 12f, 22f, TextAlignmentOptions.Bottom, 0f);
        controlsText.color = new Color(0.9f, 0.88f, 1f);
        Place(controlsText.rectTransform, new Vector2(0.25f, 0f), new Vector2(0.75f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(0f, 48f));
    }

    // Results: name plates inside the scoreboard frame, centred under the winner title
    private void BuildResultsPanel()
    {
        bool framed = art != null && art.scoreboardFrame != null;
        float height = 470f;
        float width = framed ? height * art.scoreboardFrame.rect.width / art.scoreboardFrame.rect.height : 760f;
        resultsPanel = CreatePanel("Results", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(width, height));
        var image = resultsPanel.GetComponent<Image>();
        if (framed)
        {
            image.sprite = art.scoreboardFrame;
            image.color = Color.white;
        }
        var inner = framed ? ScoreboardInner : Rect.MinMaxRect(0.03f, 0.03f, 0.97f, 0.97f);
        float tipHeight = inner.height * 0.16f;

        resultsGrid = new GameObject("Plates", typeof(RectTransform)).GetComponent<RectTransform>();
        resultsGrid.SetParent(resultsPanel.transform, false);
        Anchor(resultsGrid, Rect.MinMaxRect(inner.xMin, inner.yMin + tipHeight, inner.xMax, inner.yMax));
        resultsText = CreateText(resultsPanel.transform, "Tip", 12f, 24f, TextAlignmentOptions.Center, 0f);
        Anchor(resultsText.rectTransform, Rect.MinMaxRect(inner.xMin + 0.02f, inner.yMin, inner.xMax - 0.02f, inner.yMin + tipHeight));
        resultsPanel.SetActive(false);
    }

    // How to Play / Play / Leave in a row; PlaceMenu moves it for the lobby or the results
    private void BuildMenu()
    {
        menu = new GameObject("Menu", typeof(RectTransform));
        menu.transform.SetParent(safeArea, false);
        PlaceMenu(false);

        CreateButton("How To Play", art != null ? art.howToPlayButton : null, Rect.MinMaxRect(0f, 0.08f, 0.28f, 0.92f), ToggleGuide, out var guideLabel);
        guideLabel.text = "GUIDE  <size=70%>(H)";
        playImage = CreateButton("Play", art != null ? art.playButton : null, Rect.MinMaxRect(0.31f, 0f, 0.69f, 1f), Play, out playLabel);
        CreateButton("Leave", art != null ? art.leaveButton : null, Rect.MinMaxRect(0.72f, 0.08f, 1f, 0.92f), Leave, out var leaveLabel);
        leaveLabel.text = "LEAVE  <size=70%>(Esc)";
    }

    // Lobby: one clickable card per game mode, between the menu buttons and the join cards
    private void BuildModePanel()
    {
        modePanel = new GameObject("Modes", typeof(RectTransform));
        modePanel.transform.SetParent(safeArea, false);
        Place((RectTransform)modePanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1140f, 130f));

        float width = 1f / Modes.Length;
        for (int i = 0; i < Modes.Length; i++)
        {
            var info = Modes[i];
            var root = new GameObject($"{info.Title} Mode", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(modePanel.transform, false);
            Anchor(root, Rect.MinMaxRect(i * width + 0.015f, 0f, (i + 1) * width - 0.015f, 1f));

            var go = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(root, false);
            var background = go.GetComponent<Image>();
            Anchor(background.rectTransform, Rect.MinMaxRect(0f, 0f, 1f, 1f));
            var outline = go.AddComponent<Outline>();
            outline.effectDistance = new Vector2(4f, -4f);
            var mode = info.Mode;
            go.GetComponent<Button>().onClick.AddListener(() => match.SetMode(mode));

            var text = CreateText(background.rectTransform, "Text", 12f, 34f, TextAlignmentOptions.Center, 8f);
            modeCards.Add(new ModeCard { Root = root, Background = background, Outline = outline, Text = text });
        }
    }

    private void UpdateModeCards()
    {
        for (int i = 0; i < modeCards.Count; i++)
        {
            var info = Modes[i];
            var card = modeCards[i];
            bool selected = match.Mode == info.Mode;
            // The picked mode is lit in its colour and a bit bigger; the others sit back
            card.Background.color = selected ? Color.Lerp(PanelColor, info.Color, 0.55f) : PanelColor;
            card.Outline.effectColor = selected ? GoldColor : new Color(info.Color.r, info.Color.g, info.Color.b, 0.6f);
            float scale = Mathf.Lerp(card.Root.localScale.x, selected ? 1.06f : 0.94f, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            card.Root.localScale = Vector3.one * scale;
            string key = (i + 1).ToString();
            card.Text.text = $"<color=#{(selected ? "FFD64D" : Hex(info.Color))}>{info.Title}</color>  <size=60%>[{key}]</size>\n" +
                             $"<size=65%><color=#{(selected ? "FFFFFF" : "d8d0ff")}>{info.Blurb}</color></size>";
        }
    }

    // Full-screen colour wash for Staph It! lights: a red throb on red, a yellow flicker on yellow.
    // Also colours the event banner with the light.
    private void BuildSignalTint()
    {
        signalTint = new GameObject("Signal Tint", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        signalTint.rectTransform.SetParent(safeArea, false);
        signalTint.rectTransform.SetAsFirstSibling();
        Anchor(signalTint.rectTransform, Rect.MinMaxRect(0f, 0f, 1f, 1f));
        signalTint.raycastTarget = false;
        // Clear in the middle, coloured toward the edges
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / 1.414f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.SmoothStep(0.35f, 1f, r) * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        signalTint.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        signalTint.color = Color.clear;
    }

    private void UpdateSignal()
    {
        var staph = StaphItMode.Active;
        var light = staph != null ? staph.Light : StaphItMode.Signal.Off;
        var eventImage = eventPanel.GetComponent<Image>();
        float t = Time.time;

        Color tint = Color.clear;
        if (light == StaphItMode.Signal.Red) tint = new Color(1f, 0.1f, 0.1f, Mathf.Lerp(0.18f, 0.32f, 0.5f + 0.5f * Mathf.Sin(t * 10f)));
        else if (light == StaphItMode.Signal.Yellow) tint = new Color(1f, 0.8f, 0.1f, Mathf.PingPong(t * 3f, 0.22f));
        signalTint.color = Color.Lerp(signalTint.color, tint, 1f - Mathf.Exp(-12f * Time.deltaTime));

        if (light != StaphItMode.Signal.Off)
        {
            Color c = staph.SignalColor;
            eventImage.color = new Color(c.r * 0.55f, c.g * 0.55f, c.b * 0.55f, 0.95f);
            // Red light shouts: the banner swells
            float swell = light == StaphItMode.Signal.Red ? 1.15f + 0.05f * Mathf.Sin(t * 10f) : 1f;
            eventPanel.transform.localScale = Vector3.one * swell;
        }
        else
        {
            eventImage.color = PanelColor;
            eventPanel.transform.localScale = Vector3.one;
        }
    }

    // Lobby: a card per control scheme in its own colour, saying which key joins (hidden once used)
    private void BuildJoinPanel()
    {
        joinPanel = new GameObject("Join", typeof(RectTransform));
        joinPanel.transform.SetParent(safeArea, false);
        Place((RectTransform)joinPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -200f), new Vector2(1140f, 170f));

        float width = 1f / JoinSchemes.Length;
        for (int i = 0; i < JoinSchemes.Length; i++)
        {
            var scheme = JoinSchemes[i];
            var card = new GameObject($"{scheme.Label} Join", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            card.rectTransform.SetParent(joinPanel.transform, false);
            Anchor(card.rectTransform, Rect.MinMaxRect(i * width + 0.012f, 0f, (i + 1) * width - 0.012f, 1f));
            card.color = PanelColor;
            card.raycastTarget = false;
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = scheme.Color;
            outline.effectDistance = new Vector2(4f, -4f);

            // Coloured header with the scheme's name
            var header = new GameObject("Header", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            header.rectTransform.SetParent(card.rectTransform, false);
            Anchor(header.rectTransform, Rect.MinMaxRect(0f, 0.7f, 1f, 1f));
            header.color = scheme.Color;
            header.raycastTarget = false;
            var title = CreateText(header.rectTransform, "Scheme", 14f, 34f, TextAlignmentOptions.Center, 2f);
            title.text = scheme.Label;

            var body = CreateText(card.rectTransform, "Body", 12f, 30f, TextAlignmentOptions.Center, 0f);
            Anchor(body.rectTransform, Rect.MinMaxRect(0.04f, 0.05f, 0.96f, 0.68f));
            joinTexts.Add(body);
        }
    }

    private Image CreateButton(string name, Sprite sprite, Rect anchors, UnityEngine.Events.UnityAction onClick, out TextMeshProUGUI label)
    {
        var root = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(menu.transform, false);
        Anchor(root, anchors);

        var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(root, false);
        var image = go.GetComponent<Image>();
        Anchor(image.rectTransform, Rect.MinMaxRect(0f, 0.22f, 1f, 1f));
        if (sprite != null)
        {
            image.sprite = sprite;
            image.preserveAspect = true;
        }
        else
        {
            image.color = PanelColor;
        }
        var button = go.GetComponent<Button>();
        button.onClick.AddListener(onClick);
        var colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.95f, 0.8f);
        colors.pressedColor = new Color(0.8f, 0.75f, 0.65f);
        button.colors = colors;

        label = CreateText(root, "Label", 12f, 26f, TextAlignmentOptions.Center, 0f);
        Anchor(label.rectTransform, Rect.MinMaxRect(0f, 0f, 1f, 0.22f));
        label.text = name.ToUpper();
        return image;
    }

    private GameObject CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(safeArea, false);
        var image = go.GetComponent<Image>();
        image.color = PanelColor;
        image.raycastTarget = false;
        Place(go.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, position, size);
        return go;
    }

    // Orange edge like the frames in the UI art
    private static void AddTrim(GameObject panel)
    {
        var outline = panel.AddComponent<Outline>();
        outline.effectColor = TrimColor;
        outline.effectDistance = new Vector2(4f, -4f);
    }

    private static Image AddIcon(Transform parent, Sprite sprite, Rect anchors)
    {
        var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        icon.rectTransform.SetParent(parent, false);
        Anchor(icon.rectTransform, anchors);
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        icon.sprite = sprite;
        icon.enabled = sprite != null;
        return icon;
    }

    private static Rect Inset(Rect r, float fraction)
    {
        float dx = r.width * fraction, dy = r.height * fraction;
        return Rect.MinMaxRect(r.xMin + dx, r.yMin + dy, r.xMax - dx, r.yMax - dy);
    }

    // Stretch a rect over a fraction of its parent
    private static void Anchor(RectTransform rect, Rect anchors)
    {
        rect.anchorMin = anchors.min;
        rect.anchorMax = anchors.max;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    // Auto-sizing, wrapping text that fills its parent (minus padding)
    private TextMeshProUGUI CreateText(Transform parent, string name, float minSize, float maxSize,
        TextAlignmentOptions alignment, float padding)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);

        var text = go.GetComponent<TextMeshProUGUI>();
        if (art != null && art.font != null) text.font = art.font;
        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.richText = true;
        text.raycastTarget = false;
        text.color = Color.white;
        text.outlineWidth = 0.2f;
        text.outlineColor = OutlineColor;
        return text;
    }
}
