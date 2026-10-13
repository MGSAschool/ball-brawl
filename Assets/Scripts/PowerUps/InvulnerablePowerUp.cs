using UnityEngine;
[CreateAssetMenu(
    fileName = "Invulnerable PowerUp",
    menuName = "PowerUps/Modifier/Invulnerable"
)]
public class InvulnerablePowerUp : PowerUpModifier
{
    public override void ApplyModifier(GameObject target)
    {
        if(target.TryGetComponent<PowerUpHandler>(out var handler))
        {
            handler.AddPowerUp(this);
        }        
    }
}
