using System.Collections.Generic;
using UnityEngine;

public enum Sound
{
    Bump, BumpBig, Land, Pickup, UseShield, UsePunch, UseSpeed, Dash, Coin,
    KnockOff, Fall, Countdown, Go, Whistle, Win, Rumble, Crumble, Freeze,
    Explosion, LightGreen, LightYellow, LightRed, Zap, Blocked, Safe, Join, Respawn, Click, Select
}

// Game sound effects. Most are synthesised once at startup (no audio files needed); the ball
// bounce comes from the team's recording in GameArt. Played through a small pool of 2D sources,
// panned left/right by where the sound happens on screen, with a little pitch variety so
// repeated hits don't sound robotic.
public static class Sfx
{
    private const int Rate = 44100;
    private const int Voices = 24;

    public static float Volume = 0.8f;

    private static readonly Dictionary<Sound, AudioClip> clips = new Dictionary<Sound, AudioClip>();
    private static readonly Dictionary<Sound, float> lastPlayed = new Dictionary<Sound, float>();
    private static AudioSource[] sources;
    private static int next;

    public static void Play(Sound sound, float volume = 1f, float pitch = 1f) => Play(sound, null, volume, pitch);

    public static void Play(Sound sound, Vector3? at, float volume = 1f, float pitch = 1f)
    {
        // The same sound several times in one instant just gets louder and muddier
        if (lastPlayed.TryGetValue(sound, out float last) && Time.unscaledTime - last < 0.04f) return;
        lastPlayed[sound] = Time.unscaledTime;

        var clip = Clip(sound);
        if (clip == null) return;
        EnsureSources();

        var source = sources[next];
        next = (next + 1) % sources.Length;
        source.Stop();
        source.clip = clip;
        source.volume = Mathf.Clamp01(volume) * Volume;
        source.pitch = pitch * Random.Range(0.95f, 1.05f);
        source.panStereo = at.HasValue ? Pan(at.Value) : 0f;
        source.Play();
    }

    private static float Pan(Vector3 world)
    {
        var cam = Camera.main;
        if (cam == null) return 0f;
        Vector3 viewport = cam.WorldToViewportPoint(world);
        return Mathf.Clamp((viewport.x - 0.5f) * 1.2f, -0.6f, 0.6f);
    }

    private static void EnsureSources()
    {
        if (sources != null && sources[0] != null) return;
        var go = new GameObject("Sfx");
        Object.DontDestroyOnLoad(go);
        sources = new AudioSource[Voices];
        for (int i = 0; i < Voices; i++)
        {
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            sources[i] = s;
        }
    }

    public static AudioClip Clip(Sound sound)
    {
        if (clips.TryGetValue(sound, out var clip) && clip != null) return clip;
        clip = Build(sound);
        if (clip != null) clip.name = sound.ToString();
        clips[sound] = clip;
        return clip;
    }

    // --- Synthesis ---
    private static AudioClip Build(Sound sound)
    {
        switch (sound)
        {
            case Sound.Bump:
            {
                // Rubbery "bonk": a pitch-dropping thump, a click on top and a little wooden knock
                var s = Buffer(0.22f);
                Tone(s, 0f, 0.2f, 170f, 55f, Wave.Sine, 0.002f, 20f, 0.9f);
                Tone(s, 0f, 0.08f, 900f, 380f, Wave.Triangle, 0.001f, 45f, 0.3f);
                Noise(s, 0f, 0.05f, 5000f, 900f, 0.001f, 70f, 0.45f);
                return Make(s);
            }
            case Sound.BumpBig:
            {
                var s = Buffer(0.45f);
                Tone(s, 0f, 0.4f, 130f, 38f, Wave.Sine, 0.002f, 9f, 1f);
                Tone(s, 0f, 0.12f, 700f, 200f, Wave.Square, 0.001f, 30f, 0.25f);
                Noise(s, 0f, 0.25f, 6000f, 400f, 0.001f, 18f, 0.6f);
                return Make(s);
            }
            case Sound.Land:
            {
                var art = GameArt.Instance;
                var slice = art != null ? Slice(art.bounceSound, 0.1f, 0.3f) : null;
                if (slice != null) return slice;
                var s = Buffer(0.15f);
                Tone(s, 0f, 0.14f, 230f, 110f, Wave.Sine, 0.002f, 28f, 0.8f);
                return Make(s);
            }
            case Sound.Pickup:
            {
                // Quick rising sparkle: C6 E6 G6 C7
                var s = Buffer(0.5f);
                float[] notes = { 1047f, 1319f, 1568f, 2093f };
                for (int i = 0; i < notes.Length; i++) Tone(s, i * 0.055f, 0.3f, notes[i], notes[i], Wave.Triangle, 0.003f, 12f, 0.45f);
                Sparkle(s, 0.05f, 0.4f, 12, 0.15f);
                return Make(s);
            }
            case Sound.UseShield:
            {
                // Shimmering major chord that swells in
                var s = Buffer(0.8f);
                float[] chord = { 523f, 659f, 784f, 1047f };
                foreach (var f in chord) Tone(s, 0f, 0.8f, f * 0.98f, f, Wave.Sine, 0.12f, 4f, 0.25f, 7f);
                Sparkle(s, 0.1f, 0.7f, 16, 0.12f);
                return Make(s);
            }
            case Sound.UsePunch:
            {
                // Powering-up sweep into a heavy hit
                var s = Buffer(0.55f);
                Tone(s, 0f, 0.28f, 180f, 900f, Wave.Square, 0.01f, 3f, 0.22f);
                Tone(s, 0.26f, 0.28f, 140f, 45f, Wave.Sine, 0.002f, 12f, 0.9f);
                Noise(s, 0.26f, 0.15f, 4000f, 500f, 0.001f, 25f, 0.4f);
                return Make(s);
            }
            case Sound.UseSpeed:
            {
                var s = Buffer(0.5f);
                Tone(s, 0f, 0.45f, 300f, 1400f, Wave.Triangle, 0.01f, 5f, 0.3f);
                Noise(s, 0f, 0.45f, 600f, 6000f, 0.05f, 6f, 0.35f);
                return Make(s);
            }
            case Sound.Dash:
            {
                // Air whoosh: filtered noise that opens up then closes
                var s = Buffer(0.35f);
                Noise(s, 0f, 0.18f, 500f, 5000f, 0.03f, 4f, 0.55f);
                Noise(s, 0.15f, 0.2f, 5000f, 600f, 0.001f, 14f, 0.45f);
                Tone(s, 0f, 0.3f, 500f, 220f, Wave.Sine, 0.01f, 10f, 0.2f);
                return Make(s);
            }
            case Sound.Coin:
            {
                // Classic two-note coin: B5 then E6
                var s = Buffer(0.45f);
                Tone(s, 0f, 0.07f, 988f, 988f, Wave.Square, 0.001f, 4f, 0.22f);
                Tone(s, 0.07f, 0.38f, 1319f, 1319f, Wave.Square, 0.001f, 8f, 0.22f);
                return Make(s);
            }
            case Sound.KnockOff:
            {
                // Little "ta-da" for whoever scored the knock-off
                var s = Buffer(0.7f);
                float[] notes = { 784f, 1047f, 1319f, 1568f };
                for (int i = 0; i < notes.Length; i++)
                    Tone(s, i * 0.07f, i == notes.Length - 1 ? 0.45f : 0.12f, notes[i], notes[i], Wave.Square, 0.002f, i == notes.Length - 1 ? 5f : 14f, 0.16f);
                return Make(s);
            }
            case Sound.Fall:
            {
                // Cartoon falling whistle
                var s = Buffer(0.9f);
                Tone(s, 0f, 0.85f, 1500f, 180f, Wave.Sine, 0.02f, 2.5f, 0.45f, 6f);
                return Make(s);
            }
            case Sound.Countdown:
            {
                var s = Buffer(0.2f);
                Tone(s, 0f, 0.18f, 880f, 880f, Wave.Square, 0.002f, 12f, 0.25f);
                return Make(s);
            }
            case Sound.Go:
            {
                var s = Buffer(0.7f);
                Tone(s, 0f, 0.65f, 1319f, 1319f, Wave.Square, 0.002f, 4f, 0.2f);
                Tone(s, 0f, 0.65f, 1760f, 1760f, Wave.Square, 0.002f, 4f, 0.15f);
                Tone(s, 0f, 0.3f, 220f, 110f, Wave.Sine, 0.002f, 10f, 0.6f);
                return Make(s);
            }
            case Sound.Whistle:
            {
                // Referee whistle: a high tone with a fast trill
                var s = Buffer(0.75f);
                Tone(s, 0f, 0.7f, 2600f, 2500f, Wave.Sine, 0.01f, 2f, 0.35f, 28f, 0.04f);
                Noise(s, 0f, 0.7f, 7000f, 7000f, 0.01f, 3f, 0.08f);
                return Make(s);
            }
            case Sound.Win:
            {
                // Fanfare: C5 E5 G5, then a held C6 chord
                var s = Buffer(1.6f);
                float[] notes = { 523f, 659f, 784f };
                for (int i = 0; i < notes.Length; i++) Tone(s, i * 0.13f, 0.14f, notes[i], notes[i], Wave.Square, 0.003f, 6f, 0.18f);
                foreach (var f in new[] { 1047f, 1319f, 1568f }) Tone(s, 0.42f, 1.1f, f, f, Wave.Square, 0.005f, 2.2f, 0.12f, 5f);
                Sparkle(s, 0.45f, 1f, 20, 0.1f);
                return Make(s);
            }
            case Sound.Rumble:
            {
                // Low shaking rumble for floor about to fall
                var s = Buffer(0.9f);
                Noise(s, 0f, 0.9f, 260f, 160f, 0.08f, 2.5f, 0.9f, 18f);
                return Make(s);
            }
            case Sound.Crumble:
            {
                // Cracking stone: a heavy thud plus a scatter of crackles
                var s = Buffer(0.9f);
                Tone(s, 0f, 0.4f, 90f, 40f, Wave.Sine, 0.003f, 8f, 0.7f);
                Noise(s, 0f, 0.6f, 1800f, 300f, 0.002f, 6f, 0.45f);
                var rng = new System.Random(7);
                for (int i = 0; i < 14; i++)
                    Noise(s, (float)rng.NextDouble() * 0.6f, 0.04f, 3500f, 1200f, 0.001f, 80f, 0.35f);
                return Make(s);
            }
            case Sound.Freeze:
            {
                var s = Buffer(0.8f);
                Sparkle(s, 0f, 0.7f, 26, 0.16f);
                Noise(s, 0f, 0.7f, 9000f, 9000f, 0.05f, 4f, 0.12f);
                Tone(s, 0f, 0.6f, 1800f, 2600f, Wave.Sine, 0.05f, 5f, 0.12f);
                return Make(s);
            }
            case Sound.Explosion:
            {
                var s = Buffer(1.4f);
                Tone(s, 0f, 0.8f, 90f, 28f, Wave.Sine, 0.002f, 4f, 1f);
                Noise(s, 0f, 1.3f, 3000f, 150f, 0.002f, 3.2f, 0.9f);
                return Make(s);
            }
            case Sound.LightGreen:
            {
                // Bright "go!" double ding
                var s = Buffer(0.6f);
                Tone(s, 0f, 0.25f, 1047f, 1047f, Wave.Triangle, 0.002f, 10f, 0.4f);
                Tone(s, 0.12f, 0.45f, 1568f, 1568f, Wave.Triangle, 0.002f, 6f, 0.4f);
                return Make(s);
            }
            case Sound.LightYellow:
            {
                // Hurry-up alarm: three quick beeps
                var s = Buffer(0.6f);
                for (int i = 0; i < 3; i++) Tone(s, i * 0.18f, 0.12f, 1175f, 1175f, Wave.Square, 0.002f, 8f, 0.22f);
                return Make(s);
            }
            case Sound.LightRed:
            {
                // Harsh game-show buzzer
                var s = Buffer(0.7f);
                Tone(s, 0f, 0.65f, 110f, 104f, Wave.Square, 0.005f, 1.5f, 0.35f);
                Tone(s, 0f, 0.65f, 165f, 156f, Wave.Square, 0.005f, 1.5f, 0.25f);
                return Make(s);
            }
            case Sound.Zap:
            {
                // Electric zap: buzzing noise chopped at mains frequency, with crackles
                var s = Buffer(0.5f);
                Noise(s, 0f, 0.45f, 6000f, 2000f, 0.002f, 5f, 0.6f, 60f);
                Tone(s, 0f, 0.4f, 1200f, 300f, Wave.Square, 0.002f, 6f, 0.15f);
                return Make(s);
            }
            case Sound.Blocked:
            {
                // Shield deflects: metallic ping
                var s = Buffer(0.6f);
                Tone(s, 0f, 0.55f, 1760f, 1760f, Wave.Sine, 0.001f, 6f, 0.35f, 9f);
                Tone(s, 0f, 0.55f, 2637f, 2637f, Wave.Sine, 0.001f, 8f, 0.2f);
                return Make(s);
            }
            case Sound.Safe:
            {
                var s = Buffer(0.5f);
                Tone(s, 0f, 0.45f, 784f, 784f, Wave.Triangle, 0.003f, 7f, 0.3f);
                Tone(s, 0.08f, 0.4f, 1175f, 1175f, Wave.Triangle, 0.003f, 7f, 0.3f);
                return Make(s);
            }
            case Sound.Join:
            {
                var s = Buffer(0.45f);
                Tone(s, 0f, 0.1f, 400f, 1000f, Wave.Sine, 0.002f, 20f, 0.5f);
                Tone(s, 0.08f, 0.35f, 1319f, 1319f, Wave.Triangle, 0.002f, 8f, 0.3f);
                return Make(s);
            }
            case Sound.Respawn:
            {
                var s = Buffer(0.4f);
                Tone(s, 0f, 0.35f, 300f, 950f, Wave.Sine, 0.02f, 6f, 0.35f);
                Noise(s, 0f, 0.3f, 800f, 4000f, 0.05f, 8f, 0.2f);
                return Make(s);
            }
            case Sound.Click:
            {
                var s = Buffer(0.06f);
                Tone(s, 0f, 0.05f, 1500f, 1100f, Wave.Triangle, 0.001f, 60f, 0.4f);
                return Make(s);
            }
            case Sound.Select:
            {
                var s = Buffer(0.25f);
                Tone(s, 0f, 0.08f, 660f, 660f, Wave.Triangle, 0.002f, 20f, 0.35f);
                Tone(s, 0.07f, 0.16f, 990f, 990f, Wave.Triangle, 0.002f, 14f, 0.35f);
                return Make(s);
            }
        }
        return null;
    }

    private enum Wave { Sine, Triangle, Square }

    private static float[] Buffer(float seconds) => new float[Mathf.CeilToInt(seconds * Rate)];

    // Adds a tone that glides from f0 to f1, with an attack ramp, exponential decay
    // and optional vibrato (Hz, depth as a fraction of pitch)
    private static void Tone(float[] s, float start, float length, float f0, float f1, Wave wave,
        float attack, float decay, float volume, float vibratoRate = 0f, float vibratoDepth = 0.01f)
    {
        int begin = (int)(start * Rate);
        int count = Mathf.Min((int)(length * Rate), s.Length - begin);
        double phase = 0;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)Rate;
            float u = count > 1 ? i / (float)(count - 1) : 0f;
            float f = f0 * Mathf.Pow(f1 / f0, u);
            if (vibratoRate > 0f) f *= 1f + Mathf.Sin(t * vibratoRate * Mathf.PI * 2f) * vibratoDepth;
            phase += f / Rate;
            float p = (float)(phase - System.Math.Floor(phase));
            float v;
            switch (wave)
            {
                case Wave.Triangle: v = 1f - 4f * Mathf.Abs(p - 0.5f); break;
                // Soft square: rounded so it buzzes without harsh aliasing
                case Wave.Square: v = (float)System.Math.Tanh(Mathf.Sin(p * Mathf.PI * 2f) * 4f); break;
                default: v = Mathf.Sin(p * Mathf.PI * 2f); break;
            }
            float env = Mathf.Min(1f, t / Mathf.Max(attack, 1e-4f)) * Mathf.Exp(-decay * t);
            // Short fade-out so nothing ends with a click
            env *= Mathf.Clamp01((count - i) / (Rate * 0.005f));
            s[begin + i] += v * env * volume;
        }
    }

    // Adds noise through a low-pass filter whose cutoff glides from c0 to c1;
    // `chop` (Hz) pulses the volume for rumbles and buzzes
    private static void Noise(float[] s, float start, float length, float c0, float c1,
        float attack, float decay, float volume, float chop = 0f)
    {
        int begin = (int)(start * Rate);
        int count = Mathf.Min((int)(length * Rate), s.Length - begin);
        var rng = new System.Random(begin + count);
        float y = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)Rate;
            float u = count > 1 ? i / (float)(count - 1) : 0f;
            float cutoff = c0 * Mathf.Pow(c1 / c0, u);
            float a = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / Rate);
            float n = (float)rng.NextDouble() * 2f - 1f;
            y += a * (n - y);
            float env = Mathf.Min(1f, t / Mathf.Max(attack, 1e-4f)) * Mathf.Exp(-decay * t);
            if (chop > 0f) env *= 0.55f + 0.45f * Mathf.Sin(t * chop * Mathf.PI * 2f);
            env *= Mathf.Clamp01((count - i) / (Rate * 0.005f));
            // Low cutoffs lose energy; make up for it so volumes stay comparable
            s[begin + i] += y * env * volume * Mathf.Lerp(3f, 1.2f, Mathf.Clamp01(cutoff / 4000f));
        }
    }

    // Scatter of tiny high pings, for magic and ice
    private static void Sparkle(float[] s, float start, float length, int count, float volume)
    {
        var rng = new System.Random(count * 31 + (int)(length * 100));
        for (int i = 0; i < count; i++)
        {
            float t = start + (float)rng.NextDouble() * length;
            float f = 2000f + (float)rng.NextDouble() * 3000f;
            Tone(s, t, 0.12f, f, f, Wave.Sine, 0.002f, 30f, volume);
        }
    }

    private static AudioClip Make(float[] samples)
    {
        // Normalise so the loudest point sits just under full scale
        float peak = 0f;
        foreach (var v in samples) peak = Mathf.Max(peak, Mathf.Abs(v));
        if (peak > 0.9f)
        {
            float k = 0.9f / peak;
            for (int i = 0; i < samples.Length; i++) samples[i] *= k;
        }
        var clip = AudioClip.Create("sfx", samples.Length, 1, Rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // A piece of a recorded clip (mono), with a short fade at both ends
    private static AudioClip Slice(AudioClip source, float start, float length)
    {
        if (source == null || source.loadType != AudioClipLoadType.DecompressOnLoad) return null;
        if (source.loadState != AudioDataLoadState.Loaded && !source.LoadAudioData()) return null;

        int channels = source.channels;
        int from = Mathf.Clamp((int)(start * source.frequency), 0, source.samples - 1);
        int count = Mathf.Min((int)(length * source.frequency), source.samples - from);
        var data = new float[count * channels];
        if (!source.GetData(data, from)) return null;

        var mono = new float[count];
        int fade = Mathf.Max(1, source.frequency / 200);
        for (int i = 0; i < count; i++)
        {
            float v = 0f;
            for (int c = 0; c < channels; c++) v += data[i * channels + c];
            float env = Mathf.Clamp01(i / (float)fade) * Mathf.Clamp01((count - i) / (float)fade);
            mono[i] = v / channels * env;
        }
        var clip = AudioClip.Create("slice", count, 1, source.frequency, false);
        clip.SetData(mono, 0);
        return clip;
    }
}
