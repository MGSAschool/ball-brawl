using UnityEngine;
[CreateAssetMenu(
    fileName = "Dash PowerUp",
    menuName = "PowerUps/Modifier/Dash"
)]
public class DashPowerUp : PowerUpModifier
{
    public int dashCount = 1;
    public override void ApplyModifier(GameObject target)
    {
        if(target.TryGetComponent<PowerUpHandler>(out var handler))
        {
            handler.AddPowerUp(this);
        }   
    }
}
