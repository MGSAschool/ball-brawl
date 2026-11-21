using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ArenaFloor))]
public class FallingPlatforms : MonoBehaviour
{
    [SerializeField] private MatchManager match;

    [Header("Falling")]
    [SerializeField] private Vector2Int piecesPerFall = new Vector2Int(2, 3);
    [SerializeField] private Vector2 fallInterval = new Vector2(4f, 7f);
    [SerializeField] private float firstFallDelay = 8f;
    [SerializeField] private int maxDownAtOnce = 6;
    [SerializeField] private float warningDuration = 2f;
    [SerializeField] private float fallDuration = 1.2f;
    [SerializeField] private float goneDuration = 8f;
    [SerializeField] private float riseDuration = 1.5f;
    [SerializeField] private float fallDistance = 25f;

    [Header("Speed Up")]
    [SerializeField] private float speedUpEvery = 30f;
    [SerializeField] private float intervalMultiplier = 0.8f;
    [SerializeField] private float timingMultiplier = 0.9f;
    [SerializeField] private float minWarningDuration = 0.9f;
    [SerializeField] private int extraDownPerSpeedUp = 2;

    [Header("Brawl Final Minute")]
    [SerializeField] private float permanentFallsAt = 60f;
    [Range(0f, 1f)]
    [SerializeField] private float keepStandingFraction = 0.35f;

    private ArenaFloor floor;
    private bool running;
    private bool permanent;
    private float matchStartTime;
    private float nextFallTime;
    private int speedLevel;
    private bool explained;

    private float IntervalScale
    {
        get { return Mathf.Pow(intervalMultiplier, speedLevel); }
    }

    private float CurrentWarning
    {
        get { return Mathf.Max(minWarningDuration, warningDuration * Mathf.Pow(timingMultiplier, speedLevel)); }
    }

    private float CurrentFall
    {
        get { return fallDuration * Mathf.Pow(timingMultiplier, speedLevel); }
    }

    private void Awake()
    {
        floor = GetComponent<ArenaFloor>();
        if (match == null) match = FindAnyObjectByType<MatchManager>();
    }

    private void OnEnable()
    {
        if (match != null) match.StateChanged += OnMatchStateChanged;
    }

    private void OnDisable()
    {
        if (match != null) match.StateChanged -= OnMatchStateChanged;
    }

    private void OnMatchStateChanged(MatchState state)
    {
        running = state == MatchState.Playing;
        if (running)
        {
            matchStartTime = Time.time;
            nextFallTime = Time.time + firstFallDelay;
            speedLevel = 0;
            explained = false;
        }
        else
        {
            ResetFloor();
        }
    }

    private void Update()
    {
        if (!running || match == null) return;

        CheckSpeedUp();
        CheckFinalMinute();
        if (Time.time < nextFallTime) return;

        nextFallTime = Time.time + Random.Range(fallInterval.x, fallInterval.y) * IntervalScale;
        DropGroup();
    }

    private void CheckSpeedUp()
    {
        int level = Mathf.FloorToInt((Time.time - matchStartTime) / Mathf.Max(1f, speedUpEvery));
        if (level <= speedLevel) return;

        speedLevel = level;
        if (!permanent) match.Announce("The floor is crumbling faster!");
    }

    private void CheckFinalMinute()
    {
        if (permanent || match.Mode != GameMode.Brawl) return;
        if (match.TimeRemaining > permanentFallsAt) return;

        permanent = true;
        foreach (ArenaSection section in floor.Sections)
        {
            section.StayDown = true;
        }
        match.Announce("FINAL MINUTE! Fallen floor won't come back now!");
    }

    private void DropGroup()
    {
        int down = 0;
        List<ArenaSection> candidates = new List<ArenaSection>();
        foreach (ArenaSection section in floor.Sections)
        {
            if (!section.IsSolid) down++;
            else if (section.CanFall) candidates.Add(section);
        }
        if (candidates.Count == 0) return;

        int count = Random.Range(piecesPerFall.x, piecesPerFall.y + 1);
        if (permanent)
        {
            int keepStanding = Mathf.CeilToInt(floor.Sections.Count * keepStandingFraction);
            count = Mathf.Min(count, candidates.Count - keepStanding);
            if (count <= 0) return;
        }
        else
        {
            int room = maxDownAtOnce + speedLevel * extraDownPerSpeedUp - down;
            if (room < piecesPerFall.x) return;
            count = Mathf.Min(count, room);
        }
        count = Mathf.Min(count, candidates.Count);

        for (int i = 0; i < count; i++)
        {
            int pick = Random.Range(0, candidates.Count);
            candidates[pick].TriggerFall(CurrentWarning, CurrentFall, goneDuration, riseDuration, fallDistance);
            candidates.RemoveAt(pick);
        }

        if (!explained)
        {
            explained = true;
            match.Announce("Red, shaking floor is about to fall - get off it!");
        }
    }

    private void ResetFloor()
    {
        speedLevel = 0;
        permanent = false;
        foreach (ArenaSection section in floor.Sections)
        {
            section.ResetImmediate();
        }
    }
}
