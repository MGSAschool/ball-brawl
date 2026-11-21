using System.Collections.Generic;
using UnityEngine;

public class Music : MonoBehaviour
{
    public static float Volume = 0.5f;
    public static Music Instance { get; private set; }

    private const float FadeTime = 1.0f;
    private const float ResultsDelay = 1.6f;
    private const float HurryTime = 30f;
    private const float HurryPitch = 1.08f;
    private const float PanicTime = 10f;
    private const float PanicPitch = 1.16f;
    private const float StopTime = 0.45f;
    private const float SpinUpTime = 0.25f;

    private readonly AudioSource[] decks = new AudioSource[2];
    private readonly float[] gains = new float[2];
    private int activeDeck;
    private string currentTrack = "";
    private MatchState lastState;
    private float stateSince;
    private float pitch = 1f;
    private float stopAmount;
    private bool paused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateMusicPlayer()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("Music");
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

        for (int i = 0; i < decks.Length; i++)
        {
            GameObject child = new GameObject("Deck " + i);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
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
        MatchManager match = MatchManager.Instance;
        if (match != null && match.State != lastState)
        {
            lastState = match.State;
            stateSince = Time.unscaledTime;
        }

        string wanted = PickTrack(match);
        if (wanted != currentTrack) SwitchTo(wanted);

        float dt = Time.unscaledDeltaTime;
        bool playing = match != null && match.State == MatchState.Playing;
        pitch = Mathf.MoveTowards(pitch, TargetPitch(match, playing), dt * 0.15f);

        bool redLight = playing && IsRedLight(match);
        float stopSpeed = SpinUpTime;
        if (redLight) stopSpeed = StopTime;
        float stopTarget = 0f;
        if (redLight) stopTarget = 1f;
        stopAmount = Mathf.MoveTowards(stopAmount, stopTarget, dt / stopSpeed);
        PauseForRedLight(redLight);

        float speed = 1f - stopAmount * stopAmount;
        for (int i = 0; i < decks.Length; i++)
        {
            float target = 0f;
            if (i == activeDeck && !string.IsNullOrEmpty(currentTrack)) target = 1f;
            gains[i] = Mathf.MoveTowards(gains[i], target, dt / FadeTime);

            AudioSource deck = decks[i];
            deck.volume = gains[i] * Volume * Mathf.Clamp01(speed * 1.5f);
            deck.pitch = Mathf.Max(0.02f, pitch * speed);
            if (gains[i] <= 0f && deck.isPlaying && !paused) deck.Stop();
        }
    }

    private float TargetPitch(MatchManager match, bool playing)
    {
        if (!playing) return 1f;
        if (match.TimeRemaining <= PanicTime) return PanicPitch;
        if (match.TimeRemaining <= HurryTime) return HurryPitch;
        return 1f;
    }

    private bool IsRedLight(MatchManager match)
    {
        StaphItMode staph = StaphItMode.Active;
        return staph != null && match.Mode == GameMode.StaphIt && staph.Light == StaphItMode.Signal.Red;
    }

    private void PauseForRedLight(bool redLight)
    {
        if (redLight && stopAmount >= 1f && !paused)
        {
            foreach (AudioSource deck in decks) deck.Pause();
            paused = true;
        }
        else if (!redLight && paused)
        {
            foreach (AudioSource deck in decks) deck.UnPause();
            paused = false;
        }
    }

    private string PickTrack(MatchManager match)
    {
        if (match == null) return "Lobby";
        if (match.State == MatchState.Lobby) return "Lobby";
        if (match.State == MatchState.Countdown) return null;
        if (match.State == MatchState.MatchOver)
        {
            if (Time.unscaledTime - stateSince >= ResultsDelay) return "Results";
            return null;
        }

        if (match.Mode == GameMode.StaphIt) return "StaphIt";
        if (match.Mode == GameMode.BombaClutch) return "BombaClutch";
        return "Brawl";
    }

    private void SwitchTo(string track)
    {
        currentTrack = track;
        if (string.IsNullOrEmpty(track)) return;

        AudioClip clip = MusicLibrary.RandomVersion(track);
        if (clip == null) return;

        int nextDeck = 1 - activeDeck;
        AudioSource deck = decks[nextDeck];
        deck.Stop();
        deck.clip = clip;
        deck.time = 0f;
        deck.Play();
        gains[nextDeck] = 0f;
        activeDeck = nextDeck;
    }
}

public static class MusicLibrary
{
    private const string Prefix = "BallBrawl_";

    private static Dictionary<string, List<AudioClip>> tracks;

    public static AudioClip RandomVersion(string track)
    {
        if (tracks == null) LoadAll();

        List<AudioClip> versions;
        if (tracks.TryGetValue(track, out versions) && versions.Count > 0)
        {
            return versions[Random.Range(0, versions.Count)];
        }

        Debug.LogWarning("Music: no clip named " + Prefix + track + " in Resources/Music");
        return null;
    }

    private static void LoadAll()
    {
        tracks = new Dictionary<string, List<AudioClip>>();
        foreach (AudioClip clip in Resources.LoadAll<AudioClip>("Music"))
        {
            string name = TrackName(clip.name);
            if (!tracks.ContainsKey(name)) tracks[name] = new List<AudioClip>();
            tracks[name].Add(clip);
        }
    }

    private static string TrackName(string fileName)
    {
        string name = fileName;
        if (name.StartsWith(Prefix)) name = name.Substring(Prefix.Length);

        int underscore = name.LastIndexOf('_');
        int number;
        if (underscore > 0 && int.TryParse(name.Substring(underscore + 1), out number))
        {
            name = name.Substring(0, underscore);
        }
        return name;
    }
}
