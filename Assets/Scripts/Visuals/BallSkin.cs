using UnityEngine;

// Dresses the player's ball: swaps the plain sphere for the character ball (one per player slot),
// keeps its idle bounce playing, and shows props for active power-ups: a shield bubble while
// invulnerable, a boxing glove for Super Knockback and a bolt over its head while Dash is running.
// The character rolls like a real ball, face and all. BrawlPlayer keeps the rigidbody from
// rotating (steering needs that), so the spin is simulated here: on the ground friction drives it
// toward rolling without slipping, in the air it keeps its spin, and impacts squash the ball like
// rubber. Once the ball comes to rest it rights itself so its face looks the way it last went.
// Only reads BrawlPlayer, never changes it.
[RequireComponent(typeof(BrawlPlayer))]
public class BallSkin : MonoBehaviour
{
    // The ball models look along their local -X axis
    private static readonly Quaternion ModelFacing = Quaternion.Euler(0f, 90f, 0f);

    [Tooltip("Model size relative to the physics sphere.")]
    [SerializeField] private float sizeMultiplier = 1.05f;
    [SerializeField] private float bubbleSize = 1.45f;
    [SerializeField] private Color bubbleColor = new Color(0.35f, 0.9f, 1f, 0.22f);
    [SerializeField] private Color bubbleLinesColor = new Color(0.85f, 1f, 1f, 0.6f);

    [Header("Rolling")]
    [Tooltip("How fast ground friction matches the spin to the rolling speed.")]
    [SerializeField] private float rollGrip = 12f;
    [Tooltip("How much spin the ball loses per second in the air.")]
    [SerializeField] private float airSpinDrag = 0.3f;
    [Tooltip("Below this speed on the ground the ball rights itself, face toward where it went.")]
    [SerializeField] private float settleSpeed = 1.2f;
    [SerializeField] private float settleRate = 5f;

    [Header("Squash")]
    [Tooltip("Squash per m/s of impact speed.")]
    [SerializeField] private float squashPerSpeed = 0.035f;
    [SerializeField] private float maxSquash = 0.3f;
    [SerializeField] private float squashSpring = 260f;
    [SerializeField] private float squashDamping = 9f;

    private BrawlPlayer player;
    private Rigidbody rb;
    private float diameter = 1f;
    private Transform body;
    private Transform skin;
    private Renderer skinRenderer;
    private Transform bubble;
    private Transform glove;
    private Transform bolt;
    private Vector3 facing = Vector3.back;
    private Vector3 spin;
    private float squash;
    private float squashVelocity;
    private Vector3 squashNormal = Vector3.up;

    private void Awake()
    {
        player = GetComponent<BrawlPlayer>();
        rb = GetComponent<Rigidbody>();
        var sphere = GetComponent<SphereCollider>();
        if (sphere != null) diameter = sphere.radius * 2f * transform.lossyScale.x;
    }

    // Called by MatchManager when the player joins: index 0-3 picks the ball colour
    public bool Apply(int playerIndex)
    {
        var art = GameArt.Instance;
        if (art == null || art.ballSkins == null || art.ballSkins.Length == 0) return false;
        var model = art.ballSkins[playerIndex % art.ballSkins.Length];
        if (model == null) return false;

        if (body != null) Destroy(body.gameObject);
        // The plain sphere is replaced, not hidden, so showing the player again can't bring it back
        if (TryGetComponent<MeshRenderer>(out var sphereRenderer)) Destroy(sphereRenderer);

        // Squash happens on `body`; the skin inside it rolls freely
        body = new GameObject("Ball Body").transform;
        body.SetParent(transform, false);
        skin = ArtUtil.FitModel(model, body, diameter * sizeMultiplier, "Ball Skin");
        skin.rotation = Quaternion.LookRotation(facing, Vector3.up) * ModelFacing;
        skinRenderer = null;
        foreach (var r in skin.GetComponentsInChildren<Renderer>())
        {
            if (r.enabled) { skinRenderer = r; break; }
        }
        foreach (var anim in skin.GetComponentsInChildren<Animation>())
        {
            anim.wrapMode = WrapMode.Loop;
            anim.Play();
        }

        BuildProps(art);
        return true;
    }

    private void BuildProps(GameArt art)
    {
        if (art.shieldBubble != null && bubble == null)
        {
            bubble = ArtUtil.FitModel(art.shieldBubble, transform, diameter * bubbleSize, "Shield Bubble");
            foreach (var r in bubble.GetComponentsInChildren<Renderer>())
            {
                // The bubble has a solid base and a mesh of hex lines; make both see-through
                bool lines = r.sharedMaterial != null && r.sharedMaterial.name.ToLower().Contains("hex");
                r.sharedMaterial = lines ? ArtUtil.TransparentLit(bubbleLinesColor, 1.5f) : ArtUtil.TransparentLit(bubbleColor, 0.6f);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            bubble.gameObject.SetActive(false);
        }
        if (art.superKnockbackModel != null && glove == null)
        {
            glove = ArtUtil.FitModel(art.superKnockbackModel, transform, diameter * 0.5f, "Knockback Glove");
            glove.gameObject.SetActive(false);
        }
        if (art.dashModel != null && bolt == null)
        {
            bolt = ArtUtil.FitModel(art.dashModel, transform, diameter * 0.5f, "Dash Bolt");
            bolt.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (skin == null) return;
        float dt = Time.deltaTime;

        Vector3 velocity = rb != null ? rb.linearVelocity : Vector3.zero;
        Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
        if (flat.sqrMagnitude > 0.5f) facing = flat.normalized;
        Quaternion look = Quaternion.LookRotation(facing, Vector3.up);
        Roll(flat, look, dt);
        Squash(dt);

        Vector3 center = transform.position;
        float bob = Mathf.Sin(Time.time * 6f) * 0.05f * diameter;
        // While the match hides the ball (respawning) its props stay hidden too
        bool shown = skinRenderer == null || skinRenderer.enabled;

        if (bubble != null)
        {
            bubble.gameObject.SetActive(shown && player.IsInvulnerable);
            bubble.Rotate(Vector3.up, 40f * dt, Space.World);
        }
        if (glove != null)
        {
            glove.gameObject.SetActive(shown && player.HasSuperKnockback);
            // The glove model stands fist-up; tip it forward so it punches the way the ball is going
            glove.SetPositionAndRotation(center + facing * (diameter * 0.95f) + Vector3.up * bob, look * Quaternion.Euler(90f, 0f, 0f));
        }
        if (bolt != null)
        {
            bolt.gameObject.SetActive(shown && player.HasDash);
            bolt.position = center + Vector3.up * (diameter * 0.95f + bob);
            bolt.Rotate(Vector3.up, 180f * dt, Space.World);
        }
    }

    private void Roll(Vector3 flatVelocity, Quaternion look, float dt)
    {
        float radius = diameter * 0.5f;
        bool grounded = player.IsGrounded;
        if (grounded)
        {
            // Rolling without slipping: the top of the ball moves forward, so it spins about up x velocity
            Vector3 rolling = Vector3.Cross(Vector3.up, flatVelocity) / radius;
            spin = Vector3.Lerp(spin, rolling, 1f - Mathf.Exp(-rollGrip * dt));
        }
        else
        {
            spin *= Mathf.Exp(-airSpinDrag * dt);
        }

        float angle = spin.magnitude * dt * Mathf.Rad2Deg;
        if (angle > 1e-4f) skin.rotation = Quaternion.AngleAxis(angle, spin.normalized) * skin.rotation;

        // At rest it sits upright again so players can see whose face is whose
        if (grounded && flatVelocity.sqrMagnitude < settleSpeed * settleSpeed)
        {
            skin.rotation = Quaternion.Slerp(skin.rotation, look * ModelFacing, 1f - Mathf.Exp(-settleRate * dt));
        }
    }

    // Damped spring, so a hard landing or hit squashes the ball and it wobbles back
    private void Squash(float dt)
    {
        squashVelocity += (-squashSpring * squash - squashDamping * squashVelocity) * dt;
        squash += squashVelocity * dt;
        if (Mathf.Abs(squash) < 1e-4f && Mathf.Abs(squashVelocity) < 1e-3f) squash = squashVelocity = 0f;

        // Flatten along the impact direction and bulge sideways, so it keeps its volume
        // (the skin keeps its own world rotation when the squash direction changes)
        Quaternion skinRotation = skin.rotation;
        body.rotation = Quaternion.FromToRotation(Vector3.up, squashNormal);
        body.localScale = new Vector3(1f + squash * 0.5f, 1f - squash, 1f + squash * 0.5f);
        skin.rotation = skinRotation;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (body == null || collision.contactCount == 0) return;
        Vector3 normal = collision.GetContact(0).normal;
        float impact = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, normal));
        float amount = Mathf.Min(impact * squashPerSpeed, maxSquash);
        if (amount < 0.03f || amount < squash) return;
        squash = amount;
        squashVelocity = 0f;
        squashNormal = normal;
    }
}
