using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HUDMenuButtons : HUDPanel
{
    private GameObject logo;
    private GameObject menu;
    private Image playImage;
    private TextMeshProUGUI playLabel;

    protected override void BuildPanel()
    {
        BuildLogo();

        menu = UIKit.NewRect("Menu", Root).gameObject;
        RectTransform menuRect = (RectTransform)menu.transform;
        Vector2 bottomCenter = new Vector2(0.5f, 0f);
        UIKit.PlaceAt(menuRect, bottomCenter, new Vector2(0f, 150f), new Vector2(820f, 260f));
        menuRect.localScale = Vector3.one * 0.7f;

        Sprite guideSprite = null;
        Sprite playSprite = null;
        Sprite leaveSprite = null;
        if (Art != null)
        {
            guideSprite = Art.howToPlayButton;
            playSprite = Art.playButton;
            leaveSprite = Art.leaveButton;
        }

        TextMeshProUGUI guideLabel;
        MakeButton("How To Play", guideSprite, Rect.MinMaxRect(0f, 0.08f, 0.28f, 0.92f), hud.ToggleGuide, out guideLabel);
        guideLabel.text = "GUIDE  <size=70%>(H)";

        playImage = MakeButton("Play", playSprite, Rect.MinMaxRect(0.31f, 0f, 0.69f, 1f), OnPlayClicked, out playLabel);

        TextMeshProUGUI leaveLabel;
        MakeButton("Leave", leaveSprite, Rect.MinMaxRect(0.72f, 0.08f, 1f, 0.92f), MenuInput.QuitGame, out leaveLabel);
        leaveLabel.text = "LEAVE  <size=70%>(Esc)";
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

            if (lobby) playLabel.text = "PLAY  <size=70%>(Enter)";
            else playLabel.text = "REMATCH  <size=70%>(Enter)";
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
        Image logoImage = UIKit.NewImage("Logo", Root);
        logo = logoImage.gameObject;
        if (Art != null && Art.logo != null)
        {
            logoImage.sprite = Art.logo;
            logoImage.preserveAspect = true;
        }
        else
        {
            logoImage.enabled = false;
        }
        UIKit.PlaceAt(logoImage.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(340f, 190f));
    }

    private Image MakeButton(string name, Sprite sprite, Rect anchors, UnityAction onClick, out TextMeshProUGUI label)
    {
        RectTransform root = UIKit.NewRect(name, menu.transform);
        UIKit.Stretch(root, anchors);

        Image image = UIKit.NewImage("Button", root);
        image.raycastTarget = true;
        UIKit.Stretch(image.rectTransform, Rect.MinMaxRect(0f, 0.22f, 1f, 1f));
        if (sprite != null)
        {
            image.sprite = sprite;
            image.preserveAspect = true;
        }
        else
        {
            image.color = HUDStyle.Panel;
        }

        Button button = image.gameObject.AddComponent<Button>();
        Navigation noNavigation = new Navigation();
        noNavigation.mode = Navigation.Mode.None;
        button.navigation = noNavigation;
        button.onClick.AddListener(onClick);

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.95f, 0.8f);
        colors.pressedColor = new Color(0.8f, 0.75f, 0.65f);
        button.colors = colors;

        label = UIKit.NewAutoText(root, "Label", 12f, 26f, TextAlignmentOptions.Center, 0f);
        UIKit.Stretch(label.rectTransform, Rect.MinMaxRect(0f, 0f, 1f, 0.22f));
        label.text = name.ToUpper();
        return image;
    }
}
