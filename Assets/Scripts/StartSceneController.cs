using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StartSceneController : MonoBehaviour
{
    private const string MainSceneName = "Main";
    private static readonly Color BackgroundColor = new Color32(0x9F, 0xF2, 0xEE, 0xFF);
    private static readonly Color StartButtonColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
    private static readonly Color StartButtonHoverColor = new Color32(0xE0, 0xE0, 0xE0, 0xFF);
    private static readonly Color StartButtonPressedColor = new Color32(0xC0, 0xC0, 0xC0, 0xFF);
    private static readonly Color StartButtonDisabledColor = new Color32(0xD8, 0xD8, 0xD8, 0xFF);
    private static readonly Vector2 TitleSize = new Vector2(480f, 300f);
    private static readonly Vector2 TitlePosition = new Vector2(0f, 330f);
    private const float TitleFadeDuration = 1.0f;

    [SerializeField] private Sprite titleSprite;
    [SerializeField] private Sprite startButtonSprite;

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
        Image background = CreateImage(canvas.transform, "Background", BackgroundColor);
        Stretch(background.rectTransform);

        RectTransform card = CreateCard(background.transform);
        RectTransform title = CreateTitleImage(background.transform);
        startButton = CreateStartButton(card);

        if (title != null)
        {
            StartCoroutine(AnimateTitleFadeIn(title));
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
        Image image = CreateImage(parent, "Card", new Color(0.10f, 0.12f, 0.17f, 0));
        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(820f, 460f);
        rectTransform.anchoredPosition = Vector2.zero;
        return rectTransform;
    }

    private RectTransform CreateTitleImage(Transform parent)
    {
        if (titleSprite == null)
        {
            Debug.LogWarning("StartSceneController: Title sprite is not assigned.");
            return null;
        }

        GameObject titleObject = new GameObject("Title");
        titleObject.transform.SetParent(parent, false);

        Image image = titleObject.AddComponent<Image>();
        image.sprite = titleSprite;
        image.color = new Color(1f, 1f, 1f, 0f);
        image.preserveAspect = true;
        image.raycastTarget = false;

        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = TitleSize;
        rectTransform.anchoredPosition = TitlePosition;
        return rectTransform;
    }

    private Button CreateStartButton(Transform parent)
    {
        if (startButtonSprite == null)
        {
            Debug.LogWarning("StartSceneController: Start button sprite is not assigned.");
            return null;
        }

        GameObject buttonObject = new GameObject("StartButton");
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = startButtonSprite;
        image.color = StartButtonColor;
        image.preserveAspect = true;

        Button button = buttonObject.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = StartButtonColor;
        colors.highlightedColor = StartButtonHoverColor;
        colors.pressedColor = StartButtonPressedColor;
        colors.selectedColor = StartButtonHoverColor;
        colors.disabledColor = StartButtonDisabledColor;
        button.colors = colors;
        button.onClick.AddListener(LoadMainScene);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.sizeDelta = new Vector2(200f, 200f);
        rectTransform.anchoredPosition = new Vector2(0f, 60f);

        return button;
    }

    private IEnumerator AnimateTitleFadeIn(RectTransform title)
    {
        if (title == null)
        {
            yield break;
        }

        Image titleImage = title.GetComponent<Image>();
        if (titleImage == null)
        {
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < TitleFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / TitleFadeDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            Color color = titleImage.color;
            color.a = easedT;
            titleImage.color = color;
            yield return null;
        }

        Color finalColor = titleImage.color;
        finalColor.a = 1f;
        titleImage.color = finalColor;
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
