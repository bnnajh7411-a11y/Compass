using UnityEngine.SceneManagement;

public sealed partial class GameManager
{
    private const string MainSceneName = "Main";
    private const string MenuSceneName = "Menu";
    private const string ResultSceneName = "Result";

    public static void GoToMenuScene(bool resetStage = false)
    {
        if (resetStage)
        {
            SelectStage(FirstStageIndex);
        }

        LoadScene(MenuSceneName);
    }

    public static void GoToStageScene(int stageIndex)
    {
        SelectStage(stageIndex);
        LoadScene(MainSceneName);
    }

    public static void GoToCurrentStageScene()
    {
        LoadScene(MainSceneName);
    }

    public static void GoToNextStageOrMenuScene()
    {
        if (HasNextStage)
        {
            GoToStageScene(GetNextStageIndex());
            return;
        }

        GoToMenuScene();
    }

    public static void CompleteRun(float accuracy)
    {
        StoreResult(accuracy);
        LoadScene(ResultSceneName);
    }

    private static void LoadScene(string sceneName)
    {
        SetPaused(false);
        SceneManager.LoadScene(sceneName);
    }
}
