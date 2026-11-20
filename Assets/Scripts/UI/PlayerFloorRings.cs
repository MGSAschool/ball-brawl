using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PlayerFloorRings : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [SerializeField] private float ringDiameter = 1.9f;
    [SerializeField] private float ringMaxDrop = 6f;

    private readonly Dictionary<PlayerEntry, Transform> rings = new Dictionary<PlayerEntry, Transform>();
    private readonly List<Material> materials = new List<Material>();

    private void Awake()
    {
        if (match == null) match = FindAnyObjectByType<MatchManager>();
    }

    private void OnDestroy()
    {
        foreach (Material material in materials)
        {
            if (material != null) Destroy(material);
        }
    }

    private void LateUpdate()
    {
        if (match == null) return;

        foreach (PlayerEntry entry in match.Players)
        {
            Transform ring;
            if (!rings.TryGetValue(entry, out ring))
            {
                ring = MakeRing(entry);
                rings.Add(entry, ring);
            }
            MoveRing(entry, ring);
        }
    }

    private void MoveRing(PlayerEntry entry, Transform ring)
    {
        bool show = entry.Player != null && !entry.IsRespawning && match.State != MatchState.MatchOver;
        if (show)
        {
            Vector3 position = entry.Player.transform.position;
            RaycastHit hit;
            show = Physics.Raycast(position, Vector3.down, out hit, ringMaxDrop, ~0, QueryTriggerInteraction.Ignore);
            if (show)
            {
                ring.position = hit.point + hit.normal * 0.03f;
                ring.rotation = Quaternion.LookRotation(-hit.normal);

                float jumpHeight = Mathf.Clamp01((position.y - hit.point.y - 0.5f) / 3f);
                float pulse = 1f + 0.06f * Mathf.Sin(Time.time * 5f);
                ring.localScale = Vector3.one * (ringDiameter * Mathf.Lerp(1f, 0.6f, jumpHeight) * pulse);
            }
        }
        ring.gameObject.SetActive(show);
    }

    private Transform MakeRing(PlayerEntry entry)
    {
        Color color = new Color(entry.Color.r, entry.Color.g, entry.Color.b, 0.95f);
        Material material = ArtUtil.TransparentLit(color, 1.2f);
        material.SetTexture("_BaseMap", TextureMaker.FloorRing());
        materials.Add(material);

        Transform ring = ArtUtil.MakeShape(PrimitiveType.Quad, entry.Name + " Floor Ring", transform, material);
        ring.GetComponent<MeshRenderer>().receiveShadows = false;
        return ring;
    }
}
