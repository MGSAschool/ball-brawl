using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(BallSkin))]
public class BallPowerUpProps : MonoBehaviour
{
    [SerializeField] private float bubbleSize = 1.45f;
    [SerializeField] private Color bubbleColor = new Color(0.35f, 0.9f, 1f, 0.22f);
    [SerializeField] private Color bubbleLinesColor = new Color(0.85f, 1f, 1f, 0.6f);

    private BrawlPlayer player;
    private BallSkin skin;
    private Transform bubble;
    private Transform glove;
    private Transform bolt;
    private Transform dashArrow;

    private void Awake()
    {
        player = GetComponent<BrawlPlayer>();
        skin = GetComponent<BallSkin>();
    }

    private void OnEnable()
    {
        skin.Applied += BuildProps;
    }

    private void OnDisable()
    {
        skin.Applied -= BuildProps;
    }

    private void LateUpdate()
    {
        if (!skin.HasModel) return;

        bool shown = skin.IsShown;
        float size = skin.Diameter;
        Vector3 center = transform.position;
        Quaternion look = Quaternion.LookRotation(skin.Facing, Vector3.up);
        float bob = Mathf.Sin(Time.time * 6f) * 0.05f * size;
        Vector3 overHead = center + Vector3.up * (size * 0.95f + bob);

        if (bubble != null)
        {
            bubble.gameObject.SetActive(shown && player.IsInvulnerable);
            bubble.Rotate(Vector3.up, 40f * Time.deltaTime, Space.World);
        }
        if (glove != null)
        {
            glove.gameObject.SetActive(shown && player.HasSuperKnockback);
            Vector3 inFront = center + skin.Facing * (size * 0.95f) + Vector3.up * bob;
            glove.SetPositionAndRotation(inFront, look * Quaternion.Euler(90f, 0f, 0f));
        }
        if (bolt != null)
        {
            bolt.gameObject.SetActive(shown && player.HasSpeedBoost);
            bolt.position = overHead;
            bolt.Rotate(Vector3.up, 180f * Time.deltaTime, Space.World);
        }
        if (dashArrow != null)
        {
            dashArrow.gameObject.SetActive(shown && player.HasDash);
            float pull = Mathf.Repeat(Time.time * 2.5f, 1f);
            dashArrow.SetPositionAndRotation(overHead + skin.Facing * (size * 0.15f * pull), look);
        }
    }

    private void BuildProps()
    {
        GameArt art = GameArt.Instance;
        float size = skin.Diameter;

        if (art.shieldBubble != null && bubble == null)
        {
            bubble = ArtUtil.FitModel(art.shieldBubble, transform, size * bubbleSize, "Shield Bubble");
            MakeBubbleSeeThrough();
            bubble.gameObject.SetActive(false);
        }
        if (art.superKnockbackModel != null && glove == null)
        {
            glove = ArtUtil.FitModel(art.superKnockbackModel, transform, size * 0.5f, "Knockback Glove");
            glove.gameObject.SetActive(false);
        }
        if (art.speedBoostModel != null && bolt == null)
        {
            bolt = ArtUtil.FitModel(art.speedBoostModel, transform, size * 0.5f, "Speed Bolt");
            bolt.gameObject.SetActive(false);
        }
        if (dashArrow == null)
        {
            float arrowSize = size * 1.1f / Mathf.Max(0.01f, transform.lossyScale.x);
            dashArrow = DashArrow.Build(transform, arrowSize);
            dashArrow.gameObject.SetActive(false);
        }
    }

    private void MakeBubbleSeeThrough()
    {
        foreach (Renderer rend in bubble.GetComponentsInChildren<Renderer>())
        {
            bool isLines = rend.sharedMaterial != null && rend.sharedMaterial.name.ToLower().Contains("hex");
            if (isLines) rend.sharedMaterial = ArtUtil.TransparentLit(bubbleLinesColor, 1.5f);
            else rend.sharedMaterial = ArtUtil.TransparentLit(bubbleColor, 0.6f);
            rend.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
