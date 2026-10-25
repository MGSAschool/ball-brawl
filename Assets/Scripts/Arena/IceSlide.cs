using UnityEngine;

// Makes a ball slide on frozen floor without changing the player's own movement code.
// IceSlideSampler (runs before BrawlPlayer) records the velocity before the player steers;
// this component (runs after it) hands most of that momentum back, so on ice steering only
// nudges the ball. Added to players at runtime by FallingPlatforms.
[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(Rigidbody))]
public class IceSlide : MonoBehaviour
{
    [Tooltip("How much of the player's steering still works on ice (0 = none, 1 = normal).")]
    [Range(0f, 1f)] public float grip = 0.15f;
    [Tooltip("Rigidbody damping while on ice. The ball's normal damping is what slows it down, so ice lowers it.")]
    public float iceDamping = 0.25f;

    public bool OnIce { get; private set; }

    internal Vector3 velocityBeforeSteering;
    internal bool sampled;

    private Rigidbody rb;
    private SphereCollider sphere;
    private float normalDamping;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sphere = GetComponent<SphereCollider>();
        normalDamping = rb.linearDamping;
        if (!TryGetComponent<IceSlideSampler>(out _)) gameObject.AddComponent<IceSlideSampler>();
    }

    private void OnDisable()
    {
        if (rb != null) rb.linearDamping = normalDamping;
    }

    private void FixedUpdate()
    {
        bool wasOnIce = OnIce;
        OnIce = CheckOnIce();
        if (OnIce != wasOnIce) rb.linearDamping = OnIce ? iceDamping : normalDamping;
        if (!OnIce) return;

        Vector3 steered = rb.linearVelocity;
        Vector3 before = velocityBeforeSteering;
        Vector3 flat = Vector3.Lerp(new Vector3(before.x, 0f, before.z), new Vector3(steered.x, 0f, steered.z), grip);
        rb.linearVelocity = new Vector3(flat.x, steered.y, flat.z);
    }

    private bool CheckOnIce()
    {
        if (!sampled || rb.isKinematic) return false;
        sampled = false;

        float rayLength = (sphere != null ? sphere.radius * transform.lossyScale.y : 0.5f) + 0.3f;
        if (!Physics.Raycast(transform.position, Vector3.down, out var hit, rayLength, ~0, QueryTriggerInteraction.Ignore)) return false;
        return hit.collider.TryGetComponent<ArenaSection>(out var section) && section.IsFrozen;
    }
}
