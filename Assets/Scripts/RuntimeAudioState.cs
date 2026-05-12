using UnityEngine;

public static class RuntimeAudioState
{
    private const string PlayerPrefsKey = "Compass.AudioMuted";

    private static bool isInitialized;
    private static bool isMuted;

    public static event System.Action<bool> MutedChanged;

    public static bool IsMuted
    {
        get
        {
            EnsureInitialized();
            return isMuted;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        MutedChanged = null;
        isInitialized = false;
        EnsureInitialized();
    }

    public static void Toggle()
    {
        SetMuted(!IsMuted);
    }

    public static void SetMuted(bool muted)
    {
        EnsureInitialized();

        if (isMuted == muted)
        {
            return;
        }

        isMuted = muted;
        ApplyVolume();
        PlayerPrefs.SetInt(PlayerPrefsKey, isMuted ? 1 : 0);
        PlayerPrefs.Save();
        MutedChanged?.Invoke(isMuted);
    }

    private static void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
        isMuted = PlayerPrefs.GetInt(PlayerPrefsKey, 0) == 1;
        ApplyVolume();
    }

    private static void ApplyVolume()
    {
        AudioListener.volume = isMuted ? 0f : 1f;
    }
}
