using UnityEngine;
[CreateAssetMenu(
    fileName = "Super Knockback PowerUp",
    menuName = "PowerUps/Modifier/Speed Boost"
)]
public class SpeedBoostPowerUp: PowerUpModifier
{
    [SerializeField] private float multiplier = 2f;
    public float Multiplier => multiplier;
    public override void ApplyModifier(GameObject target)
    {
        if(target.TryGetComponent<PowerUpHandler>(out var handler))
        {
            handler.AddPowerUp(this);
        } 
    }

    public float ModifySpeed(float value)
    {
        return value * Multiplier;
    }
}
