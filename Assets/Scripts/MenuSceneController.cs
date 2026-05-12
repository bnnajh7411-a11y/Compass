using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MenuSceneController : MonoBehaviour
{
    private const string MainSceneName = "Main";
    private static readonly Color CardColor = new Color32(0x0D, 0x11, 0x16, 0xF2);
    private static readonly Vector2 CardSize = new Vector2(920f, 700f);
    private static readonly Vector2 StageButtonSize = new Vector2(150f, 150f);
    private const float StageColumnSpacing = 210f;
    private const float StageRowSpacing = 160f;
    private const float StageGridTopY = 332.8f;

    private bool isTransitioning;
    private Button[] stageButtons = System.Array.Empty<Button>();

    private void Start()
    {
        RuntimeUiFactory.EnsureEventSystem();
        BuildUi();
        SelectCurrentStageButton();
    }

    private void Update()
    {
        if (isTransitioning)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            StartSelectedStage();
        }
    }

    private void BuildUi()
    {
        int stageCount = CompassGameState.StageCount;
        if (stageCount <= 0)
        {
            Debug.LogWarning("MenuSceneController: No stages are configured.");
            return;
        }

        stageButtons = new Button[stageCount];

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

    private void StartSelectedStage()
    {
        SelectStageAndLoadMain(CompassGameState.SelectedStageIndex);
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

    private void SelectCurrentStageButton()
    {
        if (stageButtons == null || stageButtons.Length == 0)
        {
            return;
        }

        int buttonIndex = Mathf.Clamp(
            CompassGameState.SelectedStageIndex - 1,
            0,
            stageButtons.Length - 1);

        if (buttonIndex >= 0 && buttonIndex < stageButtons.Length && stageButtons[buttonIndex] != null)
        {
            stageButtons[buttonIndex].Select();
        }
    }

    private RectTransform CreateCard(Transform parent)
    {
        Image image = RuntimeUiFactory.CreateImage(parent, "Card", CardColor);
        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = CardSize;
        rectTransform.anchoredPosition = Vector2.zero;
        return rectTransform;
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

                stageButtons[stageIndex - 1] = CreateStageButton(
                    parent,
                    $"Stage{stageIndex}Button",
                    stageIndex,
                    new Vector2(x, y));
            }
        }
    }

    private Button CreateStageButton(Transform parent, string objectName, int stageIndex, Vector2 anchoredPosition)
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

        return button;
    }
}
