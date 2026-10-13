using UnityEngine;

public class KnockbackDealer : MonoBehaviour
{
    [SerializeField] private float BaseKnockbackForce = 10f;
    private PowerUpHandler powerUpHandler;
    void Awake()
    {
        powerUpHandler = GetComponent<PowerUpHandler>();
    }
    private void OnCollisionEnter(Collision collision)
    {
        

        if (collision.gameObject.TryGetComponent<IKnockable>(out var knockReceiver))
        {
            Vector3 knockbackDirection =
                (collision.transform.position - transform.position).normalized;

            float force = BaseKnockbackForce;

            if(powerUpHandler != null && powerUpHandler.ActivePowerUp is SuperKnockbackPowerUp)
            {
                if(collision.gameObject.TryGetComponent<PowerUpHandler>(out var handler) &&
                    handler.IsInvulnerable()) return;

                force = powerUpHandler.ActivePowerUp.ModifyMultiplier(force); // basically force * multiplier
            }
            
            knockReceiver.ApplyKnockback(knockbackDirection, force);
        }
    }
}