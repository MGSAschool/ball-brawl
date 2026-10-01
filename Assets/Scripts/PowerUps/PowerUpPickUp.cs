using UnityEngine;

public class PowerUpPickUp : MonoBehaviour
{
    [SerializeField] private PowerUpModifier powerUp;
    [SerializeField] private float rotationSpeed = 90f;

    public void SetupPowerUp(PowerUpModifier powerUp)
    {
        this.powerUp = powerUp;
    }

    private void Update()
    {
        // Gentle rotation animation in the arena
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check for tag OR BrawlPlayer script
        if (other.CompareTag("Player") || other.GetComponentInParent<BrawlPlayer>() != null)
        {
            if (powerUp != null)
            {
                powerUp.ApplyModifier(other.gameObject);
            }

            Destroy(gameObject);
        }
    }
}