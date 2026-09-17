using System.Collections;
using UnityEngine;

public class DeathZone : MonoBehaviour
{
    public Transform spawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<BrawlPlayer>(out var player))
        {
            StartCoroutine(RespawnRoutine(player));
        }
    }

    private IEnumerator RespawnRoutine(BrawlPlayer player)
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();
        
        // Freeze and hide player during delay
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        player.gameObject.SetActive(false);

        yield return new WaitForSeconds(3f);

        // Reset to spawn point
        player.transform.position = spawnPoint != null ? spawnPoint.position : new Vector3(0, 2, 0);
        player.gameObject.SetActive(true);
        rb.isKinematic = false;
    }
}