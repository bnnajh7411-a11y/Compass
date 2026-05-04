using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

[DisallowMultipleComponent]
public class ShapePreviewRenderer : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField, Min(4)] private int sampleCount = 96;
    [SerializeField, Min(0f)] private float padding = 0.35f;
    [SerializeField] private float lineWidth = 0.08f;
    [SerializeField] private Color lineColor = Color.white;
    private readonly List<LineRenderer> previewLines = new List<LineRenderer>();

    public void Configure(SplineContainer container, LineRenderer renderer, int samples, float width, Color color)
    {
        splineContainer = container;
        lineRenderer = renderer;
        sampleCount = Mathf.Max(4, samples);
        lineWidth = Mathf.Max(0.001f, width);
        lineColor = color;
        ResetPreviewLines();
        ApplyLineSettings();
    }

    public Bounds Refresh()
    {
        if (splineContainer == null || lineRenderer == null || splineContainer.Splines == null)
        {
            SetAllPreviewLinesEnabled(false);
            return new Bounds(Vector3.zero, Vector3.one);
        }

        ApplyLineSettings();

        IReadOnlyList<Spline> splines = splineContainer.Splines;
        int visibleLineIndex = 0;
        bool hasBounds = false;
        Bounds combinedBounds = new Bounds(Vector3.zero, Vector3.one);

        for (int splineIndex = 0; splineIndex < splines.Count; splineIndex++)
        {
            Spline spline = splines[splineIndex];
            if (spline == null || spline.Count < 1)
            {
                continue;
            }

            LineRenderer previewLine = GetOrCreatePreviewLine(visibleLineIndex++);
            Bounds splineBounds = RenderSpline(previewLine, splineIndex, spline);

            if (!hasBounds)
            {
                combinedBounds = splineBounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(splineBounds.min);
                combinedBounds.Encapsulate(splineBounds.max);
            }
        }

        SetUnusedPreviewLinesEnabled(visibleLineIndex, false);

        if (!hasBounds)
        {
            SetAllPreviewLinesEnabled(false);
            return new Bounds(Vector3.zero, Vector3.one);
        }

        combinedBounds.Expand(Mathf.Max(padding, lineWidth * 2f));
        return combinedBounds;
    }

    private void ApplyLineSettings()
    {
        if (lineRenderer == null)
        {
            return;
        }

        ApplyLineSettings(lineRenderer);
        for (int i = 1; i < previewLines.Count; i++)
        {
            if (previewLines[i] != null)
            {
                ApplyLineSettings(previewLines[i]);
            }
        }
    }

    private void ApplyLineSettings(LineRenderer target)
    {
        if (target == null)
        {
            return;
        }

        target.useWorldSpace = true;
        target.alignment = LineAlignment.View;
        target.textureMode = LineTextureMode.Stretch;
        target.numCornerVertices = 4;
        target.numCapVertices = 4;
        target.sortingLayerID = lineRenderer != null ? lineRenderer.sortingLayerID : target.sortingLayerID;
        target.sortingOrder = lineRenderer != null ? lineRenderer.sortingOrder : target.sortingOrder;
        target.startWidth = lineWidth;
        target.endWidth = lineWidth;
        target.startColor = lineColor;
        target.endColor = lineColor;
        target.material = lineRenderer != null ? lineRenderer.material : target.material;
    }

    private Bounds RenderSpline(LineRenderer target, int splineIndex, Spline spline)
    {
        int count = Mathf.Max(4, sampleCount);
        target.gameObject.SetActive(true);
        target.enabled = true;
        target.positionCount = count;
        target.loop = spline.Closed;

        Vector3 firstPosition = splineContainer.EvaluatePosition(splineIndex, 0f);
        Bounds bounds = new Bounds(firstPosition, Vector3.zero);

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            Vector3 position = splineContainer.EvaluatePosition(splineIndex, t);
            target.SetPosition(i, position);
            bounds.Encapsulate(position);
        }

        return bounds;
    }

    private LineRenderer GetOrCreatePreviewLine(int index)
    {
        EnsurePrimaryPreviewLine();

        while (previewLines.Count <= index)
        {
            previewLines.Add(CreatePreviewLine(previewLines.Count));
        }

        LineRenderer previewLine = previewLines[index];
        if (previewLine != null)
        {
            previewLine.gameObject.SetActive(true);
            previewLine.enabled = true;
        }

        return previewLine;
    }

    private void EnsurePrimaryPreviewLine()
    {
        if (lineRenderer == null)
        {
            return;
        }

        if (previewLines.Count == 0)
        {
            previewLines.Add(lineRenderer);
        }
        else if (previewLines[0] != lineRenderer)
        {
            ResetPreviewLines();
            previewLines.Add(lineRenderer);
        }
    }

    private LineRenderer CreatePreviewLine(int index)
    {
        GameObject lineObject = new GameObject($"{name} [Spline {index}]");
        lineObject.transform.SetParent(transform, false);
        lineObject.layer = lineRenderer != null ? lineRenderer.gameObject.layer : gameObject.layer;

        LineRenderer previewLine = lineObject.AddComponent<LineRenderer>();
        ApplyLineSettings(previewLine);
        previewLine.enabled = true;
        return previewLine;
    }

    private void ResetPreviewLines()
    {
        for (int i = previewLines.Count - 1; i >= 1; i--)
        {
            if (previewLines[i] == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(previewLines[i].gameObject);
            }
            else
            {
                DestroyImmediate(previewLines[i].gameObject);
            }
        }

        previewLines.Clear();
        if (lineRenderer != null)
        {
            previewLines.Add(lineRenderer);
        }
    }

    private void SetAllPreviewLinesEnabled(bool enabled)
    {
        for (int i = 0; i < previewLines.Count; i++)
        {
            if (previewLines[i] == null)
            {
                continue;
            }

            SetPreviewLineEnabled(previewLines[i], enabled);
        }
    }

    private void SetUnusedPreviewLinesEnabled(int startIndex, bool enabled)
    {
        for (int i = startIndex; i < previewLines.Count; i++)
        {
            if (previewLines[i] == null)
            {
                continue;
            }

            SetPreviewLineEnabled(previewLines[i], enabled);
        }
    }

    private void SetPreviewLineEnabled(LineRenderer previewLine, bool enabled)
    {
        if (previewLine == null)
        {
            return;
        }

        previewLine.enabled = enabled;
        if (previewLine != lineRenderer)
        {
            previewLine.gameObject.SetActive(enabled);
        }
    }
}
