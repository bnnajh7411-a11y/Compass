using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class RuntimeUiFactory
{
    private const string AudioIconResourcesPath = "AudioIcon";
    private const float AudioButtonSize = 56f;
    private const float AudioButtonMargin = 24f;
    private const float CardCornerRadius = 36f;

    private static Font defaultFont;
    private static Sprite audioIconSprite;

    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    public static void EnableEventSystemNavigation()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            return;
        }

        eventSystem.sendNavigationEvents = true;
    }

    public static void DisableEventSystemNavigation()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            return;
        }

        eventSystem.sendNavigationEvents = false;
        eventSystem.SetSelectedGameObject(null);
    }

    public static Canvas CreateCanvas(Transform parent, string canvasName)
    {
        GameObject canvasObject = new GameObject(canvasName);
        canvasObject.transform.SetParent(parent, false);

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

    public static Image CreateImage(Transform parent, string objectName, Color color, Sprite sprite = null)
    {
        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite ?? RuntimeSpriteFactory.GetWhiteSprite();
        image.color = color;
        return image;
    }

    public static Image CreateCardImage(Transform parent, string objectName, Color color, Vector2 sizeDelta)
    {
        Image image = CreateImage(
            parent,
            objectName,
            color,
            RuntimeSpriteFactory.GetRoundedRectSprite(
                Mathf.RoundToInt(sizeDelta.x),
                Mathf.RoundToInt(sizeDelta.y),
                CardCornerRadius));

        image.raycastTarget = false;
        image.preserveAspect = false;

        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.anchoredPosition = Vector2.zero;
        return image;
    }

    public static Button CreateAudioToggleButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("AudioButton");
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = GetAudioIconSprite();
        image.color = GameManager.IsMuted ? new Color(1f, 1f, 1f, 0.45f) : Color.white;
        image.preserveAspect = true;

        Button button = buttonObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.sizeDelta = new Vector2(AudioButtonSize, AudioButtonSize);
        rectTransform.anchoredPosition = new Vector2(AudioButtonMargin, -AudioButtonMargin);

        buttonObject.AddComponent<RuntimeAudioToggleButton>();
        buttonObject.transform.SetAsLastSibling();
        return button;
    }

    public static Text CreateText(
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
        text.font = GetDefaultFont();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.text = textValue;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rectTransform = text.rectTransform;
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.anchoredPosition = anchoredPosition;

        return text;
    }

    public static void ApplyThemeButton(Button button, Graphic targetGraphic)
    {
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = targetGraphic;

        ColorBlock colors = button.colors;
        colors.normalColor = RuntimeUiTheme.ButtonNormalColor;
        colors.highlightedColor = RuntimeUiTheme.ButtonHoverColor;
        colors.pressedColor = RuntimeUiTheme.ButtonPressedColor;
        colors.selectedColor = RuntimeUiTheme.ButtonHoverColor;
        colors.disabledColor = RuntimeUiTheme.ButtonDisabledColor;
        button.colors = colors;
    }

    public static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static Font GetDefaultFont()
    {
        if (defaultFont == null)
        {
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        return defaultFont;
    }

    private static Sprite GetAudioIconSprite()
    {
        if (audioIconSprite != null)
        {
            return audioIconSprite;
        }

        Sprite[] sprites = Resources.LoadAll<Sprite>(AudioIconResourcesPath);
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite != null && string.Equals(sprite.name, "AudioIcon", System.StringComparison.OrdinalIgnoreCase))
            {
                audioIconSprite = sprite;
                return audioIconSprite;
            }
        }

        if (sprites.Length > 0)
        {
            audioIconSprite = sprites[0];
            return audioIconSprite;
        }

        Debug.LogWarning("RuntimeUiFactory: Audio icon sprite was not found in Resources/AudioIcon.");
        audioIconSprite = RuntimeSpriteFactory.GetWhiteSprite();
        return audioIconSprite;
    }
}
