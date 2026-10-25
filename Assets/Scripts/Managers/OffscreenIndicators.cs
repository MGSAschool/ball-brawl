using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// When a ball leaves the screen (knocked far away, falling, or the camera can't zoom out
// any more), shows a badge in that player's colour at the screen edge with an arrow pointing
// toward it and how far away it is. Builds its own overlay canvas; scales like the HUD.
public class OffscreenIndicators : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [Tooltip("How far in from the screen edge badges sit, in reference pixels.")]
    [SerializeField] private float edgeMargin = 70f;
    [SerializeField] private float badgeSize = 64f;
    [Tooltip("A ball this close to the edge (fraction of the screen) already counts as off-screen.")]
    [SerializeField] private float offscreenBorder = 0.02f;

    private class Indicator
    {
        public RectTransform Root;
        public RectTransform Arrow;
        public TextMeshProUGUI Label;
    }

    private readonly Dictionary<MatchManager.PlayerEntry, Indicator> indicators = new Dictionary<MatchManager.PlayerEntry, Indicator>();
    private RectTransform canvasRect;
    private Canvas canvas;
    private static Sprite circleSprite;
    private static Sprite arrowSprite;

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        BuildCanvas();
    }

    private void LateUpdate()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (match == null || targetCamera == null) return;

        foreach (var entry in match.Players)
        {
            if (!indicators.TryGetValue(entry, out var indicator))
            {
                indicator = CreateIndicator(entry);
                indicators.Add(entry, indicator);
            }
            UpdateIndicator(entry, indicator);
        }
    }

    private void UpdateIndicator(MatchManager.PlayerEntry entry, Indicator indicator)
    {
        bool show = false;
        if (entry.Player != null && !entry.IsRespawning)
        {
            Vector3 world = entry.Player.transform.position;
            Vector3 viewport = targetCamera.WorldToViewportPoint(world);
            bool behind = viewport.z < 0f;
            float b = offscreenBorder;
            show = behind || viewport.x < b || viewport.x > 1f - b || viewport.y < b || viewport.y > 1f - b;

            if (show)
            {
                // Direction from the screen center toward the ball (flipped if it's behind the camera)
                Vector2 dir = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);
                if (behind) dir = -dir;
                if (dir.sqrMagnitude < 1e-6f) dir = Vector2.down;
                PlaceOnEdge(indicator, dir);
                indicator.Label.text = $"{entry.Name}\n<size=70%>{Mathf.RoundToInt(DistanceOffScreen(world))}m</size>";
            }
        }
        indicator.Root.gameObject.SetActive(show);
    }

    // Pins the badge where the line from the screen center toward `dir` meets the inset edge
    private void PlaceOnEdge(Indicator indicator, Vector2 dir)
    {
        Rect safe = Screen.safeArea;
        float margin = edgeMargin * (canvas != null ? canvas.scaleFactor : 1f);
        Vector2 center = safe.center;
        Vector2 half = new Vector2(Mathf.Max(1f, safe.width * 0.5f - margin), Mathf.Max(1f, safe.height * 0.5f - margin));

        // Stretch to screen pixels so the angle matches what players see
        Vector2 pixelDir = new Vector2(dir.x * Screen.width, dir.y * Screen.height).normalized;
        float scale = Mathf.Min(
            Mathf.Abs(pixelDir.x) > 1e-5f ? half.x / Mathf.Abs(pixelDir.x) : float.PositiveInfinity,
            Mathf.Abs(pixelDir.y) > 1e-5f ? half.y / Mathf.Abs(pixelDir.y) : float.PositiveInfinity);
        Vector2 screenPoint = center + pixelDir * scale;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out var local);
        indicator.Root.anchoredPosition = local;
        indicator.Arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(pixelDir.y, pixelDir.x) * Mathf.Rad2Deg);
    }

    // Rough metres between the ball and the nearest visible point, so players can see how far out they are
    private float DistanceOffScreen(Vector3 world)
    {
        Vector3 viewport = targetCamera.WorldToViewportPoint(world);
        Vector3 clamped = new Vector3(Mathf.Clamp01(viewport.x), Mathf.Clamp01(viewport.y), Mathf.Max(viewport.z, targetCamera.nearClipPlane));
        return Vector3.Distance(world, targetCamera.ViewportToWorldPoint(clamped));
    }

    // --- UI construction ---
    private void BuildCanvas()
    {
        var canvasGO = new GameObject("Offscreen Indicators Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Under the HUD (100) so the scoreboard and banners stay on top
        canvas.sortingOrder = 90;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        canvasRect = (RectTransform)canvasGO.transform;
    }

    private Indicator CreateIndicator(MatchManager.PlayerEntry entry)
    {
        var root = new GameObject($"{entry.Name} Indicator", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(canvasRect, false);
        root.sizeDelta = Vector2.one * badgeSize;

        // Arrow sits on a pivot at the badge center and points outward
        var arrowPivot = new GameObject("Arrow Pivot", typeof(RectTransform)).GetComponent<RectTransform>();
        arrowPivot.SetParent(root, false);
        arrowPivot.sizeDelta = Vector2.zero;
        var arrow = new GameObject("Arrow", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        arrow.SetParent(arrowPivot, false);
        arrow.sizeDelta = new Vector2(badgeSize * 0.5f, badgeSize * 0.6f);
        arrow.anchoredPosition = new Vector2(badgeSize * 0.68f, 0f);
        var arrowImage = arrow.GetComponent<Image>();
        arrowImage.sprite = ArrowSprite();
        arrowImage.color = entry.Color;

        var badge = new GameObject("Badge", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        badge.rectTransform.SetParent(root, false);
        badge.rectTransform.sizeDelta = Vector2.one * badgeSize;
        badge.sprite = CircleSprite();
        badge.color = entry.Color;

        var ring = new GameObject("Outline", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        ring.rectTransform.SetParent(badge.rectTransform, false);
        ring.rectTransform.sizeDelta = Vector2.one * (badgeSize * 0.82f);
        ring.sprite = CircleSprite();
        ring.color = new Color(0f, 0f, 0f, 0.55f);

        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        label.rectTransform.SetParent(badge.rectTransform, false);
        label.rectTransform.sizeDelta = Vector2.one * badgeSize;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = badgeSize * 0.3f;
        label.fontStyle = FontStyles.Bold;
        label.lineSpacing = -20f;
        label.color = Color.white;
        label.raycastTarget = false;

        root.gameObject.SetActive(false);
        return new Indicator { Root = root, Arrow = arrowPivot, Label = label };
    }

    private static Sprite CircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                byte a = (byte)(Mathf.Clamp01(r - d) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return circleSprite;
    }

    // Triangle pointing right (+X), so rotating it by the direction angle aims it
    private static Sprite ArrowSprite()
    {
        if (arrowSprite != null) return arrowSprite;
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Inside when |y - mid| shrinks to 0 as x goes from the base to the tip
                float u = (x + 0.5f) / size;
                float v = Mathf.Abs((y + 0.5f) / size - 0.5f) * 2f;
                float edge = (1f - u) - v;
                byte a = (byte)(Mathf.Clamp01(edge * size * 0.5f) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        arrowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return arrowSprite;
    }
}
