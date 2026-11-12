using System.Collections.Generic;
using UnityEngine;

// Background music. Picks a looping track from Resources/Music by match state and mode and
// crossfades between them. It creates itself when the game starts, so no scene setup is needed.
// Tracks are found by file name: BallBrawl_<Track>, or numbered versions like BallBrawl_Brawl_1,
// BallBrawl_Brawl_2 (one is picked at random each time the track starts).
//   Lobby      -> Lobby
//   Countdown  -> fades to silence so the 3-2-1 beeps stand out
//   Playing    -> Brawl / StaphIt / BombaClutch (starts on "GO!")
//   MatchOver  -> Results (waits for the win fanfare to finish)
// Chaos extras:
//   - Staph It! red light: the music grinds to a halt like a tape stopping, then spins back up on
//     green. Players can hear the light change, and silence means "don't move".
//   - Last 30 seconds: the music speeds up. Last 10 seconds: it speeds up again.
public class Music : MonoBehaviour
{
    public static float Volume = 0.5f;
    public static Music Instance { get; private set; }

    private const float FadeTime = 1.0f;
    private const float ResultsDelay = 1.6f;   // length of the Win fanfare in Sfx
    private const float HurryTime = 30f;
    private const float HurryPitch = 1.08f;
    private const float PanicTime = 10f;
    private const float PanicPitch = 1.16f;
    private const float StopTime = 0.45f;      // tape stop on red
    private const float SpinUpTime = 0.25f;    // spin back up on green

    private readonly AudioSource[] decks = new AudioSource[2];
    private readonly float[] gains = new float[2];
    private const string Prefix = "BallBrawl_";

    private Dictionary<string, List<AudioClip>> clips;
    private int active;
    private string current = "";
    private MatchManager.MatchState lastState;
    private float stateSince;
    private float pitch = 1f;
    private float stop;            // 0 = playing normally, 1 = fully stopped (red light)
    private bool paused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (Instance != null) return;
        var go = new GameObject("Music");
        DontDestroyOnLoad(go);
        go.AddComponent<Music>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Each deck sits on its own child so its low-pass filter only touches that deck
        for (int i = 0; i < decks.Length; i++)
        {
            var child = new GameObject("Deck " + i);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.priority = 0;
            source.volume = 0f;
            decks[i] = source;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        var match = MatchManager.Instance;
        if (match != null && match.State != lastState)
        {
            lastState = match.State;
            stateSince = Time.unscaledTime;
        }

        string want = Pick(match);
        if (want != current) SwitchTo(want);

        float dt = Time.unscaledDeltaTime;

        // Final stretch: the tempo (and pitch) climbs, then climbs again
        bool playing = match != null && match.State == MatchManager.MatchState.Playing;
        float targetPitch = !playing ? 1f
            : match.TimeRemaining <= PanicTime ? PanicPitch
            : match.TimeRemaining <= HurryTime ? HurryPitch : 1f;
        pitch = Mathf.MoveTowards(pitch, targetPitch, dt * 0.15f);

        // Staph It! red light: tape stop. Green: spin back up from where it stopped.
        var staph = StaphItMode.Active;
        bool red = playing && staph != null && match.Mode == GameMode.StaphIt && staph.Light == StaphItMode.Signal.Red;
        stop = Mathf.MoveTowards(stop, red ? 1f : 0f, dt / (red ? StopTime : SpinUpTime));
        if (red && stop >= 1f && !paused)
        {
            foreach (var d in decks) d.Pause();
            paused = true;
        }
        else if (!red && paused)
        {
            foreach (var d in decks) d.UnPause();
            paused = false;
        }
        // Speed falls off on a curve so it sounds like a motor winding down
        float speed = 1f - stop * stop;

        for (int i = 0; i < decks.Length; i++)
        {
            float target = i == active && !string.IsNullOrEmpty(current) ? 1f : 0f;
            gains[i] = Mathf.MoveTowards(gains[i], target, dt / FadeTime);
            var deck = decks[i];
            deck.volume = gains[i] * Volume * Mathf.Clamp01(speed * 1.5f);
            deck.pitch = Mathf.Max(0.02f, pitch * speed);
            if (gains[i] <= 0f && deck.isPlaying && !paused) deck.Stop();
        }
    }

    private string Pick(MatchManager match)
    {
        if (match == null) return "Lobby";
        switch (match.State)
        {
            case MatchManager.MatchState.Lobby:
                return "Lobby";
            case MatchManager.MatchState.Countdown:
                return null;
            case MatchManager.MatchState.MatchOver:
                return Time.unscaledTime - stateSince >= ResultsDelay ? "Results" : null;
            default:
                switch (match.Mode)
                {
                    case GameMode.StaphIt: return "StaphIt";
                    case GameMode.BombaClutch: return "BombaClutch";
                    default: return "Brawl";
                }
        }
    }

    private void SwitchTo(string track)
    {
        current = track;
        if (string.IsNullOrEmpty(track)) return;   // just fade out

        var clip = Load(track);
        if (clip == null) return;

        // Start the new track on the quiet deck from the top; the old one fades out
        int next = 1 - active;
        var deck = decks[next];
        deck.Stop();
        deck.clip = clip;
        deck.time = 0f;
        deck.Play();
        gains[next] = 0f;
        active = next;
    }

    private AudioClip Load(string track)
    {
        if (clips == null)
        {
            // Group every clip in Resources/Music by track name, dropping the prefix and any _N suffix
            clips = new Dictionary<string, List<AudioClip>>();
            foreach (var clip in Resources.LoadAll<AudioClip>("Music"))
            {
                string name = clip.name.StartsWith(Prefix) ? clip.name.Substring(Prefix.Length) : clip.name;
                int underscore = name.LastIndexOf('_');
                if (underscore > 0 && int.TryParse(name.Substring(underscore + 1), out _)) name = name.Substring(0, underscore);
                if (!clips.TryGetValue(name, out var list)) clips[name] = list = new List<AudioClip>();
                list.Add(clip);
            }
        }
        if (clips.TryGetValue(track, out var versions) && versions.Count > 0) return versions[Random.Range(0, versions.Count)];
        Debug.LogWarning($"Music: no clip named {Prefix}{track} in Resources/Music");
        return null;
    }
}
