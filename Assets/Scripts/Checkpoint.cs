using UnityEngine;

[DisallowMultipleComponent]
public class Checkpoint : MonoBehaviour
{
    public int index;
    public bool isPassed = false;

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D checkpointCollider;
    private SplineGuide owningGuide;
    private Color defaultColor = Color.white;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        checkpointCollider = GetComponent<CircleCollider2D>();
        if (spriteRenderer != null)
        {
            defaultColor = spriteRenderer.color;
        }
    }

    public void Initialize(int checkpointIndex, SplineGuide guide)
    {
        index = checkpointIndex;
        owningGuide = guide;
        isPassed = false;
        SetColor(defaultColor);
    }

    public void MarkPassed()
    {
        if (isPassed)
        {
            return;
        }

        isPassed = true;
        SetColor(Color.green);
        ScoreManager.Instance?.Refresh();
    }

    public float CalculateAccuracy()
    {
        if (owningGuide != null)
        {
            return owningGuide.CalculateAccuracy();
        }

        if (ScoreManager.Instance != null)
        {
            return ScoreManager.Instance.GetAccuracy();
        }

        return isPassed ? 100f : 0f;
    }

    public bool IsTouchedByTrail(Vector3[] trailPositions, int positionCount, float hitTolerance)
    {
        if (isPassed || trailPositions == null || positionCount < 2)
        {
            return false;
        }

        Vector2 center = GetCheckpointCenter();
        float hitToleranceSq = Mathf.Max(0.0001f, hitTolerance * hitTolerance);

        for (int i = 1; i < positionCount; i++)
        {
            float distanceSq = DistancePointToSegmentSquared(center, trailPositions[i - 1], trailPositions[i]);
            if (distanceSq <= hitToleranceSq)
            {
                return true;
            }
        }

        return false;
    }

    private void SetColor(Color color)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    private Vector2 GetCheckpointCenter()
    {
        if (checkpointCollider != null)
        {
            return checkpointCollider.transform.TransformPoint(checkpointCollider.offset);
        }

        return transform.position;
    }

    private static float DistancePointToSegmentSquared(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
    {
        Vector2 segment = segmentEnd - segmentStart;
        float segmentLengthSq = segment.sqrMagnitude;
        if (segmentLengthSq < Mathf.Epsilon)
        {
            return (point - segmentStart).sqrMagnitude;
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - segmentStart, segment) / segmentLengthSq);
        Vector2 closestPoint = segmentStart + (segment * t);
        return (point - closestPoint).sqrMagnitude;
    }
}
