using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UIController))]
public class PauseMenuExtras : MonoBehaviour
{
    [SerializeField] private RectTransform pauseMenu;
    [SerializeField] private RectTransform buttonArea;
    [SerializeField] private Vector2 frameSize = new Vector2(600f, 600f);
    [SerializeField] private float settingsButtonSize = 90f;
    [SerializeField] private float settingsButtonMargin = 24f;
    [SerializeField] private int drawOrder = 200;

    private UIController pauseController;
    private GameObject settingsButton;

    private void Awake()
    {
        pauseController = GetComponent<UIController>();
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null) canvas.sortingOrder = drawOrder;
        BuildFrame();
        BuildSettingsButton();
    }

    private void Update()
    {
        MatchManager match = MatchManager.Instance;
        bool playing = match != null && match.State == MatchState.Playing;
        bool paused = Time.timeScale == 0f;
        settingsButton.SetActive(playing && !paused);
    }

    private void BuildFrame()
    {
        GameArt art = GameArt.Instance;
        if (pauseMenu == null || art == null || art.pauseFrame == null) return;

        Image frame = UIKit.NewImage("Pause Frame", pauseMenu);
        frame.sprite = art.pauseFrame;
        frame.preserveAspect = true;
        Vector2 middle = new Vector2(0.5f, 0.5f);
        UIKit.PlaceAt(frame.rectTransform, middle, Vector2.zero, frameSize);

        if (buttonArea != null) frame.transform.SetSiblingIndex(buttonArea.GetSiblingIndex());
    }

    private void BuildSettingsButton()
    {
        GameArt art = GameArt.Instance;
        Image image = UIKit.NewImage("Settings Button", transform);
        if (art != null && art.settingsButton != null) image.sprite = art.settingsButton;
        image.preserveAspect = true;
        image.raycastTarget = true;

        Vector2 topRight = new Vector2(1f, 1f);
        Vector2 position = new Vector2(-settingsButtonMargin, -settingsButtonMargin);
        UIKit.Place(image.rectTransform, topRight, topRight, topRight, position, Vector2.one * settingsButtonSize);

        Button button = image.gameObject.AddComponent<Button>();
        button.onClick.AddListener(OnSettingsClicked);
        settingsButton = image.gameObject;
        settingsButton.SetActive(false);
    }

    private void OnSettingsClicked()
    {
        Sfx.Play(Sound.Click);
        pauseController.TogglePause();
    }
}
