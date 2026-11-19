using System.Collections.Generic;
using UnityEngine;

public class ArenaBounds : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [SerializeField] private ArenaFloor arena;

    [Header("Rules")]
    [SerializeField] private float allowedTimeOutside = 3f;
    [SerializeField] private float warningDelay = 0.4f;
    [SerializeField] private float edgeTolerance = 0.75f;
    [SerializeField] private float floorCheckDistance = 30f;

    private readonly Dictionary<PlayerEntry, float> timeOutside = new Dictionary<PlayerEntry, float>();

    public float AllowedTimeOutside { get { return allowedTimeOutside; } }

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        if (arena == null) arena = FindAnyObjectByType<ArenaFloor>();
    }

    private void Update()
    {
        if (match == null || arena == null) return;

        bool checking = match.State == MatchState.Playing || match.State == MatchState.Lobby;
        foreach (PlayerEntry entry in match.Players)
        {
            bool canCheck = checking && entry.Player != null && !entry.IsRespawning && entry.Player.gameObject.activeInHierarchy;
            if (!canCheck || IsOnPlatforms(entry.Player.transform.position))
            {
                timeOutside.Remove(entry);
                continue;
            }

            float outside = 0f;
            timeOutside.TryGetValue(entry, out outside);
            outside += Time.deltaTime;

            if (outside >= allowedTimeOutside)
            {
                timeOutside.Remove(entry);
                match.HandlePlayerFell(entry.Player);
            }
            else
            {
                timeOutside[entry] = outside;
            }
        }
    }

    public bool IsWarning(PlayerEntry entry)
    {
        float outside;
        return timeOutside.TryGetValue(entry, out outside) && outside >= warningDelay;
    }

    public bool TryGetTimeLeft(PlayerEntry entry, out float timeLeft)
    {
        timeLeft = 0f;
        if (!IsWarning(entry)) return false;
        timeLeft = allowedTimeOutside - timeOutside[entry];
        return true;
    }

    private bool IsOnPlatforms(Vector3 position)
    {
        Vector3 offset = position - arena.Center;
        offset.y = 0f;
        if (offset.magnitude <= arena.Radius + edgeTolerance) return true;

        RaycastHit hit;
        if (Physics.Raycast(position, Vector3.down, out hit, floorCheckDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<ArenaSection>() != null;
        }
        return false;
    }
}
