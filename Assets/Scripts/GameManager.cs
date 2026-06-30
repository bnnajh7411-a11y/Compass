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
    private const string BackgroundMusicVolumeKey = "Compass.BackgroundMusicVolume";
    private const string EffectsVolumeKey = "Compass.EffectsVolume";
    private const string FullScreenEnabledKey = "Compass.FullScreenEnabled";
    private const string ResolutionWidthKey = "Compass.ResolutionWidth";
    private const string ResolutionHeightKey = "Compass.ResolutionHeight";
    private const string AudioResourcesFolder = "Audio";
    private const int MutedVolumePercent = 0;
    private const int EnabledVolumePercent = 100;
    private const int DefaultBackgroundMusicVolumePercent = 100;
    private const int DefaultEffectsVolumePercent = 100;
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
    private bool isPaused;
    private int masterVolumePercent = EnabledVolumePercent;
    private int backgroundMusicVolumePercent = DefaultBackgroundMusicVolumePercent;
    private int effectsVolumePercent = DefaultEffectsVolumePercent;
    private bool isFullScreenEnabled = true;
    private int resolutionWidth;
    private int resolutionHeight;
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
    public static event System.Action<int> BackgroundMusicVolumeChanged;
    public static event System.Action<int> EffectsVolumeChanged;
    public static event System.Action<bool> PauseStateChanged;
    public static event System.Action<bool> FullScreenChanged;
    public static event System.Action ResolutionChanged;

    public static float LastAccuracy => EnsureExists().lastAccuracy;
    public static int SelectedStageIndex => EnsureExists().selectedStageIndex;
    public static int StageCount => StageGuideNames.Length;
    public static int LastStageIndex => StageCount;
    public static bool HasNextStage => SelectedStageIndex < LastStageIndex;
    public static bool HasInstance => instance != null;
    public static bool IsMuted => EnsureExists().masterVolumePercent == MutedVolumePercent;
    public static bool IsPaused => EnsureExists().isPaused;
    public static int BackgroundMusicVolumePercent => EnsureExists().backgroundMusicVolumePercent;
    public static int EffectsVolumePercent => EnsureExists().effectsVolumePercent;
    public static bool IsFullScreenEnabled => EnsureExists().isFullScreenEnabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        MutedChanged = null;
        BackgroundMusicVolumeChanged = null;
        EffectsVolumeChanged = null;
        PauseStateChanged = null;
        FullScreenChanged = null;
        ResolutionChanged = null;

        GameManager manager = EnsureExists();
        manager.ResetSessionState();
        manager.LoadPersistentSettings();
        manager.ApplyVolume();
        manager.ApplyDisplaySettings();
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

    public static void SetBackgroundMusicVolumePercent(int volumePercent)
    {
        GameManager manager = EnsureExists();
        int normalizedVolumePercent = Mathf.Clamp(volumePercent, MutedVolumePercent, EnabledVolumePercent);
        if (manager.backgroundMusicVolumePercent == normalizedVolumePercent)
        {
            return;
        }

        manager.backgroundMusicVolumePercent = normalizedVolumePercent;
        PlayerPrefs.SetInt(BackgroundMusicVolumeKey, normalizedVolumePercent);
        PlayerPrefs.Save();
        manager.ApplyVolume();
        BackgroundMusicVolumeChanged?.Invoke(normalizedVolumePercent);
    }

    public static void SetEffectsVolumePercent(int volumePercent)
    {
        GameManager manager = EnsureExists();
        int normalizedVolumePercent = Mathf.Clamp(volumePercent, MutedVolumePercent, EnabledVolumePercent);
        if (manager.effectsVolumePercent == normalizedVolumePercent)
        {
            return;
        }

        manager.effectsVolumePercent = normalizedVolumePercent;
        PlayerPrefs.SetInt(EffectsVolumeKey, normalizedVolumePercent);
        PlayerPrefs.Save();
        manager.ApplyVolume();
        EffectsVolumeChanged?.Invoke(normalizedVolumePercent);
    }

    public static void SetPaused(bool paused)
    {
        GameManager manager = EnsureExists();
        if (manager.isPaused == paused)
        {
            return;
        }

        manager.isPaused = paused;
        if (paused)
        {
            SetDrawSESoundActive(false);
        }

        Time.timeScale = paused ? 0f : 1f;
        PauseStateChanged?.Invoke(paused);
    }

    public static void SetFullScreenEnabled(bool isEnabled)
    {
        GameManager manager = EnsureExists();
        if (manager.isFullScreenEnabled == isEnabled)
        {
            return;
        }

        manager.isFullScreenEnabled = isEnabled;
        PlayerPrefs.SetInt(FullScreenEnabledKey, isEnabled ? 1 : 0);
        PlayerPrefs.Save();
        manager.ApplyDisplaySettings();
        FullScreenChanged?.Invoke(isEnabled);
    }

    public static string CycleResolution()
    {
        GameManager manager = EnsureExists();
        ResolutionOption[] options = GetAvailableResolutionOptions();
        if (options.Length == 0)
        {
            return manager.GetCurrentResolutionLabelInternal();
        }

        int currentIndex = manager.GetCurrentResolutionIndex(options);
        int nextIndex = (currentIndex + 1) % options.Length;
        SetResolutionByIndex(nextIndex);
        return manager.GetCurrentResolutionLabelInternal();
    }

    public static string GetCurrentResolutionLabel()
    {
        return EnsureExists().GetCurrentResolutionLabelInternal();
    }

    public static string[] GetResolutionLabels()
    {
        ResolutionOption[] options = GetAvailableResolutionOptions();
        string[] labels = new string[options.Length];

        for (int i = 0; i < options.Length; i++)
        {
            labels[i] = $"{options[i].Width} x {options[i].Height}";
        }

        return labels;
    }

    public static int GetCurrentResolutionIndex()
    {
        GameManager manager = EnsureExists();
        return manager.GetCurrentResolutionIndex(GetAvailableResolutionOptions());
    }

    public static void SetResolutionByIndex(int optionIndex)
    {
        GameManager manager = EnsureExists();
        ResolutionOption[] options = GetAvailableResolutionOptions();
        if (options.Length == 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(optionIndex, 0, options.Length - 1);
        manager.SetResolutionOption(options[clampedIndex]);
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

    public static void QuitApplication()
    {
        SetPaused(false);
        Application.Quit();
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
            BackgroundMusicVolumeChanged = null;
            EffectsVolumeChanged = null;
            PauseStateChanged = null;
            FullScreenChanged = null;
            ResolutionChanged = null;
            Time.timeScale = 1f;
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
        LoadPersistentSettings();
        ApplyVolume();
        ApplyDisplaySettings();
    }

    private void ResetSessionState()
    {
        isPaused = false;
        masterVolumePercent = EnabledVolumePercent;
        backgroundMusicVolumePercent = DefaultBackgroundMusicVolumePercent;
        effectsVolumePercent = DefaultEffectsVolumePercent;
        isFullScreenEnabled = Screen.fullScreen;
        resolutionWidth = Screen.width;
        resolutionHeight = Screen.height;
        lastAccuracy = 0f;
        selectedStageIndex = FirstStageIndex;
        backgroundMusicSource = null;
        ResetAudioSource(soundEffectSource, false);
        ResetAudioSource(drawSELoopSource, true);
        Time.timeScale = 1f;
    }

    private void ApplyVolume()
    {
        AudioListener.volume = IsMuted ? 0f : 1f;

        if (backgroundMusicSource != null)
        {
            backgroundMusicSource.volume = BackgroundMusicVolume * (backgroundMusicVolumePercent / (float)EnabledVolumePercent);
        }

        if (soundEffectSource != null)
        {
            soundEffectSource.volume = effectsVolumePercent / (float)EnabledVolumePercent;
        }

        if (drawSELoopSource != null)
        {
            drawSELoopSource.volume = DrawSELoopVolume * (effectsVolumePercent / (float)EnabledVolumePercent);
        }
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
        audioSource.volume = 1f;
        ApplyVolume();
        return audioSource;
    }

    private void ConfigureBackgroundMusicSource(AudioSource audioSource)
    {
        audioSource.spatialBlend = 0f;
        ApplyVolume();
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

    private void LoadPersistentSettings()
    {
        backgroundMusicVolumePercent = Mathf.Clamp(
            PlayerPrefs.GetInt(BackgroundMusicVolumeKey, DefaultBackgroundMusicVolumePercent),
            MutedVolumePercent,
            EnabledVolumePercent);
        effectsVolumePercent = Mathf.Clamp(
            PlayerPrefs.GetInt(EffectsVolumeKey, DefaultEffectsVolumePercent),
            MutedVolumePercent,
            EnabledVolumePercent);
        isFullScreenEnabled = PlayerPrefs.GetInt(FullScreenEnabledKey, Screen.fullScreen ? 1 : 0) != 0;

        ResolutionOption[] options = GetAvailableResolutionOptions();
        int savedWidth = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.width);
        int savedHeight = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.height);
        if (options.Length == 0)
        {
            resolutionWidth = savedWidth;
            resolutionHeight = savedHeight;
            return;
        }

        int optionIndex = GetClosestResolutionIndex(options, savedWidth, savedHeight);
        resolutionWidth = options[optionIndex].Width;
        resolutionHeight = options[optionIndex].Height;
    }

    private void ApplyDisplaySettings()
    {
        if (resolutionWidth <= 0 || resolutionHeight <= 0)
        {
            resolutionWidth = Mathf.Max(Screen.width, 1);
            resolutionHeight = Mathf.Max(Screen.height, 1);
        }

        Screen.SetResolution(resolutionWidth, resolutionHeight, isFullScreenEnabled);
    }

    private void SetResolutionOption(ResolutionOption option)
    {
        resolutionWidth = option.Width;
        resolutionHeight = option.Height;
        PlayerPrefs.SetInt(ResolutionWidthKey, resolutionWidth);
        PlayerPrefs.SetInt(ResolutionHeightKey, resolutionHeight);
        PlayerPrefs.Save();
        ApplyDisplaySettings();
        ResolutionChanged?.Invoke();
    }

    private string GetCurrentResolutionLabelInternal()
    {
        int width = resolutionWidth > 0 ? resolutionWidth : Screen.width;
        int height = resolutionHeight > 0 ? resolutionHeight : Screen.height;
        return $"{width} x {height}";
    }

    private int GetCurrentResolutionIndex(ResolutionOption[] options)
    {
        if (options == null || options.Length == 0)
        {
            return 0;
        }

        return GetClosestResolutionIndex(options, resolutionWidth, resolutionHeight);
    }

    private static int GetClosestResolutionIndex(ResolutionOption[] options, int targetWidth, int targetHeight)
    {
        if (options == null || options.Length == 0)
        {
            return 0;
        }

        int bestIndex = 0;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < options.Length; i++)
        {
            int widthDelta = Mathf.Abs(options[i].Width - targetWidth);
            int heightDelta = Mathf.Abs(options[i].Height - targetHeight);
            int totalDistance = widthDelta + heightDelta;
            if (totalDistance < bestDistance)
            {
                bestDistance = totalDistance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static ResolutionOption[] GetAvailableResolutionOptions()
    {
        Resolution[] availableResolutions = Screen.resolutions;
        if (availableResolutions == null || availableResolutions.Length == 0)
        {
            return new[]
            {
                new ResolutionOption(Mathf.Max(Screen.width, 1), Mathf.Max(Screen.height, 1))
            };
        }

        System.Collections.Generic.List<ResolutionOption> options = new System.Collections.Generic.List<ResolutionOption>();
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            Resolution resolution = availableResolutions[i];
            bool alreadyExists = false;
            for (int optionIndex = 0; optionIndex < options.Count; optionIndex++)
            {
                if (options[optionIndex].Width == resolution.width
                    && options[optionIndex].Height == resolution.height)
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (!alreadyExists)
            {
                options.Add(new ResolutionOption(resolution.width, resolution.height));
            }
        }

        options.Sort((left, right) =>
        {
            int widthComparison = left.Width.CompareTo(right.Width);
            return widthComparison != 0 ? widthComparison : left.Height.CompareTo(right.Height);
        });

        return options.ToArray();
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

    private struct ResolutionOption
    {
        public ResolutionOption(int width, int height)
        {
            Width = Mathf.Max(width, 1);
            Height = Mathf.Max(height, 1);
        }

        public int Width { get; }
        public int Height { get; }
    }
}
