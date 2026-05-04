using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultSceneController : MonoBehaviour
{
    private const string MainSceneName = "Main";
    private const string DefaultMessage = "No submitted result yet.";

    private bool isTransitioning;
    private Text accuracyText;
    private Text detailText;

    private void Start()
    {
        EnsureEventSystem();
        BuildUi();
        RefreshUi();
    }

    private void Update()
    {
        if (isTransitioning)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            LoadMainScene();
        }
    }

    private void BuildUi()
    {
        Canvas canvas = CreateCanvas("ResultCanvas");
        Image background = CreateImage(canvas.transform, "Background", new Color(0.05f, 0.06f, 0.09f, 1f));
        Stretch(background.rectTransform);

        RectTransform card = CreateCard(background.transform);
        CreateHeader(card);
        accuracyText = CreateText(
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
            new Vector2(0f, 58f));

        detailText = CreateText(
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
            new Vector2(0f, -58f));

        CreateText(
            card,
            "Hint",
            "Press Enter or click Replay to draw again",
            20,
            FontStyle.Bold,
            TextAnchor.LowerCenter,
            new Color(0.62f, 0.94f, 0.76f, 1f),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(700f, 34f),
            new Vector2(0f, 16f));

        Button replayButton = CreateReplayButton(card);
        replayButton.Select();
    }

    private void RefreshUi()
    {
        if (accuracyText != null)
        {
            accuracyText.text = $"{CompassGameState.LastAccuracy:0}%";
        }

        if (detailText != null)
        {
            detailText.text = CompassGameState.HasSubmittedResult ? CompassGameState.LastProgressText : DefaultMessage;
        }
    }

    private void LoadMainScene()
    {
        if (isTransitioning)
        {
            return;
        }

        isTransitioning = true;
        SceneManager.LoadScene(MainSceneName);
    }

    private void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private Canvas CreateCanvas(string canvasName)
    {
        GameObject canvasObject = new GameObject(canvasName);
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private Image CreateImage(Transform parent, string objectName, Color color)
    {
        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.AddComponent<Image>();
        image.sprite = RuntimeSpriteFactory.GetWhiteSprite();
        image.color = color;
        return image;
    }

    private RectTransform CreateCard(Transform parent)
    {
        Image image = CreateImage(parent, "Card", new Color(0.10f, 0.12f, 0.17f, 0.96f));
        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(760f, 440f);
        rectTransform.anchoredPosition = Vector2.zero;
        return rectTransform;
    }

    private Button CreateReplayButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("ReplayButton");
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = RuntimeSpriteFactory.GetWhiteSprite();
        image.color = new Color(0.18f, 0.48f, 0.93f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.18f, 0.48f, 0.93f, 1f);
        colors.highlightedColor = new Color(0.26f, 0.58f, 1.0f, 1f);
        colors.pressedColor = new Color(0.12f, 0.36f, 0.75f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.18f, 0.48f, 0.93f, 0.35f);
        button.colors = colors;
        button.onClick.AddListener(LoadMainScene);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.sizeDelta = new Vector2(240f, 56f);
        rectTransform.anchoredPosition = new Vector2(0f, 60f);

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);

        Text label = labelObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 24;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = "REPLAY";
        label.raycastTarget = false;

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
    }

    private void CreateHeader(Transform parent)
    {
        CreateText(
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

    private Text CreateText(
        Transform parent,
        string objectName,
        string textValue,
        int fontSize,
        FontStyle fontStyle,
        TextAnchor alignment,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 sizeDelta,
        Vector2 anchoredPosition)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.text = textValue;
        text.raycastTarget = false;

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.anchoredPosition = anchoredPosition;

        return text;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
