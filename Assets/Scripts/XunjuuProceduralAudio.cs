using System;
using UnityEngine;

// ============================================================================
// Xunjuu v0.1 - Sonidos de respaldo del mundo
// ACCION: crear voces breves para fauna, jefe y Orbitasword cuando un prefab
// no tiene un AudioClip asignado. Cada clip puede reemplazarse en Inspector.
// ============================================================================
public static class XunjuuProceduralAudio
{
    private const int SampleRate = 44100;

    private static AudioClip duckCall;
    private static AudioClip deerCall;
    private static AudioClip bossRoar;
    private static AudioClip bossCharge;
    private static AudioClip swordWhoosh;
    private static AudioClip swordImpactAccent;
    private static AudioClip orbitalWhoosh;

    public static AudioClip GetDuckCall()
    {
        if (duckCall == null)
            duckCall = BuildClip("Xunjuu_Pato_Voz", 0.48f, DuckSample, 101u);
        return duckCall;
    }

    public static AudioClip GetDeerCall()
    {
        if (deerCall == null)
            deerCall = BuildClip("Xunjuu_Venado_Voz", 0.9f, DeerSample, 203u);
        return deerCall;
    }

    public static AudioClip GetBossRoar()
    {
        if (bossRoar == null)
            bossRoar = BuildClip("Xunjuu_Ocelotl_Rugido", 1.2f, BossRoarSample, 307u);
        return bossRoar;
    }

    public static AudioClip GetBossCharge()
    {
        if (bossCharge == null)
            bossCharge = BuildClip("Xunjuu_Ocelotl_Embestida", 0.76f, BossChargeSample, 409u);
        return bossCharge;
    }

    public static AudioClip GetSwordWhoosh()
    {
        if (swordWhoosh == null)
            swordWhoosh = BuildClip("Xunjuu_Macuahuitl_Aire", 0.44f, SwordWhooshSample, 503u);
        return swordWhoosh;
    }

    public static AudioClip GetOrbitalWhoosh()
    {
        if (orbitalWhoosh == null)
            orbitalWhoosh = BuildClip("Xunjuu_Orbitasword_Aire", 1.35f, OrbitalWhooshSample, 601u);
        return orbitalWhoosh;
    }

    public static AudioClip GetSwordImpactAccent()
    {
        if (swordImpactAccent == null)
            swordImpactAccent = BuildClip("Xunjuu_Macuahuitl_Impacto", 0.18f, SwordImpactSample, 557u);
        return swordImpactAccent;
    }

    private static AudioClip BuildClip(string name, float duration, Func<float, float, float> sampler, uint seed)
    {
        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] samples = new float[sampleCount];
        uint noiseState = seed;

        for (int index = 0; index < sampleCount; index++)
        {
            float time = index / (float)SampleRate;
            float noise = NextNoise(ref noiseState);
            samples[index] = Mathf.Clamp(sampler(time, noise), -0.92f, 0.92f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static float DuckSample(float time, float noise)
    {
        float localTime = time < 0.22f ? time : time - 0.25f;
        if (localTime < 0f || localTime > 0.2f)
            return 0f;

        float progress = localTime / 0.2f;
        float envelope = Mathf.Sin(progress * Mathf.PI) * Mathf.Exp(-1.4f * progress);
        float frequency = Mathf.Lerp(760f, 430f, progress);
        float voice = Mathf.Sin(2f * Mathf.PI * frequency * localTime)
            + 0.38f * Mathf.Sin(2f * Mathf.PI * frequency * 1.92f * localTime);
        return envelope * (voice * 0.42f + noise * 0.12f);
    }

    private static float DeerSample(float time, float noise)
    {
        float envelope = AttackReleaseEnvelope(time, 0.9f, 0.08f, 0.28f);
        float frequency = Mathf.Lerp(225f, 148f, time / 0.9f) + Mathf.Sin(time * 31f) * 7f;
        float voice = Mathf.Sin(2f * Mathf.PI * frequency * time)
            + 0.3f * Mathf.Sin(2f * Mathf.PI * frequency * 2.03f * time);
        return envelope * (voice * 0.36f + noise * 0.075f);
    }

    private static float BossRoarSample(float time, float noise)
    {
        float envelope = AttackReleaseEnvelope(time, 1.2f, 0.06f, 0.34f);
        float frequency = 88f + Mathf.Sin(time * 19f) * 15f - time * 12f;
        float growl = Mathf.Sin(2f * Mathf.PI * frequency * time)
            + 0.48f * Mathf.Sin(2f * Mathf.PI * frequency * 1.97f * time)
            + 0.2f * Mathf.Sin(2f * Mathf.PI * frequency * 3.06f * time);
        return envelope * (growl * 0.3f + noise * 0.16f);
    }

    private static float BossChargeSample(float time, float noise)
    {
        float envelope = AttackReleaseEnvelope(time, 0.76f, 0.035f, 0.2f);
        float progress = time / 0.76f;
        float frequency = Mathf.Lerp(118f, 72f, progress);
        float voice = Mathf.Sin(2f * Mathf.PI * frequency * time)
            + 0.42f * Mathf.Sin(2f * Mathf.PI * frequency * 2.15f * time);
        return envelope * (voice * 0.3f + noise * (0.08f + progress * 0.12f));
    }

    private static float SwordWhooshSample(float time, float noise)
    {
        float progress = time / 0.44f;
        float envelope = Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
        float sweep = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(190f, 720f, progress) * time);
        return envelope * (noise * 0.23f + sweep * 0.08f);
    }

    private static float SwordImpactSample(float time, float noise)
    {
        float progress = Mathf.Clamp01(time / 0.18f);
        float envelope = Mathf.Exp(-7.5f * progress) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / 0.012f));
        float thump = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(92f, 48f, progress) * time);
        float wood = Mathf.Sin(2f * Mathf.PI * 310f * time) * Mathf.Exp(-13f * progress);
        return envelope * (thump * 0.62f + wood * 0.2f + noise * 0.13f);
    }

    private static float OrbitalWhooshSample(float time, float noise)
    {
        float envelope = AttackReleaseEnvelope(time, 1.35f, 0.08f, 0.18f);
        float pulse = 0.52f + Mathf.Sin(time * 35f) * 0.18f;
        float sweep = Mathf.Sin(2f * Mathf.PI * (240f + Mathf.Sin(time * 9f) * 95f) * time);
        return envelope * (noise * pulse * 0.24f + sweep * 0.075f);
    }

    private static float AttackReleaseEnvelope(float time, float duration, float attack, float release)
    {
        float attackLevel = Mathf.Clamp01(time / Mathf.Max(0.001f, attack));
        float releaseLevel = Mathf.Clamp01((duration - time) / Mathf.Max(0.001f, release));
        return Mathf.SmoothStep(0f, 1f, Mathf.Min(attackLevel, releaseLevel));
    }

    private static float NextNoise(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return (state / (float)uint.MaxValue) * 2f - 1f;
    }
}
