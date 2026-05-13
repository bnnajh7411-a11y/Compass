using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class StartSceneController : MonoBehaviour
{
    private static readonly Vector2 TitleSize = new Vector2(480f, 300f);
    private static readonly Vector2 TitlePosition = new Vector2(0f, 330f);
    private const float TitleFadeDuration = 3.0f;
    private const float TitleStartScale = 0.75f;

    [SerializeField] private Sprite titleSprite;
    [SerializeField] private Sprite startButtonSprite;

    private bool isTransitioning;

    private void Start()
    {
        RuntimeUiFactory.EnsureEventSystem();
        RuntimeUiFactory.DisableEventSystemNavigation();
        BuildUi();
    }

    private void BuildUi()
    {
        Canvas canvas = RuntimeUiFactory.CreateCanvas(transform, "StartCanvas");
        Image background = RuntimeUiFactory.CreateImage(canvas.transform, "Background", RuntimeUiTheme.BackgroundColor);
        RuntimeUiFactory.Stretch(background.rectTransform);

        RectTransform card = CreateCard(background.transform);
        RectTransform title = CreateTitleImage(background.transform);
        CreateStartButton(card);
        RuntimeUiFactory.CreateAudioToggleButton(canvas.transform);

        if (title != null)
        {
            StartCoroutine(AnimateTitleFadeIn(title));
        }
    }

    private void LoadMenuScene()
    {
        if (isTransitioning)
        {
            return;
        }

        isTransitioning = true;
        GameManager.GoToMenuScene(true);
    }

    private RectTransform CreateCard(Transform parent)
    {
        Image image = RuntimeUiFactory.CreateCardImage(
            parent,
            "Card",
            new Color(0.10f, 0.12f, 0.17f, 0f),
            new Vector2(820f, 460f));
        return image.rectTransform;
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
        rectTransform.localScale = Vector3.one * TitleStartScale;
        return rectTransform;
    }

    private void CreateStartButton(Transform parent)
    {
        if (startButtonSprite == null)
        {
            Debug.LogWarning("StartSceneController: Start button sprite is not assigned.");
            return;
        }

        GameObject buttonObject = new GameObject("StartButton");
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = startButtonSprite;
        image.color = RuntimeUiTheme.ButtonNormalColor;
        image.preserveAspect = true;

        Button button = buttonObject.AddComponent<Button>();
        RuntimeUiFactory.ApplyThemeButton(button, image);
        button.onClick.AddListener(LoadMenuScene);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.sizeDelta = new Vector2(200f, 200f);
        rectTransform.anchoredPosition = new Vector2(0f, 60f);
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

        Vector3 finalScale = Vector3.one;
        float elapsed = 0f;

        while (elapsed < TitleFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / TitleFadeDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            title.localScale = Vector3.one * Mathf.Lerp(TitleStartScale, 1f, easedT);

            Color color = titleImage.color;
            color.a = easedT;
            titleImage.color = color;
            yield return null;
        }

        title.localScale = finalScale;

        Color finalColor = titleImage.color;
        finalColor.a = 1f;
        titleImage.color = finalColor;
    }
}
