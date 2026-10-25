using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Builds the match HUD at runtime: timer, scoreboard with power-up slots, event banner,
// knock-off feed, countdown / results, control hints and the How to Play card
// (open in the lobby, toggled any time with H / gamepad Select).
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

    private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.5f);
    private static readonly Color CoinColor = new Color(1f, 0.84f, 0.3f);

    private RectTransform safeArea;
    private Rect appliedSafeArea;
    private GameObject scorePanel;
    private GameObject resultsPanel;
    private GameObject eventPanel;
    private TextMeshProUGUI timerText;
    private TextMeshProUGUI scoreboardText;
    private TextMeshProUGUI eventText;
    private TextMeshProUGUI feedText;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI resultsText;
    private TextMeshProUGUI promptText;
    private TextMeshProUGUI controlsText;
    private GameObject guidePanel;
    private TextMeshProUGUI guideText;
    private bool guideOpen = true;
    private int tipOffset;
    private readonly StringBuilder sb = new StringBuilder();

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        if (coinRush == null) coinRush = FindAnyObjectByType<CoinRush>();
        tipOffset = Random.Range(0, ControlHints.Tips.Length);
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
        if (state == MatchManager.MatchState.Countdown) guideOpen = false;
    }

    private void Update()
    {
        ApplySafeArea();
        if (match == null) return;

        if (GuideTogglePressed()) guideOpen = !guideOpen;
        guidePanel.SetActive(guideOpen);
        if (guideOpen) guideText.text = Guide();

        timerText.text = TimerLabel();

        scorePanel.SetActive(match.Players.Count > 0);
        scoreboardText.text = Scoreboard();

        string eventLabel = EventLabel();
        eventPanel.SetActive(eventLabel.Length > 0);
        eventText.text = eventLabel;

        bool showFeed = Time.time - match.AnnouncementTime < announcementDuration && match.State != MatchManager.MatchState.MatchOver;
        feedText.text = showFeed ? match.Announcement : "";

        titleText.text = guideOpen ? "" : Title();
        bool showResults = match.State == MatchManager.MatchState.MatchOver && !guideOpen;
        resultsPanel.SetActive(showResults);
        if (showResults) resultsText.text = Results();

        promptText.text = Prompt();
        controlsText.text = guideOpen
            ? "Press <b>H</b> / <b>Select</b> to close How to Play"
            : "<b>WASD:</b> Space Jump | LShift Dash | Q Invuln | E Super KB     " +
              "<b>Arrows:</b> RCtrl | RShift | . | /     " +
              "<b>Gamepad:</b> A | X | Y | B     <b>H</b> / <b>Select:</b> How to Play";
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

    private string CurrentTip() =>
        ControlHints.Tips[(tipOffset + (int)(Time.unscaledTime / tipInterval)) % ControlHints.Tips.Length];

    // How to Play card: goal, scoring, a controls table for every scheme and a rotating tip
    private string Guide()
    {
        sb.Clear();
        sb.Append("<align=center><size=130%><b>HOW TO PLAY</b></size>\n");
        sb.Append("Knock rivals off the arena. Most points when the 3 minutes run out wins.\n");
        sb.Append("<color=#9cff7a>+1</color> knock-off     <color=#FFD64D>+1</color> coin     <color=#ff6b6b>-1</color> falling off on your own</align>\n\n");

        sb.Append("<align=left><b><color=#aaaaaa>ACTION<pos=34%>WASD PLAYER<pos=56%>ARROWS PLAYER<pos=79%>GAMEPAD</color></b>\n");
        GuideRow("Move", ControlHints.Action.Move);
        GuideRow("Jump", ControlHints.Action.Jump);
        GuideRow("Dash *", ControlHints.Action.Dash);
        GuideRow("Invulnerability *", ControlHints.Action.Invulnerability);
        GuideRow("Super Knockback *", ControlHints.Action.SuperKnockback);
        sb.Append("</align><align=center><size=85%><color=#cccccc>* Uses a power-up you're holding. Grab them in the arena (2 slots). Levels I-III: bigger, brighter pickups are stronger.</color></size>\n\n");

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
            case MatchManager.MatchState.Playing: return FormatTime(match.TimeRemaining);
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
        if (coinRush != null && coinRush.IsActive)
        {
            return $"<color=#{Hex(CoinColor)}>COIN RUSH</color>  {Mathf.CeilToInt(coinRush.TimeRemaining)}s  -  {coinRush.CoinsLeft} coins left";
        }
        return "";
    }

    private string Results()
    {
        sb.Clear();
        int rank = 1;
        foreach (var p in match.Players.OrderByDescending(p => p.Score))
        {
            sb.Append($"{rank++}.  <color=#{Hex(p.Color)}><b>{p.Name}</b></color>   {p.Score} pts   ");
            sb.Append($"<color=#cccccc>({p.Knockouts} KOs, {p.Coins} coins, {p.Falls} falls)</color>\n");
        }
        sb.Append($"\n<size=75%><color=#FFD64D>TIP:</color> {CurrentTip()}</size>");
        return sb.ToString().TrimEnd();
    }

    private string Scoreboard()
    {
        sb.Clear();
        foreach (var p in match.Players)
        {
            sb.Append($"<color=#{Hex(p.Color)}><b>{p.Name}</b></color>  <b>{p.Score}</b> pts  ");
            if (p.Coins > 0) sb.Append($"<color=#{Hex(CoinColor)}>{p.Coins}c</color>  ");
            if (p.Inventory != null)
            {
                for (int i = 0; i < PlayerInventory.SlotCount; i++)
                {
                    sb.Append($"[{SlotLabel(p.Inventory.GetSlot(i))}]");
                }
            }
            if (p.IsRespawning)
            {
                sb.Append($"  <color=#aaaaaa>respawn {Mathf.CeilToInt(p.RespawnAt - Time.time)}</color>");
            }
            else
            {
                if (p.Player.IsInvulnerable) sb.Append("  <color=#7fd7ff>INVULN</color>");
                if (p.Player.HasSuperKnockback) sb.Append("  <color=#ff9f43>SUPER KB</color>");
                if (p.Player.HasSpeedBoost) sb.Append("  <color=#9cff7a>SPEED</color>");
                if (p.Player.HasDash) sb.Append("  <color=#e6a8ff>DASH</color>");
            }
            sb.Append('\n');
        }
        return sb.ToString().TrimEnd();
    }

    private string Prompt()
    {
        if (match.State != MatchManager.MatchState.Lobby && match.State != MatchManager.MatchState.MatchOver) return "";

        string join = "Join: <b>SPACE</b> (WASD)   <b>RSHIFT</b> (Arrows)   <b>A</b> (Gamepad)";
        if (match.CanStart)
            return $"{join}\nPress <b>ENTER</b> / <b>START</b> to {(match.State == MatchManager.MatchState.Lobby ? "start" : "rematch")}";
        if (match.Players.Count < match.MinPlayersToStart)
            return $"{join}\nNeed {match.MinPlayersToStart}+ players to start";
        return join;
    }

    private static string SlotLabel(PowerUpModifier powerUp)
    {
        switch (powerUp)
        {
            case null: return " - ";
            case DashPowerUp: return "Dash " + ControlHints.LevelNumeral(powerUp);
            case InvulnerablePowerUp: return "Invuln " + ControlHints.LevelNumeral(powerUp);
            case SuperKnockbackPowerUp: return "Super KB " + ControlHints.LevelNumeral(powerUp);
            default: return powerUp.name;
        }
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

    private void BuildCanvas()
    {
        var canvasGO = new GameObject("MatchHUD Canvas", typeof(Canvas), typeof(CanvasScaler));
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

        // Layout columns (fractions of the canvas width, so they never overlap at any aspect):
        // scoreboard 0-0.36 on the left, timer / event / feed in the 0.385-0.615 center column.

        // Top-left scoreboard
        scorePanel = CreatePanel("Scoreboard Panel", new Vector2(0f, 1f), new Vector2(0.36f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -24f), new Vector2(0f, 230f));
        scoreboardText = CreateText(scorePanel.transform, "Scoreboard", 16f, 32f, TextAlignmentOptions.TopLeft, 16f);

        // Top-center timer, event banner and feed
        var timerPanel = CreatePanel("Timer Panel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -24f), new Vector2(320f, 96f));
        timerText = CreateText(timerPanel.transform, "Timer", 28f, 68f, TextAlignmentOptions.Center, 8f);

        eventPanel = CreatePanel("Event Panel", new Vector2(0.385f, 1f), new Vector2(0.615f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -132f), new Vector2(0f, 60f));
        eventText = CreateText(eventPanel.transform, "Event", 16f, 34f, TextAlignmentOptions.Center, 8f);

        feedText = CreateText(safeArea, "Feed", 16f, 34f, TextAlignmentOptions.Center, 0f);
        Place(feedText.rectTransform, new Vector2(0.385f, 1f), new Vector2(0.615f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(0f, 56f));

        // Center countdown / winner title and results table (below the top bar)
        titleText = CreateText(safeArea, "Title", 40f, 150f, TextAlignmentOptions.Center, 0f);
        Place(titleText.rectTransform, new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.74f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        resultsPanel = CreatePanel("Results Panel", new Vector2(0.22f, 0.18f), new Vector2(0.78f, 0.48f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        resultsText = CreateText(resultsPanel.transform, "Results", 18f, 40f, TextAlignmentOptions.Center, 20f);

        // How to Play card: the free middle area between the top bar and the bottom prompt
        guidePanel = CreatePanel("Guide Panel", new Vector2(0.17f, 0.17f), new Vector2(0.83f, 0.74f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        guidePanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
        guideText = CreateText(guidePanel.transform, "Guide", 14f, 34f, TextAlignmentOptions.TopLeft, 28f);

        // Bottom prompt and controls
        promptText = CreateText(safeArea, "Prompt", 18f, 34f, TextAlignmentOptions.Bottom, 0f);
        Place(promptText.rectTransform, new Vector2(0.05f, 0f), new Vector2(0.95f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(0f, 90f));

        controlsText = CreateText(safeArea, "Controls", 12f, 22f, TextAlignmentOptions.Bottom, 0f);
        controlsText.color = new Color(0.85f, 0.85f, 0.85f);
        Place(controlsText.rectTransform, new Vector2(0.02f, 0f), new Vector2(0.98f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(0f, 44f));
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

    private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    // Auto-sizing, wrapping text that fills its parent (minus padding)
    private static TextMeshProUGUI CreateText(Transform parent, string name, float minSize, float maxSize,
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
        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.richText = true;
        text.raycastTarget = false;
        text.color = Color.white;
        text.outlineWidth = 0.18f;
        text.outlineColor = new Color32(0, 0, 0, 200);
        return text;
    }
}
