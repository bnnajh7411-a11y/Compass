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

    public void Configure(SplineContainer container, LineRenderer renderer, int samples, float width, Color color)
    {
        splineContainer = container;
        lineRenderer = renderer;
        sampleCount = Mathf.Max(4, samples);
        lineWidth = Mathf.Max(0.001f, width);
        lineColor = color;
        ApplyLineSettings();
    }

    public void SetSplineContainer(SplineContainer container)
    {
        splineContainer = container;
    }

    public Bounds Refresh()
    {
        if (splineContainer == null || lineRenderer == null || splineContainer.Spline == null)
        {
            return new Bounds(Vector3.zero, Vector3.one);
        }

        ApplyLineSettings();

        int count = Mathf.Max(4, sampleCount);
        lineRenderer.enabled = true;
        lineRenderer.positionCount = count;
        lineRenderer.loop = splineContainer.Spline.Closed;

        Vector3 firstPosition = splineContainer.EvaluatePosition(0f);
        Bounds bounds = new Bounds(firstPosition, Vector3.zero);

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            Vector3 position = splineContainer.EvaluatePosition(t);
            lineRenderer.SetPosition(i, position);
            bounds.Encapsulate(position);
        }

        bounds.Expand(Mathf.Max(padding, lineWidth * 2f));
        return bounds;
    }

    private void ApplyLineSettings()
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.numCapVertices = 4;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
    }
}
