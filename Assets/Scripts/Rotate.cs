using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class Rotate : MonoBehaviour
{
    private const float RadiusStep = 0.5f;
    private const float MinRadius = 1f;
    private const float MaxRadius = 10f;
    private const float ScoringSampleDistance = 0.02f;

    public GameObject target1;
    public GameObject target2;
    public GameObject target3;
    public float orbitSpeed = 50.0f;
    public float fixedRadius = 3.0f;
    [SerializeField] private AudioClip drawSEClip;

    private TrailRenderer trail;
    private Rigidbody2D body2D;
    private CircleCollider2D brushCollider;
    private Transform currentTarget;
    private Vector3[] scoredTrailPointsBuffer;
    private readonly List<Vector3> scoredTrailPoints = new List<Vector3>();
    private readonly List<int> scoredStrokeStartIndices = new List<int>();
    private bool isTransitioning;
    private bool isDrawingInputActive;
    private bool scoringDirty;
    private bool wasDrawingLastFrame;

    private void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        body2D = GetComponent<Rigidbody2D>();
        brushCollider = GetComponent<CircleCollider2D>();
        GameManager.RegisterDrawSEClip(drawSEClip);
        EnsurePlayerBrushSetup();
    }

    private void Start()
    {
        if (trail != null)
        {
            trail.emitting = false;
        }

        currentTarget = GetInitialTarget();
        SetPositionByRadius(currentTarget);
    }

    private void OnDisable()
    {
        if (GameManager.HasInstance)
        {
            GameManager.SetDrawSESoundActive(false);
        }
    }

    private void Update()
    {
        if (GameManager.IsPaused)
        {
            if (trail != null)
            {
                trail.emitting = false;
            }

            isDrawingInputActive = false;
            wasDrawingLastFrame = false;
            GameManager.SetDrawSESoundActive(false);
            return;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ClearCurrentTrail();
            ScoreManager.Instance?.ResetAttempt();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SubmitResultAndLoadScene();
            return;
        }

        bool shouldPlayMainControlSound = UpdateTargetSelection();
        shouldPlayMainControlSound |= UpdateRadiusInput();

        if (shouldPlayMainControlSound)
        {
            GameManager.PlayUiSound(RuntimeButtonSoundEffect.MainControl);
        }

        UpdateDrawingState();
        EvaluateTrailAccuracy();
    }

    private void FixedUpdate()
    {
        if (currentTarget == null)
        {
            return;
        }

        Vector2 center = (Vector2)currentTarget.position;
        Vector2 offset = GetCurrentPosition() - center;
        if (offset.sqrMagnitude < 0.0001f)
        {
            offset = Vector2.right;
        }

        Vector2 rotatedDirection = (Vector2)(Quaternion.Euler(0f, 0f, orbitSpeed * Time.fixedDeltaTime) * offset.normalized);
        MoveBrushTo(center + (rotatedDirection * fixedRadius));

        if (isDrawingInputActive)
        {
            CaptureScoredPoint(GetCurrentPosition());
        }
    }

    private bool UpdateTargetSelection()
    {
        bool targetChanged = false;

        if (Input.GetKeyDown(KeyCode.A))
        {
            targetChanged |= TryStepTarget(-1);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            targetChanged |= TryStepTarget(1);
        }

        if (targetChanged)
        {
            SetPositionByRadius(currentTarget);
        }

        return targetChanged;
    }

    private bool UpdateRadiusInput()
    {
        bool radiusChanged = false;

        if (Input.GetKeyDown(KeyCode.W))
        {
            radiusChanged |= TryAdjustRadius(RadiusStep);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            radiusChanged |= TryAdjustRadius(-RadiusStep);
        }

        return radiusChanged;
    }

    private void UpdateDrawingState()
    {
        bool isDrawing = Input.GetKey(KeyCode.Space);

        if (trail != null)
        {
            if (isDrawing && !wasDrawingLastFrame)
            {
                BeginScoredStroke();
            }

            trail.emitting = isDrawing;
        }

        if (!isDrawing && wasDrawingLastFrame)
        {
            CaptureScoredPoint(GetCurrentPosition(), true);
        }

        GameManager.SetDrawSESoundActive(isDrawing);
        isDrawingInputActive = isDrawing;
        wasDrawingLastFrame = isDrawing;
    }

    private bool TryStepTarget(int direction)
    {
        Transform previousTarget = currentTarget;
        StepTarget(direction);
        return currentTarget != previousTarget;
    }

    private bool TryAdjustRadius(float delta)
    {
        float nextRadius = Mathf.Clamp(fixedRadius + delta, MinRadius, MaxRadius);
        if (Mathf.Approximately(nextRadius, fixedRadius))
        {
            return false;
        }

        fixedRadius = nextRadius;
        SetPositionByRadius(currentTarget);
        return true;
    }

    public void EnsurePlayerBrushSetup()
    {
        gameObject.tag = "PlayerBrush";

        if (body2D == null)
        {
            body2D = GetComponent<Rigidbody2D>();
            if (body2D == null)
            {
                body2D = gameObject.AddComponent<Rigidbody2D>();
            }
        }

        body2D.bodyType = RigidbodyType2D.Kinematic;
        body2D.gravityScale = 0f;
        body2D.freezeRotation = true;
        body2D.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (brushCollider == null)
        {
            brushCollider = GetComponent<CircleCollider2D>();
            if (brushCollider == null)
            {
                brushCollider = gameObject.AddComponent<CircleCollider2D>();
            }
        }

        brushCollider.isTrigger = false;
        brushCollider.radius = 0.1f;
    }

    private Transform GetInitialTarget()
    {
        Transform[] targets = GetAvailableTargets();
        if (targets == null || targets.Length == 0)
        {
            return null;
        }

        return targets[0];
    }

    private void StepTarget(int direction)
    {
        Transform[] targets = GetAvailableTargets();
        if (targets == null || targets.Length == 0)
        {
            return;
        }

        if (direction == 0)
        {
            return;
        }

        if (currentTarget == null)
        {
            currentTarget = direction > 0 ? targets[0] : targets[targets.Length - 1];
            return;
        }

        int currentIndex = GetTargetIndex(targets, currentTarget);
        if (currentIndex < 0)
        {
            currentTarget = direction > 0 ? targets[0] : targets[targets.Length - 1];
            return;
        }

        int nextIndex = (currentIndex + direction) % targets.Length;
        if (nextIndex < 0)
        {
            nextIndex += targets.Length;
        }

        currentTarget = targets[nextIndex];
    }

    private Transform[] GetAvailableTargets()
    {
        int targetCount = 0;
        if (target1 != null)
        {
            targetCount++;
        }

        if (target2 != null)
        {
            targetCount++;
        }

        if (target3 != null)
        {
            targetCount++;
        }

        if (targetCount == 0)
        {
            return null;
        }

        Transform[] targets = new Transform[targetCount];
        int index = 0;

        if (target1 != null)
        {
            targets[index++] = target1.transform;
        }

        if (target2 != null)
        {
            targets[index++] = target2.transform;
        }

        if (target3 != null)
        {
            targets[index++] = target3.transform;
        }

        return targets;
    }

    private static int GetTargetIndex(Transform[] targets, Transform target)
    {
        if (targets == null)
        {
            return -1;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == target)
            {
                return i;
            }
        }

        return -1;
    }

    public Transform CurrentTargetTransform => currentTarget;

    public void ApplyTargetPositions(Vector3 target1Position, Vector3 target2Position, Vector3 target3Position)
    {
        if (target1 != null)
        {
            Vector3 position = target1Position;
            position.z = target1.transform.position.z;
            target1.transform.position = position;
        }

        if (target2 != null)
        {
            Vector3 position = target2Position;
            position.z = target2.transform.position.z;
            target2.transform.position = position;
        }

        if (target3 != null)
        {
            Vector3 position = target3Position;
            position.z = target3.transform.position.z;
            target3.transform.position = position;
        }

        if (currentTarget == null)
        {
            currentTarget = GetInitialTarget();
        }

        SetPositionByRadius(currentTarget);
    }

    private void SetPositionByRadius(Transform newTarget)
    {
        if (newTarget == null)
        {
            return;
        }

        Vector2 direction = GetCurrentPosition() - (Vector2)newTarget.position;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.right;
        }
        else
        {
            direction.Normalize();
        }

        MoveBrushTo((Vector2)newTarget.position + (direction * fixedRadius));
    }

    private void MoveBrushTo(Vector2 worldPosition)
    {
        if (body2D != null)
        {
            body2D.position = worldPosition;
            return;
        }

        transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
    }

    private Vector2 GetCurrentPosition()
    {
        if (body2D != null)
        {
            return body2D.position;
        }

        return transform.position;
    }

    private void EvaluateTrailAccuracy()
    {
        if (ScoreManager.Instance == null)
        {
            return;
        }

        if (!isDrawingInputActive && !scoringDirty)
        {
            return;
        }

        int positionCount = scoredTrailPoints.Count;
        if (positionCount > 0)
        {
            if (scoredTrailPointsBuffer == null || scoredTrailPointsBuffer.Length < positionCount)
            {
                scoredTrailPointsBuffer = new Vector3[positionCount];
            }

            scoredTrailPoints.CopyTo(scoredTrailPointsBuffer, 0);
        }

        ScoreManager.Instance.EvaluateTrail(positionCount > 0 ? scoredTrailPointsBuffer : null, positionCount, scoredStrokeStartIndices);
        scoringDirty = false;
    }

    public void SubmitResultAndLoadScene()
    {
        if (isTransitioning)
        {
            return;
        }

        isTransitioning = true;
        GameManager.SetDrawSESoundActive(false);
        EvaluateTrailAccuracy();

        float accuracy = 0f;

        if (ScoreManager.Instance != null)
        {
            accuracy = ScoreManager.Instance.GetAccuracy();
        }

        GameManager.CompleteRun(accuracy);
    }

    private void ClearCurrentTrail()
    {
        if (trail != null)
        {
            trail.Clear();
        }

        scoredTrailPoints.Clear();
        scoredStrokeStartIndices.Clear();
        scoredTrailPointsBuffer = null;
        isDrawingInputActive = false;
        scoringDirty = true;
        wasDrawingLastFrame = false;
    }

    private void BeginScoredStroke()
    {
        scoredStrokeStartIndices.Add(scoredTrailPoints.Count);
        CaptureScoredPoint(GetCurrentPosition(), true);
    }

    private void CaptureScoredPoint(Vector2 worldPosition, bool forceAdd = false)
    {
        Vector3 sample = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);

        if (scoredTrailPoints.Count == 0 || forceAdd)
        {
            if (scoredTrailPoints.Count == 0 || scoredTrailPoints[scoredTrailPoints.Count - 1] != sample || forceAdd)
            {
                scoredTrailPoints.Add(sample);
                scoringDirty = true;
            }

            return;
        }

        Vector3 lastPoint = scoredTrailPoints[scoredTrailPoints.Count - 1];
        Vector3 delta = sample - lastPoint;
        float distance = delta.magnitude;
        if (distance < Mathf.Epsilon)
        {
            return;
        }

        Vector3 direction = delta / distance;
        float travelled = ScoringSampleDistance;

        while (travelled <= distance)
        {
            scoredTrailPoints.Add(lastPoint + (direction * travelled));
            scoringDirty = true;
            travelled += ScoringSampleDistance;
        }

        if ((scoredTrailPoints[scoredTrailPoints.Count - 1] - sample).sqrMagnitude > 0.000001f)
        {
            scoredTrailPoints.Add(sample);
            scoringDirty = true;
        }
    }
}
