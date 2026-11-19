using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class MatchHUD : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [SerializeField] private CoinRush coinRush;
    [SerializeField] private float announcementDuration = 2.5f;
    [SerializeField] private float tipInterval = 6f;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private PowerupInventoryUI powerUpBoxPrefab;
    [SerializeField] private float cornerMargin = 24f;
    [SerializeField] private float behindAlpha = 0.3f;

    private RectTransform safeArea;
    private Rect appliedSafeArea;
    private PlayerJoiner joiner;
    private int tipOffset;

    public MatchManager Match { get { return match; } }
    public CoinRush CoinRush { get { return coinRush; } }
    public GameArt Art { get { return GameArt.Instance; } }
    public RectTransform SafeArea { get { return safeArea; } }
    public PowerupInventoryUI PowerUpBoxPrefab { get { return powerUpBoxPrefab; } }
    public float CornerMargin { get { return cornerMargin; } }
    public float BehindAlpha { get { return behindAlpha; } }
    public bool GuideOpen { get; set; }
    public bool InMenu { get { return match.IsInMenu; } }

    public int MaxPlayers
    {
        get
        {
            if (joiner != null) return joiner.MaxPlayers;
            return 4;
        }
    }

    public bool ShowAnnouncement
    {
        get { return Time.time - match.AnnouncementTime < announcementDuration; }
    }

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        if (coinRush == null) coinRush = FindAnyObjectByType<CoinRush>();
        joiner = FindAnyObjectByType<PlayerJoiner>();
        tipOffset = Random.Range(0, GameTips.All.Length);

        MakeEventSystem();
        RectTransform canvas = UIKit.MakeOverlayCanvas("MatchHUD Canvas", transform, 100, referenceResolution, true);
        safeArea = UIKit.NewRect("Safe Area", canvas);
        ApplySafeArea();

        AddPanel<HUDTopBar>();
        AddPanel<HUDResults>();
        AddPanel<HUDMenuButtons>();
        AddPanel<HUDModeCards>();
        AddPanel<HUDJoinCards>();
        AddPanel<HUDSignalTint>();
        AddPanel<HUDGuide>();
        AddPanel<HUDBottomText>();
        AddPanel<HUDPlayerCards>();
    }

    private void OnEnable()
    {
        if (match != null) match.StateChanged += OnMatchStateChanged;
    }

    private void OnDisable()
    {
        if (match != null) match.StateChanged -= OnMatchStateChanged;
    }

    private void Update()
    {
        ApplySafeArea();
        if (match == null) return;

        EventSystem events = EventSystem.current;
        if (events != null && events.currentSelectedGameObject != null) events.SetSelectedGameObject(null);

        if (!InMenu) GuideOpen = false;
    }

    public void ToggleGuide()
    {
        if (!InMenu) return;
        GuideOpen = !GuideOpen;
        Sfx.Play(Sound.Click);
    }

    public string CurrentTip()
    {
        int index = (tipOffset + (int)(Time.unscaledTime / tipInterval)) % GameTips.All.Length;
        return GameTips.All[index];
    }

    private void OnMatchStateChanged(MatchState state)
    {
        if (state == MatchState.Countdown) GuideOpen = false;
    }

    private void AddPanel<T>() where T : HUDPanel
    {
        T panel = gameObject.AddComponent<T>();
        panel.Build(this);
    }

    private void ApplySafeArea()
    {
        Rect area = Screen.safeArea;
        if (area == appliedSafeArea || Screen.width <= 0 || Screen.height <= 0) return;
        appliedSafeArea = area;

        safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        safeArea.offsetMin = Vector2.zero;
        safeArea.offsetMax = Vector2.zero;
    }

    private void MakeEventSystem()
    {
        if (EventSystem.current != null) return;
        if (FindAnyObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }
}
