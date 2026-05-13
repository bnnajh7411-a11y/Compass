[System.Obsolete("Use GameManager instead.")]
public static class RuntimeAudioState
{
    public static event System.Action<bool> MutedChanged
    {
        add => GameManager.MutedChanged += value;
        remove => GameManager.MutedChanged -= value;
    }

    public static bool IsMuted => GameManager.IsMuted;

    public static void Toggle()
    {
        GameManager.Toggle();
    }

    public static void SetMuted(bool muted)
    {
        GameManager.SetMuted(muted);
    }
}
