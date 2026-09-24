using UnityEngine;

public class SuperKnockback : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player") && other.TryGetComponent<KnockbackDealer>(out var player))
        {
            Pickup(player);
            Destroy(gameObject);
        }
    }

    void Pickup(KnockbackDealer player)
    {
        //player.knockbackType = KnockbackDealer.KnockbackType.Super;
    }
}
    