using UnityEngine;

[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(Rigidbody))]
public class IceSlide : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float grip = 0.15f;
    [SerializeField] private float iceDamping = 0.25f;

    private Rigidbody rb;
    private SphereCollider sphere;
    private float normalDamping;
    private Vector3 velocityBeforeSteering;
    private bool sampled;

    public bool OnIce { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        sphere = GetComponent<SphereCollider>();
        normalDamping = rb.linearDamping;
        if (GetComponent<IceSlideSampler>() == null) gameObject.AddComponent<IceSlideSampler>();
    }

    private void OnDisable()
    {
        if (rb != null) rb.linearDamping = normalDamping;
    }

    public void RecordVelocity(Vector3 velocity)
    {
        velocityBeforeSteering = velocity;
        sampled = true;
    }

    private void FixedUpdate()
    {
        bool wasOnIce = OnIce;
        OnIce = CheckOnIce();
        if (OnIce != wasOnIce)
        {
            if (OnIce) rb.linearDamping = iceDamping;
            else rb.linearDamping = normalDamping;
        }
        if (!OnIce) return;

        Vector3 steered = rb.linearVelocity;
        Vector3 before = new Vector3(velocityBeforeSteering.x, 0f, velocityBeforeSteering.z);
        Vector3 after = new Vector3(steered.x, 0f, steered.z);
        Vector3 flat = Vector3.Lerp(before, after, grip);
        rb.linearVelocity = new Vector3(flat.x, steered.y, flat.z);
    }

    private bool CheckOnIce()
    {
        if (!sampled || rb.isKinematic) return false;
        sampled = false;

        float rayLength = 0.5f + 0.3f;
        if (sphere != null) rayLength = sphere.radius * transform.lossyScale.y + 0.3f;

        RaycastHit hit;
        if (!Physics.Raycast(transform.position, Vector3.down, out hit, rayLength, ~0, QueryTriggerInteraction.Ignore)) return false;

        SectionFreeze ice = hit.collider.GetComponent<SectionFreeze>();
        return ice != null && ice.IsFrozen;
    }
}
