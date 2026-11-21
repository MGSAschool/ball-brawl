using UnityEngine;

[RequireComponent(typeof(MatchManager))]
public class PlayerSpawner : MonoBehaviour
{
    private const int FallbackSpotCount = 4;

    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private float spawnProtection = 1.5f;
    [SerializeField] private float fallHeight = -15f;
    [SerializeField] private ArenaFloor arena;

    private MatchManager match;

    private void Awake()
    {
        match = GetComponent<MatchManager>();
        if (arena == null) arena = FindAnyObjectByType<ArenaFloor>();
    }

    private void Update()
    {
        foreach (PlayerEntry entry in match.Players)
        {
            if (entry.Player == null) continue;

            if (entry.IsRespawning)
            {
                if (Time.time >= entry.RespawnAt) FinishRespawn(entry);
            }
            else if (entry.Player.transform.position.y < fallHeight)
            {
                match.HandlePlayerFell(entry.Player);
            }
        }
    }

    public void StartRespawn(PlayerEntry entry)
    {
        Hide(entry);
        entry.IsRespawning = true;
        entry.RespawnAt = Time.time + respawnDelay;
    }

    public void PlaceAtSpawn(PlayerEntry entry, Vector3 position)
    {
        Rigidbody rb = entry.Player.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.position = position;
        entry.Player.transform.position = position;
        entry.Player.ResetState();
        SetVisible(entry, true);
        entry.Player.ControlsEnabled = match.State == MatchState.Lobby || match.State == MatchState.Playing;
    }

    public Vector3 GetSpawnPosition(int index)
    {
        Transform[] points = ActivePoints();
        if (points != null && points.Length > 0)
        {
            return points[index % points.Length].position;
        }

        float angle = index * Mathf.PI * 2f / FallbackSpotCount;
        Vector3 ring = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 8f;
        return transform.position + ring + Vector3.up * 2f;
    }

    private void FinishRespawn(PlayerEntry entry)
    {
        entry.IsRespawning = false;
        PlaceAtSpawn(entry, GetSafestSpawnPosition(entry));
        if (match.State == MatchState.Playing) entry.Player.GrantInvulnerability(spawnProtection);

        Sfx.Play(Sound.Respawn, entry.Player.transform.position, 0.7f);
        Vfx.Poof(entry.Player.transform.position, entry.Color);
    }

    private Vector3 GetSafestSpawnPosition(PlayerEntry respawning)
    {
        Transform[] points = ActivePoints();
        int count = FallbackSpotCount;
        if (points != null && points.Length > 0) count = points.Length;

        Vector3 best = GetSpawnPosition(0);
        float bestDistance = float.NegativeInfinity;
        bool foundSafeSpot = false;

        for (int i = 0; i < count; i++)
        {
            Vector3 candidate = GetSpawnPosition(i);
            if (!IsSafeToSpawn(candidate)) continue;
            foundSafeSpot = true;

            float nearest = DistanceToNearestPlayer(candidate, respawning);
            if (nearest > bestDistance)
            {
                bestDistance = nearest;
                best = candidate;
            }
        }

        Vector3 fallback;
        if (!foundSafeSpot && TryGetFallbackSpawn(out fallback))
        {
            return fallback;
        }
        return best;
    }

    private bool IsSafeToSpawn(Vector3 point)
    {
        RaycastHit hit;
        if (!Physics.Raycast(point, Vector3.down, out hit, 6f, ~0, QueryTriggerInteraction.Ignore)) return false;

        ArenaSection section = hit.collider.GetComponent<ArenaSection>();
        return section == null || section.IsSolid;
    }

    private bool TryGetFallbackSpawn(out Vector3 position)
    {
        Vector3 point;
        if (arena != null && arena.TryGetRandomSolidPoint(0f, out point))
        {
            position = point + Vector3.up * 1.4f;
            return true;
        }

        position = Vector3.zero;
        return false;
    }

    private float DistanceToNearestPlayer(Vector3 point, PlayerEntry ignore)
    {
        float nearest = float.PositiveInfinity;
        foreach (PlayerEntry other in match.Players)
        {
            if (other == ignore || other.IsRespawning || other.Player == null) continue;
            float distance = Vector3.Distance(point, other.Player.transform.position);
            if (distance < nearest) nearest = distance;
        }
        return nearest;
    }

    private Transform[] ActivePoints()
    {
        return spawnPoints;
    }

    private void Hide(PlayerEntry entry)
    {
        Rigidbody rb = entry.Player.GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        entry.Player.ControlsEnabled = false;
        SetVisible(entry, false);
    }

    private void SetVisible(PlayerEntry entry, bool visible)
    {
        foreach (Renderer rend in entry.Player.GetComponentsInChildren<Renderer>())
        {
            rend.enabled = visible;
        }
        foreach (Collider col in entry.Player.GetComponentsInChildren<Collider>())
        {
            col.enabled = visible;
        }
    }
}
