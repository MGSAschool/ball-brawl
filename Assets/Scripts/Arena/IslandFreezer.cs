using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FinaleIsland))]
public class IslandFreezer : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [SerializeField] private Vector2 freezeInterval = new Vector2(9f, 13f);
    [SerializeField] private Vector2Int padsPerFreeze = new Vector2Int(3, 5);
    [SerializeField] private float firstFreezeDelay = 5f;
    [SerializeField] private float freezeWarningDuration = 2f;
    [SerializeField] private float frozenDuration = 4f;
    [SerializeField] private Color iceColor = new Color(0.55f, 0.85f, 1f);

    private FinaleIsland island;
    private PhysicsMaterial iceMaterial;
    private bool freezing;
    private float nextFreezeTime;
    private bool explained;

    private void Awake()
    {
        island = GetComponent<FinaleIsland>();
        if (match == null) match = FindAnyObjectByType<MatchManager>();
    }

    private void Start()
    {
        iceMaterial = MakeIceMaterial(GetComponent<ArenaFloor>().FloorPhysics);
    }

    public void StartFreezing()
    {
        freezing = true;
        explained = false;
        nextFreezeTime = Time.time + island.RiseDuration + firstFreezeDelay;
        GiveIceSlideToPlayers();
    }

    public void StopFreezing()
    {
        freezing = false;
    }

    private void Update()
    {
        if (!freezing || Time.time < nextFreezeTime) return;

        nextFreezeTime = Time.time + Random.Range(freezeInterval.x, freezeInterval.y);
        FreezeSomePads();
    }

    private void FreezeSomePads()
    {
        List<SectionFreeze> candidates = new List<SectionFreeze>();
        foreach (ArenaSection pad in island.Pads)
        {
            SectionFreeze ice = pad.GetComponent<SectionFreeze>();
            if (pad.IsSolid && ice != null && ice.State == SectionFreeze.FreezeState.None) candidates.Add(ice);
        }

        int count = Mathf.Min(Random.Range(padsPerFreeze.x, padsPerFreeze.y + 1), candidates.Count);
        for (int i = 0; i < count; i++)
        {
            int pick = Random.Range(0, candidates.Count);
            candidates[pick].TriggerFreeze(freezeWarningDuration, frozenDuration, iceColor, iceMaterial);
            candidates.RemoveAt(pick);
        }

        if (count > 0 && !explained)
        {
            explained = true;
            match.Announce("Blue, flashing floor freezes into slippery ice!");
        }
    }

    private void GiveIceSlideToPlayers()
    {
        foreach (PlayerEntry entry in match.Players)
        {
            if (entry.Player == null) continue;
            if (entry.Player.GetComponent<IceSlide>() == null) entry.Player.gameObject.AddComponent<IceSlide>();
        }
    }

    private PhysicsMaterial MakeIceMaterial(PhysicsMaterial normal)
    {
        PhysicsMaterial ice = new PhysicsMaterial("Ice");
        ice.dynamicFriction = 0f;
        ice.staticFriction = 0f;
        ice.frictionCombine = PhysicsMaterialCombine.Minimum;
        ice.bounciness = 0f;
        ice.bounceCombine = PhysicsMaterialCombine.Average;
        if (normal != null)
        {
            ice.bounciness = normal.bounciness;
            ice.bounceCombine = normal.bounceCombine;
        }
        return ice;
    }
}
