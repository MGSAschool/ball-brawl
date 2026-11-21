using System.Collections.Generic;
using UnityEngine;
using Wave = SoundSynth.Wave;

public enum Sound
{
    Bump, BumpBig, Land, Pickup, UseShield, UsePunch, UseSpeed, Dash, Coin,
    KnockOff, Fall, Countdown, Go, Whistle, Win, Rumble, Crumble,
    LightGreen, LightYellow, LightRed, Zap, Blocked, Safe, Join, Respawn, Click, Select
}

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

public static class SfxLibrary
{
    public static AudioClip Build(Sound sound)
    {
        switch (sound)
        {
            case Sound.Bump: return Bump();
            case Sound.BumpBig: return BumpBig();
            case Sound.Land: return Land();
            case Sound.Pickup: return Pickup();
            case Sound.UseShield: return UseShield();
            case Sound.UsePunch: return UsePunch();
            case Sound.UseSpeed: return UseSpeed();
            case Sound.Dash: return Dash();
            case Sound.Coin: return Coin();
            case Sound.KnockOff: return KnockOff();
            case Sound.Fall: return Fall();
            case Sound.Countdown: return Countdown();
            case Sound.Go: return Go();
            case Sound.Whistle: return Whistle();
            case Sound.Win: return Win();
            case Sound.Rumble: return Rumble();
            case Sound.Crumble: return Crumble();
            case Sound.LightGreen: return LightGreen();
            case Sound.LightYellow: return LightYellow();
            case Sound.LightRed: return LightRed();
            case Sound.Zap: return Zap();
            case Sound.Blocked: return Blocked();
            case Sound.Safe: return Safe();
            case Sound.Join: return Join();
            case Sound.Respawn: return Respawn();
            case Sound.Click: return Click();
            case Sound.Select: return Select();
        }
        return null;
    }

    private static AudioClip Bump()
    {
        float[] s = SoundSynth.Buffer(0.22f);
        SoundSynth.Tone(s, 0f, 0.2f, 170f, 55f, Wave.Sine, 0.002f, 20f, 0.9f);
        SoundSynth.Tone(s, 0f, 0.08f, 900f, 380f, Wave.Triangle, 0.001f, 45f, 0.3f);
        SoundSynth.Noise(s, 0f, 0.05f, 5000f, 900f, 0.001f, 70f, 0.45f);
        return SoundSynth.Make(s);
    }

    private static AudioClip BumpBig()
    {
        float[] s = SoundSynth.Buffer(0.45f);
        SoundSynth.Tone(s, 0f, 0.4f, 130f, 38f, Wave.Sine, 0.002f, 9f, 1f);
        SoundSynth.Tone(s, 0f, 0.12f, 700f, 200f, Wave.Square, 0.001f, 30f, 0.25f);
        SoundSynth.Noise(s, 0f, 0.25f, 6000f, 400f, 0.001f, 18f, 0.6f);
        return SoundSynth.Make(s);
    }

    private static AudioClip Land()
    {
        GameArt art = GameArt.Instance;
        if (art != null)
        {
            AudioClip recorded = SoundSynth.Slice(art.bounceSound, 0.1f, 0.3f);
            if (recorded != null) return recorded;
        }

        float[] s = SoundSynth.Buffer(0.15f);
        SoundSynth.Tone(s, 0f, 0.14f, 230f, 110f, Wave.Sine, 0.002f, 28f, 0.8f);
        return SoundSynth.Make(s);
    }

    private static AudioClip Pickup()
    {
        float[] s = SoundSynth.Buffer(0.5f);
        float[] notes = { 1047f, 1319f, 1568f, 2093f };
        for (int i = 0; i < notes.Length; i++)
        {
            SoundSynth.Tone(s, i * 0.055f, 0.3f, notes[i], notes[i], Wave.Triangle, 0.003f, 12f, 0.45f);
        }
        SoundSynth.Sparkle(s, 0.05f, 0.4f, 12, 0.15f);
        return SoundSynth.Make(s);
    }

    private static AudioClip UseShield()
    {
        float[] s = SoundSynth.Buffer(0.8f);
        float[] chord = { 523f, 659f, 784f, 1047f };
        foreach (float note in chord)
        {
            SoundSynth.Tone(s, 0f, 0.8f, note * 0.98f, note, Wave.Sine, 0.12f, 4f, 0.25f, 7f);
        }
        SoundSynth.Sparkle(s, 0.1f, 0.7f, 16, 0.12f);
        return SoundSynth.Make(s);
    }

    private static AudioClip UsePunch()
    {
        float[] s = SoundSynth.Buffer(0.55f);
        SoundSynth.Tone(s, 0f, 0.28f, 180f, 900f, Wave.Square, 0.01f, 3f, 0.22f);
        SoundSynth.Tone(s, 0.26f, 0.28f, 140f, 45f, Wave.Sine, 0.002f, 12f, 0.9f);
        SoundSynth.Noise(s, 0.26f, 0.15f, 4000f, 500f, 0.001f, 25f, 0.4f);
        return SoundSynth.Make(s);
    }

    private static AudioClip UseSpeed()
    {
        float[] s = SoundSynth.Buffer(0.5f);
        SoundSynth.Tone(s, 0f, 0.45f, 300f, 1400f, Wave.Triangle, 0.01f, 5f, 0.3f);
        SoundSynth.Noise(s, 0f, 0.45f, 600f, 6000f, 0.05f, 6f, 0.35f);
        return SoundSynth.Make(s);
    }

    private static AudioClip Dash()
    {
        float[] s = SoundSynth.Buffer(0.35f);
        SoundSynth.Noise(s, 0f, 0.18f, 500f, 5000f, 0.03f, 4f, 0.55f);
        SoundSynth.Noise(s, 0.15f, 0.2f, 5000f, 600f, 0.001f, 14f, 0.45f);
        SoundSynth.Tone(s, 0f, 0.3f, 500f, 220f, Wave.Sine, 0.01f, 10f, 0.2f);
        return SoundSynth.Make(s);
    }

    private static AudioClip Coin()
    {
        float[] s = SoundSynth.Buffer(0.45f);
        SoundSynth.Tone(s, 0f, 0.07f, 988f, 988f, Wave.Square, 0.001f, 4f, 0.22f);
        SoundSynth.Tone(s, 0.07f, 0.38f, 1319f, 1319f, Wave.Square, 0.001f, 8f, 0.22f);
        return SoundSynth.Make(s);
    }

    private static AudioClip KnockOff()
    {
        float[] s = SoundSynth.Buffer(0.7f);
        float[] notes = { 784f, 1047f, 1319f, 1568f };
        for (int i = 0; i < notes.Length; i++)
        {
            bool lastNote = i == notes.Length - 1;
            float length = lastNote ? 0.45f : 0.12f;
            float decay = lastNote ? 5f : 14f;
            SoundSynth.Tone(s, i * 0.07f, length, notes[i], notes[i], Wave.Square, 0.002f, decay, 0.16f);
        }
        return SoundSynth.Make(s);
    }

    private static AudioClip Fall()
    {
        float[] s = SoundSynth.Buffer(0.9f);
        SoundSynth.Tone(s, 0f, 0.85f, 1500f, 180f, Wave.Sine, 0.02f, 2.5f, 0.45f, 6f);
        return SoundSynth.Make(s);
    }

    private static AudioClip Countdown()
    {
        float[] s = SoundSynth.Buffer(0.2f);
        SoundSynth.Tone(s, 0f, 0.18f, 880f, 880f, Wave.Square, 0.002f, 12f, 0.25f);
        return SoundSynth.Make(s);
    }

    private static AudioClip Go()
    {
        float[] s = SoundSynth.Buffer(0.7f);
        SoundSynth.Tone(s, 0f, 0.65f, 1319f, 1319f, Wave.Square, 0.002f, 4f, 0.2f);
        SoundSynth.Tone(s, 0f, 0.65f, 1760f, 1760f, Wave.Square, 0.002f, 4f, 0.15f);
        SoundSynth.Tone(s, 0f, 0.3f, 220f, 110f, Wave.Sine, 0.002f, 10f, 0.6f);
        return SoundSynth.Make(s);
    }

    private static AudioClip Whistle()
    {
        float[] s = SoundSynth.Buffer(0.75f);
        SoundSynth.Tone(s, 0f, 0.7f, 2600f, 2500f, Wave.Sine, 0.01f, 2f, 0.35f, 28f, 0.04f);
        SoundSynth.Noise(s, 0f, 0.7f, 7000f, 7000f, 0.01f, 3f, 0.08f);
        return SoundSynth.Make(s);
    }

    private static AudioClip Win()
    {
        float[] s = SoundSynth.Buffer(1.6f);
        float[] notes = { 523f, 659f, 784f };
        for (int i = 0; i < notes.Length; i++)
        {
            SoundSynth.Tone(s, i * 0.13f, 0.14f, notes[i], notes[i], Wave.Square, 0.003f, 6f, 0.18f);
        }
        float[] chord = { 1047f, 1319f, 1568f };
        foreach (float note in chord)
        {
            SoundSynth.Tone(s, 0.42f, 1.1f, note, note, Wave.Square, 0.005f, 2.2f, 0.12f, 5f);
        }
        SoundSynth.Sparkle(s, 0.45f, 1f, 20, 0.1f);
        return SoundSynth.Make(s);
    }

    private static AudioClip Rumble()
    {
        float[] s = SoundSynth.Buffer(0.9f);
        SoundSynth.Noise(s, 0f, 0.9f, 260f, 160f, 0.08f, 2.5f, 0.9f, 18f);
        return SoundSynth.Make(s);
    }

    private static AudioClip Crumble()
    {
        float[] s = SoundSynth.Buffer(0.9f);
        SoundSynth.Tone(s, 0f, 0.4f, 90f, 40f, Wave.Sine, 0.003f, 8f, 0.7f);
        SoundSynth.Noise(s, 0f, 0.6f, 1800f, 300f, 0.002f, 6f, 0.45f);
        System.Random random = new System.Random(7);
        for (int i = 0; i < 14; i++)
        {
            float start = (float)random.NextDouble() * 0.6f;
            SoundSynth.Noise(s, start, 0.04f, 3500f, 1200f, 0.001f, 80f, 0.35f);
        }
        return SoundSynth.Make(s);
    }

    private static AudioClip LightGreen()
    {
        float[] s = SoundSynth.Buffer(0.6f);
        SoundSynth.Tone(s, 0f, 0.25f, 1047f, 1047f, Wave.Triangle, 0.002f, 10f, 0.4f);
        SoundSynth.Tone(s, 0.12f, 0.45f, 1568f, 1568f, Wave.Triangle, 0.002f, 6f, 0.4f);
        return SoundSynth.Make(s);
    }

    private static AudioClip LightYellow()
    {
        float[] s = SoundSynth.Buffer(0.6f);
        for (int i = 0; i < 3; i++)
        {
            SoundSynth.Tone(s, i * 0.18f, 0.12f, 1175f, 1175f, Wave.Square, 0.002f, 8f, 0.22f);
        }
        return SoundSynth.Make(s);
    }

    private static AudioClip LightRed()
    {
        float[] s = SoundSynth.Buffer(0.7f);
        SoundSynth.Tone(s, 0f, 0.65f, 110f, 104f, Wave.Square, 0.005f, 1.5f, 0.35f);
        SoundSynth.Tone(s, 0f, 0.65f, 165f, 156f, Wave.Square, 0.005f, 1.5f, 0.25f);
        return SoundSynth.Make(s);
    }

    private static AudioClip Zap()
    {
        float[] s = SoundSynth.Buffer(0.5f);
        SoundSynth.Noise(s, 0f, 0.45f, 6000f, 2000f, 0.002f, 5f, 0.6f, 60f);
        SoundSynth.Tone(s, 0f, 0.4f, 1200f, 300f, Wave.Square, 0.002f, 6f, 0.15f);
        return SoundSynth.Make(s);
    }

    private static AudioClip Blocked()
    {
        float[] s = SoundSynth.Buffer(0.6f);
        SoundSynth.Tone(s, 0f, 0.55f, 1760f, 1760f, Wave.Sine, 0.001f, 6f, 0.35f, 9f);
        SoundSynth.Tone(s, 0f, 0.55f, 2637f, 2637f, Wave.Sine, 0.001f, 8f, 0.2f);
        return SoundSynth.Make(s);
    }

    private static AudioClip Safe()
    {
        float[] s = SoundSynth.Buffer(0.5f);
        SoundSynth.Tone(s, 0f, 0.45f, 784f, 784f, Wave.Triangle, 0.003f, 7f, 0.3f);
        SoundSynth.Tone(s, 0.08f, 0.4f, 1175f, 1175f, Wave.Triangle, 0.003f, 7f, 0.3f);
        return SoundSynth.Make(s);
    }

    private static AudioClip Join()
    {
        float[] s = SoundSynth.Buffer(0.45f);
        SoundSynth.Tone(s, 0f, 0.1f, 400f, 1000f, Wave.Sine, 0.002f, 20f, 0.5f);
        SoundSynth.Tone(s, 0.08f, 0.35f, 1319f, 1319f, Wave.Triangle, 0.002f, 8f, 0.3f);
        return SoundSynth.Make(s);
    }

    private static AudioClip Respawn()
    {
        float[] s = SoundSynth.Buffer(0.4f);
        SoundSynth.Tone(s, 0f, 0.35f, 300f, 950f, Wave.Sine, 0.02f, 6f, 0.35f);
        SoundSynth.Noise(s, 0f, 0.3f, 800f, 4000f, 0.05f, 8f, 0.2f);
        return SoundSynth.Make(s);
    }

    private static AudioClip Click()
    {
        float[] s = SoundSynth.Buffer(0.06f);
        SoundSynth.Tone(s, 0f, 0.05f, 1500f, 1100f, Wave.Triangle, 0.001f, 60f, 0.4f);
        return SoundSynth.Make(s);
    }

    private static AudioClip Select()
    {
        float[] s = SoundSynth.Buffer(0.25f);
        SoundSynth.Tone(s, 0f, 0.08f, 660f, 660f, Wave.Triangle, 0.002f, 20f, 0.35f);
        SoundSynth.Tone(s, 0.07f, 0.16f, 990f, 990f, Wave.Triangle, 0.002f, 14f, 0.35f);
        return SoundSynth.Make(s);
    }
}

public static class SoundSynth
{
    public enum Wave { Sine, Triangle, Square }

    public const int Rate = 44100;

    public static float[] Buffer(float seconds)
    {
        return new float[Mathf.CeilToInt(seconds * Rate)];
    }

    public static void Tone(float[] samples, float start, float length, float startPitch, float endPitch, Wave wave,
        float attack, float decay, float volume, float vibratoRate = 0f, float vibratoDepth = 0.01f)
    {
        int begin = (int)(start * Rate);
        int count = Mathf.Min((int)(length * Rate), samples.Length - begin);
        double phase = 0;

        for (int i = 0; i < count; i++)
        {
            float time = i / (float)Rate;
            float progress = 0f;
            if (count > 1) progress = i / (float)(count - 1);

            float pitch = startPitch * Mathf.Pow(endPitch / startPitch, progress);
            if (vibratoRate > 0f) pitch *= 1f + Mathf.Sin(time * vibratoRate * Mathf.PI * 2f) * vibratoDepth;
            phase += pitch / Rate;
            float cycle = (float)(phase - System.Math.Floor(phase));

            float value = WaveValue(wave, cycle);
            float envelope = Envelope(time, attack, decay) * FadeOut(i, count);
            samples[begin + i] += value * envelope * volume;
        }
    }

    public static void Noise(float[] samples, float start, float length, float startCutoff, float endCutoff,
        float attack, float decay, float volume, float chop = 0f)
    {
        int begin = (int)(start * Rate);
        int count = Mathf.Min((int)(length * Rate), samples.Length - begin);
        System.Random random = new System.Random(begin + count);
        float filtered = 0f;

        for (int i = 0; i < count; i++)
        {
            float time = i / (float)Rate;
            float progress = 0f;
            if (count > 1) progress = i / (float)(count - 1);

            float cutoff = startCutoff * Mathf.Pow(endCutoff / startCutoff, progress);
            float smoothing = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / Rate);
            float noise = (float)random.NextDouble() * 2f - 1f;
            filtered += smoothing * (noise - filtered);

            float envelope = Envelope(time, attack, decay) * FadeOut(i, count);
            if (chop > 0f) envelope *= 0.55f + 0.45f * Mathf.Sin(time * chop * Mathf.PI * 2f);

            float loudnessFix = Mathf.Lerp(3f, 1.2f, Mathf.Clamp01(cutoff / 4000f));
            samples[begin + i] += filtered * envelope * volume * loudnessFix;
        }
    }

    public static void Sparkle(float[] samples, float start, float length, int count, float volume)
    {
        System.Random random = new System.Random(count * 31 + (int)(length * 100));
        for (int i = 0; i < count; i++)
        {
            float time = start + (float)random.NextDouble() * length;
            float pitch = 2000f + (float)random.NextDouble() * 3000f;
            Tone(samples, time, 0.12f, pitch, pitch, Wave.Sine, 0.002f, 30f, volume);
        }
    }

    public static AudioClip Make(float[] samples)
    {
        float peak = 0f;
        foreach (float value in samples)
        {
            peak = Mathf.Max(peak, Mathf.Abs(value));
        }
        if (peak > 0.9f)
        {
            float scale = 0.9f / peak;
            for (int i = 0; i < samples.Length; i++) samples[i] *= scale;
        }

        AudioClip clip = AudioClip.Create("sfx", samples.Length, 1, Rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public static AudioClip Slice(AudioClip source, float start, float length)
    {
        if (source == null || source.loadType != AudioClipLoadType.DecompressOnLoad) return null;
        if (source.loadState != AudioDataLoadState.Loaded && !source.LoadAudioData()) return null;

        int channels = source.channels;
        int from = Mathf.Clamp((int)(start * source.frequency), 0, source.samples - 1);
        int count = Mathf.Min((int)(length * source.frequency), source.samples - from);
        float[] data = new float[count * channels];
        if (!source.GetData(data, from)) return null;

        float[] mono = new float[count];
        int fade = Mathf.Max(1, source.frequency / 200);
        for (int i = 0; i < count; i++)
        {
            float sum = 0f;
            for (int c = 0; c < channels; c++) sum += data[i * channels + c];
            float envelope = Mathf.Clamp01(i / (float)fade) * Mathf.Clamp01((count - i) / (float)fade);
            mono[i] = sum / channels * envelope;
        }

        AudioClip clip = AudioClip.Create("slice", count, 1, source.frequency, false);
        clip.SetData(mono, 0);
        return clip;
    }

    private static float WaveValue(Wave wave, float cycle)
    {
        if (wave == Wave.Triangle) return 1f - 4f * Mathf.Abs(cycle - 0.5f);
        if (wave == Wave.Square) return (float)System.Math.Tanh(Mathf.Sin(cycle * Mathf.PI * 2f) * 4f);
        return Mathf.Sin(cycle * Mathf.PI * 2f);
    }

    private static float Envelope(float time, float attack, float decay)
    {
        return Mathf.Min(1f, time / Mathf.Max(attack, 1e-4f)) * Mathf.Exp(-decay * time);
    }

    private static float FadeOut(int index, int count)
    {
        return Mathf.Clamp01((count - index) / (Rate * 0.005f));
    }
}
