using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMarkers : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [SerializeField] private ArenaBounds bounds;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);

    [Header("Name Tags")]
    [SerializeField] private float tagHeight = 1f;
    [SerializeField] private float tagMatchShowTime = 3f;
    [SerializeField] private float tagFontSize = 30f;
    [SerializeField] private float tagCompactScale = 0.65f;

    [Header("Floor Rings")]
    [SerializeField] private float ringDiameter = 1.9f;
    [SerializeField] private float ringMaxDrop = 6f;

    [Header("Offscreen Arrows")]
    [SerializeField] private float arrowEdgeMargin = 70f;
    [SerializeField] private float arrowBadgeSize = 64f;
    [SerializeField] private float offscreenBorder = 0.02f;

    [Header("Out Of Bounds Countdown")]
    [SerializeField] private float countdownHeight = 1.6f;
    [SerializeField] private float countdownFontSize = 34f;

    private class Tag
    {
        public RectTransform Root;
        public CanvasGroup Group;
        public TextMeshProUGUI Label;
        public float ShownAt;
        public float BigUntil;
        public bool WasRespawning;
    }

    private class Arrow
    {
        public RectTransform Root;
        public RectTransform ArrowPivot;
        public TextMeshProUGUI Label;
    }

    private readonly Dictionary<PlayerEntry, Tag> tags = new Dictionary<PlayerEntry, Tag>();
    private readonly Dictionary<PlayerEntry, Transform> rings = new Dictionary<PlayerEntry, Transform>();
    private readonly Dictionary<PlayerEntry, Arrow> arrows = new Dictionary<PlayerEntry, Arrow>();
    private readonly Dictionary<PlayerEntry, TextMeshProUGUI> countdowns = new Dictionary<PlayerEntry, TextMeshProUGUI>();
    private readonly List<Material> ringMaterials = new List<Material>();

    private RectTransform tagCanvas;
    private RectTransform arrowCanvasRect;
    private Canvas arrowCanvas;
    private RectTransform countdownCanvas;

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        if (bounds == null) bounds = FindAnyObjectByType<ArenaBounds>();

        tagCanvas = UIKit.MakeOverlayCanvas("Player Tags Canvas", transform, 85, referenceResolution, false);
        arrowCanvasRect = UIKit.MakeOverlayCanvas("Offscreen Indicators Canvas", transform, 90, referenceResolution, false);
        arrowCanvas = arrowCanvasRect.GetComponent<Canvas>();
        countdownCanvas = UIKit.MakeOverlayCanvas("Arena Bounds Canvas", transform, 95, referenceResolution, false);
    }

    private void OnEnable()
    {
        if (match != null) match.StateChanged += OnMatchStateChanged;
    }

    private void OnDisable()
    {
        if (match != null) match.StateChanged -= OnMatchStateChanged;
    }

    private void OnDestroy()
    {
        foreach (Material material in ringMaterials)
        {
            if (material != null) Destroy(material);
        }
    }

    private void OnMatchStateChanged(MatchState state)
    {
        if (state != MatchState.Playing) return;
        foreach (Tag tag in tags.Values)
        {
            tag.BigUntil = Time.time + tagMatchShowTime;
        }
    }

    private void LateUpdate()
    {
        if (match == null) return;
        if (targetCamera == null) targetCamera = Camera.main;

        UpdateTags();
        UpdateRings();
        UpdateArrows();
        UpdateCountdowns();
    }

    private void UpdateTags()
    {
        int gamepadCount = 0;
        foreach (PlayerEntry entry in match.Players)
        {
            if (entry.ControlScheme == "Gamepad") gamepadCount++;
        }

        int gamepadNumber = 0;
        foreach (PlayerEntry entry in match.Players)
        {
            if (entry.ControlScheme == "Gamepad") gamepadNumber++;

            Tag tag;
            if (!tags.TryGetValue(entry, out tag))
            {
                tag = MakeTag(entry);
                tags.Add(entry, tag);
            }

            string scheme = ControlHints.SchemeLabel(entry.ControlScheme).ToUpperInvariant();
            if (entry.ControlScheme == "Gamepad" && gamepadCount > 1) scheme += " " + gamepadNumber;

            bool compact = match.State == MatchState.Playing && Time.time >= tag.BigUntil;
            if (compact) tag.Label.text = "<size=140%>" + entry.Name + "</size>";
            else tag.Label.text = "<size=140%>" + entry.Name + "</size>\n<size=85%>" + scheme + "</size>";

            MoveTag(entry, tag, compact);
        }
    }

    private void MoveTag(PlayerEntry entry, Tag tag, bool compact)
    {
        bool alive = entry.Player != null && !entry.IsRespawning;
        if (tag.WasRespawning && alive)
        {
            tag.ShownAt = Time.time;
            tag.BigUntil = Time.time + tagMatchShowTime;
        }
        tag.WasRespawning = entry.IsRespawning;

        float targetAlpha = 0f;
        if (alive && match.State != MatchState.MatchOver) targetAlpha = 1f;
        if (alive && bounds != null && bounds.IsWarning(entry)) targetAlpha = 0f;

        bool onScreen = false;
        if (alive && targetCamera != null)
        {
            Vector3 screen = targetCamera.WorldToScreenPoint(entry.Player.transform.position + Vector3.up * tagHeight);
            if (screen.z > 0f)
            {
                onScreen = true;
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(tagCanvas, screen, null, out local);
                tag.Root.anchoredPosition = local;
            }
        }
        if (!onScreen) targetAlpha = 0f;

        tag.Group.alpha = Mathf.MoveTowards(tag.Group.alpha, targetAlpha, Time.unscaledDeltaTime * 4f);
        tag.Root.gameObject.SetActive(tag.Group.alpha > 0.001f);

        float since = Time.time - tag.ShownAt;
        float pop = 1f;
        if (since < 0.5f) pop = 1f + 0.5f * Mathf.Sin(since / 0.5f * Mathf.PI) * (1f - since / 0.5f);
        float size = pop;
        if (compact) size *= tagCompactScale;
        tag.Root.localScale = Vector3.one * size;
        tag.Root.anchoredPosition += Vector2.up * (Mathf.Sin(Time.unscaledTime * 4f) * 4f);
    }

    private Tag MakeTag(PlayerEntry entry)
    {
        RectTransform root = new GameObject(entry.Name + " Tag", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
        root.SetParent(tagCanvas, false);
        root.sizeDelta = new Vector2(240f, 130f);
        root.pivot = new Vector2(0.5f, 0f);

        CanvasGroup group = root.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        Image arrow = UIKit.NewImage("Arrow", root);
        arrow.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        arrow.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        arrow.rectTransform.pivot = new Vector2(0.5f, 0f);
        arrow.rectTransform.sizeDelta = new Vector2(28f, 20f);
        arrow.sprite = TextureMaker.DownArrow();
        arrow.color = entry.Color;
        UIKit.AddOutline(arrow.gameObject, HUDStyle.Outline, 2f);

        TextMeshProUGUI label = UIKit.NewLabel(root, "Label", tagFontSize, entry.Color, 0.28f);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(0f, 22f);
        label.rectTransform.offsetMax = Vector2.zero;
        label.alignment = TextAlignmentOptions.Bottom;
        label.lineSpacing = -30f;

        root.gameObject.SetActive(false);

        Tag tag = new Tag();
        tag.Root = root;
        tag.Group = group;
        tag.Label = label;
        tag.ShownAt = Time.time;
        tag.BigUntil = Time.time + tagMatchShowTime;
        return tag;
    }

    private void UpdateRings()
    {
        foreach (PlayerEntry entry in match.Players)
        {
            Transform ring;
            if (!rings.TryGetValue(entry, out ring))
            {
                ring = MakeRing(entry);
                rings.Add(entry, ring);
            }
            MoveRing(entry, ring);
        }
    }

    private void MoveRing(PlayerEntry entry, Transform ring)
    {
        bool show = entry.Player != null && !entry.IsRespawning && match.State != MatchState.MatchOver;
        if (show)
        {
            Vector3 position = entry.Player.transform.position;
            RaycastHit hit;
            show = Physics.Raycast(position, Vector3.down, out hit, ringMaxDrop, ~0, QueryTriggerInteraction.Ignore);
            if (show)
            {
                ring.position = hit.point + hit.normal * 0.03f;
                ring.rotation = Quaternion.LookRotation(-hit.normal);

                float jumpHeight = Mathf.Clamp01((position.y - hit.point.y - 0.5f) / 3f);
                float pulse = 1f + 0.06f * Mathf.Sin(Time.time * 5f);
                ring.localScale = Vector3.one * (ringDiameter * Mathf.Lerp(1f, 0.6f, jumpHeight) * pulse);
            }
        }
        ring.gameObject.SetActive(show);
    }

    private Transform MakeRing(PlayerEntry entry)
    {
        Color color = new Color(entry.Color.r, entry.Color.g, entry.Color.b, 0.95f);
        Material material = ArtUtil.TransparentLit(color, 1.2f);
        material.SetTexture("_BaseMap", TextureMaker.FloorRing());
        ringMaterials.Add(material);

        Transform ring = ArtUtil.MakeShape(PrimitiveType.Quad, entry.Name + " Floor Ring", transform, material);
        ring.GetComponent<MeshRenderer>().receiveShadows = false;
        return ring;
    }

    private void UpdateArrows()
    {
        if (targetCamera == null) return;

        foreach (PlayerEntry entry in match.Players)
        {
            Arrow arrow;
            if (!arrows.TryGetValue(entry, out arrow))
            {
                arrow = MakeArrow(entry);
                arrows.Add(entry, arrow);
            }
            MoveArrow(entry, arrow);
        }
    }

    private void MoveArrow(PlayerEntry entry, Arrow arrow)
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
                Vector2 direction = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);
                if (behind) direction = -direction;
                if (direction.sqrMagnitude < 1e-6f) direction = Vector2.down;

                PinToEdge(arrow, direction);
                int meters = Mathf.RoundToInt(DistanceOffScreen(world));
                arrow.Label.text = entry.Name + "\n<size=70%>" + meters + "m</size>";
            }
        }
        arrow.Root.gameObject.SetActive(show);
    }

    private void PinToEdge(Arrow arrow, Vector2 direction)
    {
        Rect safe = Screen.safeArea;
        float margin = arrowEdgeMargin * arrowCanvas.scaleFactor;
        Vector2 half = new Vector2(Mathf.Max(1f, safe.width * 0.5f - margin), Mathf.Max(1f, safe.height * 0.5f - margin));

        Vector2 pixelDirection = new Vector2(direction.x * Screen.width, direction.y * Screen.height).normalized;
        float scaleX = float.PositiveInfinity;
        float scaleY = float.PositiveInfinity;
        if (Mathf.Abs(pixelDirection.x) > 1e-5f) scaleX = half.x / Mathf.Abs(pixelDirection.x);
        if (Mathf.Abs(pixelDirection.y) > 1e-5f) scaleY = half.y / Mathf.Abs(pixelDirection.y);
        Vector2 screenPoint = safe.center + pixelDirection * Mathf.Min(scaleX, scaleY);

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(arrowCanvasRect, screenPoint, null, out local);
        arrow.Root.anchoredPosition = local;

        float angle = Mathf.Atan2(pixelDirection.y, pixelDirection.x) * Mathf.Rad2Deg;
        arrow.ArrowPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private float DistanceOffScreen(Vector3 world)
    {
        Vector3 viewport = targetCamera.WorldToViewportPoint(world);
        Vector3 clamped = new Vector3(Mathf.Clamp01(viewport.x), Mathf.Clamp01(viewport.y), Mathf.Max(viewport.z, targetCamera.nearClipPlane));
        return Vector3.Distance(world, targetCamera.ViewportToWorldPoint(clamped));
    }

    private Arrow MakeArrow(PlayerEntry entry)
    {
        RectTransform root = UIKit.NewRect(entry.Name + " Indicator", arrowCanvasRect);
        root.sizeDelta = Vector2.one * arrowBadgeSize;

        RectTransform arrowPivot = UIKit.NewRect("Arrow Pivot", root);
        arrowPivot.sizeDelta = Vector2.zero;
        Image arrowImage = UIKit.NewImage("Arrow", arrowPivot);
        arrowImage.rectTransform.sizeDelta = new Vector2(arrowBadgeSize * 0.5f, arrowBadgeSize * 0.6f);
        arrowImage.rectTransform.anchoredPosition = new Vector2(arrowBadgeSize * 0.68f, 0f);
        arrowImage.sprite = TextureMaker.RightArrow();
        arrowImage.color = entry.Color;

        Image badge = UIKit.NewImage("Badge", root);
        badge.rectTransform.sizeDelta = Vector2.one * arrowBadgeSize;
        badge.sprite = TextureMaker.Circle();
        badge.color = entry.Color;

        Image inner = UIKit.NewImage("Outline", badge.rectTransform);
        inner.rectTransform.sizeDelta = Vector2.one * (arrowBadgeSize * 0.82f);
        inner.sprite = TextureMaker.Circle();
        inner.color = new Color(0f, 0f, 0f, 0.55f);

        TextMeshProUGUI label = UIKit.NewLabel(badge.rectTransform, "Label", arrowBadgeSize * 0.3f, Color.white, 0.2f);
        label.rectTransform.sizeDelta = Vector2.one * arrowBadgeSize;
        label.alignment = TextAlignmentOptions.Center;
        label.lineSpacing = -20f;

        root.gameObject.SetActive(false);

        Arrow arrow = new Arrow();
        arrow.Root = root;
        arrow.ArrowPivot = arrowPivot;
        arrow.Label = label;
        return arrow;
    }

    private void UpdateCountdowns()
    {
        if (bounds == null) return;

        foreach (PlayerEntry entry in match.Players)
        {
            float timeLeft;
            bool show = bounds.TryGetTimeLeft(entry, out timeLeft) && entry.Player != null;

            TextMeshProUGUI label;
            if (!countdowns.TryGetValue(entry, out label))
            {
                if (!show) continue;
                label = MakeCountdown(entry);
                countdowns.Add(entry, label);
            }

            if (show) show = MoveCountdown(entry, label, timeLeft);
            label.gameObject.SetActive(show);
        }
    }

    private bool MoveCountdown(PlayerEntry entry, TextMeshProUGUI label, float timeLeft)
    {
        if (targetCamera == null) return false;

        Vector3 screen = targetCamera.WorldToScreenPoint(entry.Player.transform.position + Vector3.up * countdownHeight);
        if (screen.z <= 0f) return false;

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(countdownCanvas, screen, null, out local);
        label.rectTransform.anchoredPosition = local;
        label.text = "GET BACK!\n<size=170%>" + Mathf.CeilToInt(timeLeft) + "</size>";

        float beat = 1f - (timeLeft % 1f);
        float urgency = 1f - Mathf.Clamp01(timeLeft / bounds.AllowedTimeOutside);
        label.rectTransform.localScale = Vector3.one * (1f + beat * beat * (0.1f + 0.25f * urgency));
        return true;
    }

    private TextMeshProUGUI MakeCountdown(PlayerEntry entry)
    {
        TextMeshProUGUI label = UIKit.NewLabel(countdownCanvas, entry.Name + " Out Of Bounds", countdownFontSize, entry.Color, 0.25f);
        label.rectTransform.sizeDelta = new Vector2(260f, 140f);
        label.rectTransform.pivot = new Vector2(0.5f, 0f);
        label.alignment = TextAlignmentOptions.Bottom;
        label.lineSpacing = -25f;
        label.gameObject.SetActive(false);
        return label;
    }
}
