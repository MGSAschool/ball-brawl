using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PowerupInventoryUI : MonoBehaviour
{
    private PowerupInventory powerupInventory;
     private static readonly Dictionary<int, Vector2> UIPosition = new()
    {
        { 1, new Vector2(-832, 456) },
        { 2, new Vector2(-832, -456) },
        { 3, new Vector2(832, 456) },
        { 4, new Vector2(832, -456) }
    };

    [SerializeField]private RectTransform rect;
    public Image[] icons;

    void Start()
    {
        icons[0].enabled = false;
        icons[1].enabled = false;
    }
    public void Initialize(int playerIndex, PowerupInventory playerInventory)
    {
        this.powerupInventory = playerInventory;
        if (UIPosition.TryGetValue(playerIndex, out Vector2 position))
        {
            rect.anchoredPosition = position;
        }
        powerupInventory.OnPowerupPickup += RefreshUI;
        powerupInventory.OnPowerupUse += RefreshUI;
    }

    private void RefreshUI()
    {
        for (int i = 0; i < icons.Length; i++)
        {
            if (i < powerupInventory.powerupInventory.Count)
            {
                PowerUpModifier powerUp =
                    powerupInventory.powerupInventory[i];

                icons[i].sprite = powerUp.Sprite;
                icons[i].enabled = powerUp.Sprite != null;
            }
            else
            {
                icons[i].sprite = null;
                icons[i].enabled = false;
            }
        }
    }
}
