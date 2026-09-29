using UnityEngine;

public interface IKnockable
{
    void ApplyKnockback(Vector3 knockbackDirection, float knockbackForce);
}
