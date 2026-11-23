using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HUDMenuButtons : HUDPanel
{
    public static readonly Rect ButtonArtArea = Rect.MinMaxRect(0.094f, 0.311f, 0.906f, 0.693f);
    private static readonly Rect LogoArtArea = Rect.MinMaxRect(0.019f, 0.167f, 0.988f, 0.826f);
    private static readonly Rect WholeArt = Rect.MinMaxRect(0f, 0f, 1f, 1f);
    private static readonly Rect GuideArtArea = Rect.MinMaxRect(0.092f, 0.108f, 0.957f, 0.928f);
    private static readonly Rect MenuIconArtArea = Rect.MinMaxRect(0.195f, 0.163f, 0.831f, 0.805f);
    private const float PlayHeight = 110f;
    private const float QuitHeight = 84f;
    private const float GuideHeight = 72f;
    private const float ButtonGap = 28f;

    private GameObject logo;
    private GameObject menu;
    private Image playImage;

    protected override void BuildPanel()
    {
        BuildLogo();

        menu = UIKit.NewRect("Menu", Root).gameObject;
        RectTransform menuRect = (RectTransform)menu.transform;
        Vector2 bottomCenter = new Vector2(0.5f, 0f);
        UIKit.PlaceAt(menuRect, bottomCenter, new Vector2(0f, 165f), new Vector2(1000f, 110f));

        Sprite guideSprite = null;
        Sprite playSprite = null;
        Sprite leaveSprite = null;
        if (Art != null)
        {
            guideSprite = Art.howToPlayButton;
            playSprite = Art.playButton;
            leaveSprite = Art.leaveButton;
        }

        float playWidth = ArtWidth(playSprite, MenuIconArtArea, PlayHeight);
        float guideWidth = ArtWidth(guideSprite, GuideArtArea, GuideHeight);
        float quitWidth = ArtWidth(leaveSprite, MenuIconArtArea, QuitHeight);

        float guideX = -(playWidth * 0.5f + ButtonGap + guideWidth * 0.5f);
        float quitX = playWidth * 0.5f + ButtonGap + quitWidth * 0.5f;

        playImage = MakeButton("Play", playSprite, MenuIconArtArea, 0f, playWidth, PlayHeight, OnPlayClicked);
        MakeButton("How To Play", guideSprite, GuideArtArea, guideX, guideWidth, GuideHeight, hud.ToggleGuide);
        MakeButton("Quit", leaveSprite, MenuIconArtArea, quitX, quitWidth, QuitHeight, MenuInput.QuitGame);

        TextMeshProUGUI guideLabel = UIKit.NewAutoText(menu.transform, "Guide Label", 10f, 18f, TextAlignmentOptions.Center, 0f);
        Vector2 middle = new Vector2(0.5f, 0.5f);
        Vector2 labelPosition = new Vector2(guideX, -(GuideHeight * 0.5f + 12f));
        UIKit.PlaceAt(guideLabel.rectTransform, middle, labelPosition, new Vector2(guideWidth + 60f, 22f));
        guideLabel.text = "GUIDE <size=80%>(H)";
    }

    private float ArtWidth(Sprite sprite, Rect artArea, float height)
    {
        if (sprite == null) return height * 3f;

        float visibleWidth = sprite.rect.width * artArea.width;
        float visibleHeight = sprite.rect.height * artArea.height;
        return height * visibleWidth / visibleHeight;
    }

    public override void Tick()
    {
        if (hud == null || Match == null) return;

        if (hud.InMenu && MenuInput.LeavePressed()) MenuInput.QuitGame();

        bool lobby = Match.State == MatchState.Lobby;
        logo.SetActive(lobby && !hud.GuideOpen);
        menu.SetActive(hud.InMenu && !hud.GuideOpen);

        if (menu.activeSelf)
        {
            if (Match.CanStart) playImage.color = Color.white;
            else playImage.color = new Color(0.55f, 0.55f, 0.55f, 0.8f);
        }

        FadeIfBallBehind(menu);
    }

    private void OnPlayClicked()
    {
        Sfx.Play(Sound.Click);
        if (!Match.TryStartMatch())
        {
            Match.Announce("Need " + Match.MinPlayersToStart + "+ players to start - join with SPACE, RSHIFT or A");
        }
    }

    private void BuildLogo()
    {
        RectTransform logoArea = UIKit.NewRect("Logo", Root);
        logo = logoArea.gameObject;
        UIKit.PlaceAt(logoArea, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(340f, 190f));

        Image logoImage = UIKit.NewImage("Art", logoArea);
        if (Art != null && Art.logo != null)
        {
            logoImage.sprite = Art.logo;
            logoImage.preserveAspect = true;
            UIKit.StretchToArt(logoImage.rectTransform, WholeArt, LogoArtArea);
        }
        else
        {
            logoImage.enabled = false;
        }
    }

    private Image MakeButton(string name, Sprite sprite, Rect artArea, float centerX, float width, float height, UnityAction onClick)
    {
        RectTransform root = UIKit.NewRect(name, menu.transform);
        UIKit.PlaceAt(root, new Vector2(0.5f, 0.5f), new Vector2(centerX, 0f), new Vector2(width, height));

        Image clickArea = UIKit.NewImage("Button", root);
        clickArea.raycastTarget = true;
        clickArea.color = Color.clear;
        UIKit.Stretch(clickArea.rectTransform, WholeArt);

        Image image = UIKit.NewImage("Art", clickArea.rectTransform);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.preserveAspect = true;
            UIKit.StretchToArt(image.rectTransform, WholeArt, artArea);
        }
        else
        {
            UIKit.Stretch(image.rectTransform, WholeArt);
            image.color = HUDStyle.Panel;
        }

        Button button = clickArea.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Navigation noNavigation = new Navigation();
        noNavigation.mode = Navigation.Mode.None;
        button.navigation = noNavigation;
        button.onClick.AddListener(onClick);

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.95f, 0.8f);
        colors.pressedColor = new Color(0.8f, 0.75f, 0.65f);
        button.colors = colors;
        return image;
    }
}

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

    public override void Tick()
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

public class HUDJoinCards : HUDPanel
{
    private readonly List<Image> cards = new List<Image>();
    private readonly List<TextMeshProUGUI> bodies = new List<TextMeshProUGUI>();
    private GameObject column;

    protected override void BuildPanel()
    {
        string[] schemes = ControlHints.Schemes;
        column = UIKit.NewRect("Join", Root).gameObject;
        Vector2 rightMiddle = new Vector2(1f, 0.5f);
        UIKit.PlaceAt((RectTransform)column.transform, rightMiddle, new Vector2(-hud.CornerMargin - 12f, 0f),
            new Vector2(HUDModeCards.ColumnWidth, HUDModeCards.CardHeight * schemes.Length));

        float rowHeight = 1f / schemes.Length;
        for (int i = 0; i < schemes.Length; i++)
        {
            string scheme = schemes[i];
            string label = ControlHints.SchemeLabel(scheme).ToUpper();
            Color schemeColor = ControlHints.SchemeColor(scheme);

            Image card = UIKit.NewImage(label + " Join", column.transform);
            UIKit.Stretch(card.rectTransform, Rect.MinMaxRect(0f, 1f - (i + 1) * rowHeight + 0.02f, 1f, 1f - i * rowHeight - 0.02f));
            card.color = HUDStyle.Panel;
            UIKit.AddOutline(card.gameObject, schemeColor, 4f);

            Image header = UIKit.NewImage("Header", card.rectTransform);
            UIKit.Stretch(header.rectTransform, Rect.MinMaxRect(0f, 0.68f, 1f, 1f));
            header.color = schemeColor;
            TextMeshProUGUI title = UIKit.NewAutoText(header.rectTransform, "Scheme", 14f, 34f, TextAlignmentOptions.Center, 2f);
            title.text = label;

            TextMeshProUGUI body = UIKit.NewAutoText(card.rectTransform, "Body", 12f, 30f, TextAlignmentOptions.Center, 0f);
            UIKit.Stretch(body.rectTransform, Rect.MinMaxRect(0.04f, 0.06f, 0.96f, 0.66f));

            cards.Add(card);
            bodies.Add(body);
        }
    }

    public override void Tick()
    {
        if (hud == null || Match == null) return;

        column.SetActive(Match.State == MatchState.Lobby && !hud.GuideOpen);
        if (column.activeSelf) RefreshCards();
        FadeIfBallBehind(column);
    }

    private void RefreshCards()
    {
        string[] schemes = ControlHints.Schemes;
        for (int i = 0; i < schemes.Length; i++)
        {
            string scheme = schemes[i];
            bool isGamepad = scheme == "Gamepad";
            string names = JoinedNames(scheme);
            bool anyJoined = names.Length > 0;
            string powerKey = ControlHints.CapFor(scheme, ControlHints.Action.UsePowerUp);
            string powerLine = "\n<size=72%>" + UIKit.Colored("Power-up", HUDStyle.SoftTextHex) + " " + powerKey + "</size>";

            string text;
            if (anyJoined && (!isGamepad || LobbyFull))
            {
                text = names + " JOINED" + powerLine;
            }
            else if (LobbyFull)
            {
                text = UIKit.Colored("LOBBY FULL", "9a93b8");
            }
            else
            {
                string joinKey = ControlHints.Cap(ControlHints.JoinKey(scheme));
                if (isGamepad) text = "Press " + joinKey + " on each pad";
                else text = "Press " + joinKey + " to join";

                if (isGamepad && anyJoined)
                {
                    text += "\n<size=72%>" + names + " " + UIKit.Colored("joined - Power-up", HUDStyle.SoftTextHex) + " " + powerKey + "</size>";
                }
                else
                {
                    text += powerLine;
                }
            }
            bodies[i].text = text;

            bool used = (anyJoined && !isGamepad) || LobbyFull;
            if (used) cards[i].color = new Color(HUDStyle.Panel.r * 0.75f, HUDStyle.Panel.g * 0.75f, HUDStyle.Panel.b * 0.75f, HUDStyle.Panel.a);
            else cards[i].color = HUDStyle.Panel;
        }
    }

    private string JoinedNames(string scheme)
    {
        string names = "";
        foreach (PlayerEntry entry in Match.Players)
        {
            if (entry.ControlScheme != scheme) continue;
            if (names.Length > 0) names += " ";
            names += UIKit.Colored(entry.Name, entry.Color);
        }
        return names;
    }
}

public class HUDGuide : HUDPanel
{
    private static readonly Rect FrameInside = Rect.MinMaxRect(0.14f, 0.10f, 0.88f, 0.80f);
    private const float LabelColumn = 0.25f;
    private const float HeaderHeight = 0.18f;
    private const float FrameSize = 760f;
    private const float FrameGap = 30f;

    private GameObject panel;
    private TextMeshProUGUI goalText;
    private string labelHex = HUDStyle.GoldHex;

    protected override void BuildPanel()
    {
        Vector2 center = new Vector2(0.5f, 0.5f);
        panel = UIKit.NewPanel("Guide Panel", Root, center, center, center, new Vector2(0f, -30f), new Vector2(FrameSize * 2f + FrameGap, FrameSize));

        Image clickArea = panel.GetComponent<Image>();
        clickArea.raycastTarget = true;
        clickArea.color = Color.clear;

        Button closeButton = panel.AddComponent<Button>();
        closeButton.transition = Selectable.Transition.None;
        Navigation noNavigation = new Navigation();
        noNavigation.mode = Navigation.Mode.None;
        closeButton.navigation = noNavigation;
        closeButton.onClick.AddListener(hud.ToggleGuide);

        Sprite howToPlaySprite = null;
        Sprite controlsSprite = null;
        if (Art != null)
        {
            howToPlaySprite = Art.howToPlayFrame;
            controlsSprite = Art.controlsFrame;
        }
        if (howToPlaySprite != null && controlsSprite != null) labelHex = HUDStyle.FrameTitleHex;

        float half = FrameSize / (FrameSize * 2f + FrameGap);
        RectTransform howToPlay = MakeFrame("How To Play", howToPlaySprite, Rect.MinMaxRect(0f, 0f, half, 1f));
        RectTransform controls = MakeFrame("Controls", controlsSprite, Rect.MinMaxRect(1f - half, 0f, 1f, 1f));

        goalText = UIKit.NewAutoText(howToPlay, "Goal", 14f, 34f, TextAlignmentOptions.Center, 0f);
        UIKit.Stretch(goalText.rectTransform, Rect.MinMaxRect(0f, 0.5f, 1f, 1f));
        BuildFooter(howToPlay);

        BuildControlsGrid(controls);
        panel.SetActive(false);
    }

    private RectTransform MakeFrame(string name, Sprite sprite, Rect anchors)
    {
        Image frame = UIKit.NewImage(name, panel.transform);
        UIKit.Stretch(frame.rectTransform, anchors);

        RectTransform content = UIKit.NewRect("Content", frame.transform);
        if (sprite != null)
        {
            frame.sprite = sprite;
            UIKit.Stretch(content, FrameInside);
        }
        else
        {
            frame.color = HUDStyle.Panel;
            UIKit.Stretch(content, Rect.MinMaxRect(0.04f, 0.04f, 0.96f, 0.96f));
        }
        content.offsetMin = new Vector2(16f, 16f);
        content.offsetMax = new Vector2(-16f, -16f);
        return content;
    }

    public override void Tick()
    {
        if (hud == null || Match == null) return;

        if (hud.InMenu && MenuInput.GuidePressed()) hud.ToggleGuide();

        panel.SetActive(hud.GuideOpen);
        if (hud.GuideOpen) goalText.text = GoalText();
    }

    private string GoalText()
    {
        string plusOne = UIKit.Colored("+1", HUDStyle.GoodHex);
        string minusOne = UIKit.Colored("-1", HUDStyle.BadHex);
        string mode = UIKit.Colored(GameModeInfo.Name(Match.Mode), labelHex) + "   ";

        if (Match.Mode == GameMode.StaphIt)
        {
            return mode + "On " + UIKit.Colored("RED", HUDStyle.BadHex) + ", be inside the glowing dome.\n" +
                   "<size=80%>" + plusOne + " in the dome     " + minusOne + " caught outside     " + plusOne + " knock-off</size>";
        }
        if (Match.Mode == GameMode.BombaClutch)
        {
            return mode + "Roll into bombs to set them off - the blast throws you too.\n" +
                   "<size=80%>" + plusOne + " per bomb set off     " + minusOne + " falling off on your own</size>";
        }
        return mode + "Knock rivals off the arena.\n" +
               "<size=80%>" + plusOne + " knock-off     " + UIKit.Colored("+1", labelHex) + " coin     " + minusOne + " falling off on your own</size>";
    }

    private void BuildControlsGrid(RectTransform content)
    {
        RectTransform grid = UIKit.NewRect("Controls", content);
        UIKit.Stretch(grid, Rect.MinMaxRect(0f, 0f, 1f, 1f));

        string[] schemes = ControlHints.Schemes;
        float column = (1f - LabelColumn) / schemes.Length;
        for (int c = 0; c < schemes.Length; c++)
        {
            Image header = UIKit.NewImage(ControlHints.SchemeLabel(schemes[c]) + " Header", grid);
            UIKit.Stretch(header.rectTransform, Rect.MinMaxRect(LabelColumn + c * column + 0.01f, 1f - HeaderHeight, LabelColumn + (c + 1) * column - 0.01f, 1f));
            header.color = ControlHints.SchemeColor(schemes[c]);

            TextMeshProUGUI headerText = UIKit.NewAutoText(header.rectTransform, "Label", 12f, 30f, TextAlignmentOptions.Center, 2f);
            headerText.text = ControlHints.SchemeLabel(schemes[c]).ToUpper();
            headerText.color = HUDStyle.Outline;
            headerText.outlineWidth = 0f;
        }

        AddControlRow(grid, 0, ControlHints.Action.Move, "MOVE", "");
        AddControlRow(grid, 1, ControlHints.Action.Jump, "JUMP", "");
        AddControlRow(grid, 2, ControlHints.Action.UsePowerUp, "POWER-UP", "uses the left one");
        AddControlRow(grid, 3, ControlHints.Action.Dash, "DASH", "with Dash power-up");
    }

    private void AddControlRow(RectTransform grid, int row, ControlHints.Action action, string label, string note)
    {
        int rowCount = 4;
        float rowHeight = (1f - HeaderHeight) / rowCount;
        float top = 1f - HeaderHeight - row * rowHeight;
        string[] schemes = ControlHints.Schemes;
        float column = (1f - LabelColumn) / schemes.Length;

        if (row % 2 == 0)
        {
            Image stripe = UIKit.NewImage("Stripe", grid);
            stripe.rectTransform.SetAsFirstSibling();
            UIKit.Stretch(stripe.rectTransform, Rect.MinMaxRect(0f, top - rowHeight, 1f, top));
            stripe.color = new Color(1f, 1f, 1f, 0.06f);
        }

        TextMeshProUGUI labelText = UIKit.NewAutoText(grid, label, 12f, 28f, TextAlignmentOptions.Left, 0f);
        UIKit.Stretch(labelText.rectTransform, Rect.MinMaxRect(0.015f, top - rowHeight, LabelColumn, top));
        labelText.text = UIKit.Colored(label, labelHex);
        if (note.Length > 0) labelText.text += "\n<size=60%>" + UIKit.Colored(note, HUDStyle.SoftTextHex) + "</size>";
        labelText.lineSpacing = -20f;

        for (int c = 0; c < schemes.Length; c++)
        {
            TextMeshProUGUI cell = UIKit.NewAutoText(grid, ControlHints.SchemeLabel(schemes[c]) + " " + label, 12f, 26f, TextAlignmentOptions.Center, 0f);
            UIKit.Stretch(cell.rectTransform, Rect.MinMaxRect(LabelColumn + c * column, top - rowHeight, LabelColumn + (c + 1) * column, top));
            cell.text = ControlHints.CapFor(schemes[c], action);
        }
    }

    private void BuildFooter(RectTransform content)
    {
        TextMeshProUGUI footer = UIKit.NewAutoText(content, "Footer", 12f, 24f, TextAlignmentOptions.Center, 0f);
        UIKit.Stretch(footer.rectTransform, Rect.MinMaxRect(0f, 0f, 1f, 0.46f));

        string slash = UIKit.Colored("/", HUDStyle.SoftTextHex);
        footer.text = UIKit.Colored("POWER-UPS", labelHex) + "\nHold 2  -  Use fires the left one\nThe number (1-3) is its strength\n" +
                      "<size=80%>" + UIKit.Colored("Close", HUDStyle.SoftTextHex) + " " + ControlHints.Cap("H") + " " + slash + " " +
                      ControlHints.Cap("Select") + " " + UIKit.Colored("or click the card", HUDStyle.SoftTextHex) + "</size>";
    }
}
