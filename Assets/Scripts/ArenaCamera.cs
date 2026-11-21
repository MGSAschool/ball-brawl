using UnityEngine;

public class ArenaCamera : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private float minDistance = 20f;
    [SerializeField] private float maxDistance = 45f;
    [SerializeField] private float zoomPerUnitSpread = 1.1f;
    [SerializeField] private float smoothTime = 0.4f;
    [SerializeField] private float referenceAspect = 16f / 9f;
    [SerializeField] private float lobbyDistance = 40f;

    private Vector3 velocity;
    private Camera cam;

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        Vector3 center = Vector3.zero;
        if (arenaCenter != null) center = arenaCenter.position;

        Bounds view = new Bounds(center, Vector3.zero);
        bool anyPlayer = AddPlayersToView(ref view);

        float distance = minDistance * 1.5f;
        if (anyPlayer)
        {
            float spread = new Vector2(view.size.x, view.size.z).magnitude;
            distance = Mathf.Clamp(minDistance + spread * zoomPerUnitSpread, minDistance, maxDistance);
        }

        if (match != null && match.State == MatchState.Lobby)
        {
            view.center = center;
            distance = lobbyDistance;
        }

        float aspect = referenceAspect;
        if (cam != null) aspect = cam.aspect;
        distance *= Mathf.Max(1f, referenceAspect / Mathf.Max(0.1f, aspect));

        Vector3 target = view.center - transform.forward * distance;
        transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, smoothTime);
    }

    private bool AddPlayersToView(ref Bounds view)
    {
        if (match == null) return false;

        bool anyPlayer = false;
        foreach (PlayerEntry entry in match.Players)
        {
            if (entry.Player == null || entry.IsRespawning) continue;

            Vector3 position = entry.Player.transform.position;
            if (!anyPlayer) view = new Bounds(position, Vector3.zero);
            else view.Encapsulate(position);
            anyPlayer = true;
        }
        return anyPlayer;
    }
}
