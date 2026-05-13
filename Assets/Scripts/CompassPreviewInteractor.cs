using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class CompassPreviewInteractor : MonoBehaviour,
    IPointerClickHandler,
    IScrollHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private const float ResizeLerpSpeed = 14f;
    private const float ScrollZoomStep = 0.18f;
    private const float DefaultPreviewAreaScale = 1f;
    private const float ExpandedPreviewAreaScale = 2.2f;
    private const float MinZoomFactor = 0.2f;
    private const float MaxZoomFactor = 3.0f;

    private RectTransform panelRect;
    private RectTransform previewRect;
    private Camera previewCamera;
    private bool shouldRenderCamera = true;

    private Vector2 compactPanelSize;
    private Vector2 compactPreviewSize;

    private Vector3 baseCameraPosition;
    private Quaternion baseCameraRotation;
    private float baseOrthographicSize;
    private float minOrthographicSize;
    private float maxOrthographicSize;
    private float targetPreviewAreaScale = DefaultPreviewAreaScale;

    private bool isDragging;
    private Vector2 dragStartLocalPoint;
    private Vector3 dragStartCameraPosition;
    private float dragStartOrthographicSize;

    public event System.Action<bool> PreviewAreaExpandedChanged;

    public bool IsExpanded
    {
        get
        {
            return Mathf.Abs(targetPreviewAreaScale - ExpandedPreviewAreaScale) < 0.01f;
        }
    }

    public void Configure(RectTransform panelRect, RectTransform previewRect, Camera previewCamera, bool shouldRenderCamera = true)
    {
        this.panelRect = panelRect;
        this.previewRect = previewRect;
        this.previewCamera = previewCamera;
        this.shouldRenderCamera = shouldRenderCamera;

        compactPanelSize = panelRect != null ? panelRect.sizeDelta : new Vector2(300f, 360f);
        compactPreviewSize = previewRect != null ? previewRect.sizeDelta : new Vector2(256f, 256f);
        targetPreviewAreaScale = DefaultPreviewAreaScale;

        CacheCameraState();
        ApplyPreviewAreaScale(targetPreviewAreaScale, true);
    }

    private void Update()
    {
        if (panelRect == null || previewRect == null)
        {
            return;
        }

        Vector2 targetPanelSize = compactPanelSize * targetPreviewAreaScale;
        Vector2 targetPreviewSize = compactPreviewSize * targetPreviewAreaScale;
        float t = 1f - Mathf.Exp(-ResizeLerpSpeed * Time.unscaledDeltaTime);

        panelRect.sizeDelta = Vector2.Lerp(panelRect.sizeDelta, targetPanelSize, t);
        previewRect.sizeDelta = Vector2.Lerp(previewRect.sizeDelta, targetPreviewSize, t);

        if (Vector2.Distance(panelRect.sizeDelta, targetPanelSize) < 0.25f)
        {
            panelRect.sizeDelta = targetPanelSize;
        }

        if (Vector2.Distance(previewRect.sizeDelta, targetPreviewSize) < 0.25f)
        {
            previewRect.sizeDelta = targetPreviewSize;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsOverPreview(eventData) || eventData.clickCount < 2)
        {
            return;
        }

        ResetView();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (!IsOverPreview(eventData) || previewCamera == null || previewRect == null)
        {
            return;
        }

        float scroll = eventData.scrollDelta.y;
        if (Mathf.Approximately(scroll, 0f))
        {
            return;
        }

        ApplyZoom(1f - (scroll * ScrollZoomStep), eventData);
    }

    public void ExpandPreviewArea()
    {
        ApplyPreviewAreaScale(ExpandedPreviewAreaScale, false);
    }

    public void CollapsePreviewArea()
    {
        ApplyPreviewAreaScale(DefaultPreviewAreaScale, false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsOverPreview(eventData) || previewCamera == null)
        {
            return;
        }

        if (!TryGetLocalPoint(eventData, out dragStartLocalPoint))
        {
            return;
        }

        isDragging = true;
        dragStartCameraPosition = previewCamera.transform.position;
        dragStartOrthographicSize = previewCamera.orthographicSize;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || previewCamera == null)
        {
            return;
        }

        Vector2 currentLocalPoint;
        if (!TryGetLocalPoint(eventData, out currentLocalPoint))
        {
            return;
        }

        Vector2 delta = currentLocalPoint - dragStartLocalPoint;
        Vector2 size = GetPreviewSize();
        float aspect = Mathf.Max(previewCamera.aspect, 0.01f);

        Vector3 cameraPosition = dragStartCameraPosition;
        cameraPosition.x -= (delta.x / Mathf.Max(size.x, 1f)) * 2f * dragStartOrthographicSize * aspect;
        cameraPosition.y -= (delta.y / Mathf.Max(size.y, 1f)) * 2f * dragStartOrthographicSize;
        previewCamera.transform.position = cameraPosition;
        RenderCameraIfNeeded();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }

    private void ResetView()
    {
        isDragging = false;
        if (panelRect != null)
        {
            CollapsePreviewArea();
        }

        ResetCameraState();
    }

    private void ApplyPreviewAreaScale(float scale, bool immediate)
    {
        bool wasExpanded = IsExpanded;
        targetPreviewAreaScale = Mathf.Clamp(scale, DefaultPreviewAreaScale, ExpandedPreviewAreaScale);

        if (!immediate)
        {
            NotifyPreviewAreaStateChanged(wasExpanded);
            return;
        }

        if (panelRect != null)
        {
            panelRect.sizeDelta = compactPanelSize * targetPreviewAreaScale;
        }

        if (previewRect != null)
        {
            previewRect.sizeDelta = compactPreviewSize * targetPreviewAreaScale;
        }

        NotifyPreviewAreaStateChanged(wasExpanded);
    }

    private void NotifyPreviewAreaStateChanged(bool wasExpanded)
    {
        bool isExpanded = IsExpanded;
        if (wasExpanded == isExpanded)
        {
            return;
        }

        PreviewAreaExpandedChanged?.Invoke(isExpanded);
    }

    private void CacheCameraState()
    {
        if (previewCamera == null)
        {
            baseCameraPosition = Vector3.zero;
            baseCameraRotation = Quaternion.identity;
            baseOrthographicSize = 1f;
            minOrthographicSize = 0.25f;
            maxOrthographicSize = 2.5f;
            return;
        }

        baseCameraPosition = previewCamera.transform.position;
        baseCameraRotation = previewCamera.transform.rotation;
        baseOrthographicSize = previewCamera.orthographicSize;
        minOrthographicSize = Mathf.Max(baseOrthographicSize * MinZoomFactor, 0.05f);
        maxOrthographicSize = Mathf.Max(baseOrthographicSize * MaxZoomFactor, minOrthographicSize + 0.01f);
    }

    private void ResetCameraState()
    {
        if (previewCamera == null)
        {
            return;
        }

        previewCamera.transform.position = baseCameraPosition;
        previewCamera.transform.rotation = baseCameraRotation;
        previewCamera.orthographicSize = baseOrthographicSize;
        RenderCameraIfNeeded();
    }

    private bool IsOverPreview(PointerEventData eventData)
    {
        if (previewRect == null)
        {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(
            previewRect,
            eventData.position,
            eventData.pressEventCamera);
    }

    private bool TryGetLocalPoint(PointerEventData eventData, out Vector2 localPoint)
    {
        if (previewRect == null)
        {
            localPoint = Vector2.zero;
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            previewRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint);
    }

    private Vector2 GetPreviewSize()
    {
        if (previewRect != null)
        {
            return previewRect.rect.size;
        }

        return compactPreviewSize;
    }

    private void ApplyZoom(float zoomFactor, PointerEventData eventData)
    {
        if (previewCamera == null || previewRect == null)
        {
            return;
        }

        float oldSize = previewCamera.orthographicSize;
        float newSize = Mathf.Clamp(oldSize * zoomFactor, minOrthographicSize, maxOrthographicSize);

        if (Mathf.Approximately(oldSize, newSize))
        {
            return;
        }

        if (eventData != null && TryGetLocalPoint(eventData, out Vector2 localPoint))
        {
            float aspect = Mathf.Max(previewCamera.aspect, 0.01f);
            Vector2 size = GetPreviewSize();
            float sizeDelta = oldSize - newSize;

            Vector3 cameraPosition = previewCamera.transform.position;
            cameraPosition.x += (localPoint.x / Mathf.Max(size.x, 1f)) * 2f * sizeDelta * aspect;
            cameraPosition.y += (localPoint.y / Mathf.Max(size.y, 1f)) * 2f * sizeDelta;
            previewCamera.transform.position = cameraPosition;
        }

        previewCamera.orthographicSize = newSize;
        RenderCameraIfNeeded();
    }

    private void RenderCameraIfNeeded()
    {
        if (!shouldRenderCamera || previewCamera == null)
        {
            return;
        }

        previewCamera.Render();
    }
}
