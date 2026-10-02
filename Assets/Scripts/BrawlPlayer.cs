using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class BrawlPlayer : MonoBehaviour, IKnockable
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float dashForce = 20f;
    public float dashCooldown = 2f;
    private float dashTimer;

    [Header("Combat")]
    public bool isInvulnerable = false;
    public bool hasSuperKnockback = false;
    public float baseHitForce = 12f;
    public float superKnockbackMultiplier = 2.5f;

    private Rigidbody rb;
    private Vector2 inputDirection;
    private bool isKnockedBack = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    public void OnMove(InputValue value)
    {
        inputDirection = value.Get<Vector2>();
    }

    public void OnDash(InputValue value)
    {
        if (value.isPressed && dashTimer <= 0 && !isKnockedBack)
        {
            TriggerDash();
        }
    }

    void Update()
    {
        if (dashTimer > 0)
        {
            dashTimer -= Time.deltaTime;
        }
    }

    void FixedUpdate()
    {
        // Prevent player movement loop from overriding external explosion impulse
        if (isKnockedBack) return;

        if (inputDirection.sqrMagnitude > 0.01f)
        {
            Vector3 moveTarget = new Vector3(inputDirection.x, 0f, inputDirection.y) * moveSpeed;
            moveTarget.y = rb.linearVelocity.y;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, moveTarget, 0.25f);
        }
    }

    private void TriggerDash()
    {
        dashTimer = dashCooldown;
        Vector3 dir = new Vector3(inputDirection.x, 0f, inputDirection.y).normalized;
        if (dir == Vector3.zero) dir = transform.forward;
        rb.AddForce(dir * dashForce, ForceMode.Impulse);
    }

    // --- IKnockable Implementation ---
    public void ApplyKnockback(Vector3 direction, float force)
    {
        if (isInvulnerable) return;

        // Reset existing velocity so knockback takes full effect
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(direction * force, ForceMode.Impulse);

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(KnockbackRoutine(0.35f));
        }
    }

    private IEnumerator KnockbackRoutine(float duration)
    {
        isKnockedBack = true;
        yield return new WaitForSeconds(duration);
        isKnockedBack = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<BrawlPlayer>(out var otherPlayer))
        {
            if (otherPlayer.isInvulnerable) return;

            Vector3 launchDirection = (collision.transform.position - transform.position).normalized;
            launchDirection.y = 0.4f;

            float force = baseHitForce * (hasSuperKnockback ? superKnockbackMultiplier : 1f);
            otherPlayer.ApplyKnockback(launchDirection.normalized, force);
        }
    }
}