using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OffscreenIndicators : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private float edgeMargin = 70f;
    [SerializeField] private float badgeSize = 64f;
    [SerializeField] private float offscreenBorder = 0.02f;

    private class Indicator
    {
        public RectTransform Root;
        public RectTransform ArrowPivot;
        public TextMeshProUGUI Label;
    }

    private readonly Dictionary<PlayerEntry, Indicator> indicators = new Dictionary<PlayerEntry, Indicator>();
    private RectTransform canvasRect;
    private Canvas canvas;

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        canvasRect = UIKit.MakeOverlayCanvas("Offscreen Indicators Canvas", transform, 90, referenceResolution, false);
        canvas = canvasRect.GetComponent<Canvas>();
    }

    private void LateUpdate()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (match == null || targetCamera == null) return;

        foreach (PlayerEntry entry in match.Players)
        {
            Indicator indicator;
            if (!indicators.TryGetValue(entry, out indicator))
            {
                indicator = MakeIndicator(entry);
                indicators.Add(entry, indicator);
            }
            MoveIndicator(entry, indicator);
        }
    }

    private void MoveIndicator(PlayerEntry entry, Indicator indicator)
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

                PinToEdge(indicator, direction);
                int meters = Mathf.RoundToInt(DistanceOffScreen(world));
                indicator.Label.text = entry.Name + "\n<size=70%>" + meters + "m</size>";
            }
        }
        indicator.Root.gameObject.SetActive(show);
    }

    private void PinToEdge(Indicator indicator, Vector2 direction)
    {
        Rect safe = Screen.safeArea;
        float margin = edgeMargin * canvas.scaleFactor;
        Vector2 half = new Vector2(Mathf.Max(1f, safe.width * 0.5f - margin), Mathf.Max(1f, safe.height * 0.5f - margin));

        Vector2 pixelDirection = new Vector2(direction.x * Screen.width, direction.y * Screen.height).normalized;
        float scaleX = float.PositiveInfinity;
        float scaleY = float.PositiveInfinity;
        if (Mathf.Abs(pixelDirection.x) > 1e-5f) scaleX = half.x / Mathf.Abs(pixelDirection.x);
        if (Mathf.Abs(pixelDirection.y) > 1e-5f) scaleY = half.y / Mathf.Abs(pixelDirection.y);
        Vector2 screenPoint = safe.center + pixelDirection * Mathf.Min(scaleX, scaleY);

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out local);
        indicator.Root.anchoredPosition = local;

        float angle = Mathf.Atan2(pixelDirection.y, pixelDirection.x) * Mathf.Rad2Deg;
        indicator.ArrowPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private float DistanceOffScreen(Vector3 world)
    {
        Vector3 viewport = targetCamera.WorldToViewportPoint(world);
        Vector3 clamped = new Vector3(Mathf.Clamp01(viewport.x), Mathf.Clamp01(viewport.y), Mathf.Max(viewport.z, targetCamera.nearClipPlane));
        return Vector3.Distance(world, targetCamera.ViewportToWorldPoint(clamped));
    }

    private Indicator MakeIndicator(PlayerEntry entry)
    {
        RectTransform root = UIKit.NewRect(entry.Name + " Indicator", canvasRect);
        root.sizeDelta = Vector2.one * badgeSize;

        RectTransform arrowPivot = UIKit.NewRect("Arrow Pivot", root);
        arrowPivot.sizeDelta = Vector2.zero;
        Image arrow = UIKit.NewImage("Arrow", arrowPivot);
        arrow.rectTransform.sizeDelta = new Vector2(badgeSize * 0.5f, badgeSize * 0.6f);
        arrow.rectTransform.anchoredPosition = new Vector2(badgeSize * 0.68f, 0f);
        arrow.sprite = TextureMaker.RightArrow();
        arrow.color = entry.Color;

        Image badge = UIKit.NewImage("Badge", root);
        badge.rectTransform.sizeDelta = Vector2.one * badgeSize;
        badge.sprite = TextureMaker.Circle();
        badge.color = entry.Color;

        Image inner = UIKit.NewImage("Outline", badge.rectTransform);
        inner.rectTransform.sizeDelta = Vector2.one * (badgeSize * 0.82f);
        inner.sprite = TextureMaker.Circle();
        inner.color = new Color(0f, 0f, 0f, 0.55f);

        TextMeshProUGUI label = UIKit.NewLabel(badge.rectTransform, "Label", badgeSize * 0.3f, Color.white, 0.2f);
        label.rectTransform.sizeDelta = Vector2.one * badgeSize;
        label.alignment = TextAlignmentOptions.Center;
        label.lineSpacing = -20f;

        root.gameObject.SetActive(false);

        Indicator indicator = new Indicator();
        indicator.Root = root;
        indicator.ArrowPivot = arrowPivot;
        indicator.Label = label;
        return indicator;
    }
}
