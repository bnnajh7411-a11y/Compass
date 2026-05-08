using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StartSceneController : MonoBehaviour
{
    private const string MainSceneName = "Main";

    private bool isTransitioning;
    private Button startButton;

    private void Start()
    {
        EnsureEventSystem();
        BuildUi();

        if (startButton != null)
        {
            startButton.Select();
        }
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
        Canvas canvas = CreateCanvas("StartCanvas");
        Image background = CreateImage(canvas.transform, "Background", new Color(0.05f, 0.06f, 0.09f, 1f));
        Stretch(background.rectTransform);

        RectTransform card = CreateCard(background.transform);

        startButton = CreateStartButton(card);

        CreateText(
            card,
            "Hint",
            "Press Enter or click Start",
            30,
            FontStyle.Bold,
            TextAnchor.LowerCenter,
            new Color(0.62f, 0.94f, 0.76f, 1f),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(680f, 34f),
            new Vector2(0f, 18f));
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
        Image image = CreateImage(parent, "Card", new Color(0.10f, 0.12f, 0.17f, 0));
        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(820f, 460f);
        rectTransform.anchoredPosition = Vector2.zero;
        return rectTransform;
    }

    private Button CreateStartButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("StartButton");
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = RuntimeSpriteFactory.GetWhiteSprite();
        image.color = new Color(0.18f, 0.68f, 0.42f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.18f, 0.68f, 0.42f, 1f);
        colors.highlightedColor = new Color(0.34f, 0.89f, 0.60f, 1f);
        colors.pressedColor = new Color(0.14f, 0.54f, 0.34f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.18f, 0.68f, 0.42f, 0.35f);
        button.colors = colors;
        button.onClick.AddListener(LoadMainScene);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.sizeDelta = new Vector2(520f, 120f);
        rectTransform.anchoredPosition = new Vector2(0f, 72f);

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);

        Text label = labelObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 52;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = "START";
        label.raycastTarget = false;

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
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
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

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
