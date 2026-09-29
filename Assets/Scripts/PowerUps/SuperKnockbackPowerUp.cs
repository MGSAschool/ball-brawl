using UnityEngine;
using System.Collections;

[CreateAssetMenu(
    fileName = "Super Knockback PowerUp",
    menuName = "PowerUps/Modifier/Super Knockback"
)]
public class SuperKnockbackPowerUp: PowerUpModifier
{
    [SerializeField] private float multiplier = 2f;
    public override void ApplyModifier(GameObject target)
    {
        if(target.TryGetComponent<PowerUpHandler>(out var handler))
        {
            handler.AddPowerUp(this);
        }        
    }

    public override float ModifyKnockback(float value)
    {
        return value * multiplier;
    }
}
