using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class SplineGuide : MonoBehaviour
{
    public SplineContainer splineContainer;
    public GameObject checkpointPrefab;
    public int resolution = 20;

    [SerializeField] private Transform checkpointRoot;
    [SerializeField, Min(8)] private int accuracyResolution = 120;
    [SerializeField, Min(0.01f)] private float checkpointHitTolerance = 0.04f;
    [SerializeField, Min(0.01f)] private float coverageTolerance = 0.045f;
    [SerializeField, Min(0.01f)] private float perfectTrailTolerance = 0.04f;
    [SerializeField, Min(0.01f)] private float missTrailTolerance = 0.085f;

    private readonly List<Checkpoint> generatedCheckpoints = new List<Checkpoint>();
    private readonly List<Vector2> accuracySamples = new List<Vector2>();
    private bool hasBuilt;

    public float CheckpointHitTolerance => checkpointHitTolerance;
    public float CoverageTolerance => coverageTolerance;
    public float PerfectTrailTolerance => perfectTrailTolerance;
    public float MissTrailTolerance => Mathf.Max(missTrailTolerance, perfectTrailTolerance + 0.001f);
    public int AccuracySampleCount => accuracySamples.Count;

    void Awake()
    {
        if (splineContainer == null)
        {
            splineContainer = GetComponent<SplineContainer>();
        }
    }

    void Start()
    {
        if (!hasBuilt)
        {
            GenerateCheckpoints();
        }
    }

    public void GenerateCheckpoints()
    {
        GenerateCheckpoints(false);
    }

    public void RebuildCheckpoints()
    {
        GenerateCheckpoints(true);
    }

    public float CalculateAccuracy()
    {
        if (ScoreManager.Instance != null)
        {
            return ScoreManager.Instance.GetAccuracy();
        }

        int totalCount = 0;
        int passedCount = 0;

        foreach (Checkpoint checkpoint in generatedCheckpoints)
        {
            if (checkpoint == null)
            {
                continue;
            }

            totalCount++;
            if (checkpoint.isPassed)
            {
                passedCount++;
            }
        }

        if (totalCount == 0)
        {
            return 0f;
        }

        return (float)passedCount / totalCount * 100f;
    }

    private void GenerateCheckpoints(bool force)
    {
        if (splineContainer == null)
        {
            Debug.LogWarning($"{name}: SplineContainer is missing.", this);
            return;
        }

        if (hasBuilt && !force)
        {
            return;
        }

        if (checkpointPrefab == null)
        {
            checkpointPrefab = CreateDefaultCheckpointPrefab();
        }

        Transform root = EnsureCheckpointRoot();
        ClearGeneratedCheckpoints(root);

        int count = Mathf.Max(2, resolution);
        for (int i = 0; i <= count; i++)
        {
            float t = (float)i / count;
            Vector3 worldPos = splineContainer.EvaluatePosition(t);

            GameObject checkpointObject = Instantiate(checkpointPrefab, worldPos, Quaternion.identity, root);
            checkpointObject.SetActive(true);

            Checkpoint checkpoint = checkpointObject.GetComponent<Checkpoint>();
            if (checkpoint == null)
            {
                continue;
            }

            checkpoint.Initialize(i, this);
            generatedCheckpoints.Add(checkpoint);
            ScoreManager.Instance?.RegisterCheckpoint(checkpoint);
        }

        RebuildAccuracySamples();
        hasBuilt = true;
        ScoreManager.Instance?.Refresh();
    }

    private void ClearGeneratedCheckpoints(Transform root)
    {
        ScoreManager.Instance?.Clear();
        generatedCheckpoints.Clear();

        if (root == null)
        {
            return;
        }

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
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

    private Transform EnsureCheckpointRoot()
    {
        if (checkpointRoot != null)
        {
            return checkpointRoot;
        }

        Transform existing = transform.Find("Generated Checkpoints");
        if (existing != null)
        {
            checkpointRoot = existing;
            return checkpointRoot;
        }

        GameObject rootObject = new GameObject("Generated Checkpoints");
        rootObject.transform.SetParent(transform, false);
        checkpointRoot = rootObject.transform;
        return checkpointRoot;
    }

    private GameObject CreateDefaultCheckpointPrefab()
    {
        GameObject checkpointObject = new GameObject("CheckpointTemplate");
        checkpointObject.transform.SetParent(transform, false);
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

    public int CountCoveredSamples(Vector3[] trailPositions, int positionCount)
    {
        if (trailPositions == null || positionCount < 2 || accuracySamples.Count == 0)
        {
            return 0;
        }

        float toleranceSq = CoverageTolerance * CoverageTolerance;
        int coveredCount = 0;

        foreach (Vector2 sample in accuracySamples)
        {
            if (IsTrailNearPoint(trailPositions, positionCount, sample, toleranceSq))
            {
                coveredCount++;
            }
        }

        return coveredCount;
    }

    public float GetDistanceToGuide(Vector2 point)
    {
        if (accuracySamples.Count == 0)
        {
            return float.PositiveInfinity;
        }

        if (accuracySamples.Count == 1)
        {
            return Vector2.Distance(point, accuracySamples[0]);
        }

        float bestDistanceSq = float.PositiveInfinity;
        for (int i = 1; i < accuracySamples.Count; i++)
        {
            float distanceSq = DistancePointToSegmentSquared(point, accuracySamples[i - 1], accuracySamples[i]);
            if (distanceSq < bestDistanceSq)
            {
                bestDistanceSq = distanceSq;
            }
        }

        return Mathf.Sqrt(bestDistanceSq);
    }

    private void RebuildAccuracySamples()
    {
        accuracySamples.Clear();

        if (splineContainer == null || splineContainer.Spline == null)
        {
            return;
        }

        int count = Mathf.Max(8, accuracyResolution);
        for (int i = 0; i <= count; i++)
        {
            float t = (float)i / count;
            Vector3 worldPos = splineContainer.EvaluatePosition(t);
            accuracySamples.Add(new Vector2(worldPos.x, worldPos.y));
        }
    }

    private static bool IsTrailNearPoint(Vector3[] trailPositions, int positionCount, Vector2 point, float toleranceSq)
    {
        for (int i = 1; i < positionCount; i++)
        {
            if (DistancePointToSegmentSquared(point, trailPositions[i - 1], trailPositions[i]) <= toleranceSq)
            {
                return true;
            }
        }

        return false;
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
