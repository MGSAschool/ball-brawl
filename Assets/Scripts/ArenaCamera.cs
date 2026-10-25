using UnityEngine;

// Shared local-multiplayer camera: keeps its scene rotation and pulls back to frame every
// player on the platform. Looks at the arena center when nobody is in play.
public class ArenaCamera : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private float minDistance = 22f;
    [SerializeField] private float maxDistance = 70f;
    [Tooltip("Extra distance per metre of spread between players.")]
    [SerializeField] private float zoomPerUnitSpread = 1.1f;
    [SerializeField] private float smoothTime = 0.4f;

    private Vector3 velocity;

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
    }

    private void LateUpdate()
    {
        Vector3 fallbackCenter = arenaCenter != null ? arenaCenter.position : Vector3.zero;
        var bounds = new Bounds(fallbackCenter, Vector3.zero);
        bool any = false;

        if (match != null)
        {
            foreach (var p in match.Players)
            {
                if (p.Player == null || p.IsRespawning) continue;
                if (!any) bounds = new Bounds(p.Player.transform.position, Vector3.zero);
                else bounds.Encapsulate(p.Player.transform.position);
                any = true;
            }
        }

        float spread = new Vector2(bounds.size.x, bounds.size.z).magnitude;
        float distance = any ? Mathf.Clamp(minDistance + spread * zoomPerUnitSpread, minDistance, maxDistance) : minDistance * 1.5f;
        Vector3 target = bounds.center - transform.forward * distance;

        transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime);
    }
}
