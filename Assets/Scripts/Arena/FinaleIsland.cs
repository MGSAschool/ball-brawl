using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ArenaFloor))]
public class FinaleIsland : MonoBehaviour
{
    [Header("Shape")]
    [SerializeField] private float islandRadius = 8f;
    [SerializeField] private float islandDiscRadius = 2.5f;
    [SerializeField] private int islandRingCount = 3;
    [SerializeField] private float islandSectionLength = 4.5f;

    [Header("Placement")]
    [SerializeField] private float islandGap = 3f;
    [SerializeField] private bool randomizeIslandSide = true;
    [SerializeField] private float islandAngle = 90f;

    [Header("Rising")]
    [SerializeField] private float islandRiseDuration = 3f;
    [SerializeField] private float riseDistance = 25f;

    [Header("Rock Under The Island")]
    [SerializeField] private Mesh islandModel;
    [SerializeField] private Material islandModelMaterial;

    private ArenaFloor floor;
    private Transform root;
    private readonly List<ArenaSection> pads = new List<ArenaSection>();
    private Transform[] spawns;

    public bool IsUp { get; private set; }
    public float Radius { get { return islandRadius; } }
    public float RiseDuration { get { return islandRiseDuration; } }
    public Vector3 Position { get { return root.position; } }
    public Transform[] Spawns { get { return spawns; } }
    public IReadOnlyList<ArenaSection> Pads { get { return pads; } }

    private void Start()
    {
        floor = GetComponent<ArenaFloor>();
        BuildIsland();
        ResetIsland();
    }

    public void Raise()
    {
        IsUp = true;
        foreach (ArenaSection pad in pads)
        {
            pad.Emerge(islandRiseDuration, riseDistance);
        }
    }

    public void ResetIsland()
    {
        IsUp = false;
        if (root == null) return;

        float angle = islandAngle;
        if (randomizeIslandSide) angle = Random.Range(0f, 360f);
        MoveToSide(angle);

        foreach (ArenaSection pad in pads)
        {
            pad.HideImmediate();
        }
    }

    public Vector3 RandomSpawnPoint()
    {
        return spawns[Random.Range(0, spawns.Length)].position;
    }

    private void MoveToSide(float angleDegrees)
    {
        float distance = floor.Radius + islandGap + islandRadius;
        float angle = angleDegrees * Mathf.Deg2Rad;
        root.position = floor.Center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
        root.rotation = Quaternion.Euler(0f, -(angleDegrees + 180f), 0f);

        foreach (ArenaSection pad in pads)
        {
            pad.SetHome(root.position);
        }
    }

    private void BuildIsland()
    {
        root = new GameObject("Island").transform;
        root.SetParent(transform.parent, false);
        root.position = floor.Center;

        ArenaSection middle = AddPad("Island Middle", 0f, islandDiscRadius, 0f, Mathf.PI * 2f, floor.FloorColor(0, 0));

        int rings = Mathf.Max(1, islandRingCount);
        float ringWidth = (islandRadius - islandDiscRadius) / rings;
        for (int ring = 0; ring < rings; ring++)
        {
            float r0 = islandDiscRadius + ring * ringWidth;
            float r1 = r0 + ringWidth;
            int count = floor.PiecesInRing(r0, r1, Mathf.Max(1f, islandSectionLength));
            float pieceAngle = Mathf.PI * 2f / count;
            float offset = ring * 0.5f * pieceAngle;

            int colorIndex = ring + 1;
            if (ring == rings - 1 && floor.RingColorCount > 0) colorIndex = floor.RingColorCount - 1;

            for (int i = 0; i < count; i++)
            {
                float a0 = offset + i * pieceAngle;
                string name = "Island Ring " + (ring + 1) + " Piece " + (i + 1);
                AddPad(name, r0, r1, a0, a0 + pieceAngle, floor.FloorColor(colorIndex, i));
            }
        }

        if (islandModel != null) AddRock(middle.transform);
        MakeSpawns(islandDiscRadius + ringWidth * 0.5f);
    }

    private ArenaSection AddPad(string name, float r0, float r1, float a0, float a1, Color color)
    {
        ArenaSection pad = floor.MakeSection(root, name, r0, r1, a0, a1, color, false);
        pad.CanFall = false;
        pad.gameObject.AddComponent<SectionFreeze>();
        pads.Add(pad);
        return pad;
    }

    private void AddRock(Transform parent)
    {
        Transform rock = new GameObject("Rock Base", typeof(MeshFilter), typeof(MeshRenderer)).transform;
        rock.SetParent(parent, false);
        rock.GetComponent<MeshFilter>().sharedMesh = islandModel;
        rock.GetComponent<MeshRenderer>().sharedMaterial = islandModelMaterial;

        Bounds bounds = islandModel.bounds;
        float reach = Mathf.Max(0.01f, Mathf.Max(bounds.extents.x, bounds.extents.z));
        float scale = islandRadius * 1.12f / reach;
        rock.localScale = Vector3.one * scale;

        float top = -floor.Thickness * 0.6f - bounds.max.y * scale;
        rock.localPosition = new Vector3(-bounds.center.x * scale, top, -bounds.center.z * scale);
    }

    private void MakeSpawns(float distanceFromMiddle)
    {
        spawns = new Transform[4];
        for (int i = 0; i < spawns.Length; i++)
        {
            float angle = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
            Transform spawn = new GameObject("Island Spawn " + (i + 1)).transform;
            spawn.SetParent(root, false);
            spawn.localPosition = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distanceFromMiddle + Vector3.up * 1.4f;
            spawns[i] = spawn;
        }
    }
}
