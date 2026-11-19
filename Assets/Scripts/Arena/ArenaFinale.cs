using UnityEngine;

[RequireComponent(typeof(FallingPlatforms), typeof(FinaleIsland), typeof(ArenaStorm))]
public class ArenaFinale : MonoBehaviour
{
    private enum Phase { Off, Normal, Permanent, Island, Storm, Ruins }

    [SerializeField] private MatchManager match;
    [SerializeField] private CoinRush coinRush;

    [Header("Seconds Left On The Timer")]
    [SerializeField] private float permanentFallsAt = 80f;
    [SerializeField] private float islandAppearsAt = 60f;
    [SerializeField] private float stormAt = 40f;

    [Header("Ruins")]
    [Range(0f, 1f)]
    [SerializeField] private float ruinsFraction = 0.35f;
    [SerializeField] private int ruinsCoins = 12;
    [SerializeField] private float ruinsCoinDuration = 15f;

    private ArenaFloor floor;
    private FallingPlatforms falls;
    private FinaleIsland island;
    private IslandFreezer freezer;
    private ArenaStorm storm;
    private Phase phase = Phase.Off;
    private float treasureAt;

    private void Awake()
    {
        floor = GetComponent<ArenaFloor>();
        falls = GetComponent<FallingPlatforms>();
        island = GetComponent<FinaleIsland>();
        freezer = GetComponent<IslandFreezer>();
        storm = GetComponent<ArenaStorm>();
        if (match == null) match = FindAnyObjectByType<MatchManager>();
        if (coinRush == null) coinRush = FindAnyObjectByType<CoinRush>();
    }

    private void OnEnable()
    {
        if (match != null) match.StateChanged += OnMatchStateChanged;
    }

    private void OnDisable()
    {
        if (match != null) match.StateChanged -= OnMatchStateChanged;
    }

    public bool TryGetFinaleView(out Bounds view)
    {
        view = new Bounds();
        if (phase != Phase.Storm && phase != Phase.Ruins) return false;

        view = new Bounds(island.Position, Vector3.one * island.Radius * 2f);
        view.Encapsulate(new Bounds(floor.Center, Vector3.one * floor.Radius));
        return true;
    }

    private void OnMatchStateChanged(MatchState state)
    {
        if (state == MatchState.Playing && match.Mode == GameMode.Brawl)
        {
            phase = Phase.Normal;
        }
        else
        {
            ResetFinale();
        }
    }

    private void Update()
    {
        if (phase == Phase.Off) return;
        float timeLeft = match.TimeRemaining;

        if (phase == Phase.Normal && timeLeft <= permanentFallsAt) StartPermanentFalls();
        if (phase == Phase.Permanent && timeLeft <= islandAppearsAt) RaiseIsland();
        if (phase == Phase.Island && timeLeft <= stormAt + storm.WarningTime) StartStorm();
        if (phase == Phase.Storm && storm.HasStruck) TurnIntoRuins();
        if (phase == Phase.Ruins && treasureAt > 0f && Time.time >= treasureAt) DropTreasure(timeLeft);
    }

    private void StartPermanentFalls()
    {
        phase = Phase.Permanent;
        falls.MakeFallsPermanent();
        match.Announce("Fallen floor won't come back now!");
    }

    private void RaiseIsland()
    {
        phase = Phase.Island;
        island.Raise();
        match.Spawner.SetSpawnOverride(island.Spawns);
        if (freezer != null) freezer.StartFreezing();

        int piecesToKeep = Mathf.CeilToInt(floor.Sections.Count * ruinsFraction);
        falls.PaceFallsUntil(piecesToKeep, stormAt + storm.WarningTime);
        match.Announce("NEW PLATFORM! Get over there before the storm hits!");
    }

    private void StartStorm()
    {
        phase = Phase.Storm;
        falls.StopFalling();
        storm.Begin();
    }

    private void TurnIntoRuins()
    {
        phase = Phase.Ruins;
        treasureAt = Time.time + 3f;
    }

    private void DropTreasure(float timeLeft)
    {
        treasureAt = 0f;
        if (coinRush == null) return;

        float duration = Mathf.Min(ruinsCoinDuration, Mathf.Max(1f, timeLeft - 1f));
        coinRush.StartRushNow("TREASURE ON THE RUINS! Jump back for coins!", ruinsCoins, duration);
    }

    private void ResetFinale()
    {
        phase = Phase.Off;
        treasureAt = 0f;
        storm.ResetStorm();
        island.ResetIsland();
        if (freezer != null) freezer.StopFreezing();
        if (match != null && match.Spawner != null) match.Spawner.ClearSpawnOverride();
    }
}
