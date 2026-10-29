using System;
using UnityEngine;

// Solo preferencias de presentación; no sustituye un sistema de guardado de partida.
public static class XunjuuPresentationPreferences
{
    public const string MusicKey = "dyanatro.presentation.music";
    public const string MotionKey = "dyanatro.presentation.reducedMotion";
    public static event Action Changed;
    public static float MusicVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 1f));
    public static bool ReducedMotion => PlayerPrefs.GetInt(MotionKey, 0) == 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetListeners() { Changed = null; }

    public static void SetMusicVolume(float value)
    {
        PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value));
        Changed?.Invoke();
    }

    public static void SetReducedMotion(bool value)
    {
        PlayerPrefs.SetInt(MotionKey, value ? 1 : 0);
        Changed?.Invoke();
    }

    public static void Save() { PlayerPrefs.Save(); }
}
