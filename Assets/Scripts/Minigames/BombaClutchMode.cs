using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BombSpawner))]
public class BombaClutchMode : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [SerializeField] private int pointsPerBomb = 1;

    [Header("Chain Reactions")]
    [SerializeField] private float chainRadiusFraction = 0.55f;
    [SerializeField] private float chainDelay = 0.15f;

    private readonly Dictionary<BrawlPlayer, int> chains = new Dictionary<BrawlPlayer, int>();
    private BombSpawner spawner;
    private bool running;
    private bool explained;

    private void Awake()
    {
        spawner = GetComponent<BombSpawner>();
        if (spawner == null) spawner = gameObject.AddComponent<BombSpawner>();
        if (match == null) match = FindAnyObjectByType<MatchManager>();
    }

    private void OnEnable()
    {
        if (match != null) match.StateChanged += OnMatchStateChanged;
        Bomb.Detonated += OnDetonated;
    }

    private void OnDisable()
    {
        if (match != null) match.StateChanged -= OnMatchStateChanged;
        Bomb.Detonated -= OnDetonated;
        StopRound();
    }

    private void OnMatchStateChanged(MatchState state)
    {
        running = state == MatchState.Playing && match.Mode == GameMode.BombaClutch;
        if (running)
        {
            explained = false;
            spawner.StartSpawning();
        }
        else
        {
            StopRound();
        }
    }

    private void Update()
    {
        if (!running || explained) return;
        if (match.TimeRemaining < match.Duration - 2f)
        {
            explained = true;
            match.Announce("Roll into bombs to set them off: +1 each. Chains count too!");
        }
    }

    private void OnDetonated(Bomb bomb, BrawlPlayer setOffBy)
    {
        Vfx.Explosion(bomb.transform.position, bomb.ExplosionRadius);
        if (!running || setOffBy == null) return;

        int chain = 0;
        chains.TryGetValue(setOffBy, out chain);
        chain++;
        chains[setOffBy] = chain;

        if (match.AwardPoints(setOffBy, pointsPerBomb))
        {
            PlayerEntry entry = match.EntryFor(setOffBy);
            if (chain >= 2 && entry != null) match.Announce(entry.Name + " CHAIN x" + chain + "!");
        }

        bool startedChain = false;
        float reach = bomb.ExplosionRadius * chainRadiusFraction;
        foreach (Bomb other in spawner.Bombs)
        {
            if (other == null || other == bomb || other.IsPrimed) continue;
            if (Vector3.Distance(other.transform.position, bomb.transform.position) > reach) continue;

            StartCoroutine(PrimeLater(other, setOffBy));
            startedChain = true;
        }
        if (!startedChain) StartCoroutine(EndChainLater(setOffBy));
    }

    private IEnumerator PrimeLater(Bomb bomb, BrawlPlayer setOffBy)
    {
        yield return new WaitForSeconds(chainDelay);
        if (bomb != null) bomb.Prime(setOffBy);
    }

    private IEnumerator EndChainLater(BrawlPlayer setOffBy)
    {
        yield return new WaitForSeconds(chainDelay * 2f);
        chains[setOffBy] = 0;
    }

    private void StopRound()
    {
        StopAllCoroutines();
        chains.Clear();
        if (spawner != null) spawner.StopAndClear();
    }
}
