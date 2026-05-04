using UnityEngine;

public static class CompassGameState
{
    private const string DefaultProgressText = "Accuracy 0% (0/0) | Line 0%";

    public static bool HasSubmittedResult { get; private set; }
    public static float LastAccuracy { get; private set; }
    public static string LastProgressText { get; private set; } = DefaultProgressText;

    public static void StoreResult(float accuracy, string progressText)
    {
        HasSubmittedResult = true;
        LastAccuracy = Mathf.Clamp(accuracy, 0f, 100f);
        LastProgressText = string.IsNullOrWhiteSpace(progressText) ? DefaultProgressText : progressText;
    }

}
