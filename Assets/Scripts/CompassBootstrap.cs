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
            Debug.LogWarning("CompassBootstrap: Main Camera was not found.");
            return;
        }

        int previewLayer = LayerMask.NameToLayer(PreviewLayerName);
        if (previewLayer < 0)
        {
            Debug.LogWarning($"CompassBootstrap: Layer '{PreviewLayerName}' is missing.");
        }
        else
        {
            mainCamera.cullingMask &= ~(1 << previewLayer);
        }

        SplineGuide guide = SelectStageGuide(CompassGameState.SelectedStageIndex, out SplineContainer splineContainer);
        if (guide == null || splineContainer == null)
        {
            Debug.LogWarning("CompassBootstrap: No usable SplineGuide or SplineContainer was found.");
            return;
        }

        runtimeRoot = new GameObject("[CompassRuntime]");
        runtimeRoot.transform.SetParent(transform, false);

        ScoreManager scoreManager = CreateScoreManager();
        Text statusText = CreateStatusText();
        scoreManager.BindStatusText(statusText);

        Rotate rotate = Object.FindFirstObjectByType<Rotate>();
        if (rotate != null)
        {
            rotate.EnsurePlayerBrushSetup();
            if (rotate.target3 == null)
            {
                Debug.LogWarning("CompassBootstrap: Rotate.target3 is not assigned in the scene.");
            }

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
            CreatePreviewPanel();
        }
        else
        {
            Bounds gridBounds = GetFallbackGridBounds(mainCamera);
            CreateGridBackdrop("[CompassGrid]", gridBounds, mainCamera.gameObject.layer);
        }

        RuntimeUiFactory.CreateAudioToggleButton(GetOrCreateCanvas().transform);
        scoreManager.Refresh();
    }

    private SplineGuide SelectStageGuide(int stageIndex, out SplineContainer splineContainer)
    {
        splineContainer = null;

        SplineGuide[] guides = Object.FindObjectsByType<SplineGuide>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (guides != null && guides.Length > 0)
        {
            string targetGuideName = CompassGameState.GetStageGuideName(stageIndex);
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
                if (selectedGuide != null)
                {
                    Debug.LogWarning($"CompassBootstrap: Stage guide '{targetGuideName}' was not found. Falling back to '{selectedGuide.name}'.");
                }
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
                    Debug.Log($"CompassBootstrap: Selected guide '{selectedGuide.name}' for stage {CompassGameState.GetCurrentStageLabel()}.");

                    return selectedGuide;
                }

                Debug.LogWarning($"CompassBootstrap: Selected guide '{selectedGuide.name}' does not have a SplineContainer.");
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

    private Text CreateStatusText()
    {
        GameObject canvasObject = GetOrCreateCanvas();

        GameObject scoreObject = new GameObject("AccuracyText");
        scoreObject.transform.SetParent(canvasObject.transform, false);

        Text text = scoreObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 25;
        text.alignment = TextAnchor.LowerLeft;
        text.color = RuntimeUiTheme.TextColor;
        text.text = "정확도 0% (0/0)";

        RectTransform rectTransform = scoreObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.sizeDelta = new Vector2(300f, 28f);
        rectTransform.anchoredPosition = new Vector2(-24f, 20f);

        return text;
    }

    private void CreatePreviewPanel()
    {
        GameObject panelObject = new GameObject("PreviewPanel");
        panelObject.transform.SetParent(GetOrCreateCanvas().transform, false);

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
        title.text = "<보기>";

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
