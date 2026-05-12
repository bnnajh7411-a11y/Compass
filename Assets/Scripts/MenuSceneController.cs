using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MenuSceneController : MonoBehaviour
{
    private const string MainSceneName = "Main";
    private static readonly Color CardColor = new Color32(0xE7, 0xF3, 0xF1, 0xC5);
    private static readonly Vector2 CardSize = new Vector2(920f, 700f);
    private static readonly Vector2 StageButtonSize = new Vector2(150f, 150f);
    private const float StageColumnSpacing = 210f;
    private const float StageRowSpacing = 160f;
    private const float StageGridTopY = 332.8f;

    private bool isTransitioning;

    private void Start()
    {
        RuntimeUiFactory.EnsureEventSystem();
        RuntimeUiFactory.DisableEventSystemNavigation();
        BuildUi();
    }

    private void BuildUi()
    {
        int stageCount = CompassGameState.StageCount;
        if (stageCount <= 0)
        {
            Debug.LogWarning("MenuSceneController: No stages are configured.");
            return;
        }

        Canvas canvas = RuntimeUiFactory.CreateCanvas(transform, "MenuCanvas");
        Image background = RuntimeUiFactory.CreateImage(canvas.transform, "Background", RuntimeUiTheme.BackgroundColor);
        RuntimeUiFactory.Stretch(background.rectTransform);

        RectTransform card = CreateCard(background.transform);

        RuntimeUiFactory.CreateText(
            card,
            "Title",
            "STAGE SELECT",
            40,
            FontStyle.Bold,
            TextAnchor.UpperCenter,
            new Color(0.96f, 0.97f, 0.99f, 1f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(700f, 54f),
            new Vector2(0f, -18f));

        CreateStageButtons(card, stageCount);
        RuntimeUiFactory.CreateAudioToggleButton(canvas.transform);
    }

    private void SelectStageAndLoadMain(int stageIndex)
    {
        if (isTransitioning)
        {
            return;
        }

        isTransitioning = true;
        CompassGameState.SelectStage(stageIndex);
        SceneManager.LoadScene(MainSceneName);
    }

    private RectTransform CreateCard(Transform parent)
    {
        Image image = RuntimeUiFactory.CreateCardImage(parent, "Card", CardColor, CardSize);
        return image.rectTransform;
    }

    private void CreateStageButtons(Transform parent, int stageCount)
    {
        int columns = Mathf.Min(3, stageCount);
        float columnSpacing = columns > 1 ? StageColumnSpacing : 0f;
        int rows = Mathf.CeilToInt(stageCount / (float)columns);

        for (int row = 0; row < rows; row++)
        {
            int buttonsInRow = Mathf.Min(columns, stageCount - (row * columns));
            float rowWidth = (buttonsInRow - 1) * columnSpacing;
            float startX = -rowWidth * 0.5f;
            float y = StageGridTopY - (row * StageRowSpacing);

            for (int column = 0; column < buttonsInRow; column++)
            {
                int stageIndex = (row * columns) + column + 1;
                float x = startX + (column * columnSpacing);

                CreateStageButton(
                    parent,
                    $"Stage{stageIndex}Button",
                    stageIndex,
                    new Vector2(x, y));
            }
        }
    }

    private void CreateStageButton(Transform parent, string objectName, int stageIndex, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = RuntimeSpriteFactory.GetCircleSprite();
        image.color = RuntimeUiTheme.ButtonNormalColor;
        image.preserveAspect = true;

        Button button = buttonObject.AddComponent<Button>();
        RuntimeUiFactory.ApplyThemeButton(button, image);
        button.onClick.AddListener(() => SelectStageAndLoadMain(stageIndex));

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.sizeDelta = StageButtonSize;
        rectTransform.anchoredPosition = anchoredPosition;

        Text label = RuntimeUiFactory.CreateText(
            buttonObject.transform,
            "Label",
            stageIndex.ToString(),
            30,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            RuntimeUiTheme.ButtonLabelColor,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero);

        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }
}
