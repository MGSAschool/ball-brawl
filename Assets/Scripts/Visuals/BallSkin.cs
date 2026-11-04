using System.Collections.Generic;
using UnityEngine;

// Dresses the player's ball: swaps the plain sphere for the character ball (one per player slot)
// and shows props for active power-ups: a shield bubble while invulnerable, a boxing glove for
// Super Knockback and a bolt over its head while Dash is running.
// The character rolls like a real ball, face and all. BrawlPlayer keeps the rigidbody from
// rotating (steering needs that), so the spin is simulated here: on the ground friction drives it
// toward rolling without slipping, in the air it keeps its spin. Once the ball comes to rest it
// rights itself so its face looks the way it last went, and only then plays its idle bounce
// (the idle squashes the rig, which would make a rolling ball look oblong).
// Physics feel: impacts squash it like rubber, and at high speed it stretches along the way it
// is flying and leaves a trail. Hits and hard landings get sparks, dust and sound.
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
    [Tooltip("Below this speed on the ground the ball rights itself and plays its idle bounce.")]
    [SerializeField] private float settleSpeed = 1.2f;
    [SerializeField] private float settleRate = 5f;
    [Tooltip("How fast the idle bounce fades in and out.")]
    [SerializeField] private float idleBlendRate = 6f;

    [Header("Squash")]
    [Tooltip("Squash per m/s of impact speed.")]
    [SerializeField] private float squashPerSpeed = 0.035f;
    [SerializeField] private float maxSquash = 0.3f;
    [SerializeField] private float squashSpring = 260f;
    [SerializeField] private float squashDamping = 9f;

    [Header("Speed stretch")]
    [Tooltip("Speed (m/s) where the ball starts to stretch. Normal rolling is about 10.")]
    [SerializeField] private float stretchStartSpeed = 13f;
    [Tooltip("Speed where the stretch is at its biggest.")]
    [SerializeField] private float stretchFullSpeed = 30f;
    [SerializeField] private float maxStretch = 0.4f;
    [SerializeField] private float stretchResponse = 14f;

    [Header("Effects")]
    [SerializeField] private float trailSpeed = 12f;
    [Tooltip("Landing speed (m/s) that kicks up dust and a bounce sound.")]
    [SerializeField] private float landingImpact = 4f;

    private struct Pose
    {
        public Transform Bone;
        public Vector3 Position, Scale;
        public Quaternion Rotation;
    }

    private BrawlPlayer player;
    private Rigidbody rb;
    private float diameter = 1f;
    private Color color = Color.white;
    // Nesting: transform > stretch (along velocity) > squash (along impact) > skin (rolls)
    private Transform stretchNode;
    private Transform squashNode;
    private Transform skin;
    private Renderer skinRenderer;
    private Transform bubble;
    private Transform glove;
    private Transform bolt;
    private TrailRenderer trail;
    private readonly List<Pose> restPose = new List<Pose>();
    private float idleWeight = 1f;
    private Vector3 facing = Vector3.back;
    private Quaternion roll = Quaternion.identity;
    private Vector3 spin;
    private float squash;
    private float squashVelocity;
    private Vector3 squashNormal = Vector3.up;
    private float stretch;
    private Vector3 stretchAxis = Vector3.forward;
    private bool wasShown = true;
    private static int nextId;
    private int id;

    private void Awake()
    {
        id = nextId++;
        player = GetComponent<BrawlPlayer>();
        rb = GetComponent<Rigidbody>();
        var sphere = GetComponent<SphereCollider>();
        if (sphere != null) diameter = sphere.radius * 2f * transform.lossyScale.x;
    }

    // Called by MatchManager when the player joins: index 0-3 picks the ball
    public bool Apply(int playerIndex, Color playerColor)
    {
        color = playerColor;
        var art = GameArt.Instance;
        if (art == null || art.ballSkins == null || art.ballSkins.Length == 0) return false;
        var model = art.ballSkins[playerIndex % art.ballSkins.Length];
        if (model == null) return false;

        if (stretchNode != null) Destroy(stretchNode.gameObject);
        // The plain sphere is replaced, not hidden, so showing the player again can't bring it back
        if (TryGetComponent<MeshRenderer>(out var sphereRenderer)) Destroy(sphereRenderer);

        stretchNode = new GameObject("Ball Stretch").transform;
        stretchNode.SetParent(transform, false);
        squashNode = new GameObject("Ball Squash").transform;
        squashNode.SetParent(stretchNode, false);
        skin = ArtUtil.FitModel(model, squashNode, diameter * sizeMultiplier, "Ball Skin");
        roll = Quaternion.LookRotation(facing, Vector3.up) * ModelFacing;
        skinRenderer = null;
        foreach (var r in skin.GetComponentsInChildren<Renderer>())
        {
            if (r.enabled) { skinRenderer = r; break; }
        }

        // Remember the rig at rest, so the idle bounce can be faded out while rolling
        restPose.Clear();
        foreach (var bone in skin.GetComponentsInChildren<Transform>())
        {
            restPose.Add(new Pose { Bone = bone, Position = bone.localPosition, Rotation = bone.localRotation, Scale = bone.localScale });
        }
        foreach (var anim in skin.GetComponentsInChildren<Animation>())
        {
            anim.wrapMode = WrapMode.Loop;
            anim.Play();
        }

        BuildProps(art);
        BuildTrail();
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

    // Streak in the player's colour behind a fast ball
    private void BuildTrail()
    {
        if (trail != null) return;
        var go = new GameObject("Speed Trail");
        go.transform.SetParent(transform, false);
        trail = go.AddComponent<TrailRenderer>();
        trail.sharedMaterial = Vfx.Alpha();
        trail.time = 0.25f;
        trail.minVertexDistance = 0.1f;
        trail.widthCurve = new AnimationCurve(new Keyframe(0f, diameter * 0.75f), new Keyframe(1f, 0f));
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.Lerp(color, Color.white, 0.4f), 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = g;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.emitting = false;
    }

    private void LateUpdate()
    {
        if (skin == null) return;
        float dt = Time.deltaTime;

        // While the match hides the ball (respawning) its props and trail stay hidden too
        bool shown = skinRenderer == null || skinRenderer.enabled;
        if (shown && !wasShown && trail != null) trail.Clear();
        wasShown = shown;

        Vector3 velocity = rb != null && !rb.isKinematic ? rb.linearVelocity : Vector3.zero;
        Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
        if (flat.sqrMagnitude > 0.5f) facing = flat.normalized;
        Quaternion look = Quaternion.LookRotation(facing, Vector3.up);
        bool grounded = player.IsGrounded;
        bool resting = grounded && flat.sqrMagnitude < settleSpeed * settleSpeed;

        Roll(flat, look, grounded, resting, dt);
        BlendIdle(resting, dt);
        Deform(velocity, dt);
        if (trail != null) trail.emitting = shown && velocity.magnitude > trailSpeed;

        Vector3 center = transform.position;
        float bob = Mathf.Sin(Time.time * 6f) * 0.05f * diameter;

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

    private void Roll(Vector3 flatVelocity, Quaternion look, bool grounded, bool resting, float dt)
    {
        float radius = diameter * 0.5f;
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
        if (angle > 1e-4f) roll = Quaternion.AngleAxis(angle, spin.normalized) * roll;

        // At rest it sits upright again so players can see whose face is whose
        if (resting) roll = Quaternion.Slerp(roll, look * ModelFacing, 1f - Mathf.Exp(-settleRate * dt));
        roll = Quaternion.Normalize(roll);
    }

    // The Animation component has already posed the rig this frame; pull it back toward the
    // rest pose by however much the idle should be faded out
    private void BlendIdle(bool resting, float dt)
    {
        idleWeight = Mathf.MoveTowards(idleWeight, resting ? 1f : 0f, idleBlendRate * dt);
        if (idleWeight >= 0.999f) return;
        float k = 1f - idleWeight;
        foreach (var pose in restPose)
        {
            if (pose.Bone == null) continue;
            pose.Bone.localPosition = Vector3.Lerp(pose.Bone.localPosition, pose.Position, k);
            pose.Bone.localRotation = Quaternion.Slerp(pose.Bone.localRotation, pose.Rotation, k);
            pose.Bone.localScale = Vector3.Lerp(pose.Bone.localScale, pose.Scale, k);
        }
    }

    // Stretch along the flight direction when going fast, squash along the last impact. Each node
    // scales along its own axis: rotate to the axis, scale, and rotate back for the next node,
    // so the skin inside still gets exactly the rolling rotation.
    private void Deform(Vector3 velocity, float dt)
    {
        squashVelocity += (-squashSpring * squash - squashDamping * squashVelocity) * dt;
        squash += squashVelocity * dt;
        if (Mathf.Abs(squash) < 1e-4f && Mathf.Abs(squashVelocity) < 1e-3f) squash = squashVelocity = 0f;

        float speed = velocity.magnitude;
        float target = Mathf.InverseLerp(stretchStartSpeed, stretchFullSpeed, speed) * maxStretch;
        stretch = Mathf.Lerp(stretch, target, 1f - Mathf.Exp(-stretchResponse * dt));
        if (speed > 1f) stretchAxis = velocity / speed;

        Quaternion parent = transform.rotation;
        Quaternion toStretch = Quaternion.FromToRotation(Vector3.up, stretchAxis);
        Quaternion toSquash = Quaternion.FromToRotation(Vector3.up, squashNormal);

        // Keep the volume: longer along the axis, thinner across it
        float across = 1f / Mathf.Sqrt(1f + stretch);
        stretchNode.localRotation = Quaternion.Inverse(parent) * toStretch;
        stretchNode.localScale = new Vector3(across, 1f + stretch, across);
        squashNode.localRotation = Quaternion.Inverse(toStretch) * toSquash;
        squashNode.localScale = new Vector3(1f + squash * 0.5f, 1f - squash, 1f + squash * 0.5f);
        skin.localRotation = Quaternion.Inverse(toSquash) * roll;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (squashNode == null || collision.contactCount == 0) return;
        var contact = collision.GetContact(0);
        Vector3 normal = contact.normal;
        float impact = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, normal));

        float amount = Mathf.Min(impact * squashPerSpeed, maxSquash);
        if (amount >= 0.03f && amount >= squash)
        {
            squash = amount;
            squashVelocity = 0f;
            squashNormal = normal;
        }

        if (collision.gameObject.TryGetComponent<BrawlPlayer>(out var other))
        {
            // Both balls get this message; only one of them plays the hit
            if (other.TryGetComponent<BallSkin>(out var pairSkin) && id < pairSkin.id) return;
            bool big = player.HasSuperKnockback || other.HasSuperKnockback;
            float strength = Mathf.InverseLerp(3f, 20f, collision.relativeVelocity.magnitude);
            Color hitColor = other.TryGetComponent<BallSkin>(out var otherSkin) ? Color.Lerp(color, otherSkin.color, 0.5f) : color;
            Vfx.Hit(contact.point, hitColor, big ? 1f : strength);
            Sfx.Play(big ? Sound.BumpBig : Sound.Bump, contact.point, Mathf.Lerp(0.5f, 1f, strength));
            Vfx.Shake(big ? 0.45f : 0.12f + strength * 0.2f);
        }
        else if (impact > landingImpact && normal.y > 0.5f)
        {
            float strength = Mathf.InverseLerp(landingImpact, 18f, impact);
            Vfx.Dust(contact.point, strength);
            Sfx.Play(Sound.Land, contact.point, Mathf.Lerp(0.35f, 0.9f, strength), Mathf.Lerp(1.15f, 0.9f, strength));
        }
    }
}
