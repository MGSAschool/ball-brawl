using UnityEngine;

public class KnockbackComponent : MonoBehaviour, IKnockable
{
    private Rigidbody rb;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    public void ApplyKnockback(Vector3 knockbackDirection, float knockbackForce)
    {
        if (rb != null)
        {
            rb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
        }
    }
}
