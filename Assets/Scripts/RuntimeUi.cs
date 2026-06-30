using System.Collections.Generic;
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

[DisallowMultipleComponent]
public sealed class RuntimePauseMenu : MonoBehaviour
{
    private static readonly Color OverlayColor = new Color(1f, 1f, 1f, 0.72f);
    private static readonly Color CardColor = new Color(1f, 1f, 1f, 0.92f);
    private static readonly Color SliderBackgroundColor = new Color32(0xDD, 0xDD, 0xDD, 0xFF);
    private static readonly Color SliderFillColor = new Color32(0xFF, 0x7C, 0xB2, 0xFF);

    private bool includeLevelSelectButton;
    private bool isOpen;
    private GameObject overlayObject;
    private GameObject pausePanelObject;
    private GameObject settingsPanelObject;
    private Slider backgroundMusicSlider;
    private Slider effectsSlider;
    private Toggle fullScreenToggle;
    private Button resolutionButton;
    private Text backgroundMusicValueText;
    private Text effectsValueText;
    private Text resolutionValueText;
    private GameObject resolutionDropdownObject;
    private readonly List<Button> resolutionOptionButtons = new List<Button>();

    public static RuntimePauseMenu Create(Transform parent, bool includeLevelSelectButton)
    {
        if (parent == null)
        {
            return null;
        }

        GameObject menuObject = new GameObject("PauseMenu");
        menuObject.transform.SetParent(parent, false);

        RuntimePauseMenu pauseMenu = menuObject.AddComponent<RuntimePauseMenu>();
        pauseMenu.Configure(includeLevelSelectButton);
        return pauseMenu;
    }

    public void Configure(bool shouldIncludeLevelSelectButton)
    {
        includeLevelSelectButton = shouldIncludeLevelSelectButton;
        BuildUi();
        RefreshSettingsUi();
        CloseInstantly();
    }

    private void OnEnable()
    {
        GameManager.BackgroundMusicVolumeChanged += HandleBackgroundMusicVolumeChanged;
        GameManager.EffectsVolumeChanged += HandleEffectsVolumeChanged;
        GameManager.FullScreenChanged += HandleFullScreenChanged;
        GameManager.ResolutionChanged += HandleResolutionChanged;
        RefreshSettingsUi();
    }

    private void OnDisable()
    {
        GameManager.BackgroundMusicVolumeChanged -= HandleBackgroundMusicVolumeChanged;
        GameManager.EffectsVolumeChanged -= HandleEffectsVolumeChanged;
        GameManager.FullScreenChanged -= HandleFullScreenChanged;
        GameManager.ResolutionChanged -= HandleResolutionChanged;
        CloseInstantly();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (isOpen)
        {
            CloseMenu();
            return;
        }

        OpenMenu();
    }

    private void BuildUi()
    {
        overlayObject = new GameObject("Overlay");
        overlayObject.transform.SetParent(transform, false);
        overlayObject.transform.SetAsLastSibling();

        Image overlayImage = overlayObject.AddComponent<Image>();
        overlayImage.sprite = RuntimeSpriteFactory.GetWhiteSprite();
        overlayImage.color = OverlayColor;
        overlayImage.raycastTarget = true;
        RuntimeUiFactory.Stretch(overlayImage.rectTransform);

        pausePanelObject = CreatePausePanel(overlayObject.transform);
        settingsPanelObject = CreateSettingsPanel(overlayObject.transform);
    }

    private GameObject CreatePausePanel(Transform parent)
    {
        Image card = RuntimeUiFactory.CreateCardImage(parent, "PausePanel", CardColor, new Vector2(560f, 460f));
        card.raycastTarget = true;

        RectTransform cardRect = card.rectTransform;
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;

        RuntimeUiFactory.CreateText(
            card.transform,
            "Title",
            "PAUSE",
            36,
            FontStyle.Bold,
            TextAnchor.UpperCenter,
            RuntimeUiTheme.TextColor,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(420f, 48f),
            new Vector2(0f, -36f));

        RuntimeUiFactory.CreateText(
            card.transform,
            "Hint",
            "\u0045\u0053\u0043\ub85c \ub3cc\uc544\uac00\uae30",
            18,
            FontStyle.Bold,
            TextAnchor.UpperCenter,
            RuntimeUiTheme.TextColor,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(420f, 28f),
            new Vector2(0f, -82f));

        RectTransform buttonRoot = CreateVerticalButtonRoot(card.transform, new Vector2(0f, -20f));
        CreateMenuButton(buttonRoot, "\uc124\uc815", ShowSettingsPanel);

        if (includeLevelSelectButton)
        {
            CreateMenuButton(buttonRoot, "\ub808\ubca8 \uc120\ud0dd \uc52c\uc73c\ub85c", () =>
            {
                CloseMenu();
                GameManager.GoToMenuScene();
            });
        }

        CreateMenuButton(buttonRoot, "\uac8c\uc784 \uc885\ub8cc", () =>
        {
            CloseMenu();
            GameManager.QuitApplication();
        });

        return card.gameObject;
    }

    private GameObject CreateSettingsPanel(Transform parent)
    {
        Image card = RuntimeUiFactory.CreateCardImage(parent, "SettingsPanel", CardColor, new Vector2(780f, 640f));
        card.raycastTarget = true;

        RectTransform cardRect = card.rectTransform;
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;

        RuntimeUiFactory.CreateText(
            card.transform,
            "Title",
            "\uc124\uc815",
            34,
            FontStyle.Bold,
            TextAnchor.UpperCenter,
            RuntimeUiTheme.TextColor,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(520f, 44f),
            new Vector2(0f, -28f));

        RectTransform contentRoot = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        contentRoot.SetParent(card.transform, false);
        contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
        contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
        contentRoot.pivot = new Vector2(0.5f, 0.5f);
        contentRoot.sizeDelta = new Vector2(660f, 450f);
        contentRoot.anchoredPosition = new Vector2(0f, -4f);

        CreateSliderRow(contentRoot, "\ubc30\uacbd\uc74c", out backgroundMusicSlider, out backgroundMusicValueText, new Vector2(0f, 165f));
        CreateSliderRow(contentRoot, "\ud6a8\uacfc\uc74c", out effectsSlider, out effectsValueText, new Vector2(0f, 55f));
        CreateToggleRow(contentRoot, "\uc804\uccb4\ud654\uba74", out fullScreenToggle, new Vector2(0f, -45f));
        CreateResolutionRow(contentRoot, new Vector2(0f, -150f));

        CreateMenuButton(card.transform, "\ub4a4\ub85c", ShowPausePanel, new Vector2(220f, 62f), new Vector2(0f, -265f));

        backgroundMusicSlider.onValueChanged.AddListener(value => GameManager.SetBackgroundMusicVolumePercent(Mathf.RoundToInt(value)));
        effectsSlider.onValueChanged.AddListener(value => GameManager.SetEffectsVolumePercent(Mathf.RoundToInt(value)));
        fullScreenToggle.onValueChanged.AddListener(GameManager.SetFullScreenEnabled);

        return card.gameObject;
    }

    private RectTransform CreateVerticalButtonRoot(Transform parent, Vector2 anchoredPosition)
    {
        RectTransform root = new GameObject("ButtonRoot", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(360f, 260f);
        root.anchoredPosition = anchoredPosition;

        VerticalLayoutGroup layoutGroup = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 22f;
        layoutGroup.padding = new RectOffset(0, 0, 0, 0);
        return root;
    }

    private void CreateSliderRow(Transform parent, string label, out Slider slider, out Text valueText, Vector2 anchoredPosition)
    {
        const float sliderTrackWidth = 460f;
        const float sliderTrackHeight = 16f;

        RectTransform row = CreateSettingsRow(parent, label, anchoredPosition);

        valueText = RuntimeUiFactory.CreateText(
            row,
            "Value",
            "100%",
            20,
            FontStyle.Bold,
            TextAnchor.UpperRight,
            RuntimeUiTheme.TextColor,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(110f, 30f),
            new Vector2(-8f, -6f));

        GameObject sliderObject = new GameObject("Slider", typeof(RectTransform));
        sliderObject.transform.SetParent(row, false);

        slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.wholeNumbers = true;
        slider.direction = Slider.Direction.LeftToRight;

        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0f);
        sliderRect.anchorMax = new Vector2(0.5f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0f);
        sliderRect.sizeDelta = new Vector2(sliderTrackWidth, 34f);
        sliderRect.anchoredPosition = new Vector2(0f, 10f);

        Image background = RuntimeUiFactory.CreateImage(
            sliderObject.transform,
            "Background",
            SliderBackgroundColor,
            RuntimeSpriteFactory.GetRoundedRectSprite(
                Mathf.RoundToInt(sliderTrackWidth),
                Mathf.RoundToInt(sliderTrackHeight),
                sliderTrackHeight * 0.5f));
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.sizeDelta = new Vector2(sliderTrackWidth, sliderTrackHeight);
        backgroundRect.anchoredPosition = Vector2.zero;

        GameObject fillAreaObject = new GameObject("FillArea", typeof(RectTransform), typeof(RectMask2D));
        fillAreaObject.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0.5f, 0.5f);
        fillAreaRect.anchorMax = new Vector2(0.5f, 0.5f);
        fillAreaRect.pivot = new Vector2(0.5f, 0.5f);
        fillAreaRect.sizeDelta = new Vector2(sliderTrackWidth, sliderTrackHeight);
        fillAreaRect.anchoredPosition = Vector2.zero;

        Image fill = RuntimeUiFactory.CreateImage(
            fillAreaObject.transform,
            "Fill",
            SliderFillColor,
            RuntimeSpriteFactory.GetRoundedRectSprite(
                Mathf.RoundToInt(sliderTrackWidth),
                Mathf.RoundToInt(sliderTrackHeight),
                sliderTrackHeight * 0.5f));
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.pivot = new Vector2(0.5f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        GameObject handleAreaObject = new GameObject("HandleSlideArea", typeof(RectTransform));
        handleAreaObject.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleAreaObject.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = new Vector2(0f, 0f);
        handleAreaRect.anchorMax = new Vector2(1f, 1f);
        handleAreaRect.offsetMin = Vector2.zero;
        handleAreaRect.offsetMax = Vector2.zero;

        Image handle = RuntimeUiFactory.CreateImage(
            handleAreaObject.transform,
            "Handle",
            SliderFillColor,
            RuntimeSpriteFactory.GetCircleSprite());
        RectTransform handleRect = handle.rectTransform;
        handleRect.anchorMin = new Vector2(0f, 0.5f);
        handleRect.anchorMax = new Vector2(0f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(28f, 28f);
        handleRect.anchoredPosition = Vector2.zero;

        slider.targetGraphic = handle;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
    }

    private void CreateToggleRow(Transform parent, string label, out Toggle toggle, Vector2 anchoredPosition)
    {
        RectTransform row = CreateSettingsRow(parent, label, anchoredPosition);
        Transform labelTransform = row.Find("Label");
        if (labelTransform != null)
        {
            RectTransform labelRect = labelTransform.GetComponent<RectTransform>();
            if (labelRect != null)
            {
                labelRect.anchoredPosition = new Vector2(84f, -6f);
                labelRect.sizeDelta = new Vector2(220f, 32f);
            }
        }

        GameObject toggleObject = new GameObject("Toggle", typeof(RectTransform));
        toggleObject.transform.SetParent(row, false);
        toggle = toggleObject.AddComponent<Toggle>();

        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0f, 0.5f);
        toggleRect.anchorMax = new Vector2(0f, 0.5f);
        toggleRect.pivot = new Vector2(0f, 0.5f);
        toggleRect.sizeDelta = new Vector2(52f, 52f);
        toggleRect.anchoredPosition = new Vector2(8f, -8f);

        Image background = RuntimeUiFactory.CreateImage(
            toggleObject.transform,
            "Background",
            RuntimeUiTheme.ButtonNormalColor,
            RuntimeSpriteFactory.GetRoundedRectSprite(52, 52, 14f));
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.sizeDelta = new Vector2(52f, 52f);
        backgroundRect.anchoredPosition = Vector2.zero;

        Image checkmark = RuntimeUiFactory.CreateImage(toggleObject.transform, "Checkmark", SliderFillColor, RuntimeSpriteFactory.GetWhiteSprite());
        RectTransform checkmarkRect = checkmark.rectTransform;
        checkmarkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkmarkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkmarkRect.pivot = new Vector2(0.5f, 0.5f);
        checkmarkRect.sizeDelta = new Vector2(26f, 26f);
        checkmarkRect.anchoredPosition = Vector2.zero;

        toggle.targetGraphic = background;
        toggle.graphic = checkmark;

        ColorBlock colors = toggle.colors;
        colors.normalColor = RuntimeUiTheme.ButtonNormalColor;
        colors.highlightedColor = RuntimeUiTheme.ButtonHoverColor;
        colors.pressedColor = RuntimeUiTheme.ButtonPressedColor;
        colors.selectedColor = RuntimeUiTheme.ButtonHoverColor;
        colors.disabledColor = RuntimeUiTheme.ButtonDisabledColor;
        toggle.colors = colors;
    }

    private void CreateResolutionRow(Transform parent, Vector2 anchoredPosition)
    {
        RectTransform row = CreateSettingsRow(parent, "\ud574\uc0c1\ub3c4", anchoredPosition);

        resolutionButton = CreateMenuButton(
            row,
            GameManager.GetCurrentResolutionLabel(),
            ToggleResolutionDropdown,
            new Vector2(260f, 58f),
            new Vector2(160f, -6f));

        resolutionValueText = resolutionButton.GetComponentInChildren<Text>();

        resolutionDropdownObject = new GameObject("ResolutionDropdown");
        resolutionDropdownObject.transform.SetParent(row, false);

        BuildResolutionDropdown();
        SetResolutionDropdownOpen(false);
    }

    private void BuildResolutionDropdown()
    {
        if (resolutionDropdownObject == null)
        {
            return;
        }

        resolutionOptionButtons.Clear();
        string[] resolutionLabels = GameManager.GetResolutionLabels();
        int optionCount = Mathf.Max(1, resolutionLabels.Length);
        float dropdownHeight = 18f + (optionCount * 48f);

        Image dropdownBackground = resolutionDropdownObject.AddComponent<Image>();
        dropdownBackground.sprite = RuntimeSpriteFactory.GetRoundedRectSprite(280, Mathf.RoundToInt(dropdownHeight), 22f);
        dropdownBackground.color = new Color(1f, 1f, 1f, 0.96f);
        dropdownBackground.preserveAspect = false;

        RectTransform dropdownRect = resolutionDropdownObject.GetComponent<RectTransform>();
        dropdownRect.anchorMin = new Vector2(0.5f, 0.5f);
        dropdownRect.anchorMax = new Vector2(0.5f, 0.5f);
        dropdownRect.pivot = new Vector2(0.5f, 0f);
        dropdownRect.sizeDelta = new Vector2(280f, dropdownHeight);
        dropdownRect.anchoredPosition = new Vector2(160f, 34f);

        RectTransform contentRoot = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        contentRoot.SetParent(resolutionDropdownObject.transform, false);
        RuntimeUiFactory.Stretch(contentRoot);
        contentRoot.offsetMin = new Vector2(10f, 10f);
        contentRoot.offsetMax = new Vector2(-10f, -10f);

        VerticalLayoutGroup layoutGroup = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.LowerCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 6f;
        layoutGroup.padding = new RectOffset(0, 0, 0, 0);

        if (resolutionLabels.Length == 0)
        {
            CreateResolutionOptionButton(contentRoot, GameManager.GetCurrentResolutionLabel(), -1);
            return;
        }

        for (int i = 0; i < resolutionLabels.Length; i++)
        {
            CreateResolutionOptionButton(contentRoot, resolutionLabels[i], i);
        }
    }

    private void CreateResolutionOptionButton(Transform parent, string label, int optionIndex)
    {
        GameObject buttonObject = new GameObject($"ResolutionOption{Mathf.Max(optionIndex, 0)}");
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = RuntimeSpriteFactory.GetRoundedRectSprite(260, 42, 18f);
        image.color = RuntimeUiTheme.ButtonNormalColor;
        image.preserveAspect = false;

        Button button = buttonObject.AddComponent<Button>();
        RuntimeUiFactory.ApplyThemeButton(button, image);
        button.onClick.AddListener(() =>
        {
            if (optionIndex >= 0)
            {
                GameManager.SetResolutionByIndex(optionIndex);
            }

            SetResolutionDropdownOpen(false);
            RefreshSettingsUi();
        });

        LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 42f;
        layoutElement.minHeight = 42f;
        layoutElement.flexibleWidth = 1f;

        RuntimeUiFactory.CreateText(
            buttonObject.transform,
            "Label",
            label,
            20,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            RuntimeUiTheme.TextColor,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero);

        resolutionOptionButtons.Add(button);
    }

    private void SetResolutionDropdownOpen(bool isOpen)
    {
        if (resolutionDropdownObject == null)
        {
            return;
        }

        resolutionDropdownObject.SetActive(isOpen);
        RefreshResolutionOptionButtons();
    }

    private void ToggleResolutionDropdown()
    {
        if (resolutionDropdownObject == null)
        {
            return;
        }

        SetResolutionDropdownOpen(!resolutionDropdownObject.activeSelf);
    }

    private void RefreshResolutionOptionButtons()
    {
        int currentResolutionIndex = GameManager.GetCurrentResolutionIndex();

        for (int i = 0; i < resolutionOptionButtons.Count; i++)
        {
            Button button = resolutionOptionButtons[i];
            if (button == null)
            {
                continue;
            }

            bool isSelected = i == currentResolutionIndex;
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = isSelected ? SliderFillColor : RuntimeUiTheme.ButtonNormalColor;
            }

            Text labelText = button.GetComponentInChildren<Text>();
            if (labelText != null)
            {
                labelText.color = isSelected ? Color.white : RuntimeUiTheme.TextColor;
            }
        }
    }

    private RectTransform CreateSettingsRow(Transform parent, string label, Vector2 anchoredPosition)
    {
        RectTransform row = new GameObject($"{label}Row", typeof(RectTransform)).GetComponent<RectTransform>();
        row.SetParent(parent, false);
        row.anchorMin = new Vector2(0.5f, 0.5f);
        row.anchorMax = new Vector2(0.5f, 0.5f);
        row.pivot = new Vector2(0.5f, 0.5f);
        row.sizeDelta = new Vector2(640f, 88f);
        row.anchoredPosition = anchoredPosition;

        RuntimeUiFactory.CreateText(
            row,
            "Label",
            label,
            24,
            FontStyle.Bold,
            TextAnchor.UpperLeft,
            RuntimeUiTheme.TextColor,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(220f, 32f),
            new Vector2(8f, -6f));

        return row;
    }

    private Button CreateMenuButton(Transform parent, string label, UnityAction onClick)
    {
        return CreateMenuButton(parent, label, onClick, new Vector2(320f, 68f), Vector2.zero);
    }

    private Button CreateMenuButton(
        Transform parent,
        string label,
        UnityAction onClick,
        Vector2 sizeDelta,
        Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject($"{label}Button");
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = RuntimeSpriteFactory.GetRoundedRectSprite(
            Mathf.RoundToInt(sizeDelta.x),
            Mathf.RoundToInt(sizeDelta.y),
            24f);
        image.color = RuntimeUiTheme.ButtonNormalColor;
        image.preserveAspect = false;

        Button button = buttonObject.AddComponent<Button>();
        RuntimeUiFactory.ApplyThemeButton(button, image);
        button.onClick.AddListener(onClick);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.anchoredPosition = anchoredPosition;

        RuntimeUiFactory.CreateText(
            buttonObject.transform,
            "Label",
            label,
            24,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            RuntimeUiTheme.TextColor,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero);

        return button;
    }

    private void OpenMenu()
    {
        isOpen = true;
        overlayObject.SetActive(true);
        overlayObject.transform.SetAsLastSibling();
        SetResolutionDropdownOpen(false);
        ShowPausePanel();
        GameManager.SetPaused(true);
        RefreshSettingsUi();
    }

    private void CloseMenu()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        GameManager.SetPaused(false);
        SetResolutionDropdownOpen(false);
        overlayObject.SetActive(false);
    }

    private void CloseInstantly()
    {
        isOpen = false;
        if (overlayObject != null)
        {
            overlayObject.SetActive(false);
        }

        if (GameManager.HasInstance && GameManager.IsPaused)
        {
            GameManager.SetPaused(false);
        }
    }

    private void ShowPausePanel()
    {
        if (pausePanelObject != null)
        {
            pausePanelObject.SetActive(true);
        }

        SetResolutionDropdownOpen(false);

        if (settingsPanelObject != null)
        {
            settingsPanelObject.SetActive(false);
        }
    }

    private void ShowSettingsPanel()
    {
        if (pausePanelObject != null)
        {
            pausePanelObject.SetActive(false);
        }

        if (settingsPanelObject != null)
        {
            settingsPanelObject.SetActive(true);
        }

        SetResolutionDropdownOpen(false);
        RefreshSettingsUi();
    }

    private void RefreshSettingsUi()
    {
        if (backgroundMusicSlider != null)
        {
            backgroundMusicSlider.SetValueWithoutNotify(GameManager.BackgroundMusicVolumePercent);
        }

        if (backgroundMusicValueText != null)
        {
            backgroundMusicValueText.text = $"{GameManager.BackgroundMusicVolumePercent}%";
        }

        if (effectsSlider != null)
        {
            effectsSlider.SetValueWithoutNotify(GameManager.EffectsVolumePercent);
        }

        if (effectsValueText != null)
        {
            effectsValueText.text = $"{GameManager.EffectsVolumePercent}%";
        }

        if (fullScreenToggle != null)
        {
            fullScreenToggle.SetIsOnWithoutNotify(GameManager.IsFullScreenEnabled);
        }

        if (resolutionValueText != null)
        {
            resolutionValueText.text = GameManager.GetCurrentResolutionLabel();
        }

        RefreshResolutionOptionButtons();
    }

    private void HandleBackgroundMusicVolumeChanged(int volumePercent)
    {
        if (backgroundMusicValueText != null)
        {
            backgroundMusicValueText.text = $"{volumePercent}%";
        }

        if (backgroundMusicSlider != null)
        {
            backgroundMusicSlider.SetValueWithoutNotify(volumePercent);
        }
    }

    private void HandleEffectsVolumeChanged(int volumePercent)
    {
        if (effectsValueText != null)
        {
            effectsValueText.text = $"{volumePercent}%";
        }

        if (effectsSlider != null)
        {
            effectsSlider.SetValueWithoutNotify(volumePercent);
        }
    }

    private void HandleFullScreenChanged(bool isEnabled)
    {
        if (fullScreenToggle != null)
        {
            fullScreenToggle.SetIsOnWithoutNotify(isEnabled);
        }
    }

    private void HandleResolutionChanged()
    {
        if (resolutionValueText != null)
        {
            resolutionValueText.text = GameManager.GetCurrentResolutionLabel();
        }

        RefreshResolutionOptionButtons();
    }
}
