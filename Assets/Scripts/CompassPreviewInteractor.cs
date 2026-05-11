using System.Collections;
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
    private const float DoubleClickDelay = 0.25f;
    private const float PanelExpandScale = 2.4f;
    private const float PreviewExpandScale = 2.2f;
    private const float ResizeLerpSpeed = 14f;
    private const float ScrollZoomStep = 0.18f;
    private const float ButtonZoomInFactor = 0.65f;
    private const float ButtonZoomOutFactor = 1.35f;
    private const float MinZoomFactor = 0.2f;
    private const float MaxZoomFactor = 3.0f;

    private RectTransform panelRect;
    private RectTransform previewRect;
    private Camera previewCamera;

    private Vector2 compactPanelSize;
    private Vector2 compactPreviewSize;
    private Vector2 expandedPanelSize;
    private Vector2 expandedPreviewSize;

    private Vector3 baseCameraPosition;
    private Quaternion baseCameraRotation;
    private float baseOrthographicSize;
    private float minOrthographicSize;
    private float maxOrthographicSize;

    private bool isExpanded;
    private bool isDragging;
    private bool pendingSingleClick;
    private float lastClickTime;
    private Vector2 dragStartLocalPoint;
    private Vector3 dragStartCameraPosition;
    private float dragStartOrthographicSize;
    private Coroutine singleClickRoutine;

    public void Configure(RectTransform panelRect, RectTransform previewRect, Camera previewCamera)
    {
        this.panelRect = panelRect;
        this.previewRect = previewRect;
        this.previewCamera = previewCamera;

        compactPanelSize = panelRect != null ? panelRect.sizeDelta : new Vector2(300f, 360f);
        compactPreviewSize = previewRect != null ? previewRect.sizeDelta : new Vector2(256f, 256f);
        expandedPanelSize = compactPanelSize * PanelExpandScale;
        expandedPreviewSize = compactPreviewSize * PreviewExpandScale;

        CacheCameraState();
        SetExpanded(false, true);
    }

    private void Update()
    {
        if (panelRect == null || previewRect == null)
        {
            return;
        }

        Vector2 targetPanelSize = isExpanded ? expandedPanelSize : compactPanelSize;
        Vector2 targetPreviewSize = isExpanded ? expandedPreviewSize : compactPreviewSize;
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
        if (!IsOverPreview(eventData))
        {
            return;
        }

        bool looksLikeDoubleClick = eventData.clickCount >= 2
            || (pendingSingleClick && (Time.unscaledTime - lastClickTime) <= DoubleClickDelay);

        lastClickTime = Time.unscaledTime;

        if (looksLikeDoubleClick)
        {
            ResetView();
            return;
        }

        QueueSingleClickToggle();
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

    public void ZoomIn()
    {
        ApplyZoom(ButtonZoomInFactor, null);
    }

    public void ZoomOut()
    {
        ApplyZoom(ButtonZoomOutFactor, null);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!IsOverPreview(eventData) || previewCamera == null)
        {
            return;
        }

        CancelPendingSingleClick();

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
        previewCamera.Render();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }

    private void QueueSingleClickToggle()
    {
        CancelPendingSingleClick();
        pendingSingleClick = true;
        singleClickRoutine = StartCoroutine(ResolveSingleClick());
    }

    private IEnumerator ResolveSingleClick()
    {
        float startTime = Time.unscaledTime;
        while (Time.unscaledTime - startTime < DoubleClickDelay)
        {
            if (!pendingSingleClick)
            {
                singleClickRoutine = null;
                yield break;
            }

            yield return null;
        }

        singleClickRoutine = null;
        if (!pendingSingleClick)
        {
            yield break;
        }

        pendingSingleClick = false;
        ToggleExpandedState();
    }

    private void ToggleExpandedState()
    {
        isExpanded = !isExpanded;
    }

    private void ResetView()
    {
        CancelPendingSingleClick();
        isDragging = false;
        isExpanded = false;

        ResetCameraState();
    }

    private void SetExpanded(bool expanded, bool immediate)
    {
        isExpanded = expanded;

        if (!immediate)
        {
            return;
        }

        if (panelRect != null)
        {
            panelRect.sizeDelta = expanded ? expandedPanelSize : compactPanelSize;
        }

        if (previewRect != null)
        {
            previewRect.sizeDelta = expanded ? expandedPreviewSize : compactPreviewSize;
        }
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
        previewCamera.Render();
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

    private void CancelPendingSingleClick()
    {
        pendingSingleClick = false;

        if (singleClickRoutine != null)
        {
            StopCoroutine(singleClickRoutine);
            singleClickRoutine = null;
        }
    }

    private void ApplyZoom(float zoomFactor, PointerEventData eventData)
    {
        if (previewCamera == null || previewRect == null)
        {
            return;
        }

        CancelPendingSingleClick();

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
        previewCamera.Render();
    }
}
