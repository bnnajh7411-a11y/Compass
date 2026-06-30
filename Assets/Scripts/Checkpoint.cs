using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Checkpoint : MonoBehaviour
{
    public bool isPassed = false;

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D checkpointCollider;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        checkpointCollider = GetComponent<CircleCollider2D>();
        HideVisual();
    }

    public void Initialize()
    {
        isPassed = false;
        HideVisual();
    }

    public void MarkPassed()
    {
        if (isPassed)
        {
            return;
        }

        isPassed = true;
        ScoreManager.Instance?.Refresh();
    }

    public bool IsTouchedByTrail(Vector3[] trailPositions, int positionCount, IReadOnlyList<int> strokeStartIndices, float hitTolerance)
    {
        if (isPassed || trailPositions == null || positionCount < 2)
        {
            return false;
        }

        Vector2 center = GetCheckpointCenter();
        float hitToleranceSq = Mathf.Max(0.0001f, hitTolerance * hitTolerance);
        int strokeCount = strokeStartIndices != null && strokeStartIndices.Count > 0 ? strokeStartIndices.Count : 1;

        for (int strokeIndex = 0; strokeIndex < strokeCount; strokeIndex++)
        {
            int startIndex = strokeStartIndices != null && strokeStartIndices.Count > 0
                ? Mathf.Clamp(strokeStartIndices[strokeIndex], 0, positionCount)
                : 0;
            int endExclusive = strokeIndex + 1 < strokeCount
                ? Mathf.Clamp(strokeStartIndices[strokeIndex + 1], 0, positionCount)
                : positionCount;

            for (int i = startIndex + 1; i < endExclusive; i++)
            {
                float distanceSq = DistancePointToSegmentSquared(center, trailPositions[i - 1], trailPositions[i]);
                if (distanceSq <= hitToleranceSq)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void HideVisual()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
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
