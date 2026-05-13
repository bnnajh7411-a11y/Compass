using UnityEngine;

[DisallowMultipleComponent]
public sealed partial class GameManager : MonoBehaviour
{
    private const string StageBestAccuracyKeyPrefix = "Compass.StageBestAccuracy.";
    public const int FirstStageIndex = 1;
    private static readonly string[] StageGuideNames =
    {
        "Spline1",
        "Spline2",
        "Spline3",
        "Spline4",
        "Spline5",
        "Spline6",
    };

    private static GameManager instance;

    private bool isInitialized;
    private bool isMuted;
    private float lastAccuracy;
    private int selectedStageIndex = FirstStageIndex;
    private AudioSource backgroundMusicSource;

    public static event System.Action<bool> MutedChanged;

    public static GameManager Instance
    {
        get
        {
            return EnsureExists();
        }
    }

    public static float LastAccuracy => Instance.lastAccuracy;
    public static int SelectedStageIndex => Instance.selectedStageIndex;
    public static int StageCount => StageGuideNames.Length;
    public static int LastStageIndex => StageCount;
    public static bool HasNextStage => SelectedStageIndex < LastStageIndex;
    public static bool IsMuted => Instance.isMuted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        MutedChanged = null;

        GameManager manager = EnsureExists();
        manager.ResetSessionState();
        manager.ApplyVolume();
    }

    public static void Toggle()
    {
        SetMuted(!IsMuted);
    }

    public static void SetMuted(bool muted)
    {
        GameManager manager = EnsureExists();

        if (manager.isMuted == muted)
        {
            return;
        }

        manager.isMuted = muted;
        manager.ApplyVolume();
        MutedChanged?.Invoke(manager.isMuted);
    }

    public static void RegisterBackgroundMusic(AudioSource audioSource)
    {
        if (audioSource == null)
        {
            return;
        }

        GameManager manager = EnsureExists();

        if (manager.backgroundMusicSource == audioSource)
        {
            return;
        }

        if (manager.backgroundMusicSource != null)
        {
            audioSource.Stop();
            Object.Destroy(audioSource.gameObject);
            return;
        }

        manager.backgroundMusicSource = audioSource;
        Object.DontDestroyOnLoad(audioSource.gameObject);
        manager.ApplyVolume();
    }

    public static void UnregisterBackgroundMusic(AudioSource audioSource)
    {
        GameManager manager = instance;
        if (manager == null || audioSource == null)
        {
            return;
        }

        if (manager.backgroundMusicSource == audioSource)
        {
            manager.backgroundMusicSource = null;
        }
    }

    public static void SelectStage(int stageIndex)
    {
        GameManager manager = EnsureExists();
        manager.selectedStageIndex = Mathf.Clamp(stageIndex, FirstStageIndex, LastStageIndex);
    }

    private static void StoreResult(float accuracy)
    {
        GameManager manager = EnsureExists();
        manager.lastAccuracy = Mathf.Clamp(accuracy, 0f, 100f);
        UpdateBestAccuracy(manager.selectedStageIndex, manager.lastAccuracy);
    }

    public static int GetNextStageIndex()
    {
        return Mathf.Min(SelectedStageIndex + 1, LastStageIndex);
    }

    public static string GetStageLabel(int stageIndex)
    {
        return $"Stage {Mathf.Clamp(stageIndex, FirstStageIndex, LastStageIndex)}";
    }

    public static string GetCurrentStageLabel()
    {
        return GetStageLabel(SelectedStageIndex);
    }

    public static bool TryGetBestAccuracy(int stageIndex, out float bestAccuracy)
    {
        int clampedStageIndex = Mathf.Clamp(stageIndex, FirstStageIndex, LastStageIndex);
        string key = GetBestAccuracyKey(clampedStageIndex);
        if (!PlayerPrefs.HasKey(key))
        {
            bestAccuracy = 0f;
            return false;
        }

        bestAccuracy = Mathf.Clamp(PlayerPrefs.GetFloat(key, 0f), 0f, 100f);
        return true;
    }

    public static string GetBestAccuracyText(int stageIndex)
    {
        return TryGetBestAccuracy(stageIndex, out float bestAccuracy)
            ? $"BEST {bestAccuracy:0}%"
            : "BEST --";
    }

    public static string GetStageGuideName(int stageIndex)
    {
        if (StageGuideNames.Length == 0)
        {
            return "Spline1";
        }

        int clampedIndex = Mathf.Clamp(stageIndex, FirstStageIndex, LastStageIndex) - FirstStageIndex;
        return StageGuideNames[clampedIndex];
    }

    private static GameManager EnsureExists()
    {
        if (instance != null)
        {
            instance.InitializeIfNeeded();
            return instance;
        }

        GameObject managerObject = new GameObject("[GameManager]");
        instance = managerObject.AddComponent<GameManager>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeIfNeeded();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            backgroundMusicSource = null;
            MutedChanged = null;
        }
    }

    private void InitializeIfNeeded()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
        ResetSessionState();
        ApplyVolume();
    }

    private void ResetSessionState()
    {
        isMuted = false;
        lastAccuracy = 0f;
        selectedStageIndex = FirstStageIndex;
        backgroundMusicSource = null;
    }

    private void ApplyVolume()
    {
        AudioListener.volume = isMuted ? 0f : 1f;
    }

    private static void UpdateBestAccuracy(int stageIndex, float accuracy)
    {
        int clampedStageIndex = Mathf.Clamp(stageIndex, FirstStageIndex, LastStageIndex);
        string key = GetBestAccuracyKey(clampedStageIndex);
        bool hasExistingValue = PlayerPrefs.HasKey(key);
        float existingBest = hasExistingValue ? PlayerPrefs.GetFloat(key, 0f) : 0f;

        if (hasExistingValue && accuracy <= existingBest)
        {
            return;
        }

        PlayerPrefs.SetFloat(key, accuracy);
        PlayerPrefs.Save();
    }

    private static string GetBestAccuracyKey(int stageIndex)
    {
        return $"{StageBestAccuracyKeyPrefix}{Mathf.Clamp(stageIndex, FirstStageIndex, LastStageIndex)}";
    }
}
