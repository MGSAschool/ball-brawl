using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OutOfBoundsLabels : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [SerializeField] private ArenaBounds bounds;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private float labelHeight = 1.6f;
    [SerializeField] private float labelFontSize = 34f;

    private readonly Dictionary<PlayerEntry, TextMeshProUGUI> labels = new Dictionary<PlayerEntry, TextMeshProUGUI>();
    private RectTransform canvasRect;

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        if (bounds == null) bounds = FindAnyObjectByType<ArenaBounds>();
        canvasRect = UIKit.MakeOverlayCanvas("Arena Bounds Canvas", transform, 95, referenceResolution, false);
    }

    private void LateUpdate()
    {
        if (match == null || bounds == null) return;
        if (targetCamera == null) targetCamera = Camera.main;

        foreach (PlayerEntry entry in match.Players)
        {
            float timeLeft;
            bool show = bounds.TryGetTimeLeft(entry, out timeLeft) && entry.Player != null;

            TextMeshProUGUI label;
            if (!labels.TryGetValue(entry, out label))
            {
                if (!show) continue;
                label = MakeLabel(entry);
                labels.Add(entry, label);
            }

            if (show) show = MoveLabel(entry, label, timeLeft);
            label.gameObject.SetActive(show);
        }
    }

    private bool MoveLabel(PlayerEntry entry, TextMeshProUGUI label, float timeLeft)
    {
        if (targetCamera == null) return false;

        Vector3 screen = targetCamera.WorldToScreenPoint(entry.Player.transform.position + Vector3.up * labelHeight);
        if (screen.z <= 0f) return false;

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out local);
        label.rectTransform.anchoredPosition = local;
        label.text = "GET BACK!\n<size=170%>" + Mathf.CeilToInt(timeLeft) + "</size>";

        float beat = 1f - (timeLeft % 1f);
        float urgency = 1f - Mathf.Clamp01(timeLeft / bounds.AllowedTimeOutside);
        label.rectTransform.localScale = Vector3.one * (1f + beat * beat * (0.1f + 0.25f * urgency));
        return true;
    }

    private TextMeshProUGUI MakeLabel(PlayerEntry entry)
    {
        TextMeshProUGUI label = UIKit.NewLabel(canvasRect, entry.Name + " Out Of Bounds", labelFontSize, entry.Color, 0.25f);
        label.rectTransform.sizeDelta = new Vector2(260f, 140f);
        label.rectTransform.pivot = new Vector2(0.5f, 0f);
        label.alignment = TextAlignmentOptions.Bottom;
        label.lineSpacing = -25f;
        label.gameObject.SetActive(false);
        return label;
    }
}
