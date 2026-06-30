using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
public class CompassBootstrap : MonoBehaviour
{
    private const float GridSpacing = 0.5f;
    private const float GridLineWidth = 0.02f;
    private const float GridPadding = 1.25f;
    private const int GridSortingOrder = -100;
    private const string MainSceneName = "Main";
    private const string PreviewLayerName = "CompassPreview";
    private static readonly Vector2 AccuracyGaugeSize = new Vector2(560f, 30f);
    private static readonly Vector2 AccuracyGaugePosition = new Vector2(0f, 24f);
    private static readonly Color GridColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);

    private static CompassBootstrap instance;

    private GameObject runtimeRoot;
    private RenderTexture previewTexture;
    private Camera previewCamera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (instance != null)
        {
            return;
        }

        GameObject bootstrapObject = new GameObject("[CompassBootstrap]");
        instance = bootstrapObject.AddComponent<CompassBootstrap>();
        DontDestroyOnLoad(bootstrapObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != MainSceneName)
        {
            CleanupRuntime();
            return;
        }

        BuildScene();
    }

    private void BuildScene()
    {
        CleanupRuntime();
        RuntimeUiFactory.EnsureEventSystem();
        RuntimeUiFactory.EnableEventSystemNavigation();

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        int previewLayer = LayerMask.NameToLayer(PreviewLayerName);
        if (previewLayer >= 0)
        {
            mainCamera.cullingMask &= ~(1 << previewLayer);
        }

        SplineGuide guide = SelectStageGuide(GameManager.SelectedStageIndex, out SplineContainer splineContainer);
        if (guide == null || splineContainer == null)
        {
            return;
        }

        runtimeRoot = new GameObject("[CompassRuntime]");
        runtimeRoot.transform.SetParent(transform, false);

        GameObject canvasObject = GetOrCreateCanvas();
        Transform canvasTransform = canvasObject.transform;
        CreateMainCameraInteractionOverlay(canvasTransform, mainCamera);

        ScoreManager scoreManager = CreateScoreManager();
        AccuracyGaugeView accuracyGauge = CreateAccuracyGauge(canvasTransform);
        scoreManager.BindAccuracyGauge(accuracyGauge);

        Rotate rotate = Object.FindFirstObjectByType<Rotate>();
        if (rotate != null)
        {
            rotate.EnsurePlayerBrushSetup();
            rotate.ApplyTargetPositions(
                guide.Center1WorldPosition,
                guide.Center2WorldPosition,
                guide.Center3WorldPosition);
        }

        guide.splineContainer = splineContainer;
        if (guide.checkpointPrefab == null)
        {
            guide.checkpointPrefab = CreateCheckpointTemplate();
        }
        guide.RebuildCheckpoints();
        scoreManager.BindGuide(guide);

        if (previewLayer >= 0)
        {
            ShapePreviewRenderer previewRenderer = CreatePreviewRenderer(splineContainer, previewLayer);
            Bounds bounds = previewRenderer.Refresh();
            Vector2 gridCenter = new Vector2(bounds.center.x, bounds.center.y);
            Vector2 gridHalfExtents = GetGridHalfExtents(mainCamera, bounds);
            Bounds gridBounds = new Bounds(
                new Vector3(gridCenter.x, gridCenter.y, 0f),
                new Vector3(gridHalfExtents.x * 2f, gridHalfExtents.y * 2f, 1f));
            CreateGridBackdrop("[CompassGrid]", gridBounds, mainCamera.gameObject.layer);
            CreateGridBackdrop("[CompassPreviewGrid]", gridBounds, previewLayer);
            CreatePreviewCamera(previewLayer, bounds);
            CreatePreviewPanel(canvasTransform);
        }
        else
        {
            Bounds gridBounds = GetFallbackGridBounds(mainCamera);
            CreateGridBackdrop("[CompassGrid]", gridBounds, mainCamera.gameObject.layer);
        }

        if (rotate != null)
        {
            RuntimeUiFactory.CreateCheckIconButton(canvasTransform, rotate.SubmitResultAndLoadScene);
        }

        RuntimeUiFactory.CreateAudioToggleButton(canvasTransform);
        RuntimePauseMenu.Create(canvasTransform, true);
        scoreManager.Refresh();
    }

    private SplineGuide SelectStageGuide(int stageIndex, out SplineContainer splineContainer)
    {
        splineContainer = null;

        SplineGuide[] guides = Object.FindObjectsByType<SplineGuide>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (guides != null && guides.Length > 0)
        {
            string targetGuideName = GameManager.GetStageGuideName(stageIndex);
            SplineGuide selectedGuide = null;
            SplineGuide firstAvailableGuide = null;

            for (int i = 0; i < guides.Length; i++)
            {
                SplineGuide guide = guides[i];
                if (guide == null)
                {
                    continue;
                }

                if (firstAvailableGuide == null)
                {
                    firstAvailableGuide = guide;
                }

                if (string.Equals(guide.gameObject.name, targetGuideName, System.StringComparison.OrdinalIgnoreCase))
                {
                    selectedGuide = guide;
                }
            }

            if (selectedGuide == null)
            {
                selectedGuide = firstAvailableGuide;
            }

            if (selectedGuide != null)
            {
                for (int i = 0; i < guides.Length; i++)
                {
                    SplineGuide guide = guides[i];
                    if (guide == null)
                    {
                        continue;
                    }

                    bool isSelected = guide == selectedGuide;
                    guide.enabled = isSelected;
                    guide.gameObject.SetActive(isSelected);
                }

                splineContainer = ResolveSplineContainer(selectedGuide);
                if (splineContainer != null)
                {
                    selectedGuide.splineContainer = splineContainer;
                    return selectedGuide;
                }
            }
        }

        splineContainer = Object.FindFirstObjectByType<SplineContainer>();
        if (splineContainer == null)
        {
            return null;
        }

        SplineGuide fallbackGuide = splineContainer.GetComponent<SplineGuide>();
        if (fallbackGuide == null)
        {
            fallbackGuide = splineContainer.gameObject.AddComponent<SplineGuide>();
        }

        fallbackGuide.enabled = true;
        fallbackGuide.splineContainer = splineContainer;
        return fallbackGuide;
    }

    private static SplineContainer ResolveSplineContainer(SplineGuide guide)
    {
        if (guide == null)
        {
            return null;
        }

        if (guide.splineContainer != null)
        {
            return guide.splineContainer;
        }

        SplineContainer container = guide.GetComponent<SplineContainer>();
        if (container != null)
        {
            return container;
        }

        return guide.GetComponentInChildren<SplineContainer>(true);
    }

    private ScoreManager CreateScoreManager()
    {
        GameObject scoreObject = new GameObject("[CompassScoreManager]");
        scoreObject.transform.SetParent(runtimeRoot.transform, false);
        return scoreObject.AddComponent<ScoreManager>();
    }

    private AccuracyGaugeView CreateAccuracyGauge(Transform parent)
    {
        const float borderThickness = 4f;

        Image background = RuntimeUiFactory.CreateCardImage(
            parent,
            "AccuracyGauge",
            Color.white,
            AccuracyGaugeSize);
        background.raycastTarget = false;

        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = new Vector2(0.5f, 0f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0f);
        backgroundRect.pivot = new Vector2(0.5f, 0f);
        backgroundRect.sizeDelta = AccuracyGaugeSize;
        backgroundRect.anchoredPosition = AccuracyGaugePosition;

        AccuracyGaugeView gauge = background.gameObject.AddComponent<AccuracyGaugeView>();

        GameObject fillMaskObject = new GameObject("FillMask", typeof(RectTransform), typeof(RectMask2D));
        fillMaskObject.transform.SetParent(background.transform, false);
        fillMaskObject.transform.SetAsLastSibling();

        RectTransform fillMaskRect = fillMaskObject.GetComponent<RectTransform>();
        Vector2 innerSize = new Vector2(
            Mathf.Max(0f, AccuracyGaugeSize.x - (borderThickness * 2f)),
            Mathf.Max(0f, AccuracyGaugeSize.y - (borderThickness * 2f)));

        fillMaskRect.anchorMin = new Vector2(0f, 0.5f);
        fillMaskRect.anchorMax = new Vector2(0f, 0.5f);
        fillMaskRect.pivot = new Vector2(0f, 0.5f);
        fillMaskRect.sizeDelta = new Vector2(0f, innerSize.y);
        fillMaskRect.anchoredPosition = new Vector2(borderThickness, 0f);

        Image fill = RuntimeUiFactory.CreateImage(
            fillMaskObject.transform,
            "Fill",
            new Color32(0xFF, 0x7C, 0xB2, 0xFF),
            RuntimeSpriteFactory.GetRoundedRectSprite(
                Mathf.Max(1, Mathf.RoundToInt(innerSize.x)),
                Mathf.Max(1, Mathf.RoundToInt(innerSize.y)),
                innerSize.y * 0.5f));
        fill.raycastTarget = false;

        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0.5f);
        fillRect.anchorMax = new Vector2(0f, 0.5f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.sizeDelta = innerSize;
        fillRect.anchoredPosition = Vector2.zero;

        gauge.Configure(fillMaskRect, innerSize.x, innerSize.y);
        return gauge;
    }

    private void CreatePreviewPanel(Transform parent)
    {
        GameObject panelObject = new GameObject("PreviewPanel");
        panelObject.transform.SetParent(parent, false);

        Image background = panelObject.AddComponent<Image>();
        background.sprite = RuntimeSpriteFactory.GetRoundedRectSprite(300, 360, 36f);
        background.color = new Color32(0xE7, 0xF3, 0xF1, 0xC5);
        background.raycastTarget = false;

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.sizeDelta = new Vector2(300f, 360f);
        panelRect.anchoredPosition = new Vector2(-24f, -24f);

        GameObject titleObject = new GameObject("Title");
        titleObject.transform.SetParent(panelObject.transform, false);

        Text title = titleObject.AddComponent<Text>();
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize = 24;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.UpperCenter;
        title.color = RuntimeUiTheme.TextColor;
        title.raycastTarget = false;
        title.text = "\u003c\ubcf4\uae30\u003e";

        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(-32f, 30f);
        titleRect.anchoredPosition = new Vector2(0f, -12f);

        GameObject previewObject = new GameObject("Preview");
        previewObject.transform.SetParent(panelObject.transform, false);

        RawImage rawImage = previewObject.AddComponent<RawImage>();
        rawImage.texture = previewTexture;
        rawImage.color = Color.white;
        rawImage.raycastTarget = false;

        RectTransform previewRect = previewObject.GetComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0.5f, 0.5f);
        previewRect.anchorMax = new Vector2(0.5f, 0.5f);
        previewRect.pivot = new Vector2(0.5f, 0.5f);
        previewRect.sizeDelta = new Vector2(256f, 256f);
        previewRect.anchoredPosition = new Vector2(0f, -4f);

        GameObject overlayObject = new GameObject("InteractionOverlay");
        overlayObject.transform.SetParent(panelObject.transform, false);

        Image overlay = overlayObject.AddComponent<Image>();
        overlay.sprite = RuntimeSpriteFactory.GetWhiteSprite();
        overlay.color = new Color(1f, 1f, 1f, 0f);
        overlay.raycastTarget = true;

        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        RuntimeUiFactory.Stretch(overlayRect);
        overlayObject.transform.SetAsLastSibling();

        CompassPreviewInteractor interactor = overlayObject.AddComponent<CompassPreviewInteractor>();
        interactor.Configure(panelRect, previewRect, previewCamera);

        CreatePreviewScaleButtons(panelObject.transform, interactor, new Vector2(50f, 28f));
    }

    private void CreateMainCameraInteractionOverlay(Transform parent, Camera mainCamera)
    {
        if (mainCamera == null)
        {
            return;
        }

        GameObject overlayObject = new GameObject("MainCameraInteractionOverlay");
        overlayObject.transform.SetParent(parent, false);
        overlayObject.transform.SetAsFirstSibling();

        Image overlay = overlayObject.AddComponent<Image>();
        overlay.sprite = RuntimeSpriteFactory.GetWhiteSprite();
        overlay.color = new Color(1f, 1f, 1f, 0f);
        overlay.raycastTarget = true;

        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        RuntimeUiFactory.Stretch(overlayRect);

        CompassPreviewInteractor interactor = overlayObject.AddComponent<CompassPreviewInteractor>();
        interactor.Configure(null, overlayRect, mainCamera, false);
    }

    private void CreatePreviewScaleButtons(Transform parent, CompassPreviewInteractor interactor, Vector2 anchoredPosition)
    {
        Button expandButton = CreatePreviewScaleButton(
            parent,
            "PreviewScaleExpandButton",
            "+",
            anchoredPosition,
            interactor.ExpandPreviewArea);

        Button collapseButton = CreatePreviewScaleButton(
            parent,
            "PreviewScaleCollapseButton",
            "-",
            anchoredPosition,
            interactor.CollapsePreviewArea);

        System.Action<bool> refreshButtons = isExpanded =>
        {
            if (expandButton != null)
            {
                expandButton.gameObject.SetActive(!isExpanded);
            }

            if (collapseButton != null)
            {
                collapseButton.gameObject.SetActive(isExpanded);
            }
        };

        interactor.PreviewAreaExpandedChanged += refreshButtons;
        refreshButtons(interactor.IsExpanded);
    }

    private Button CreatePreviewScaleButton(Transform parent, string objectName, string labelText, Vector2 anchoredPosition, System.Action onClick)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = RuntimeSpriteFactory.GetCircleSprite();
        image.color = RuntimeUiTheme.ButtonNormalColor;
        image.preserveAspect = true;

        Button button = buttonObject.AddComponent<Button>();
        RuntimeUiFactory.ApplyThemeButton(button, image);
        button.onClick.AddListener(() => onClick?.Invoke());

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 0f);
        rectTransform.anchorMax = new Vector2(0f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(36f, 36f);
        rectTransform.anchoredPosition = anchoredPosition;

        Text label = RuntimeUiFactory.CreateText(
            buttonObject.transform,
            "Label",
            labelText,
            24,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            RuntimeUiTheme.TextColor,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero);

        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
    }

    private GameObject GetOrCreateCanvas()
    {
        Transform existing = runtimeRoot.transform.Find("CompassCanvas");
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject canvasObject = new GameObject("CompassCanvas");
        canvasObject.transform.SetParent(runtimeRoot.transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvasObject;
    }

    private ShapePreviewRenderer CreatePreviewRenderer(SplineContainer splineContainer, int previewLayer)
    {
        GameObject previewObject = new GameObject("[CompassPreviewShape]");
        previewObject.transform.SetParent(runtimeRoot.transform, false);
        previewObject.layer = previewLayer;

        LineRenderer lineRenderer = previewObject.AddComponent<LineRenderer>();
        lineRenderer.material = CreatePreviewMaterial();
        lineRenderer.sortingOrder = 10;

        ShapePreviewRenderer renderer = previewObject.AddComponent<ShapePreviewRenderer>();
        renderer.Configure(splineContainer, lineRenderer, 96, 0.08f, Color.white);
        return renderer;
    }

    private GridBackdropRenderer CreateGridBackdrop(string objectName, Bounds bounds, int layer)
    {
        GameObject gridObject = new GameObject(objectName);
        gridObject.transform.SetParent(runtimeRoot.transform, false);
        gridObject.layer = layer;

        GridBackdropRenderer renderer = gridObject.AddComponent<GridBackdropRenderer>();
        renderer.Configure(bounds, GridSpacing, GridLineWidth, GridColor, GridSortingOrder);
        renderer.Refresh();
        return renderer;
    }

    private void CreatePreviewCamera(int previewLayer, Bounds bounds)
    {
        GameObject cameraObject = new GameObject("[CompassPreviewCamera]");
        cameraObject.transform.SetParent(runtimeRoot.transform, false);

        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.orthographic = true;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);
        previewCamera.cullingMask = 1 << previewLayer;
        previewCamera.nearClipPlane = 0.1f;
        previewCamera.farClipPlane = 50f;

        previewTexture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
        previewTexture.name = "CompassPreviewTexture";
        previewTexture.filterMode = FilterMode.Bilinear;
        previewTexture.wrapMode = TextureWrapMode.Clamp;
        previewTexture.Create();
        previewCamera.targetTexture = previewTexture;

        FramePreviewCamera(bounds);
    }

    private static Vector2 GetGridHalfExtents(Camera mainCamera, Bounds previewBounds)
    {
        Vector2 cameraHalfExtents = GetCameraHalfExtents(mainCamera);
        Vector2 previewHalfExtents = new Vector2(previewBounds.extents.x, previewBounds.extents.y);
        return Vector2.Max(cameraHalfExtents, previewHalfExtents) + Vector2.one * GridPadding;
    }

    private static Vector2 GetCameraHalfExtents(Camera camera)
    {
        if (camera == null)
        {
            return Vector2.one * 5f;
        }

        float aspect = Mathf.Max(camera.aspect, 0.01f);
        return new Vector2(camera.orthographicSize * aspect, camera.orthographicSize);
    }

    private static Bounds GetFallbackGridBounds(Camera mainCamera)
    {
        Vector2 halfExtents = GetCameraHalfExtents(mainCamera) + Vector2.one * GridPadding;
        Vector3 center = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
        return new Bounds(
            new Vector3(center.x, center.y, 0f),
            new Vector3(halfExtents.x * 2f, halfExtents.y * 2f, 1f));
    }

    private void FramePreviewCamera(Bounds bounds)
    {
        if (previewCamera == null)
        {
            return;
        }

        previewCamera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
        previewCamera.transform.rotation = Quaternion.identity;

        float aspect = previewTexture != null ? previewTexture.width / (float)previewTexture.height : 1f;
        previewCamera.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.x / Mathf.Max(aspect, 0.01f)) + 0.25f;
        previewCamera.Render();
    }

    private Material CreatePreviewMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("UI/Default");
        }

        Material material = new Material(shader);
        material.color = Color.white;
        return material;
    }

    private GameObject CreateCheckpointTemplate()
    {
        GameObject checkpointObject = new GameObject("CheckpointTemplate");
        checkpointObject.transform.SetParent(runtimeRoot.transform, false);
        checkpointObject.SetActive(false);

        SpriteRenderer spriteRenderer = checkpointObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = RuntimeSpriteFactory.GetWhiteSprite();
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = 10;

        checkpointObject.transform.localScale = Vector3.one * 0.18f;

        CircleCollider2D collider = checkpointObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.5f;

        checkpointObject.AddComponent<Checkpoint>();
        return checkpointObject;
    }

    private void CleanupRuntime()
    {
        ScoreManager.ResetSingleton();

        if (previewCamera != null)
        {
            previewCamera.targetTexture = null;
        }

        if (runtimeRoot != null)
        {
            Destroy(runtimeRoot);
        }

        if (previewTexture != null)
        {
            Destroy(previewTexture);
        }

        runtimeRoot = null;
        previewTexture = null;
        previewCamera = null;
    }
}
