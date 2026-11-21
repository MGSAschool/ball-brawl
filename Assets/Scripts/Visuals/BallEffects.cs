using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(BallSkin))]
public class BallEffects : MonoBehaviour
{
    private static int nextId;

    [Header("Speed Trail")]
    [SerializeField] private float trailSpeed = 12f;
    [SerializeField] private float trailTime = 0.25f;

    [Header("Hits And Landings")]
    [SerializeField] private float landingImpact = 4f;

    private BrawlPlayer player;
    private BallSkin skin;
    private Rigidbody rb;
    private TrailRenderer trail;
    private bool wasShown = true;
    private int id;

    private void Awake()
    {
        id = nextId;
        nextId++;
        player = GetComponent<BrawlPlayer>();
        skin = GetComponent<BallSkin>();
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        skin.Applied += BuildTrail;
    }

    private void OnDisable()
    {
        skin.Applied -= BuildTrail;
    }

    private void LateUpdate()
    {
        if (trail == null) return;

        bool shown = skin.IsShown;
        if (shown && !wasShown) trail.Clear();
        wasShown = shown;

        float speed = 0f;
        if (rb != null && !rb.isKinematic) speed = rb.linearVelocity.magnitude;
        trail.emitting = shown && speed > trailSpeed;
    }

    private void BuildTrail()
    {
        if (trail != null) return;

        GameObject go = new GameObject("Speed Trail");
        go.transform.SetParent(transform, false);
        trail = go.AddComponent<TrailRenderer>();
        trail.sharedMaterial = VfxMaterials.Alpha();
        trail.time = trailTime;
        trail.minVertexDistance = 0.1f;
        trail.widthCurve = new AnimationCurve(new Keyframe(0f, skin.Diameter * 0.75f), new Keyframe(1f, 0f));
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.emitting = false;

        Color color = skin.PlayerColor;
        GradientColorKey[] colors = { new GradientColorKey(Color.Lerp(color, Color.white, 0.4f), 0f), new GradientColorKey(color, 1f) };
        GradientAlphaKey[] alphas = { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) };
        Gradient gradient = new Gradient();
        gradient.SetKeys(colors, alphas);
        trail.colorGradient = gradient;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!skin.HasModel || collision.contactCount == 0) return;

        ContactPoint contact = collision.GetContact(0);
        BrawlPlayer other = collision.gameObject.GetComponent<BrawlPlayer>();
        if (other != null)
        {
            PlayHit(collision, contact, other);
            return;
        }

        float impact = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, contact.normal));
        if (impact > landingImpact && contact.normal.y > 0.5f) PlayLanding(contact.point, impact);
    }

    private void PlayHit(Collision collision, ContactPoint contact, BrawlPlayer other)
    {
        BallEffects otherEffects = other.GetComponent<BallEffects>();
        if (otherEffects != null && id < otherEffects.id) return;

        bool big = player.HasSuperKnockback || other.HasSuperKnockback;
        float strength = Mathf.InverseLerp(3f, 20f, collision.relativeVelocity.magnitude);

        Color hitColor = skin.PlayerColor;
        BallSkin otherSkin = other.GetComponent<BallSkin>();
        if (otherSkin != null) hitColor = Color.Lerp(skin.PlayerColor, otherSkin.PlayerColor, 0.5f);

        if (big)
        {
            Vfx.Hit(contact.point, hitColor, 1f);
            Sfx.Play(Sound.BumpBig, contact.point, Mathf.Lerp(0.5f, 1f, strength));
            Vfx.Shake(0.45f);
        }
        else
        {
            Vfx.Hit(contact.point, hitColor, strength);
            Sfx.Play(Sound.Bump, contact.point, Mathf.Lerp(0.5f, 1f, strength));
            Vfx.Shake(0.12f + strength * 0.2f);
        }
    }

    private void PlayLanding(Vector3 point, float impact)
    {
        float strength = Mathf.InverseLerp(landingImpact, 18f, impact);
        Vfx.Dust(point, strength);
        Sfx.Play(Sound.Land, point, Mathf.Lerp(0.35f, 0.9f, strength), Mathf.Lerp(1.15f, 0.9f, strength));
    }
}
