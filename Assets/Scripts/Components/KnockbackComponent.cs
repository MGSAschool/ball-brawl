using Unity.VisualScripting;
using UnityEngine;
public class KnockbackComponent : MonoBehaviour, IKnockable
{
    private Rigidbody rb;
    private PowerUpHandler powerUpHandler;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        powerUpHandler = GetComponent<PowerUpHandler>();
    }
    public void ApplyKnockback(Vector3 knockbackDirection, float knockbackForce)
    {
        if(powerUpHandler.IsInvulnerable() && powerUpHandler.ActivePowerUp != null) return; // If the player is invulnerable, do not apply knockback
        if (rb != null )
        {
            rb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
        }
    }
}
