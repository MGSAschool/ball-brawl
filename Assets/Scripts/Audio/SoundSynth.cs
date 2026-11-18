using UnityEngine;

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
