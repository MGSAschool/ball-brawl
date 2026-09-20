using UnityEngine;

public class KnockbackDealer : MonoBehaviour
{
    public enum KnockbackType { Normal, Super }

    public KnockbackType knockbackType = KnockbackType.Normal;
    public float knockbackForce = 10f;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<IKnockable>(out var knockable))
        {
            Vector3 knockbackDirection =
                (collision.transform.position - transform.position).normalized;

            float force = knockbackForce;

            if (knockbackType == KnockbackType.Super)
            {
                force *= 2f;
                knockbackType = KnockbackType.Normal;
            }
            knockable.ApplyKnockback(knockbackDirection, force);
        }
    }
}