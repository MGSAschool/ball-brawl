using TMPro;
using UnityEngine;

[CreateAssetMenu(menuName = "Ball Brawl/Game Art", fileName = "GameArt")]
public class GameArt : ScriptableObject
{
    [Header("Players (P1, P2, P3, P4)")]
    public GameObject[] ballSkins;

    [Header("Power-up pickups")]
    public GameObject speedBoostModel;
    public GameObject invulnerabilityModel;
    public GameObject superKnockbackModel;

    [Header("Effects on the ball")]
    public GameObject shieldBubble;

    [Header("Minigames")]
    public GameObject bombModel;

    [Header("Sound")]
    public AudioClip bounceSound;

    [Header("UI")]
    public TMP_FontAsset font;
    public Sprite logo;
    public Sprite scoreboardFrame;
    public Sprite howToPlayFrame;
    public Sprite controlsFrame;
    public Sprite inventoryFrame;
    public Sprite namePlate;
    public Sprite pointsIcon;
    public Sprite coinIcon;
    public Sprite playButton;
    public Sprite howToPlayButton;
    public Sprite leaveButton;
    public Sprite modeLabel;

    private static GameArt instance;

    public static GameArt Instance
    {
        get
        {
            if (instance == null) instance = Resources.Load<GameArt>("GameArt");
            return instance;
        }
    }
}
