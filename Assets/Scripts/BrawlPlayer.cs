using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BrawlPlayer : MonoBehaviour
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
    private Vector3 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    void Update()
    {
        // WASD or Arrow Keys
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        moveInput = new Vector3(h, 0f, v).normalized;

        if (dashTimer > 0)
        {
            dashTimer -= Time.deltaTime;
        }

        // Left Shift to Dash
        if (Input.GetKeyDown(KeyCode.LeftShift) && dashTimer <= 0)
        {
            TriggerDash();
        }

        // Q for Invulnerability
        if (Input.GetKeyDown(KeyCode.Q) && !isInvulnerable)
        {
            StartCoroutine(InvulnerableRoutine(3f));
        }

        // E for Super Knockback
        if (Input.GetKeyDown(KeyCode.E) && !hasSuperKnockback)
        {
            StartCoroutine(SuperKnockbackRoutine(4f));
        }
    }

    void FixedUpdate()
    {
        if (moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 targetVel = moveInput * moveSpeed;
            targetVel.y = rb.linearVelocity.y;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVel, 0.25f);
        }
    }

    private void TriggerDash()
    {
        dashTimer = dashCooldown;
        Vector3 dir = moveInput != Vector3.zero ? moveInput : transform.forward;
        rb.AddForce(dir * dashForce, ForceMode.Impulse);
    }

    private IEnumerator InvulnerableRoutine(float dur)
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(dur);
        isInvulnerable = false;
    }

    private IEnumerator SuperKnockbackRoutine(float dur)
    {
        hasSuperKnockback = true;
        yield return new WaitForSeconds(dur);
        hasSuperKnockback = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<BrawlPlayer>(out var otherPlayer))
        {
            if (otherPlayer.isInvulnerable) return;

            Vector3 launchDirection = (collision.transform.position - transform.position).normalized;
            launchDirection.y = 0.4f; // Launch angle

            float force = baseHitForce * (hasSuperKnockback ? superKnockbackMultiplier : 1f);
            otherPlayer.GetComponent<Rigidbody>().AddForce(launchDirection * force, ForceMode.Impulse);
        }
    }
}