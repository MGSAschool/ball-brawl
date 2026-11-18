using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDResults : HUDPanel
{
    private static readonly Rect FrameInside = Rect.MinMaxRect(0.13f, 0.12f, 0.87f, 0.79f);
    private static readonly Rect PlateCircle = Rect.MinMaxRect(0.10f, 0.19f, 0.40f, 0.82f);
    private static readonly Rect PlateName = Rect.MinMaxRect(0.45f, 0.56f, 0.90f, 0.84f);
    private static readonly Rect PlateBar = Rect.MinMaxRect(0.45f, 0.26f, 0.90f, 0.545f);

    private GameObject panel;
    private RectTransform plates;
    private TextMeshProUGUI tipText;
    private bool platesBuilt;

    protected override void BuildPanel()
    {
        bool framed = Art != null && Art.scoreboardFrame != null;
        float height = 470f;
        float width = 760f;
        if (framed) width = height * Art.scoreboardFrame.rect.width / Art.scoreboardFrame.rect.height;

        Vector2 center = new Vector2(0.5f, 0.5f);
        panel = UIKit.NewPanel("Results", Root, center, center, center, Vector2.zero, new Vector2(width, height));
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
        tipText.text = UIKit.Colored("TIP:", HUDStyle.GoldHex) + " " + hud.CurrentTip();
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
        RectTransform slot = UIKit.NewRect(entry.Name + " Slot", plates);
        UIKit.Stretch(slot, cell);

        Image plate = UIKit.NewImage(entry.Name + " Plate", slot);
        if (Art != null && Art.namePlate != null)
        {
            plate.sprite = Art.namePlate;
            AspectRatioFitter fitter = plate.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = Art.namePlate.rect.width / Art.namePlate.rect.height;
        }
        else
        {
            UIKit.Stretch(plate.rectTransform, Rect.MinMaxRect(0f, 0f, 1f, 1f));
            plate.color = HUDStyle.Panel;
        }

        Image badge = UIKit.NewImage("Rank", plate.rectTransform);
        UIKit.Stretch(badge.rectTransform, UIKit.Inset(PlateCircle, 0.1f));
        badge.sprite = TextureMaker.Circle();
        badge.color = entry.Color;
        badge.preserveAspect = true;
        TextMeshProUGUI rankText = UIKit.NewAutoText(badge.rectTransform, "Rank", 14f, 60f, TextAlignmentOptions.Center, 0f);
        rankText.text = RankName(rank);

        TextMeshProUGUI nameText = UIKit.NewAutoText(plate.rectTransform, "Name", 12f, 40f, TextAlignmentOptions.Left, 0f);
        UIKit.Stretch(nameText.rectTransform, PlateName);
        nameText.text = UIKit.Colored(entry.Name, entry.Color) + "  <size=70%>" + entry.Knockouts + " KO</size>";

        Sprite pointsIcon = null;
        if (Art != null) pointsIcon = Art.pointsIcon;
        UIKit.NewIcon(plate.rectTransform, pointsIcon, Rect.MinMaxRect(PlateBar.xMin + 0.02f, PlateBar.yMin + 0.03f, PlateBar.xMin + 0.12f, PlateBar.yMax - 0.03f));

        TextMeshProUGUI pointsText = UIKit.NewAutoText(plate.rectTransform, "Points", 12f, 40f, TextAlignmentOptions.Left, 0f);
        UIKit.Stretch(pointsText.rectTransform, Rect.MinMaxRect(PlateBar.xMin + 0.14f, PlateBar.yMin, PlateBar.xMax - 0.02f, PlateBar.yMax));
        pointsText.text = entry.Score + " pts";
    }

    private string RankName(int rank)
    {
        if (rank == 1) return "1st";
        if (rank == 2) return "2nd";
        if (rank == 3) return "3rd";
        return rank + "th";
    }
}
