using UnityEngine;

[RequireComponent(typeof(BallSkin))]
public class BallHitEffects : MonoBehaviour
{
    private static int nextId;

    [SerializeField] private float landingImpact = 4f;

    private BrawlPlayer player;
    private BallSkin skin;
    private int id;

    private void Awake()
    {
        id = nextId;
        nextId++;
        player = GetComponent<BrawlPlayer>();
        skin = GetComponent<BallSkin>();
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
        BallHitEffects otherEffects = other.GetComponent<BallHitEffects>();
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
