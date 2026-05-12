using UnityEngine;

public static class CompassGameState
{
    private const string StageBestAccuracyKeyPrefix = "Compass.StageBestAccuracy.";
    private const string DefaultProgressText = "Accuracy 0% (0/0) | Line 0%";
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

    public static bool HasSubmittedResult { get; private set; }
    public static float LastAccuracy { get; private set; }
    public static string LastProgressText { get; private set; } = DefaultProgressText;
    public static int SelectedStageIndex { get; private set; } = FirstStageIndex;
    public static int StageCount => StageGuideNames.Length;
    public static int LastStageIndex => StageCount;

    public static void StoreResult(float accuracy, string progressText)
    {
        HasSubmittedResult = true;
        LastAccuracy = Mathf.Clamp(accuracy, 0f, 100f);
        LastProgressText = string.IsNullOrWhiteSpace(progressText) ? DefaultProgressText : progressText;
        UpdateBestAccuracy(SelectedStageIndex, LastAccuracy);
    }

    public static void SelectStage(int stageIndex)
    {
        SelectedStageIndex = Mathf.Clamp(stageIndex, FirstStageIndex, LastStageIndex);
    }

    public static bool HasNextStage => SelectedStageIndex < LastStageIndex;

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
