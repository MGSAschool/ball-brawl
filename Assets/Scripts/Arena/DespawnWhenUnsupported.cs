using UnityEngine;

public class DespawnWhenUnsupported : MonoBehaviour
{
    [SerializeField] private float checkInterval = 0.25f;
    [SerializeField] private float maxDrop = 4f;

    private float nextCheck;

    private void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + checkInterval;

        bool floorBelow = Physics.Raycast(transform.position, Vector3.down, maxDrop, ~0, QueryTriggerInteraction.Ignore);
        if (!floorBelow) Destroy(gameObject);
    }
}
