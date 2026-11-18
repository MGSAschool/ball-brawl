using UnityEngine;

[RequireComponent(typeof(ArenaFloor), typeof(StormSky))]
public class ArenaStorm : MonoBehaviour
{
    [SerializeField] private MatchManager match;
    [SerializeField] private float stormWarning = 5f;
    [SerializeField] private Color stormColor = new Color(0.55f, 0.35f, 1f);

    private ArenaFloor floor;
    private StormSky sky;
    private float startTime;
    private int lastCount;

    public bool IsRunning { get; private set; }
    public bool HasStruck { get; private set; }
    public float WarningTime { get { return stormWarning; } }

    private void Awake()
    {
        floor = GetComponent<ArenaFloor>();
        sky = GetComponent<StormSky>();
        if (match == null) match = FindAnyObjectByType<MatchManager>();
    }

    public void Begin()
    {
        IsRunning = true;
        HasStruck = false;
        startTime = Time.time;
        lastCount = -1;
        sky.StartDimming(stormWarning);
        Sfx.Play(Sound.Rumble, floor.Center, 1f, 0.6f);
    }

    public void ResetStorm()
    {
        IsRunning = false;
        HasStruck = false;
        sky.Restore();
    }

    private void Update()
    {
        if (!IsRunning) return;

        float elapsed = Time.time - startTime;
        float t = Mathf.Clamp01(elapsed / stormWarning);

        CountDown(elapsed, t);
        GlowOldArena(elapsed, t);
        CrackleLightning(t);

        if (elapsed >= stormWarning) Strike();
    }

    private void CountDown(float elapsed, float t)
    {
        int count = Mathf.CeilToInt(stormWarning - elapsed);
        if (count == lastCount || count <= 0) return;

        lastCount = count;
        match.Announce("STORM IN " + count + "! Get to the new platform!");
        Sfx.Play(Sound.Countdown, 0.7f, 0.8f);
        Sfx.Play(Sound.Rumble, floor.Center, 0.5f + 0.4f * t, 0.6f);
        Vfx.Shake(0.15f + 0.3f * t);
    }

    private void GlowOldArena(float elapsed, float t)
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * Mathf.Lerp(4f, 16f, t));
        float glow = Mathf.Lerp(0.45f, 0.85f, t) * Mathf.Lerp(0.65f, 1f, pulse);
        foreach (ArenaSection section in floor.Sections)
        {
            section.Tint(stormColor, glow);
        }
    }

    private void CrackleLightning(float t)
    {
        if (Random.value >= Time.deltaTime * Mathf.Lerp(1.5f, 6f, t)) return;

        Vector3 spot;
        if (!floor.TryGetRandomSolidPoint(0f, out spot)) return;
        Vfx.Zap(spot, stormColor);
        Sfx.Play(Sound.Zap, spot, 0.35f, Random.Range(0.8f, 1.2f));
    }

    private void Strike()
    {
        IsRunning = false;
        HasStruck = true;
        sky.FlashAndClear();
        Vfx.Shake(1f);
        Sfx.Play(Sound.Explosion, floor.Center, 1f, 0.7f);
        foreach (ArenaSection section in floor.Sections)
        {
            section.Tint(stormColor, 0f);
        }

        int struck = 0;
        string blockedBy = null;
        foreach (PlayerEntry entry in match.Players)
        {
            if (entry.Player == null || entry.IsRespawning) continue;
            Vector3 position = entry.Player.transform.position;
            if (!IsOverOldArena(position)) continue;

            Vfx.Zap(position, stormColor);
            if (entry.Player.IsInvulnerable)
            {
                Sfx.Play(Sound.Blocked, position, 1f);
                blockedBy = entry.Name;
                continue;
            }

            Sfx.Play(Sound.Zap, position, 1f);
            Vfx.Explosion(position, 3f);
            match.KnockOutWithoutPenalty(entry.Player);
            struck++;
        }

        if (blockedBy != null) match.Announce(blockedBy + "'s shield blocked the storm!");
        else if (struck == 1) match.Announce("The storm struck a player! The old arena is now ruins.");
        else if (struck > 1) match.Announce("The storm struck " + struck + " players! The old arena is now ruins.");
        else match.Announce("The storm passed! The old arena is now ruins.");
    }

    private bool IsOverOldArena(Vector3 position)
    {
        Vector3 offset = position - floor.Center;
        offset.y = 0f;
        return offset.magnitude <= floor.Radius + 0.75f;
    }
}
