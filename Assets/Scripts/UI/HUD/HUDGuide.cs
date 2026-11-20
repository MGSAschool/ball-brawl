using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private void Update()
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
