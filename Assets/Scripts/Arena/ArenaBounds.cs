using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Stops players from camping off the platforms (on the stadium stands, a ledge, etc.).
// A ball that isn't over the arena or the late-match island gets a countdown above it;
// if it's still out when the countdown runs out it counts as falling off, so whoever
// knocked it there still gets the knock-off. Put this next to FallingPlatforms.
public class ArenaBounds : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [SerializeField] private FallingPlatforms arena;

    [Header("Rules")]
    [Tooltip("Seconds a player may stay off the platforms before counting as fallen.")]
    [SerializeField] private float allowedTimeOutside = 3f;
    [Tooltip("The countdown only shows after this long outside, so normal jumps between platforms don't flash it.")]
    [SerializeField] private float warningDelay = 0.4f;
    [Tooltip("A ball this far past the arena edge still counts as on it (about the ball's radius).")]
    [SerializeField] private float edgeTolerance = 0.75f;
    [Tooltip("How far below a ball to look for platform floor.")]
    [SerializeField] private float floorCheckDistance = 30f;

    [Header("Countdown Label")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private float labelHeight = 1.6f;
    [SerializeField] private float labelFontSize = 34f;

    private readonly Dictionary<MatchManager.PlayerEntry, float> timeOutside = new Dictionary<MatchManager.PlayerEntry, float>();
    private readonly Dictionary<MatchManager.PlayerEntry, TextMeshProUGUI> labels = new Dictionary<MatchManager.PlayerEntry, TextMeshProUGUI>();
    private readonly HashSet<MatchManager.PlayerEntry> seen = new HashSet<MatchManager.PlayerEntry>();
    private RectTransform canvasRect;

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        if (arena == null) arena = FindAnyObjectByType<FallingPlatforms>();
        BuildCanvas();
    }

    private void Update()
    {
        if (match == null || arena == null) return;
        if (targetCamera == null) targetCamera = Camera.main;

        // Lobby counts too, so nobody parks on the stands while waiting; only matches keep score
        bool checking = match.State == MatchManager.MatchState.Playing || match.State == MatchManager.MatchState.Lobby;
        seen.Clear();

        foreach (var entry in match.Players)
        {
            seen.Add(entry);
            float timeLeft = -1f;
            bool showLabel = false;

            if (checking && entry.Player != null && !entry.IsRespawning && entry.Player.gameObject.activeInHierarchy)
            {
                if (IsOnPlatforms(entry.Player.transform.position))
                {
                    timeOutside.Remove(entry);
                }
                else
                {
                    timeOutside.TryGetValue(entry, out float outside);
                    outside += Time.deltaTime;
                    timeLeft = allowedTimeOutside - outside;

                    if (timeLeft <= 0f)
                    {
                        timeOutside.Remove(entry);
                        match.HandlePlayerFell(entry.Player);
                    }
                    else
                    {
                        timeOutside[entry] = outside;
                        showLabel = outside >= warningDelay;
                    }
                }
            }
            else
            {
                timeOutside.Remove(entry);
            }

            UpdateLabel(entry, showLabel, timeLeft);
        }

        // Players who left the game
        foreach (var pair in labels)
        {
            if (!seen.Contains(pair.Key) && pair.Value != null) pair.Value.gameObject.SetActive(false);
        }
    }

    // True while this player's "GET BACK!" countdown is on screen
    public bool IsWarning(MatchManager.PlayerEntry entry) =>
        timeOutside.TryGetValue(entry, out float outside) && outside >= warningDelay;

    // On the arena footprint (holes there are the death zone's job) or above any platform piece
    private bool IsOnPlatforms(Vector3 position)
    {
        Vector3 offset = position - arena.Center;
        offset.y = 0f;
        if (offset.magnitude <= arena.Radius + edgeTolerance) return true;

        // The ray starts inside the ball, so it never hits the ball itself
        if (Physics.Raycast(position, Vector3.down, out var hit, floorCheckDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<ArenaSection>() != null;
        }
        return false;
    }

    // --- Countdown label ---
    private void UpdateLabel(MatchManager.PlayerEntry entry, bool show, float timeLeft)
    {
        if (!labels.TryGetValue(entry, out var label) || label == null)
        {
            if (!show) return;
            label = CreateLabel(entry);
            labels[entry] = label;
        }

        if (show && targetCamera != null)
        {
            Vector3 screen = targetCamera.WorldToScreenPoint(entry.Player.transform.position + Vector3.up * labelHeight);
            if (screen.z > 0f)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local);
                label.rectTransform.anchoredPosition = local;
                label.text = $"GET BACK!\n<size=170%>{Mathf.CeilToInt(timeLeft)}</size>";
                // Throb once per second, harder as time runs out
                float beat = 1f - (timeLeft % 1f);
                float urgency = 1f - Mathf.Clamp01(timeLeft / allowedTimeOutside);
                label.rectTransform.localScale = Vector3.one * (1f + beat * beat * (0.1f + 0.25f * urgency));
            }
            else
            {
                show = false;
            }
        }

        label.gameObject.SetActive(show);
    }

    private void BuildCanvas()
    {
        var canvasGO = new GameObject("Arena Bounds Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Above the off-screen badges (90), under the HUD (100)
        canvas.sortingOrder = 95;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        canvasRect = (RectTransform)canvasGO.transform;
    }

    private TextMeshProUGUI CreateLabel(MatchManager.PlayerEntry entry)
    {
        var label = new GameObject($"{entry.Name} Out Of Bounds", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        label.rectTransform.SetParent(canvasRect, false);
        label.rectTransform.sizeDelta = new Vector2(260f, 140f);
        label.rectTransform.pivot = new Vector2(0.5f, 0f);
        label.alignment = TextAlignmentOptions.Bottom;
        label.fontSize = labelFontSize;
        var art = GameArt.Instance;
        if (art != null && art.font != null) label.font = art.font;
        else label.fontStyle = FontStyles.Bold;
        label.color = entry.Color;
        label.outlineWidth = 0.25f;
        label.outlineColor = new Color(0.17f, 0.10f, 0.29f);
        label.lineSpacing = -25f;
        label.raycastTarget = false;
        label.gameObject.SetActive(false);
        return label;
    }
}
