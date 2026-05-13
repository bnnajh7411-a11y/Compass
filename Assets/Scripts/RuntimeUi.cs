using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class RuntimeUiTheme
{
    public static readonly Color BackgroundColor = new Color32(0xBC, 0xF2, 0xEE, 0xFF);
    public static readonly Color TextColor = new Color32(0xFF, 0x7C, 0xB2, 0xFF);
    public static readonly Color ButtonNormalColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
    public static readonly Color ButtonHoverColor = new Color32(0xE0, 0xE0, 0xE0, 0xFF);
    public static readonly Color ButtonPressedColor = new Color32(0xC0, 0xC0, 0xC0, 0xFF);
    public static readonly Color ButtonDisabledColor = new Color32(0xD8, 0xD8, 0xD8, 0xFF);
}

public static class RuntimeUiFactory
{
    private const string AudioIconResourcesPath = "AudioIcon";
    private const string CheckIconResourcesPath = "CheckIcon";
    private const float AudioButtonSize = 56f * 1.15f;
    private const float AudioButtonMargin = 24f;
    private const float CheckButtonSize = 112f * 1.15f;
    private const float CheckButtonMargin = 24f;
    private const float CardCornerRadius = 36f;

    private static Font defaultFont;
    private static Sprite audioIconSprite;
    private static Sprite checkIconSprite;

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
        Button button = CreateIconButton(
            parent,
            "AudioButton",
            GetAudioIconSprite(),
            GameManager.IsMuted ? new Color(1f, 1f, 1f, 0.45f) : Color.white,
            new Vector2(AudioButtonSize, AudioButtonSize),
            new Vector2(AudioButtonMargin * 1.8f, -AudioButtonMargin * 1.5f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            false,
            soundEffect: RuntimeButtonSoundEffect.None);

        button.gameObject.AddComponent<RuntimeAudioToggleButton>();
        return button;
    }

    public static Button CreateCheckIconButton(Transform parent, UnityAction onClick)
    {
        Button button = CreateIconButton(
            parent,
            "CheckButton",
            GetCheckIconSprite(),
            RuntimeUiTheme.ButtonNormalColor,
            new Vector2(CheckButtonSize, CheckButtonSize),
            new Vector2(-CheckButtonMargin * 1.8f, CheckButtonMargin * 1.5f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            true,
            onClick,
            RuntimeButtonSoundEffect.Result);
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

    public static void ApplyThemeButton(
        Button button,
        Graphic targetGraphic,
        RuntimeButtonSoundEffect soundEffect = RuntimeButtonSoundEffect.ButtonTab)
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
        ConfigureButtonSound(button, soundEffect);
    }

    public static RuntimeButtonSound ConfigureButtonSound(
        Button button,
        RuntimeButtonSoundEffect soundEffect = RuntimeButtonSoundEffect.ButtonTab)
    {
        if (button == null)
        {
            return null;
        }

        RuntimeButtonSound buttonSound = button.GetComponent<RuntimeButtonSound>();
        if (buttonSound == null)
        {
            buttonSound = button.gameObject.AddComponent<RuntimeButtonSound>();
        }

        buttonSound.Configure(soundEffect);
        return buttonSound;
    }

    public static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static Button CreateIconButton(
        Transform parent,
        string objectName,
        Sprite iconSprite,
        Color color,
        Vector2 sizeDelta,
        Vector2 anchoredPosition,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        bool useThemeButton,
        UnityAction onClick = null,
        RuntimeButtonSoundEffect soundEffect = RuntimeButtonSoundEffect.ButtonTab)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = iconSprite ?? RuntimeSpriteFactory.GetWhiteSprite();
        image.color = color;
        image.preserveAspect = true;

        Button button = buttonObject.AddComponent<Button>();
        if (useThemeButton)
        {
            ApplyThemeButton(button, image, soundEffect);
        }
        else
        {
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;
            ConfigureButtonSound(button, soundEffect);
        }

        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.anchoredPosition = anchoredPosition;

        buttonObject.transform.SetAsLastSibling();
        return button;
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

        audioIconSprite = RuntimeSpriteFactory.GetWhiteSprite();
        return audioIconSprite;
    }

    private static Sprite GetCheckIconSprite()
    {
        if (checkIconSprite != null)
        {
            return checkIconSprite;
        }

        Sprite[] sprites = Resources.LoadAll<Sprite>(CheckIconResourcesPath);
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite != null && sprite.name.IndexOf("CheckIcon", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                checkIconSprite = sprite;
                return checkIconSprite;
            }
        }

        if (sprites.Length > 0)
        {
            checkIconSprite = sprites[0];
            return checkIconSprite;
        }

        checkIconSprite = RuntimeSpriteFactory.GetWhiteSprite();
        return checkIconSprite;
    }
}

[DisallowMultipleComponent]
public sealed class RuntimeButtonSound : MonoBehaviour
{
    private Button button;
    private RuntimeButtonSoundEffect soundEffect = RuntimeButtonSoundEffect.ButtonTab;

    public void Configure(RuntimeButtonSoundEffect configuredSoundEffect)
    {
        soundEffect = configuredSoundEffect;
    }

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.AddListener(PlaySound);
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlaySound);
        }
    }

    private void PlaySound()
    {
        if (soundEffect == RuntimeButtonSoundEffect.None)
        {
            return;
        }

        GameManager.PlayUiSound(soundEffect);
    }
}

[DisallowMultipleComponent]
public sealed class RuntimeAudioToggleButton : MonoBehaviour
{
    private static readonly Color EnabledColor = Color.white;
    private static readonly Color MutedColor = new Color(1f, 1f, 1f, 0.45f);

    private Image iconImage;
    private Button button;

    private void Awake()
    {
        iconImage = GetComponent<Image>();
        button = GetComponent<Button>();

        if (button != null)
        {
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }
    }

    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }

        GameManager.MutedChanged += HandleMutedChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }

        GameManager.MutedChanged -= HandleMutedChanged;
    }

    private void HandleClick()
    {
        GameManager.Toggle();
    }

    private void HandleMutedChanged(bool _)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (iconImage == null)
        {
            return;
        }

        iconImage.color = GameManager.IsMuted ? MutedColor : EnabledColor;
    }
}
