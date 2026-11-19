using UnityEngine;
using Wave = SoundSynth.Wave;

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
            case Sound.Freeze: return Freeze();
            case Sound.Explosion: return Explosion();
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

    private static AudioClip Freeze()
    {
        float[] s = SoundSynth.Buffer(0.8f);
        SoundSynth.Sparkle(s, 0f, 0.7f, 26, 0.16f);
        SoundSynth.Noise(s, 0f, 0.7f, 9000f, 9000f, 0.05f, 4f, 0.12f);
        SoundSynth.Tone(s, 0f, 0.6f, 1800f, 2600f, Wave.Sine, 0.05f, 5f, 0.12f);
        return SoundSynth.Make(s);
    }

    private static AudioClip Explosion()
    {
        float[] s = SoundSynth.Buffer(1.4f);
        SoundSynth.Tone(s, 0f, 0.8f, 90f, 28f, Wave.Sine, 0.002f, 4f, 1f);
        SoundSynth.Noise(s, 0f, 1.3f, 3000f, 150f, 0.002f, 3.2f, 0.9f);
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
