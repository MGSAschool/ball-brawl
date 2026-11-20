using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HUDMenuButtons : HUDPanel
{
    public static readonly Rect ButtonArtArea = Rect.MinMaxRect(0.094f, 0.311f, 0.906f, 0.693f);
    private static readonly Rect LogoArtArea = Rect.MinMaxRect(0.019f, 0.167f, 0.988f, 0.826f);
    private static readonly Rect WholeArt = Rect.MinMaxRect(0f, 0f, 1f, 1f);
    private static readonly Rect GuideSpot = Rect.MinMaxRect(0f, 0.08f, 0.125f, 1.05f);
    private static readonly Rect PlaySpot = Rect.MinMaxRect(0.158f, 0f, 0.526f, 1f);
    private static readonly Rect QuitSpot = Rect.MinMaxRect(0.553f, 0.1f, 0.842f, 0.9f);

    private GameObject logo;
    private GameObject menu;
    private Image playImage;

    protected override void BuildPanel()
    {
        BuildLogo();

        menu = UIKit.NewRect("Menu", Root).gameObject;
        RectTransform menuRect = (RectTransform)menu.transform;
        Vector2 bottomCenter = new Vector2(0.5f, 0f);
        UIKit.PlaceAt(menuRect, bottomCenter, new Vector2(0f, 165f), new Vector2(760f, 100f));

        Sprite guideSprite = null;
        Sprite playSprite = null;
        Sprite leaveSprite = null;
        if (Art != null)
        {
            guideSprite = Art.howToPlayButton;
            playSprite = Art.playButton;
            leaveSprite = Art.leaveButton;
        }

        MakeButton("How To Play", guideSprite, WholeArt, GuideSpot, hud.ToggleGuide);
        TextMeshProUGUI guideLabel = UIKit.NewAutoText(menu.transform, "Guide Label", 10f, 18f, TextAlignmentOptions.Center, 0f);
        UIKit.Stretch(guideLabel.rectTransform, Rect.MinMaxRect(GuideSpot.xMin - 0.03f, -0.12f, GuideSpot.xMax + 0.03f, 0.08f));
        guideLabel.text = "GUIDE <size=80%>(H)";

        playImage = MakeButton("Play", playSprite, ButtonArtArea, PlaySpot, OnPlayClicked);
        MakeButton("Quit", leaveSprite, ButtonArtArea, QuitSpot, MenuInput.QuitGame);
    }

    private void Update()
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

    private Image MakeButton(string name, Sprite sprite, Rect artArea, Rect anchors, UnityAction onClick)
    {
        RectTransform root = UIKit.NewRect(name, menu.transform);
        UIKit.Stretch(root, anchors);

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
