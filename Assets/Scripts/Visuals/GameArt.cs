using TMPro;
using UnityEngine;

// One place for the game's art, so the scripts that build things at runtime (HUD, player balls,
// power-up pickups) can find it, and anyone can swap a piece in the Inspector.
// The asset lives at Resources/GameArt so it loads without being wired into every scene.
[CreateAssetMenu(menuName = "Ball Brawl/Game Art", fileName = "GameArt")]
public class GameArt : ScriptableObject
{
    [Header("Players (P1, P2, P3, P4)")]
    public GameObject[] ballSkins;

    [Header("Power-up pickups")]
    public GameObject dashModel;
    public GameObject invulnerabilityModel;
    public GameObject superKnockbackModel;

    [Header("Effects on the ball")]
    [Tooltip("Bubble shown around a ball while it is invulnerable.")]
    public GameObject shieldBubble;

    [Header("UI")]
    public TMP_FontAsset font;
    public Sprite logo;
    public Sprite scoreboardFrame;
    public Sprite howToPlayFrame;
    public Sprite inventoryFrame;
    public Sprite namePlate;
    public Sprite pointsIcon;
    public Sprite coinIcon;
    public Sprite playButton;
    public Sprite howToPlayButton;
    public Sprite leaveButton;

    private static GameArt instance;
    public static GameArt Instance => instance != null ? instance : instance = Resources.Load<GameArt>("GameArt");
}
