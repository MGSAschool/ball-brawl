using System.Collections;
using UnityEngine;

public enum PowerUpType
{
    None,
    SpeedBoost,
    SuperKnockback,
    Invulnerability
}

public class PlayerInventory : MonoBehaviour
{
    [Header("Current Stored Power-Up")]
    [SerializeField] private PowerUpType currentPowerUp = PowerUpType.None;
    [SerializeField] private KeyCode useKey = KeyCode.F;

    private BrawlPlayer player;

    private void Awake()
    {
        player = GetComponent<BrawlPlayer>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(useKey) && currentPowerUp != PowerUpType.None)
        {
            UsePowerUp();
        }
    }

    public bool StorePowerUp(PowerUpType type)
    {
        // Prevent pickup if already holding one
        if (currentPowerUp != PowerUpType.None) return false;

        currentPowerUp = type;
        Debug.Log($"Collected: {type}. Press {useKey} to activate!");
        return true;
    }

    private void UsePowerUp()
    {
        switch (currentPowerUp)
        {
            case PowerUpType.SpeedBoost:
                StartCoroutine(SpeedBoostRoutine(5f, 1.8f));
                break;
            case PowerUpType.SuperKnockback:
                StartCoroutine(SuperKnockbackRoutine(5f));
                break;
            case PowerUpType.Invulnerability:
                StartCoroutine(InvulnerabilityRoutine(4f));
                break;
        }

        currentPowerUp = PowerUpType.None;
    }

    private IEnumerator SpeedBoostRoutine(float duration, float multiplier)
    {
        float originalSpeed = player.moveSpeed;
        player.moveSpeed *= multiplier;
        yield return new WaitForSeconds(duration);
        player.moveSpeed = originalSpeed;
    }

    private IEnumerator SuperKnockbackRoutine(float duration)
    {
        player.hasSuperKnockback = true;
        yield return new WaitForSeconds(duration);
        player.hasSuperKnockback = false;
    }

    private IEnumerator InvulnerabilityRoutine(float duration)
    {
        player.isInvulnerable = true;
        yield return new WaitForSeconds(duration);
        player.isInvulnerable = false;
    }
}