using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDModeCards : HUDPanel
{
    public const float CardHeight = 130f;
    public const float ColumnWidth = 360f;

    private class ModeCard
    {
        public GameMode Mode;
        public Image Background;
        public Outline Outline;
        public TextMeshProUGUI Text;
    }

    private readonly List<ModeCard> cards = new List<ModeCard>();
    private GameObject column;

    protected override void BuildPanel()
    {
        column = UIKit.NewRect("Modes", Root).gameObject;
        Vector2 leftMiddle = new Vector2(0f, 0.5f);
        UIKit.PlaceAt((RectTransform)column.transform, leftMiddle, new Vector2(hud.CornerMargin + 12f, 0f), new Vector2(ColumnWidth, CardHeight * GameModeInfo.Count));

        float rowHeight = 1f / GameModeInfo.Count;
        for (int i = 0; i < GameModeInfo.Count; i++)
        {
            GameMode mode = (GameMode)i;
            RectTransform slot = UIKit.NewRect(GameModeInfo.CardTitle(mode) + " Mode", column.transform);
            UIKit.Stretch(slot, Rect.MinMaxRect(0f, 1f - (i + 1) * rowHeight + 0.02f, 1f, 1f - i * rowHeight - 0.02f));
            cards.Add(MakeCard(slot, mode));
        }

        MakeTitleLabel();
    }

    private void MakeTitleLabel()
    {
        if (Art == null || Art.modeLabel == null) return;

        float columnHeight = CardHeight * GameModeInfo.Count;
        float labelWidth = 0.8f;
        float artAspect = Art.modeLabel.rect.width * HUDMenuButtons.ButtonArtArea.width / (Art.modeLabel.rect.height * HUDMenuButtons.ButtonArtArea.height);
        float labelHeight = ColumnWidth * labelWidth / artAspect / columnHeight;
        float gap = 10f / columnHeight;

        Image label = UIKit.NewImage("Game Mode Label", column.transform);
        label.sprite = Art.modeLabel;
        label.preserveAspect = true;
        Rect area = Rect.MinMaxRect(0.5f - labelWidth / 2f, 1f + gap, 0.5f + labelWidth / 2f, 1f + gap + labelHeight);
        UIKit.StretchToArt(label.rectTransform, area, HUDMenuButtons.ButtonArtArea);
    }

    private void Update()
    {
        if (hud == null || Match == null) return;

        if (hud.InMenu) CheckModeKeys();

        column.SetActive(Match.State == MatchState.Lobby && !hud.GuideOpen);
        if (column.activeSelf) RefreshCards();
        FadeIfBallBehind(column);
    }

    private void CheckModeKeys()
    {
        GameMode picked;
        if (MenuInput.TryGetModeKey(out picked))
        {
            Match.SetMode(picked);
            return;
        }

        int step = MenuInput.ModeStep();
        if (step != 0) Match.SetMode(GameModeInfo.Step(Match.Mode, step));
    }

    private ModeCard MakeCard(RectTransform slot, GameMode mode)
    {
        Image background = UIKit.NewImage("Card", slot);
        background.raycastTarget = true;
        UIKit.Stretch(background.rectTransform, Rect.MinMaxRect(0f, 0f, 1f, 1f));
        Outline outline = UIKit.AddOutline(background.gameObject, Color.white, 4f);

        Button button = background.gameObject.AddComponent<Button>();
        Navigation noNavigation = new Navigation();
        noNavigation.mode = Navigation.Mode.None;
        button.navigation = noNavigation;
        button.onClick.AddListener(() => Match.SetMode(mode));

        ModeCard card = new ModeCard();
        card.Mode = mode;
        card.Background = background;
        card.Outline = outline;
        card.Text = UIKit.NewAutoText(background.rectTransform, "Text", 12f, 34f, TextAlignmentOptions.Center, 8f);
        return card;
    }

    private void RefreshCards()
    {
        for (int i = 0; i < cards.Count; i++)
        {
            ModeCard card = cards[i];
            Color modeColor = GameModeInfo.CardColor(card.Mode);
            bool selected = Match.Mode == card.Mode;

            string titleHex = UIKit.Hex(modeColor);
            string blurbHex = HUDStyle.SoftTextHex;
            if (selected)
            {
                card.Background.color = Color.Lerp(HUDStyle.Panel, modeColor, 0.55f);
                card.Outline.effectColor = HUDStyle.Gold;
                titleHex = HUDStyle.GoldHex;
                blurbHex = "FFFFFF";
            }
            else
            {
                card.Background.color = HUDStyle.Panel;
                card.Outline.effectColor = new Color(modeColor.r, modeColor.g, modeColor.b, 0.6f);
            }

            string key = ControlHints.Cap((i + 1).ToString());
            string title = UIKit.Colored(GameModeInfo.CardTitle(card.Mode), titleHex) + "  <size=65%>" + key + "</size>";
            string blurb = "<size=65%>" + UIKit.Colored(GameModeInfo.Blurb(card.Mode), blurbHex) + "</size>";
            card.Text.text = title + "\n" + blurb;
        }
    }
}
