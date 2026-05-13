[System.Obsolete("Use GameManager instead.")]
public static class CompassGameState
{
    public const int FirstStageIndex = GameManager.FirstStageIndex;

    public static bool HasSubmittedResult => GameManager.HasSubmittedResult;
    public static float LastAccuracy => GameManager.LastAccuracy;
    public static string LastProgressText => GameManager.LastProgressText;
    public static int SelectedStageIndex => GameManager.SelectedStageIndex;
    public static int StageCount => GameManager.StageCount;
    public static int LastStageIndex => GameManager.LastStageIndex;
    public static bool HasNextStage => GameManager.HasNextStage;

    public static void StoreResult(float accuracy, string progressText)
    {
        GameManager.StoreResult(accuracy, progressText);
    }

    public static void SelectStage(int stageIndex)
    {
        GameManager.SelectStage(stageIndex);
    }

    public static int GetNextStageIndex()
    {
        return GameManager.GetNextStageIndex();
    }

    public static string GetStageLabel(int stageIndex)
    {
        return GameManager.GetStageLabel(stageIndex);
    }

    public static string GetCurrentStageLabel()
    {
        return GameManager.GetCurrentStageLabel();
    }

    public static bool TryGetBestAccuracy(int stageIndex, out float bestAccuracy)
    {
        return GameManager.TryGetBestAccuracy(stageIndex, out bestAccuracy);
    }

    public static string GetBestAccuracyText(int stageIndex)
    {
        return GameManager.GetBestAccuracyText(stageIndex);
    }

    public static string GetStageGuideName(int stageIndex)
    {
        return GameManager.GetStageGuideName(stageIndex);
    }
}
