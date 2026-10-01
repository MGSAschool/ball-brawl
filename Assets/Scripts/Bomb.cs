using System.Collections;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    [Header("Detonation Settings")]
    [SerializeField] private float fuseTime = 0.6f;
    [SerializeField] private float explosionRadius = 2.5f;
    [SerializeField] private float explosionForce = 6;
    [SerializeField] private int scoreValue = 100;

    [Header("Visual Feedback")]
    [SerializeField] private MeshRenderer bombRenderer;
    [SerializeField] private Color triggeredColor = Color.red;
    [SerializeField] private GameObject explosionVFXPrefab;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip explosionSFX;
    [Range(0f, 1f)] [SerializeField] private float explosionVolume = 1f;

    private bool isTriggered = false;
    private BombaClutchMinigame gameManager;

    public void Initialize(BombaClutchMinigame manager)
    {
        gameManager = manager;
    }

    private void OnTriggerEnter(Collider other)
{
    if (isTriggered) return;

    // Verify player contact
    if (other.CompareTag("Player") || other.GetComponentInParent<BrawlPlayer>() != null)
    {
        StartCoroutine(DetonationSequence(other.gameObject));
    }
}
    private IEnumerator DetonationSequence(GameObject triggeringPlayer)
    {
        isTriggered = true;

        // Warning phase (flash color or change material)
        if (bombRenderer != null)
        {
            bombRenderer.material.color = triggeredColor;
        }

        yield return new WaitForSeconds(fuseTime);

        Explode();
    }

    private void Explode()
{
    // 1. Award points to the minigame manager
    if (gameManager != null)
    {
        gameManager.AddScore(scoreValue);
    }

    // 2. Apply blast / knockback to players in radius
    Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
    foreach (Collider hit in hits)
    {
        if (hit.TryGetComponent<IKnockable>(out var knockable))
        {
            Vector3 knockDirection = (hit.transform.position - transform.position).normalized;
            knockDirection.y = 0.5f; // Lift slightly
            knockDirection.Normalize();

            // Pass direction and force as separate arguments
            knockable.ApplyKnockback(knockDirection, explosionForce);
        }
        else if (hit.attachedRigidbody != null)
        {
            hit.attachedRigidbody.AddExplosionForce(explosionForce * 6f, transform.position, explosionRadius, 1f, ForceMode.Impulse);
        }
    }

    // 3. Spawn VFX and clean up
    if (explosionVFXPrefab != null)
    {
        Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);
    }

    if (explosionSFX != null)
    {
        AudioSource.PlayClipAtPoint(explosionSFX, transform.position, explosionVolume);
    }

    Destroy(gameObject);
}
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}