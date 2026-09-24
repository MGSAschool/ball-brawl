using UnityEngine;
public class PowerUpSpawner : MonoBehaviour
{
    [SerializeField] private PowerUpModifier[] powerUps;
    [SerializeField] private GameObject pickupPrefab;


    private PowerUpModifier GetWeightPowerUp()
    {
        float totalWeight = 0f;

        foreach(var powerUp in powerUps)
        {
            if(powerUp != null)
                totalWeight += powerUp.Weight;
        }

        if(totalWeight <= 0)
            return null;

        float random = Random.Range(0f, totalWeight);

        foreach(var powerUp in powerUps)
        {
            if(powerUp == null)
                continue;

            random -= powerUp.Weight;

            if(random <= 0)
                return powerUp;
        }
        return null;
    }

    private void Spawn()
    {
        PowerUpModifier selected = GetWeightPowerUp();
        Debug.Log(selected.name);
        if(selected == null)
            return;

        GameObject pickup = Instantiate(
            pickupPrefab, 
            transform.position, 
            Quaternion.identity
        );

        pickup.GetComponent<PowerUpPickUp>().SetupPowerUp(selected);
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Spawn();
        }
    }
}
