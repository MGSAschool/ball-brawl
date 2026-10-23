using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// Builds a simple screen-space HUD at runtime for MatchManager: timer, scoreboard with
// power-up slots, countdown / results banner, knock-off feed and control hints.
public class MatchHUD : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [SerializeField] private float announcementDuration = 2.5f;

    private Text timerText;
    private Text scoreboardText;
    private Text bannerText;
    private Text feedText;
    private Text hintText;
    private readonly StringBuilder sb = new StringBuilder();

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        BuildCanvas();
    }

    private void Update()
    {
        if (match == null) return;

        timerText.text = TimerLabel();
        scoreboardText.text = Scoreboard();
        bannerText.text = Banner();
        hintText.text = Hint();

        bool showFeed = Time.time - match.AnnouncementTime < announcementDuration && match.State != MatchManager.MatchState.MatchOver;
        feedText.text = showFeed ? match.Announcement : "";
    }

    private string TimerLabel()
    {
        switch (match.State)
        {
            case MatchManager.MatchState.Lobby: return "LOBBY";
            case MatchManager.MatchState.Countdown: return FormatTime(0f);
            case MatchManager.MatchState.Playing: return FormatTime(match.TimeRemaining);
            default: return "TIME!";
        }
    }

    private string Banner()
    {
        switch (match.State)
        {
            case MatchManager.MatchState.Countdown:
                return Mathf.CeilToInt(match.CountdownRemaining).ToString();
            case MatchManager.MatchState.Playing:
                return match.TimeRemaining <= 10f ? Mathf.CeilToInt(match.TimeRemaining).ToString() : "";
            case MatchManager.MatchState.MatchOver:
                return Results();
            default:
                return "";
        }
    }

    private string Results()
    {
        sb.Clear();
        var winners = match.Winners;
        if (winners.Count == 1)
            sb.AppendLine($"<color=#{Hex(winners[0].Color)}>{winners[0].Name}</color> WINS!");
        else
            sb.AppendLine("DRAW! " + string.Join(" & ", winners.Select(w => $"<color=#{Hex(w.Color)}>{w.Name}</color>")));

        sb.Append("<size=26>");
        foreach (var p in match.Players.OrderByDescending(p => p.Score))
        {
            sb.Append($"\n<color=#{Hex(p.Color)}>{p.Name}</color>   {p.Score} pts   ({p.Knockouts} KOs, {p.Falls} falls)");
        }
        sb.Append("</size>");
        return sb.ToString();
    }

    private string Scoreboard()
    {
        sb.Clear();
        foreach (var p in match.Players)
        {
            sb.Append($"<color=#{Hex(p.Color)}><b>{p.Name}</b></color>  {p.Score,3} pts  ");
            if (p.Inventory != null)
            {
                for (int i = 0; i < PlayerInventory.SlotCount; i++)
                {
                    sb.Append($"[{SlotLabel(p.Inventory.GetSlot(i))}]");
                }
            }
            if (p.IsRespawning)
            {
                sb.Append($"  respawn {Mathf.CeilToInt(p.RespawnAt - Time.time)}");
            }
            else
            {
                if (p.Player.IsInvulnerable) sb.Append("  <color=#7fd7ff>INVULN</color>");
                if (p.Player.HasSuperKnockback) sb.Append("  <color=#ff9f43>SUPER KB</color>");
                if (p.Player.HasSpeedBoost) sb.Append("  <color=#9cff7a>SPEED</color>");
                if (p.Player.HasDash) sb.Append("  <color=#e6a8ff>DASH</color>");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private string Hint()
    {
        string controls = "WASD player: Space Jump | LShift Dash | Q Invuln | E Super KB      Arrows player: RCtrl | RShift | . | /      Gamepad: A | X | Y | B";
        switch (match.State)
        {
            case MatchManager.MatchState.Lobby:
            case MatchManager.MatchState.MatchOver:
                string join = "Join: SPACE (WASD)   RSHIFT (Arrows)   A (Gamepad)";
                string start = match.CanStart
                    ? "Press ENTER / START to " + (match.State == MatchManager.MatchState.Lobby ? "start" : "rematch")
                    : match.Players.Count < match.MinPlayersToStart
                        ? $"Need {match.MinPlayersToStart}+ players to start"
                        : "";
                return $"{join}\n{start}\n<size=16>{controls}</size>";
            default:
                return $"<size=16>{controls}</size>";
        }
    }

    private static string SlotLabel(PowerUpModifier powerUp)
    {
        switch (powerUp)
        {
            case null: return " - ";
            case DashPowerUp: return "Dash";
            case InvulnerablePowerUp: return "Invuln";
            case SuperKnockbackPowerUp: return "Super KB";
            default: return powerUp.name;
        }
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
        return $"{total / 60}:{total % 60:00}";
    }

    private static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

    // --- UI construction ---
    private void BuildCanvas()
    {
        var canvasGO = new GameObject("MatchHUD Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        timerText = CreateText(canvasGO.transform, font, "Timer", 56, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(400f, 80f));
        feedText = CreateText(canvasGO.transform, font, "Feed", 30, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(1200f, 50f));
        scoreboardText = CreateText(canvasGO.transform, font, "Scoreboard", 26, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(30f, -20f), new Vector2(800f, 220f));
        bannerText = CreateText(canvasGO.transform, font, "Banner", 96, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1400f, 600f));
        hintText = CreateText(canvasGO.transform, font, "Hint", 24, TextAnchor.LowerCenter,
            new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1600f, 140f));
    }

    private static Text CreateText(Transform parent, Font font, string name, int size, TextAnchor alignment,
        Vector2 anchor, Vector2 position, Vector2 dimensions)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        var outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);
        return text;
    }
}
