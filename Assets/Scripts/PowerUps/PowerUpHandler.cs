using System.Collections;
using UnityEngine;

public class PowerUpHandler : MonoBehaviour
{
    public PowerUpModifier ActivePowerUp { get; private set; }

    private Coroutine powerUpCoroutine;

 
    public void AddPowerUp(PowerUpModifier powerUp)
    {
    
        if(ActivePowerUp != null)
        {
            ActivePowerUp = null;
            powerUpCoroutine = null;
            ActivePowerUp = powerUp;
        }else
            ActivePowerUp = powerUp;

        if (powerUpCoroutine != null)
        {
            StopCoroutine(powerUpCoroutine);
        }

        powerUpCoroutine = StartCoroutine(
            RemoveAfterDuration(powerUp)
        );
    }

    private IEnumerator RemoveAfterDuration(PowerUpModifier powerUp)
    {
        yield return new WaitForSeconds(powerUp.Duration);

        if (ActivePowerUp == powerUp)
        {
            ActivePowerUp = null;
        }

        powerUpCoroutine = null;
    }
    public bool IsInvulnerable()
    {
        return ActivePowerUp is InvulnerablePowerUp;
    }

    public void EndPowerup()
    {
        if(ActivePowerUp != null)
        {
            ActivePowerUp = null;
            powerUpCoroutine = null;
        }
    }
}