using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class BrawlPlayer : MonoBehaviour, IKnockable
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float dashForce = 20f;
    [Tooltip("Seconds between dashes while the Dash power-up is active.")]
    public float dashCooldown = 0.75f;
    [Tooltip("Jump apex height in metres. Kept low: enough to hop over a charge, not to fly.")]
    public float jumpHeight = 1.2f;
    public float groundCheckDistance = 0.15f;

    [Header("Combat")]
    public float baseHitForce = 12f;
    public float knockbackStunDuration = 0.35f;

    // Who last knocked this player around, used to credit knock-offs
    public BrawlPlayer LastAttacker { get; private set; }
    public float LastHitTime { get; private set; } = float.NegativeInfinity;

    public bool ControlsEnabled { get; set; } = true;
    public bool IsInvulnerable => Time.time < invulnerableUntil;
    public bool HasSuperKnockback => Time.time < superKnockbackUntil;
    public bool HasSpeedBoost => Time.time < speedBoostUntil;
    public bool HasDash => Time.time < dashUntil;
    public bool IsGrounded => Physics.Raycast(transform.position, Vector3.down, GroundRayLength, ~0, QueryTriggerInteraction.Ignore);

    private Rigidbody rb;
    private SphereCollider sphere;
    private float dashUntil;
    private float nextDashTime;
    private Vector2 inputDirection;
    private float knockedBackUntil;
    private float invulnerableUntil;
    private float superKnockbackUntil;
    private float superKnockbackMultiplier = 1f;
    private float speedBoostUntil;
    private float speedBoostMultiplier = 1f;

    private bool IsKnockedBack => Time.time < knockedBackUntil;
    private float CurrentMoveSpeed => moveSpeed * (HasSpeedBoost ? speedBoostMultiplier : 1f);
    private float GroundRayLength => (sphere != null ? sphere.radius * transform.lossyScale.y : 0.5f) + groundCheckDistance;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        sphere = GetComponent<SphereCollider>();
    }

    public void OnMove(InputValue value)
    {
        inputDirection = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (!value.isPressed || !ControlsEnabled || IsKnockedBack || rb.isKinematic || !IsGrounded) return;

        // Launch speed that peaks at exactly jumpHeight
        float jumpVelocity = Mathf.Sqrt(2f * -Physics.gravity.y * jumpHeight);
        Vector3 velocity = rb.linearVelocity;
        velocity.y = jumpVelocity;
        rb.linearVelocity = velocity;
    }

    void FixedUpdate()
    {
        // Prevent player movement loop from overriding external explosion impulse
        if (!ControlsEnabled || IsKnockedBack) return;

        if (inputDirection.sqrMagnitude > 0.01f)
        {
            Vector3 moveTarget = new Vector3(inputDirection.x, 0f, inputDirection.y) * CurrentMoveSpeed;
            moveTarget.y = rb.linearVelocity.y;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, moveTarget, 0.25f);
        }
    }

    // --- Power-up effects (triggered by PlayerInventory) ---
    public bool Dash()
    {
        if (!ControlsEnabled || IsKnockedBack || Time.time < nextDashTime) return false;
        nextDashTime = Time.time + dashCooldown;

        Vector3 dir = new Vector3(inputDirection.x, 0f, inputDirection.y).normalized;
        if (dir == Vector3.zero)
        {
            Vector3 flatVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            dir = flatVelocity.sqrMagnitude > 0.01f ? flatVelocity.normalized : transform.forward;
        }
        rb.AddForce(dir * dashForce, ForceMode.Impulse);
        return true;
    }

    // Opens a window where every Dash press dashes (cooldown permitting)
    public void GrantDash(float duration)
    {
        dashUntil = Time.time + duration;
    }

    public void GrantInvulnerability(float duration)
    {
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + duration);
    }

    public void GrantSuperKnockback(float multiplier, float duration)
    {
        superKnockbackMultiplier = multiplier;
        superKnockbackUntil = Time.time + duration;
    }

    public void GrantSpeedBoost(float multiplier, float duration)
    {
        speedBoostMultiplier = multiplier;
        speedBoostUntil = Time.time + duration;
    }

    // Clears effects, momentum and hit credit (used on respawn / match start)
    public void ResetState()
    {
        invulnerableUntil = 0f;
        superKnockbackUntil = 0f;
        speedBoostUntil = 0f;
        dashUntil = 0f;
        nextDashTime = 0f;
        knockedBackUntil = 0f;
        ClearLastAttacker();
        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void ClearLastAttacker()
    {
        LastAttacker = null;
        LastHitTime = float.NegativeInfinity;
    }

    // --- IKnockable Implementation ---
    public void ApplyKnockback(Vector3 direction, float force)
    {
        ApplyKnockback(direction, force, null);
    }

    public void ApplyKnockback(Vector3 direction, float force, BrawlPlayer attacker)
    {
        if (IsInvulnerable || rb.isKinematic) return;

        if (attacker != null && attacker != this)
        {
            LastAttacker = attacker;
            LastHitTime = Time.time;
        }

        // Reset existing velocity so knockback takes full effect
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(direction * force, ForceMode.Impulse);
        knockedBackUntil = Time.time + knockbackStunDuration;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<BrawlPlayer>(out var otherPlayer))
        {
            if (otherPlayer.IsInvulnerable) return;

            Vector3 launchDirection = (collision.transform.position - transform.position).normalized;
            launchDirection.y = 0.4f;

            float force = baseHitForce * (HasSuperKnockback ? superKnockbackMultiplier : 1f);
            otherPlayer.ApplyKnockback(launchDirection.normalized, force, this);
        }
    }
}
