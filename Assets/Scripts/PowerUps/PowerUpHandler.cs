using System.Collections;
using UnityEngine;

public class PowerUpHandler : MonoBehaviour
{
    public PowerUpModifier ActivePowerUp { get; private set; }

    private Coroutine powerUpCoroutine;
    public bool hasPowerUpActive = false;

    public void AddPowerUp(PowerUpModifier powerUp)
    {
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
}