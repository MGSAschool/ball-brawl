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
    [Tooltip("Screen shape the distances were tuned for. Narrower screens (4:3, portrait) pull back to keep the sides in view.")]
    [SerializeField] private float referenceAspect = 16f / 9f;

    private Vector3 velocity;
    private Camera cam;

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        cam = GetComponent<Camera>();
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

        // Horizontal view shrinks with narrower screens, so back off by the same ratio
        float aspect = cam != null ? cam.aspect : referenceAspect;
        distance *= Mathf.Max(1f, referenceAspect / Mathf.Max(0.1f, aspect));

        Vector3 target = bounds.center - transform.forward * distance;

        transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime);
    }
}
