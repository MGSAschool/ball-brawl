using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.TryGetComponent<PlayerController>(out var player))
        {
            player.Eliminated();
        }
    }
}
