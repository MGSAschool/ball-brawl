using UnityEngine;

public class ArenaSpawnRules : MonoBehaviour
{
    [SerializeField] private ArenaFloor floor;
    [SerializeField] private FinaleIsland island;

    private void Awake()
    {
        if (floor == null) floor = FindAnyObjectByType<ArenaFloor>();
        if (island == null) island = FindAnyObjectByType<FinaleIsland>();
    }

    public bool IsSafeToSpawn(Vector3 point)
    {
        RaycastHit hit;
        if (!Physics.Raycast(point, Vector3.down, out hit, 6f, ~0, QueryTriggerInteraction.Ignore)) return false;

        ArenaSection section = hit.collider.GetComponent<ArenaSection>();
        return section == null || section.IsSolid;
    }

    public bool TryGetFallbackSpawn(out Vector3 position)
    {
        if (island != null && island.IsUp)
        {
            position = island.RandomSpawnPoint();
            return true;
        }

        Vector3 point;
        if (floor != null && floor.TryGetRandomSolidPoint(0f, out point))
        {
            position = point + Vector3.up * 1.4f;
            return true;
        }

        position = Vector3.zero;
        return false;
    }
}
