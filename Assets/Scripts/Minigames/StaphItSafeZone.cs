using System.Collections.Generic;
using UnityEngine;

public class StaphItSafeZone : MonoBehaviour
{
    private static readonly Vector2[] FootprintChecks =
    {
        Vector2.zero, new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
        new Vector2(0.7f, 0.7f), new Vector2(-0.7f, 0.7f), new Vector2(0.7f, -0.7f), new Vector2(-0.7f, -0.7f)
    };

    [SerializeField] private ArenaFloor arena;
    [SerializeField] private float radiusAtStart = 7f;
    [SerializeField] private float radiusAtEnd = 4f;
    [SerializeField] private float minMoveDistance = 12f;

    private readonly List<ArenaSection> lockedSections = new List<ArenaSection>();
    private bool placed;

    public Vector3 Center { get; private set; }
    public float Radius { get; private set; }
    public float MovedAt { get; private set; }
    public bool IsPlaced { get { return placed; } }

    private void Awake()
    {
        if (arena == null) arena = FindAnyObjectByType<ArenaFloor>();
    }

    public void MoveToNewSpot(float progress)
    {
        Radius = Mathf.Lerp(radiusAtStart, radiusAtEnd, progress);
        Vector3 chosen = Vector3.zero;
        if (arena != null) chosen = PickSpot();

        Release();
        Center = chosen;
        LockFloorUnder(chosen, Radius);
        MovedAt = Time.time;
        placed = true;
    }

    public bool Contains(Vector3 position)
    {
        Vector3 flat = new Vector3(position.x - Center.x, 0f, position.z - Center.z);
        return flat.magnitude <= Radius && position.y > Center.y - 1.5f;
    }

    public void Release()
    {
        foreach (ArenaSection section in lockedSections)
        {
            if (section != null) section.CanFall = true;
        }
        lockedSections.Clear();
        placed = false;
    }

    private Vector3 PickSpot()
    {
        Vector3 previous = Center;
        bool hadSpot = placed;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector3 candidate;
            if (!arena.TryGetRandomSolidPoint(0f, out candidate)) break;

            bool tooClose = Vector3.Distance(candidate, previous) < minMoveDistance;
            if (hadSpot && attempt < 20 && tooClose) continue;
            if (!FloorIsSolidUnder(candidate, Radius)) continue;
            return candidate;
        }
        return arena.Center;
    }

    private bool FloorIsSolidUnder(Vector3 center, float radius)
    {
        foreach (Vector2 offset in FootprintChecks)
        {
            Vector3 point = center + new Vector3(offset.x, 0f, offset.y) * radius * 0.85f;
            ArenaSection section = SectionBelow(point);
            if (section == null || !section.IsSolid) return false;
        }
        return true;
    }

    private void LockFloorUnder(Vector3 center, float radius)
    {
        int steps = 6;
        for (int x = -steps; x <= steps; x++)
        {
            for (int z = -steps; z <= steps; z++)
            {
                Vector3 offset = new Vector3(x, 0f, z) / steps * radius;
                if (offset.magnitude > radius) continue;

                ArenaSection section = SectionBelow(center + offset);
                if (section == null || lockedSections.Contains(section)) continue;
                section.CanFall = false;
                lockedSections.Add(section);
            }
        }
    }

    private ArenaSection SectionBelow(Vector3 point)
    {
        RaycastHit hit;
        if (!Physics.Raycast(point + Vector3.up * 3f, Vector3.down, out hit, 8f, ~0, QueryTriggerInteraction.Ignore)) return null;
        return hit.collider.GetComponent<ArenaSection>();
    }
}
