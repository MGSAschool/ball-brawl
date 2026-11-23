using UnityEngine;

[DefaultExecutionOrder(100)]
public class BallPhysics : MonoBehaviour
{
    [Header("Wall Slide")]
    [SerializeField] private float minFloorFlatness = 0.5f;
    [SerializeField] private float slideDownSpeed = 4f;

    [Header("Speed Hits")]
    [SerializeField] private float hitTransfer = 1f;
    [SerializeField] private float maxHitForce = 100f;
    [SerializeField] private float upwardLift = 0.4f;

    private BrawlPlayer player;
    private Rigidbody rb;
    private Vector3 speedBeforeStep;

    private bool touchingWall;
    private bool touchingFloor;
    private Vector3 wallNormal;

    private BrawlPlayer hitTarget;
    private Vector3 hitDirection;
    private float hitSpeed;

    private void Awake()
    {
        player = GetComponent<BrawlPlayer>();
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (rb == null || rb.isKinematic)
        {
            ClearStepInfo();
            return;
        }

        ThrowHitTarget();
        SlideOffWall();

        speedBeforeStep = rb.linearVelocity;
        ClearStepInfo();
    }

    private void ClearStepInfo()
    {
        touchingWall = false;
        touchingFloor = false;
        hitTarget = null;
    }

    private void SlideOffWall()
    {
        if (!touchingWall || touchingFloor) return;

        Vector3 velocity = rb.linearVelocity;
        float intoWall = Vector3.Dot(velocity, wallNormal);
        if (intoWall < 0f) velocity -= wallNormal * intoWall;

        if (velocity.y < 1f && velocity.y > -slideDownSpeed)
        {
            velocity.y = Mathf.MoveTowards(velocity.y, -slideDownSpeed, slideDownSpeed * 4f * Time.fixedDeltaTime);
        }
        rb.linearVelocity = velocity;
    }

    private void ThrowHitTarget()
    {
        if (hitTarget == null) return;

        Rigidbody targetBody = hitTarget.GetComponent<Rigidbody>();
        if (targetBody == null || targetBody.isKinematic) return;

        float wantedSpeed = hitSpeed * hitTransfer;
        if (targetBody.linearVelocity.magnitude >= wantedSpeed) return;

        float force = Mathf.Min(targetBody.mass * wantedSpeed, maxHitForce);
        hitTarget.ApplyKnockback(hitDirection, force, player);
    }

    private void OnCollisionEnter(Collision collision)
    {
        BrawlPlayer otherPlayer = collision.gameObject.GetComponent<BrawlPlayer>();
        if (otherPlayer != null)
        {
            RememberHit(otherPlayer);
            return;
        }
        ReadContacts(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.GetComponent<BrawlPlayer>() != null) return;
        ReadContacts(collision);
    }

    private void ReadContacts(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;
            if (normal.y >= minFloorFlatness)
            {
                touchingFloor = true;
            }
            else if (normal.y > -minFloorFlatness)
            {
                touchingWall = true;
                wallNormal = new Vector3(normal.x, 0f, normal.z).normalized;
            }
        }
    }

    private void RememberHit(BrawlPlayer otherPlayer)
    {
        if (player == null) return;

        Vector3 toOther = otherPlayer.transform.position - transform.position;
        toOther.y = 0f;
        if (toOther.sqrMagnitude < 0.0001f) return;
        toOther.Normalize();

        Vector3 flatSpeed = new Vector3(speedBeforeStep.x, 0f, speedBeforeStep.z);
        float speedTowardOther = Vector3.Dot(flatSpeed, toOther);
        if (speedTowardOther <= 0f) return;
        if (hitTarget != null && speedTowardOther <= hitSpeed) return;

        hitTarget = otherPlayer;
        hitSpeed = speedTowardOther;
        hitDirection = (toOther + Vector3.up * upwardLift).normalized;
    }
}
