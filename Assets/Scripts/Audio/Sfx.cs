using System.Collections.Generic;
using UnityEngine;

public static class Sfx
{
    private const int VoiceCount = 24;
    private const float RepeatBlockTime = 0.04f;

    public static float Volume = 0.8f;

    private static readonly Dictionary<Sound, AudioClip> clips = new Dictionary<Sound, AudioClip>();
    private static readonly Dictionary<Sound, float> lastPlayed = new Dictionary<Sound, float>();
    private static AudioSource[] voices;
    private static int nextVoice;

    public static void Play(Sound sound, float volume = 1f, float pitch = 1f)
    {
        PlayAt(sound, false, Vector3.zero, volume, pitch);
    }

    public static void Play(Sound sound, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        PlayAt(sound, true, position, volume, pitch);
    }

    public static AudioClip Clip(Sound sound)
    {
        AudioClip clip;
        if (clips.TryGetValue(sound, out clip) && clip != null) return clip;

        clip = SfxLibrary.Build(sound);
        if (clip != null) clip.name = sound.ToString();
        clips[sound] = clip;
        return clip;
    }

    private static void PlayAt(Sound sound, bool hasPosition, Vector3 position, float volume, float pitch)
    {
        float last;
        if (lastPlayed.TryGetValue(sound, out last) && Time.unscaledTime - last < RepeatBlockTime) return;
        lastPlayed[sound] = Time.unscaledTime;

        AudioClip clip = Clip(sound);
        if (clip == null) return;
        MakeVoices();

        AudioSource voice = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Length;
        voice.Stop();
        voice.clip = clip;
        voice.volume = Mathf.Clamp01(volume) * Volume;
        voice.pitch = pitch * Random.Range(0.95f, 1.05f);
        voice.panStereo = 0f;
        if (hasPosition) voice.panStereo = PanFor(position);
        voice.Play();
    }

    private static float PanFor(Vector3 position)
    {
        Camera cam = Camera.main;
        if (cam == null) return 0f;

        Vector3 viewport = cam.WorldToViewportPoint(position);
        return Mathf.Clamp((viewport.x - 0.5f) * 1.2f, -0.6f, 0.6f);
    }

    private static void MakeVoices()
    {
        if (voices != null && voices[0] != null) return;

        GameObject go = new GameObject("Sfx");
        Object.DontDestroyOnLoad(go);
        voices = new AudioSource[VoiceCount];
        for (int i = 0; i < VoiceCount; i++)
        {
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            voices[i] = source;
        }
    }
}
