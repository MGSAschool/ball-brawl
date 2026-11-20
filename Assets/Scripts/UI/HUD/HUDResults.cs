using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDResults : HUDPanel
{
    private static readonly Rect FrameInside = Rect.MinMaxRect(0.15f, 0.11f, 0.87f, 0.80f);
    private static readonly Rect RowRank = Rect.MinMaxRect(0.03f, 0.1f, 0.25f, 0.9f);
    private static readonly Rect RowName = Rect.MinMaxRect(0.28f, 0.15f, 0.58f, 0.85f);
    private static readonly Rect RowStar = Rect.MinMaxRect(0.60f, 0.22f, 0.72f, 0.78f);
    private static readonly Rect RowPoints = Rect.MinMaxRect(0.74f, 0.15f, 0.97f, 0.85f);

    private static readonly Color RowColor = new Color(0.45f, 0.24f, 0.10f, 0.95f);
    private static readonly Color RowEdgeColor = new Color(0.28f, 0.13f, 0.05f);

    private GameObject panel;
    private RectTransform plates;
    private TextMeshProUGUI tipText;
    private bool platesBuilt;

    protected override void BuildPanel()
    {
        bool framed = Art != null && Art.scoreboardFrame != null;
        float height = 600f;
        float width = 760f;
        if (framed) width = height * Art.scoreboardFrame.rect.width / Art.scoreboardFrame.rect.height;

        Vector2 center = new Vector2(0.5f, 0.5f);
        panel = UIKit.NewPanel("Results", Root, center, center, center, new Vector2(0f, 30f), new Vector2(width, height));
        if (framed)
        {
            Image background = panel.GetComponent<Image>();
            background.sprite = Art.scoreboardFrame;
            background.color = Color.white;
        }

        Rect inside = Rect.MinMaxRect(0.03f, 0.03f, 0.97f, 0.97f);
        if (framed) inside = FrameInside;
        float tipHeight = inside.height * 0.16f;

        plates = UIKit.NewRect("Plates", panel.transform);
        UIKit.Stretch(plates, Rect.MinMaxRect(inside.xMin, inside.yMin + tipHeight, inside.xMax, inside.yMax));

        tipText = UIKit.NewAutoText(panel.transform, "Tip", 12f, 24f, TextAlignmentOptions.Center, 0f);
        UIKit.Stretch(tipText.rectTransform, Rect.MinMaxRect(inside.xMin + 0.02f, inside.yMin, inside.xMax - 0.02f, inside.yMin + tipHeight));
        panel.SetActive(false);
    }

    private void Update()
    {
        if (hud == null || Match == null) return;

        bool over = Match.State == MatchState.MatchOver;
        if (!over) platesBuilt = false;

        bool show = over && !hud.GuideOpen;
        panel.SetActive(show);
        if (!show) return;

        if (!platesBuilt) BuildPlates();
        tipText.text = UIKit.Colored("TIP:", HUDStyle.FrameTitleHex) + " " + hud.CurrentTip();
    }

    private void BuildPlates()
    {
        platesBuilt = true;
        for (int i = plates.childCount - 1; i >= 0; i--)
        {
            Destroy(plates.GetChild(i).gameObject);
        }

        List<PlayerEntry> ranked = RankByScore();
        int columns = 1;
        if (ranked.Count > 2) columns = 2;
        int rows = Mathf.Max(1, Mathf.CeilToInt(ranked.Count / (float)columns));

        for (int i = 0; i < ranked.Count; i++)
        {
            int col = i % columns;
            int row = i / columns;
            Rect cell = Rect.MinMaxRect(col / (float)columns, 1f - (row + 1) / (float)rows, (col + 1) / (float)columns, 1f - row / (float)rows);
            MakePlate(ranked[i], i + 1, UIKit.Inset(cell, 0.03f));
        }
    }

    private List<PlayerEntry> RankByScore()
    {
        List<PlayerEntry> ranked = new List<PlayerEntry>();
        foreach (PlayerEntry entry in Match.Players)
        {
            int index = ranked.Count;
            while (index > 0 && ranked[index - 1].Score < entry.Score) index--;
            ranked.Insert(index, entry);
        }
        return ranked;
    }

    private void MakePlate(PlayerEntry entry, int rank, Rect cell)
    {
        Image row = UIKit.NewImage(entry.Name + " Row", plates);
        UIKit.Stretch(row.rectTransform, UIKit.Inset(cell, 0.04f));
        row.color = RowColor;
        UIKit.AddOutline(row.gameObject, RowEdgeColor, 3f);

        Image badge = UIKit.NewImage("Rank", row.rectTransform);
        UIKit.Stretch(badge.rectTransform, RowRank);
        badge.sprite = TextureMaker.Circle();
        badge.color = entry.Color;
        badge.preserveAspect = true;
        TextMeshProUGUI rankText = UIKit.NewAutoText(badge.rectTransform, "Rank", 14f, 50f, TextAlignmentOptions.Center, 0f);
        rankText.text = RankName(rank);

        TextMeshProUGUI nameText = UIKit.NewAutoText(row.rectTransform, "Name", 14f, 56f, TextAlignmentOptions.Left, 0f);
        UIKit.Stretch(nameText.rectTransform, RowName);
        nameText.text = UIKit.Colored(entry.Name, entry.Color);

        Sprite pointsIcon = null;
        if (Art != null) pointsIcon = Art.pointsIcon;
        UIKit.NewIcon(row.rectTransform, pointsIcon, RowStar);

        TextMeshProUGUI pointsText = UIKit.NewAutoText(row.rectTransform, "Points", 14f, 56f, TextAlignmentOptions.Left, 0f);
        UIKit.Stretch(pointsText.rectTransform, RowPoints);
        pointsText.text = entry.Score.ToString();
    }

    private string RankName(int rank)
    {
        if (rank == 1) return "1st";
        if (rank == 2) return "2nd";
        if (rank == 3) return "3rd";
        return rank + "th";
    }
}
