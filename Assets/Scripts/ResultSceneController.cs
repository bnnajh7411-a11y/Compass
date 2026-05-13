using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultSceneController : MonoBehaviour
{
    private const string MainSceneName = "Main";
    private const string MenuSceneName = "Menu";
    private static readonly Color CardColor = new Color32(0xE7, 0xF3, 0xF1, 0xC5);
    private static readonly Vector2 ActionButtonSize = new Vector2(112f, 112f);
    private const float IconButtonSpacing = 230.4f;

    [SerializeField] private Sprite retryButtonSprite;
    [SerializeField] private Sprite menuButtonSprite;
    [SerializeField] private Sprite nextStageButtonSprite;

    private bool isTransitioning;
    private Text accuracyText;

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

        accuracyText = RuntimeUiFactory.CreateText(
            card,
            "Accuracy",
            "0%",
            68,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            RuntimeUiTheme.TextColor,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(700f, 76f),
            new Vector2(0f, 80f));

        CreateActionButton(
            card,
            "RetryButton",
            retryButtonSprite,
            ActionButtonSize,
            new Vector2(-IconButtonSpacing, 56f),
            LoadCurrentStageScene);

        if (GameManager.HasNextStage)
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
        if (accuracyText != null)
        {
            accuracyText.text = $"{GameManager.LastAccuracy:0}%";
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

        if (GameManager.HasNextStage)
        {
            isTransitioning = true;
            GameManager.SelectStage(GameManager.GetNextStageIndex());
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

}
