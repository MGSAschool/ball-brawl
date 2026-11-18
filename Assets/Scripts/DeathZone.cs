using System.Collections;
using UnityEngine;

public class DeathZone : MonoBehaviour
{
    public Transform spawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        BrawlPlayer player = other.GetComponentInParent<BrawlPlayer>();
        if (player == null) return;

        BombaClutchMinigame minigame = Object.FindAnyObjectByType<BombaClutchMinigame>();
        if (minigame != null)
        {
            minigame.PlayerFell(player);
        }

        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.HandlePlayerFell(player);
            return;
        }

        StartCoroutine(RespawnRoutine(player));
    }
    private IEnumerator RespawnRoutine(BrawlPlayer player)
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        player.gameObject.SetActive(false);

        yield return new WaitForSeconds(3f);

        player.transform.position = spawnPoint != null ? spawnPoint.position : new Vector3(0, 2, 0);
        player.gameObject.SetActive(true);
        rb.isKinematic = false;
    }
}
