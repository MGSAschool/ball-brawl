using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private void Update()
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
