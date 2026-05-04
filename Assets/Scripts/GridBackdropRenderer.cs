using UnityEngine;

[DisallowMultipleComponent]
public class GridBackdropRenderer : MonoBehaviour
{
    private static Material sharedMaterial;

    [SerializeField] private Bounds gridBounds;
    [SerializeField, Min(0.01f)] private float spacing = 0.5f;
    [SerializeField, Min(0.001f)] private float lineWidth = 0.02f;
    [SerializeField] private Color lineColor = new Color(0.82f, 0.87f, 0.95f, 0.06f);
    [SerializeField] private int sortingOrder = -100;

    public void Configure(Bounds bounds, float gridSpacing, float width, Color color, int order)
    {
        gridBounds = bounds;
        spacing = Mathf.Max(0.01f, gridSpacing);
        lineWidth = Mathf.Max(0.001f, width);
        lineColor = color;
        sortingOrder = order;
    }

    public void Refresh()
    {
        ClearLines();
        BuildGrid();
    }

    private void BuildGrid()
    {
        if (spacing <= 0f)
        {
            return;
        }

        Vector3 min = gridBounds.min;
        Vector3 max = gridBounds.max;

        float startX = Mathf.Floor(min.x / spacing) * spacing;
        float endX = Mathf.Ceil(max.x / spacing) * spacing;
        float startY = Mathf.Floor(min.y / spacing) * spacing;
        float endY = Mathf.Ceil(max.y / spacing) * spacing;

        for (float x = startX; x <= endX + 0.0001f; x += spacing)
        {
            CreateLine(new Vector3(x, startY, 0f), new Vector3(x, endY, 0f), $"Vertical_{x:0.##}");
        }

        for (float y = startY; y <= endY + 0.0001f; y += spacing)
        {
            CreateLine(new Vector3(startX, y, 0f), new Vector3(endX, y, 0f), $"Horizontal_{y:0.##}");
        }
    }

    private void CreateLine(Vector3 start, Vector3 end, string objectName)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.layer = gameObject.layer;
        lineObject.transform.SetParent(transform, false);

        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.numCapVertices = 0;
        lineRenderer.numCornerVertices = 0;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        lineRenderer.sortingOrder = sortingOrder;
        lineRenderer.material = GetOrCreateMaterial();
    }

    private void ClearLines()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private static Material GetOrCreateMaterial()
    {
        if (sharedMaterial != null)
        {
            return sharedMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("UI/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        sharedMaterial = new Material(shader);
        sharedMaterial.color = Color.white;
        sharedMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        sharedMaterial.hideFlags = HideFlags.HideAndDontSave;
        return sharedMaterial;
    }
}
