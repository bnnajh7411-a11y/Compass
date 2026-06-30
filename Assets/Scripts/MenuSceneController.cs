using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MenuSceneController : MonoBehaviour
{
    private static readonly Color CardColor = new Color32(0xE7, 0xF3, 0xF1, 0xC5);
    private static readonly Vector2 CardSize = new Vector2(920f, 700f);
    private static readonly Vector2 StageButtonSize = new Vector2(150f, 150f);
    private const float StageColumnSpacing = 283.5f;
    private const float StageRowSpacing = 198f;
    private const float StageGridTopY = 400f;
    private static readonly Vector2 StageNumberTextSize = new Vector2(120f, 40f);
    private static readonly Vector2 StageBestTextSize = new Vector2(120f, 28f);
    private static readonly Vector2 StageNumberTextPosition = new Vector2(0f, 20f);
    private static readonly Vector2 StageBestTextPosition = new Vector2(0f, -38f);
    private static readonly Vector2 ControlsTextSize = new Vector2(270f, 170f);
    private static readonly Vector2 ControlsTextPosition = new Vector2(-28f, -28f);
    private static readonly Vector2 ControlsBackgroundSize = new Vector2(320f, 204f);
    private const int StageBestFontSize = 18;
    private const int ControlsFontSize = 20;

    private bool isTransitioning;

    private void Start()
    {
        RuntimeUiFactory.EnsureEventSystem();
        RuntimeUiFactory.DisableEventSystemNavigation();
        BuildUi();
    }

    private void BuildUi()
    {
        int stageCount = GameManager.StageCount;
        if (stageCount <= 0)
        {
            return;
        }

        Canvas canvas = RuntimeUiFactory.CreateCanvas(transform, "MenuCanvas");
        Image background = RuntimeUiFactory.CreateImage(canvas.transform, "Background", RuntimeUiTheme.BackgroundColor);
        RuntimeUiFactory.Stretch(background.rectTransform);

        RectTransform card = CreateCard(background.transform);

        Image controlsBackground = RuntimeUiFactory.CreateCardImage(
            canvas.transform,
            "ControlsBackground",
            CardColor,
            ControlsBackgroundSize);
        RectTransform controlsBackgroundRect = controlsBackground.rectTransform;
        controlsBackgroundRect.anchorMin = new Vector2(1f, 1f);
        controlsBackgroundRect.anchorMax = new Vector2(1f, 1f);
        controlsBackgroundRect.pivot = new Vector2(1f, 1f);
        controlsBackgroundRect.sizeDelta = ControlsBackgroundSize;
        controlsBackgroundRect.anchoredPosition = ControlsTextPosition;

        RuntimeUiFactory.CreateText(
            card,
            "Title",
            "STAGE SELECT",
            40,
            FontStyle.Bold,
            TextAnchor.UpperCenter,
            RuntimeUiTheme.TextColor,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(700f, 54f),
            new Vector2(0f, -18f));

        Text controlsText = RuntimeUiFactory.CreateText(
            controlsBackground.transform,
            "Controls",
            "\u003c\uc870\uc791\u0020\ubc29\ubc95\u003e\u000d\n\u0041\u002f\u0044\u0020\u0020\uc911\uc2ec\u0020\uad50\uccb4\u000d\n\u0057\u002f\u0053\u0020\u0020\uae38\uc774\u0020\uc870\uc808\u000d\n\u0052\u0020\u0020\uadf8\ub9bc\u0020\ucd08\uae30\ud654\u000d\n\u0053\u0070\u0061\u0063\u0065\u0020\u0020\uadf8\ub9ac\uae30\u000d\n\u0045\u006e\u0074\u0065\u0072\u0020\u0020\uacb0\uacfc\u0020\ubcf4\uae30",
            ControlsFontSize,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            RuntimeUiTheme.TextColor,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-24f, -24f),
            new Vector2(0f, ControlsBackgroundSize.y * 0.05f));
        controlsText.supportRichText = false;

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
        GameManager.GoToStageScene(stageIndex);
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
        RuntimeUiFactory.ApplyThemeButton(button, image, RuntimeButtonSoundEffect.Stage);
        button.onClick.AddListener(() => SelectStageAndLoadMain(stageIndex));

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.sizeDelta = StageButtonSize;
        rectTransform.anchoredPosition = anchoredPosition;

        RuntimeUiFactory.CreateText(
            buttonObject.transform,
            "Label",
            stageIndex.ToString(),
            30,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            RuntimeUiTheme.TextColor,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            StageNumberTextSize,
            StageNumberTextPosition);

        RuntimeUiFactory.CreateText(
            buttonObject.transform,
            "Best",
            GameManager.GetBestAccuracyText(stageIndex),
            StageBestFontSize,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            RuntimeUiTheme.TextColor,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            StageBestTextSize,
            StageBestTextPosition);
    }
}
