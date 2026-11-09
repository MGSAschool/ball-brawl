using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Who's who, so players can find their ball in a pile-up:
//   tag  - floating label above each ball: "P1" in the player's colour, which controls they're on
//          ("WASD", "ARROWS", "GAMEPAD 2") and an arrow pointing down at the ball. Full size in the
//          lobby (pops in on join), the countdown, and for a few seconds after the start and each
//          respawn; the rest of the match it shrinks to just "P1" so it never hides the action.
//   ring - a glowing ring in the player's colour on the floor under the ball, always on, so
//          balls stay identifiable even when they're bunched up and the tags overlap.
public class PlayerTags : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [SerializeField] private ArenaBounds bounds;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [Tooltip("How high above the ball's center the arrow tip sits, in metres.")]
    [SerializeField] private float height = 1f;
    [Tooltip("During a match, tags show this long after the start and after each respawn.")]
    [SerializeField] private float matchShowTime = 3f;
    [SerializeField] private float fontSize = 36f;
    [Tooltip("Size of the compact in-match tag compared to the full one.")]
    [SerializeField] private float compactScale = 0.65f;

    [Header("Floor Ring")]
    [SerializeField] private float ringDiameter = 1.9f;
    [Tooltip("The ring only shows when there's floor within this distance under the ball.")]
    [SerializeField] private float ringMaxDrop = 6f;

    private class Tag
    {
        public RectTransform Root;
        public CanvasGroup Group;
        public TextMeshProUGUI Label;
        public float ShownAt;
        public float HideAt;
        public bool WasRespawning;
        public Transform Ring;
        public Material RingMaterial;
    }

    private static Texture2D ringTexture;

    private readonly Dictionary<MatchManager.PlayerEntry, Tag> tags = new Dictionary<MatchManager.PlayerEntry, Tag>();
    private readonly HashSet<MatchManager.PlayerEntry> seen = new HashSet<MatchManager.PlayerEntry>();
    private RectTransform canvasRect;
    private static Sprite arrowSprite;

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        if (bounds == null) bounds = FindAnyObjectByType<ArenaBounds>();
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

    // Remind everyone who's who as the match kicks off
    private void OnMatchStateChanged(MatchManager.MatchState state)
    {
        if (state != MatchManager.MatchState.Playing) return;
        foreach (var tag in tags.Values) tag.HideAt = Time.time + matchShowTime;
    }

    private void LateUpdate()
    {
        if (match == null) return;
        if (targetCamera == null) targetCamera = Camera.main;
        seen.Clear();

        int gamepads = 0;
        int totalGamepads = 0;
        foreach (var entry in match.Players) if (entry.ControlScheme == "Gamepad") totalGamepads++;

        foreach (var entry in match.Players)
        {
            seen.Add(entry);
            if (entry.ControlScheme == "Gamepad") gamepads++;

            if (!tags.TryGetValue(entry, out var tag))
            {
                tag = CreateTag(entry);
                tag.ShownAt = Time.time;
                tag.HideAt = Time.time + matchShowTime;
                tags.Add(entry, tag);
            }

            // Several pads share one scheme, so number them
            string scheme = ControlHints.SchemeLabel(entry.ControlScheme).ToUpperInvariant();
            if (entry.ControlScheme == "Gamepad" && totalGamepads > 1) scheme += $" {gamepads}";
            bool compact = IsCompact(tag);
            tag.Label.text = compact ? $"<size=140%>{entry.Name}</size>" : $"<size=140%>{entry.Name}</size>\n<size=85%>{scheme}</size>";

            UpdateTag(entry, tag, compact);
            UpdateRing(entry, tag);
        }

        foreach (var pair in tags)
        {
            if (seen.Contains(pair.Key)) continue;
            pair.Value.Root.gameObject.SetActive(false);
            if (pair.Value.Ring != null) pair.Value.Ring.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        foreach (var tag in tags.Values)
        {
            if (tag.RingMaterial != null) Destroy(tag.RingMaterial);
        }
    }

    // Mid-match, once the "here you are" moment has passed
    private bool IsCompact(Tag tag) => match.State == MatchManager.MatchState.Playing && Time.time >= tag.HideAt;

    private void UpdateRing(MatchManager.PlayerEntry entry, Tag tag)
    {
        if (tag.Ring == null) CreateRing(entry, tag);

        bool show = entry.Player != null && !entry.IsRespawning && match.State != MatchManager.MatchState.MatchOver;
        if (show)
        {
            // The ray starts inside the ball, so it never hits the ball itself
            Vector3 position = entry.Player.transform.position;
            show = Physics.Raycast(position, Vector3.down, out var hit, ringMaxDrop, ~0, QueryTriggerInteraction.Ignore);
            if (show)
            {
                tag.Ring.position = hit.point + hit.normal * 0.03f;
                tag.Ring.rotation = Quaternion.LookRotation(-hit.normal) ;
                // Shrinks as the ball rises (jumps), like a shadow, and pulses gently
                float height = Mathf.Clamp01((position.y - hit.point.y - 0.5f) / 3f);
                float pulse = 1f + 0.06f * Mathf.Sin(Time.time * 5f);
                tag.Ring.localScale = Vector3.one * (ringDiameter * Mathf.Lerp(1f, 0.6f, height) * pulse);
            }
        }
        tag.Ring.gameObject.SetActive(show);
    }

    private void CreateRing(MatchManager.PlayerEntry entry, Tag tag)
    {
        var ring = GameObject.CreatePrimitive(PrimitiveType.Quad);
        ring.name = $"{entry.Name} Floor Ring";
        Destroy(ring.GetComponent<Collider>());
        ring.transform.SetParent(transform, false);
        var renderer = ring.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        var material = ArtUtil.TransparentLit(new Color(entry.Color.r, entry.Color.g, entry.Color.b, 0.95f), 1.2f);
        material.SetTexture("_BaseMap", RingTexture());
        renderer.sharedMaterial = material;
        tag.Ring = ring.transform;
        tag.RingMaterial = material;
    }

    // Bright outer ring with a faint fill, so it reads on any floor colour
    private static Texture2D RingTexture()
    {
        if (ringTexture != null) return ringTexture;
        const int size = 128;
        ringTexture = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float edge = Mathf.Clamp01((1f - d) * size * 0.5f);        // soft outer edge
                float band = Mathf.Clamp01((d - 0.72f) * size * 0.5f);     // inner edge of the band
                float alpha = edge * Mathf.Max(band, 0.18f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }
        ringTexture.SetPixels32(pixels);
        ringTexture.Apply();
        return ringTexture;
    }

    private void UpdateTag(MatchManager.PlayerEntry entry, Tag tag, bool compact)
    {
        bool alive = entry.Player != null && !entry.IsRespawning;

        // Coming back from a fall: pop the tag again so they can find themselves
        if (tag.WasRespawning && alive)
        {
            tag.ShownAt = Time.time;
            tag.HideAt = Time.time + matchShowTime;
        }
        tag.WasRespawning = entry.IsRespawning;

        float targetAlpha = 0f;
        if (alive)
        {
            switch (match.State)
            {
                case MatchManager.MatchState.Lobby:
                case MatchManager.MatchState.Countdown:
                    targetAlpha = 1f;
                    break;
                case MatchManager.MatchState.Playing:
                    // Never fully gone mid-match: the compact "P1" stays so a clash stays readable
                    targetAlpha = 1f;
                    break;
            }
            // The out-of-bounds countdown uses the same spot and matters more
            if (bounds != null && bounds.IsWarning(entry)) targetAlpha = 0f;
        }

        bool onScreen = false;
        if (alive && targetCamera != null)
        {
            Vector3 screen = targetCamera.WorldToScreenPoint(entry.Player.transform.position + Vector3.up * height);
            if (screen.z > 0f)
            {
                onScreen = true;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local);
                tag.Root.anchoredPosition = local;
            }
        }

        tag.Group.alpha = Mathf.MoveTowards(tag.Group.alpha, onScreen ? targetAlpha : 0f, Time.unscaledDeltaTime * 4f);
        tag.Root.gameObject.SetActive(tag.Group.alpha > 0.001f);

        // Springy pop when the tag (re)appears, plus a gentle bob so it reads as "this is you"
        float since = Time.time - tag.ShownAt;
        float pop = since < 0.5f ? 1f + 0.5f * Mathf.Sin(since / 0.5f * Mathf.PI) * (1f - since / 0.5f) : 1f;
        tag.Root.localScale = Vector3.one * (pop * (compact ? compactScale : 1f));
        tag.Root.anchoredPosition += Vector2.up * (Mathf.Sin(Time.unscaledTime * 4f) * 4f);
    }

    // --- UI construction ---
    private void BuildCanvas()
    {
        var canvasGO = new GameObject("Player Tags Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Under the HUD (100) and off-screen badges (90)
        canvas.sortingOrder = 85;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        canvasRect = (RectTransform)canvasGO.transform;
    }

    private Tag CreateTag(MatchManager.PlayerEntry entry)
    {
        // Pivot at the arrow tip, so the root sits right above the ball
        var root = new GameObject($"{entry.Name} Tag", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
        root.SetParent(canvasRect, false);
        root.sizeDelta = new Vector2(240f, 130f);
        root.pivot = new Vector2(0.5f, 0f);
        var group = root.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        var arrow = new GameObject("Arrow", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        arrow.rectTransform.SetParent(root, false);
        arrow.rectTransform.anchorMin = arrow.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        arrow.rectTransform.pivot = new Vector2(0.5f, 0f);
        arrow.rectTransform.sizeDelta = new Vector2(28f, 20f);
        arrow.sprite = ArrowSprite();
        arrow.color = entry.Color;
        arrow.raycastTarget = false;
        var arrowOutline = arrow.gameObject.AddComponent<Outline>();
        arrowOutline.effectColor = new Color(0.17f, 0.10f, 0.29f);
        arrowOutline.effectDistance = new Vector2(2f, -2f);

        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        label.rectTransform.SetParent(root, false);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(0f, 22f);
        label.rectTransform.offsetMax = Vector2.zero;
        label.alignment = TextAlignmentOptions.Bottom;
        label.fontSize = fontSize;
        var art = GameArt.Instance;
        if (art != null && art.font != null) label.font = art.font;
        else label.fontStyle = FontStyles.Bold;
        label.color = entry.Color;
        label.outlineWidth = 0.28f;
        label.outlineColor = new Color(0.17f, 0.10f, 0.29f);
        label.lineSpacing = -30f;
        label.raycastTarget = false;

        root.gameObject.SetActive(false);
        return new Tag { Root = root, Group = group, Label = label };
    }

    // Triangle pointing down
    private static Sprite ArrowSprite()
    {
        if (arrowSprite != null) return arrowSprite;
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Full width at the top, narrowing to a point at the bottom
                float v = (y + 0.5f) / size;
                float u = Mathf.Abs((x + 0.5f) / size - 0.5f) * 2f;
                float edge = v - u;
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
