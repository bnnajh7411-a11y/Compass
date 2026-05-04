using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class SplineGuide : MonoBehaviour
{
    public SplineContainer splineContainer;
    public GameObject checkpointPrefab;
    public int resolution = 20;

    [Header("Orbit Centers")]
    // World-space offsets so the guide scale does not stretch the center positions.
    [SerializeField] private Vector3 center1WorldOffset = new Vector3(-2f, 0f, 0f);
    [SerializeField] private Vector3 center2WorldOffset = new Vector3(2f, 0f, 0f);

    [SerializeField] private Transform checkpointRoot;
    [SerializeField, Min(8)] private int accuracyResolution = 120;
    [SerializeField, Min(0.01f)] private float checkpointHitTolerance = 0.04f;
    [SerializeField, Min(0.01f)] private float coverageTolerance = 0.045f;
    [SerializeField, Min(0.01f)] private float perfectTrailTolerance = 0.04f;
    [SerializeField, Min(0.01f)] private float missTrailTolerance = 0.085f;

    private readonly List<SplineSamplePath> accuracySamplePaths = new List<SplineSamplePath>();
    private bool hasBuilt;

    private sealed class SplineSamplePath
    {
        public SplineSamplePath(List<Vector2> points, bool closed)
        {
            Points = points;
            Closed = closed;
        }

        public List<Vector2> Points { get; }
        public bool Closed { get; }
    }

    public float CheckpointHitTolerance => checkpointHitTolerance;
    public float CoverageTolerance => coverageTolerance;
    public float PerfectTrailTolerance => perfectTrailTolerance;
    public Vector3 Center1WorldPosition => transform.position + center1WorldOffset;
    public Vector3 Center2WorldPosition => transform.position + center2WorldOffset;
    public float MissTrailTolerance
    {
        get => Mathf.Max(missTrailTolerance, perfectTrailTolerance + 0.001f);
    }
    public int AccuracySampleCount
    {
        get
        {
            int totalCount = 0;
            for (int i = 0; i < accuracySamplePaths.Count; i++)
            {
                List<Vector2> points = accuracySamplePaths[i].Points;
                if (points != null)
                {
                    totalCount += points.Count;
                }
            }

            return totalCount;
        }
    }

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
            GenerateCheckpoints(false);
        }
    }

    public void RebuildCheckpoints()
    {
        GenerateCheckpoints(true);
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

        accuracySamplePaths.Clear();

        Transform root = EnsureCheckpointRoot();
        ClearGeneratedCheckpoints(root);

        if (!TryGetUsableSplines(out IReadOnlyList<Spline> splines))
        {
            Debug.LogWarning($"{name}: No usable splines were found in the SplineContainer.", this);
            hasBuilt = true;
            ScoreManager.Instance?.Refresh();
            return;
        }

        for (int splineIndex = 0; splineIndex < splines.Count; splineIndex++)
        {
            Spline spline = splines[splineIndex];
            if (spline == null || spline.Count < 1)
            {
                continue;
            }

            int count = Mathf.Max(2, resolution);
            int pointCount = spline.Closed ? count : count + 1;
            List<Vector2> sampledPoints = new List<Vector2>(pointCount);

            for (int i = 0; i < pointCount; i++)
            {
                float t = (float)i / count;
                Vector3 worldPos = splineContainer.EvaluatePosition(splineIndex, t);
                sampledPoints.Add(new Vector2(worldPos.x, worldPos.y));

                GameObject checkpointObject = Instantiate(checkpointPrefab, worldPos, Quaternion.identity, root);
                checkpointObject.SetActive(true);

                Checkpoint checkpoint = checkpointObject.GetComponent<Checkpoint>();
                if (checkpoint == null)
                {
                    continue;
                }

                checkpoint.Initialize();
                ScoreManager.Instance?.RegisterCheckpoint(checkpoint);
            }

            accuracySamplePaths.Add(new SplineSamplePath(sampledPoints, spline.Closed));
        }

        RebuildAccuracySamples();
        hasBuilt = true;
        ScoreManager.Instance?.Refresh();
    }

    private void ClearGeneratedCheckpoints(Transform root)
    {
        ScoreManager.Instance?.Clear();

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
        if (trailPositions == null || positionCount < 2 || accuracySamplePaths.Count == 0)
        {
            return 0;
        }

        int coveredCount = 0;

        float toleranceSq = CoverageTolerance * CoverageTolerance;
        foreach (SplineSamplePath path in accuracySamplePaths)
        {
            if (path == null || path.Points == null)
            {
                continue;
            }

            foreach (Vector2 sample in path.Points)
            {
                if (IsTrailNearPoint(trailPositions, positionCount, sample, toleranceSq))
                {
                    coveredCount++;
                }
            }
        }

        return coveredCount;
    }

    public float GetDistanceToGuide(Vector2 point)
    {
        if (accuracySamplePaths.Count == 0)
        {
            return float.PositiveInfinity;
        }

        float bestDistanceSq = float.PositiveInfinity;
        for (int pathIndex = 0; pathIndex < accuracySamplePaths.Count; pathIndex++)
        {
            SplineSamplePath path = accuracySamplePaths[pathIndex];
            if (path == null || path.Points == null || path.Points.Count == 0)
            {
                continue;
            }

            if (path.Points.Count == 1)
            {
                float singlePointDistanceSq = (point - path.Points[0]).sqrMagnitude;
                if (singlePointDistanceSq < bestDistanceSq)
                {
                    bestDistanceSq = singlePointDistanceSq;
                }
                continue;
            }

            int segmentCount = path.Closed ? path.Points.Count : path.Points.Count - 1;
            for (int i = 0; i < segmentCount; i++)
            {
                int nextIndex = path.Closed ? (i + 1) % path.Points.Count : i + 1;
                float distanceSq = DistancePointToSegmentSquared(point, path.Points[i], path.Points[nextIndex]);
                if (distanceSq < bestDistanceSq)
                {
                    bestDistanceSq = distanceSq;
                }
            }
        }

        return Mathf.Sqrt(bestDistanceSq);
    }

    private void RebuildAccuracySamples()
    {
        accuracySamplePaths.Clear();

        if (splineContainer == null)
        {
            return;
        }

        if (!TryGetUsableSplines(out IReadOnlyList<Spline> splines))
        {
            return;
        }

        int count = Mathf.Max(8, accuracyResolution);
        for (int splineIndex = 0; splineIndex < splines.Count; splineIndex++)
        {
            Spline spline = splines[splineIndex];
            if (spline == null || spline.Count < 1)
            {
                continue;
            }

            int pointCount = spline.Closed ? count : count + 1;
            List<Vector2> sampledPoints = new List<Vector2>(pointCount);
            for (int i = 0; i < pointCount; i++)
            {
                float t = (float)i / count;
                Vector3 worldPos = splineContainer.EvaluatePosition(splineIndex, t);
                sampledPoints.Add(new Vector2(worldPos.x, worldPos.y));
            }

            accuracySamplePaths.Add(new SplineSamplePath(sampledPoints, spline.Closed));
        }
    }

    private bool TryGetUsableSplines(out IReadOnlyList<Spline> splines)
    {
        splines = null;

        if (splineContainer == null || splineContainer.Splines == null)
        {
            return false;
        }

        bool hasUsableSpline = false;
        splines = splineContainer.Splines;
        for (int i = 0; i < splines.Count; i++)
        {
            Spline spline = splines[i];
            if (spline != null && spline.Count >= 1)
            {
                hasUsableSpline = true;
                break;
            }
        }

        return hasUsableSpline;
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
