using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerTags : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [SerializeField] private ArenaBounds bounds;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private float height = 1f;
    [SerializeField] private float matchShowTime = 3f;
    [SerializeField] private float fontSize = 30f;
    [SerializeField] private float compactScale = 0.65f;

    private class Tag
    {
        public RectTransform Root;
        public CanvasGroup Group;
        public TextMeshProUGUI Label;
        public float ShownAt;
        public float BigUntil;
        public bool WasRespawning;
    }

    private readonly Dictionary<PlayerEntry, Tag> tags = new Dictionary<PlayerEntry, Tag>();
    private RectTransform canvasRect;

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        if (bounds == null) bounds = FindAnyObjectByType<ArenaBounds>();
        canvasRect = UIKit.MakeOverlayCanvas("Player Tags Canvas", transform, 85, referenceResolution, false);
    }

    private void OnEnable()
    {
        if (match != null) match.StateChanged += OnMatchStateChanged;
    }

    private void OnDisable()
    {
        if (match != null) match.StateChanged -= OnMatchStateChanged;
    }

    private void OnMatchStateChanged(MatchState state)
    {
        if (state != MatchState.Playing) return;
        foreach (Tag tag in tags.Values)
        {
            tag.BigUntil = Time.time + matchShowTime;
        }
    }

    private void LateUpdate()
    {
        if (match == null) return;
        if (targetCamera == null) targetCamera = Camera.main;

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
            tag.BigUntil = Time.time + matchShowTime;
        }
        tag.WasRespawning = entry.IsRespawning;

        float targetAlpha = 0f;
        if (alive && match.State != MatchState.MatchOver) targetAlpha = 1f;
        if (alive && bounds != null && bounds.IsWarning(entry)) targetAlpha = 0f;

        bool onScreen = false;
        if (alive && targetCamera != null)
        {
            Vector3 screen = targetCamera.WorldToScreenPoint(entry.Player.transform.position + Vector3.up * height);
            if (screen.z > 0f)
            {
                onScreen = true;
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out local);
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
        if (compact) size *= compactScale;
        tag.Root.localScale = Vector3.one * size;
        tag.Root.anchoredPosition += Vector2.up * (Mathf.Sin(Time.unscaledTime * 4f) * 4f);
    }

    private Tag MakeTag(PlayerEntry entry)
    {
        RectTransform root = new GameObject(entry.Name + " Tag", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
        root.SetParent(canvasRect, false);
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

        TextMeshProUGUI label = UIKit.NewLabel(root, "Label", fontSize, entry.Color, 0.28f);
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
        tag.BigUntil = Time.time + matchShowTime;
        return tag;
    }
}
