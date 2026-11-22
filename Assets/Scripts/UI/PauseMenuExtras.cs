using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UIController))]
public class PauseMenuExtras : MonoBehaviour
{
    [SerializeField] private RectTransform pauseMenu;
    [SerializeField] private RectTransform buttonArea;
    [SerializeField] private Vector2 frameSize = new Vector2(600f, 600f);
    [SerializeField] private int drawOrder = 200;

    [Header("Settings Button (next to the HUD timer)")]
    [SerializeField] private float settingsButtonSize = 80f;
    [SerializeField] private float gapFromTimer = 16f;
    [SerializeField] private float timerWidth = 320f;
    [SerializeField] private float timerMiddleFromTop = 72f;
    [SerializeField] private Vector2 hudReferenceSize = new Vector2(1920f, 1080f);

    private UIController pauseController;
    private GameObject settingsButton;
    private RectTransform settingsButtonRect;

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
        if (settingsButton.activeSelf) PlaceSettingsButtonNextToTimer();
    }

    private void PlaceSettingsButtonNextToTimer()
    {
        float hudScale = Mathf.Min(Screen.width / hudReferenceSize.x, Screen.height / hudReferenceSize.y);
        float x = timerWidth * 0.5f + gapFromTimer + settingsButtonSize * 0.5f;
        settingsButtonRect.anchoredPosition = new Vector2(x, -timerMiddleFromTop) * hudScale;
        settingsButtonRect.sizeDelta = Vector2.one * settingsButtonSize * hudScale;
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

        Vector2 topCenter = new Vector2(0.5f, 1f);
        Vector2 middle = new Vector2(0.5f, 0.5f);
        UIKit.Place(image.rectTransform, topCenter, topCenter, middle, Vector2.zero, Vector2.one * settingsButtonSize);

        Button button = image.gameObject.AddComponent<Button>();
        button.onClick.AddListener(OnSettingsClicked);
        settingsButton = image.gameObject;
        settingsButtonRect = image.rectTransform;
        settingsButton.SetActive(false);
    }

    private void OnSettingsClicked()
    {
        Sfx.Play(Sound.Click);
        pauseController.TogglePause();
    }
}
