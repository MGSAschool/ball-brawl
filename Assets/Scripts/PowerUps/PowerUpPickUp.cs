using UnityEngine;

public class PowerUpPickUp : MonoBehaviour
{
    private PowerUpModifier powerUp;

    public void SetupPowerUp(PowerUpModifier powerUp)
    {
        this.powerUp = powerUp;
    }
     private void OnTriggerEnter(Collider other)
    {   
        if(other.CompareTag("Player"))
        {
            powerUp.ApplyModifier(other.gameObject);
            Destroy(gameObject);
        }
    }
}
