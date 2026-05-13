using UnityEngine;

public enum RuntimeButtonSoundEffect
{
    None,
    ButtonTab,
    MainControl,
    DrawSE,
    Start,
    Stage,
    Result,
}

[DisallowMultipleComponent]
public sealed partial class GameManager : MonoBehaviour
{
    private const string StageBestAccuracyKeyPrefix = "Compass.StageBestAccuracy.";
    private const string AudioResourcesFolder = "Audio";
    private const int MutedVolumePercent = 0;
    private const int EnabledVolumePercent = 100;
    private const float BackgroundMusicVolume = 1f;
    private const float UiSoundVolumeScale = 1.35f;
    private const float DrawSELoopVolume = 1f;
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
    private int masterVolumePercent = EnabledVolumePercent;
    private float lastAccuracy;
    private int selectedStageIndex = FirstStageIndex;
    private AudioSource backgroundMusicSource;
    private AudioSource soundEffectSource;
    private AudioSource drawSELoopSource;
    private AudioClip buttonTabSound;
    private AudioClip mainControlSound;
    private AudioClip drawSESound;
    private AudioClip startSound;
    private AudioClip stageSound;
    private AudioClip resultSound;

    public static event System.Action<bool> MutedChanged;

    public static float LastAccuracy => EnsureExists().lastAccuracy;
    public static int SelectedStageIndex => EnsureExists().selectedStageIndex;
    public static int StageCount => StageGuideNames.Length;
    public static int LastStageIndex => StageCount;
    public static bool HasNextStage => SelectedStageIndex < LastStageIndex;
    public static bool HasInstance => instance != null;
    public static bool IsMuted => EnsureExists().masterVolumePercent == MutedVolumePercent;

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
        SetMasterVolumePercent(IsMuted ? EnabledVolumePercent : MutedVolumePercent);
    }

    public static void SetMasterVolumePercent(int volumePercent)
    {
        GameManager manager = EnsureExists();
        int normalizedVolumePercent = NormalizeVolumePercent(volumePercent);

        if (manager.masterVolumePercent == normalizedVolumePercent)
        {
            return;
        }

        manager.masterVolumePercent = normalizedVolumePercent;
        manager.ApplyVolume();
        MutedChanged?.Invoke(manager.masterVolumePercent == MutedVolumePercent);
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
        manager.ConfigureBackgroundMusicSource(audioSource);
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

    public static void RegisterDrawSEClip(AudioClip audioClip)
    {
        if (audioClip == null)
        {
            return;
        }

        GameManager manager = EnsureExists();
        manager.drawSESound = audioClip;
    }

    public static void PlayUiSound(RuntimeButtonSoundEffect soundEffect)
    {
        GameManager manager = EnsureExists();
        AudioClip clip = manager.GetUiSoundClip(soundEffect);
        if (clip == null)
        {
            return;
        }

        manager.GetOrCreateSoundEffectSource().PlayOneShot(clip, UiSoundVolumeScale);
    }

    public static void SetDrawSESoundActive(bool isActive)
    {
        GameManager manager = EnsureExists();
        AudioClip clip = manager.GetUiSoundClip(RuntimeButtonSoundEffect.DrawSE);
        if (clip == null)
        {
            return;
        }

        AudioSource drawSESource = manager.GetOrCreateDrawSELoopSource();

        if (isActive)
        {
            if (drawSESource.clip != clip)
            {
                drawSESource.clip = clip;
            }

            if (!drawSESource.isPlaying)
            {
                drawSESource.Play();
            }

            return;
        }

        if (drawSESource.isPlaying)
        {
            drawSESource.Stop();
        }
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
        masterVolumePercent = EnabledVolumePercent;
        lastAccuracy = 0f;
        selectedStageIndex = FirstStageIndex;
        backgroundMusicSource = null;
        ResetAudioSource(soundEffectSource, false);
        ResetAudioSource(drawSELoopSource, true);
    }

    private void ApplyVolume()
    {
        AudioListener.volume = Mathf.Clamp01(masterVolumePercent / (float)EnabledVolumePercent);
    }

    private AudioSource GetOrCreateSoundEffectSource()
    {
        if (soundEffectSource != null)
        {
            return soundEffectSource;
        }

        soundEffectSource = CreateUiAudioSource(false);
        return soundEffectSource;
    }

    private AudioSource GetOrCreateDrawSELoopSource()
    {
        if (drawSELoopSource != null)
        {
            return drawSELoopSource;
        }

        drawSELoopSource = CreateUiAudioSource(true);
        return drawSELoopSource;
    }

    private AudioSource CreateUiAudioSource(bool isLooping)
    {
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = isLooping;
        audioSource.spatialBlend = 0f;
        audioSource.volume = isLooping ? DrawSELoopVolume : 1f;
        return audioSource;
    }

    private void ConfigureBackgroundMusicSource(AudioSource audioSource)
    {
        audioSource.spatialBlend = 0f;
        audioSource.volume = BackgroundMusicVolume;
    }

    private AudioClip GetUiSoundClip(RuntimeButtonSoundEffect soundEffect)
    {
        switch (soundEffect)
        {
            case RuntimeButtonSoundEffect.None:
                return null;
            case RuntimeButtonSoundEffect.Start:
                return GetOrLoadUiSoundClip(ref startSound, "Start");
            case RuntimeButtonSoundEffect.MainControl:
                return GetOrLoadUiSoundClip(ref mainControlSound, "MainControl");
            case RuntimeButtonSoundEffect.DrawSE:
                return drawSESound;
            case RuntimeButtonSoundEffect.Stage:
                return GetOrLoadUiSoundClip(ref stageSound, "Stage");
            case RuntimeButtonSoundEffect.Result:
                return GetOrLoadUiSoundClip(ref resultSound, "Result");
            default:
                return GetOrLoadUiSoundClip(ref buttonTabSound, "ButtonTab");
        }
    }

    private AudioClip GetOrLoadUiSoundClip(ref AudioClip clip, string clipName)
    {
        if (clip == null)
        {
            clip = LoadUiSoundClip(clipName);
        }

        return clip;
    }

    private static AudioClip LoadUiSoundClip(string clipName)
    {
        return Resources.Load<AudioClip>($"{AudioResourcesFolder}/{clipName}");
    }

    private static int NormalizeVolumePercent(int volumePercent)
    {
        return volumePercent <= MutedVolumePercent
            ? MutedVolumePercent
            : EnabledVolumePercent;
    }

    private static void ResetAudioSource(AudioSource audioSource, bool clearClip)
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.Stop();
        if (clearClip)
        {
            audioSource.clip = null;
        }
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
