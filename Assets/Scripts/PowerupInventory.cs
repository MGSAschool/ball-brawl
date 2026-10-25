using System;
using System.Collections.Generic;
using UnityEngine;

public class PowerupInventory : MonoBehaviour
{
    [SerializeField] private int maxCapacity = 2;

    public List<PowerUpModifier> powerupInventory = new();
    private PlayerInputReader playerInput;
    public event Action OnPowerupPickup;
    public event Action OnPowerupUse;
    void Awake()
    {
        playerInput = GetComponent<PlayerInputReader>();
    }
    void OnEnable()
    {
        if (playerInput != null)
            playerInput.PowerupPressed += UsePowerUp;
    }

    void OnDisable()
    {
        if (playerInput != null)
            playerInput.PowerupPressed -= UsePowerUp;
    }

    public void StorePowerUp(PowerUpModifier powerUp)
    {
        if (powerUp == null || maxCapacity <= 0) return;

        if (powerupInventory.Count >= maxCapacity)
        {
            powerupInventory.RemoveAt(0);
        }

        powerupInventory.Add(powerUp);
        OnPowerupPickup?.Invoke();
    }

    private void UsePowerUp()
    {
 
        if (powerupInventory.Count == 0) return;

        PowerUpModifier powerUp = powerupInventory[0];
        //Removes oldest stored powerup
        powerupInventory.RemoveAt(0);
        //applies effects 
        powerUp.ApplyModifier(gameObject);
        OnPowerupUse?.Invoke();
    }
}