using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultSceneController : MonoBehaviour
{
    private const string MainSceneName = "Main";
    private const string MenuSceneName = "Menu";
    private const string DefaultMessage = "No submitted result yet.";
    private static readonly Color CardColor = new Color32(0xE7, 0xF3, 0xF1, 0xC5);
    private static readonly Vector2 ActionButtonSize = new Vector2(112f, 112f);
    private const float IconButtonSpacing = 230.4f;

    [SerializeField] private Sprite retryButtonSprite;
    [SerializeField] private Sprite menuButtonSprite;
    [SerializeField] private Sprite nextStageButtonSprite;

    private bool isTransitioning;
    private Text stageText;
    private Text accuracyText;
    private Text detailText;

    private void Start()
    {
        RuntimeUiFactory.EnsureEventSystem();
        RuntimeUiFactory.DisableEventSystemNavigation();
        BuildUi();
        RefreshUi();
    }

    private void BuildUi()
    {
        Canvas canvas = RuntimeUiFactory.CreateCanvas(transform, "ResultCanvas");
        Image background = RuntimeUiFactory.CreateImage(canvas.transform, "Background", RuntimeUiTheme.BackgroundColor);
        RuntimeUiFactory.Stretch(background.rectTransform);

        RectTransform card = CreateCard(background.transform);
        CreateHeader(card);

        stageText = RuntimeUiFactory.CreateText(
            card,
            "Stage",
            "STAGE 1 COMPLETE",
            26,
            FontStyle.Bold,
            TextAnchor.UpperCenter,
            new Color(0.62f, 0.94f, 0.76f, 1f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(700f, 34f),
            new Vector2(0f, -62f));

        accuracyText = RuntimeUiFactory.CreateText(
            card,
            "Accuracy",
            "0%",
            68,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            new Color(0.96f, 0.96f, 0.98f, 1f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(700f, 76f),
            new Vector2(0f, 50f));

        detailText = RuntimeUiFactory.CreateText(
            card,
            "Detail",
            DefaultMessage,
            22,
            FontStyle.Normal,
            TextAnchor.MiddleCenter,
            new Color(0.84f, 0.88f, 0.94f, 1f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(700f, 64f),
            new Vector2(0f, -70f));

        CreateActionButton(
            card,
            "RetryButton",
            retryButtonSprite,
            ActionButtonSize,
            new Vector2(-IconButtonSpacing, 56f),
            LoadCurrentStageScene);

        if (CompassGameState.HasNextStage)
        {
            CreateActionButton(
                card,
                "MenuButton",
                menuButtonSprite,
                ActionButtonSize,
                new Vector2(0f, 56f),
                LoadMenuScene);

            CreateActionButton(
                card,
                "PrimaryButton",
                nextStageButtonSprite,
                ActionButtonSize,
                new Vector2(IconButtonSpacing, 56f),
                LoadPrimaryAction);
        }
        else
        {
            CreateActionButton(
                card,
                "PrimaryButton",
                menuButtonSprite,
                ActionButtonSize,
                new Vector2(0f, 56f),
                LoadMenuScene);
        }
        RuntimeUiFactory.CreateAudioToggleButton(canvas.transform);
    }

    private void RefreshUi()
    {
        if (stageText != null)
        {
            stageText.text = $"{CompassGameState.GetCurrentStageLabel().ToUpperInvariant()} COMPLETE";
        }

        if (accuracyText != null)
        {
            accuracyText.text = $"{CompassGameState.LastAccuracy:0}%";
        }

        if (detailText != null)
        {
            detailText.text = CompassGameState.HasSubmittedResult ? CompassGameState.LastProgressText : DefaultMessage;
        }
    }

    private void LoadCurrentStageScene()
    {
        if (isTransitioning)
        {
            return;
        }

        isTransitioning = true;
        SceneManager.LoadScene(MainSceneName);
    }

    private void LoadPrimaryAction()
    {
        if (isTransitioning)
        {
            return;
        }

        if (CompassGameState.HasNextStage)
        {
            isTransitioning = true;
            CompassGameState.SelectStage(CompassGameState.GetNextStageIndex());
            SceneManager.LoadScene(MainSceneName);
            return;
        }

        isTransitioning = true;
        SceneManager.LoadScene(MenuSceneName);
    }

    private void LoadMenuScene()
    {
        if (isTransitioning)
        {
            return;
        }

        isTransitioning = true;
        SceneManager.LoadScene(MenuSceneName);
    }

    private RectTransform CreateCard(Transform parent)
    {
        Image image = RuntimeUiFactory.CreateCardImage(
            parent,
            "Card",
            CardColor,
            new Vector2(800f, 480f));
        return image.rectTransform;
    }

    private void CreateActionButton(
        Transform parent,
        string objectName,
        Sprite iconSprite,
        Vector2 sizeDelta,
        Vector2 anchoredPosition,
        UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = iconSprite ?? RuntimeSpriteFactory.GetWhiteSprite();
        image.color = RuntimeUiTheme.ButtonNormalColor;
        image.preserveAspect = true;

        Button button = buttonObject.AddComponent<Button>();
        RuntimeUiFactory.ApplyThemeButton(button, image);
        button.onClick.AddListener(onClick);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.anchoredPosition = anchoredPosition;
    }

    private void CreateHeader(Transform parent)
    {
        RuntimeUiFactory.CreateText(
            parent,
            "Title",
            "RESULT",
            28,
            FontStyle.Bold,
            TextAnchor.UpperCenter,
            new Color(0.95f, 0.95f, 0.98f, 1f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(680f, 44f),
            new Vector2(0f, -20f));
    }
}
