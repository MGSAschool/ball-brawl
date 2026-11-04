using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    [Header("Detonation Settings")]
    [SerializeField] private float fuseTime = 0.5f;
    [SerializeField] private float explosionRadius = 6f;
    [SerializeField] private float explosionForce = 16f;
    [SerializeField] private int scoreValue = 100;

    [Header("Visual & Audio Feedback")]
    [SerializeField] private MeshRenderer bombRenderer;
    [SerializeField] private Color triggeredColor = Color.red;
    [SerializeField] private GameObject explosionVFXPrefab;
    [SerializeField] private AudioClip explosionSFX;
    [Range(0f, 1f)] [SerializeField] private float explosionVolume = 1f;

    private bool isTriggered = false;
    private BombaClutchMinigame gameManager;
    private BrawlPlayer triggeredByPlayer;

    // Raised just before any bomb blows up, with whoever set it off (used by BombaClutchMode)
    public static event System.Action<Bomb, BrawlPlayer> Detonated;
    public bool IsPrimed => isTriggered;
    public float ExplosionRadius => explosionRadius;

    // Sets the bomb off on behalf of a player (chain reactions)
    public void Prime(BrawlPlayer by)
    {
        if (isTriggered) return;
        triggeredByPlayer = by;
        StartCoroutine(DetonationSequence());
    }

    public void Initialize(BombaClutchMinigame manager, float fuseModifier = 1f, float forceModifier = 1f)
    {
        gameManager = manager;
        fuseTime *= fuseModifier;
        explosionForce *= forceModifier;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;

        BrawlPlayer player = other.GetComponentInParent<BrawlPlayer>();
        if (player != null)
        {
            triggeredByPlayer = player;
            StartCoroutine(DetonationSequence());
        }
    }

    private IEnumerator DetonationSequence()
    {
        isTriggered = true;

        if (bombRenderer != null)
        {
            bombRenderer.material.color = triggeredColor;
        }

        yield return new WaitForSeconds(fuseTime);

        Explode();
    }

    private void Explode()
    {
        Detonated?.Invoke(this, triggeredByPlayer);

        // Award points to whoever primed this bomb
        if (gameManager != null && triggeredByPlayer != null)
        {
            gameManager.AwardScore(triggeredByPlayer, scoreValue);
        }

        // Apply knockback to everyone in blast radius
        HashSet<GameObject> affected = new HashSet<GameObject>();
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in hits)
        {
            GameObject targetObj = hit.attachedRigidbody != null ? hit.attachedRigidbody.gameObject : hit.gameObject;
            if (affected.Contains(targetObj)) continue;
            affected.Add(targetObj);

            Vector3 blastDir = (targetObj.transform.position - transform.position);
            blastDir.y = 0f;
            blastDir = blastDir.normalized;
            blastDir.y = 0.45f;
            blastDir.Normalize();

            IKnockable knockable = targetObj.GetComponentInParent<IKnockable>();
            if (knockable is BrawlPlayer brawler)
            {
                // Credit whoever set the bomb off, so a blast-off isn't counted as falling on your own
                brawler.ApplyKnockback(blastDir, explosionForce, triggeredByPlayer);
            }
            else if (knockable != null)
            {
                knockable.ApplyKnockback(blastDir, explosionForce);
            }
            else if (targetObj.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.AddForce(blastDir * explosionForce, ForceMode.Impulse);
            }
        }

        if (explosionSFX != null)
        {
            AudioSource.PlayClipAtPoint(explosionSFX, Camera.main != null ? Camera.main.transform.position : transform.position, explosionVolume);
        }

        if (explosionVFXPrefab != null)
        {
            Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}