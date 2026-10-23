using UnityEngine;

// Removes a floating pickup once the floor beneath it is gone (e.g. a falling platform).
public class DespawnWhenUnsupported : MonoBehaviour
{
    [SerializeField] private float checkInterval = 0.25f;
    [Tooltip("How far below the object the floor may be before it counts as unsupported.")]
    [SerializeField] private float maxDrop = 4f;

    private float nextCheck;

    private void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + checkInterval;

        if (!Physics.Raycast(transform.position, Vector3.down, maxDrop, ~0, QueryTriggerInteraction.Ignore))
        {
            Destroy(gameObject);
        }
    }
}
