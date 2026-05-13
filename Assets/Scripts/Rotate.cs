using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(TrailRenderer))]
public class Rotate : MonoBehaviour
{
    private const string ResultSceneName = "Result";

    public GameObject target1;
    public GameObject target2;
    public GameObject target3;
    public float orbitSpeed = 50.0f;
    public float fixedRadius = 3.0f;

    private TrailRenderer trail;
    private Rigidbody2D body2D;
    private CircleCollider2D brushCollider;
    private Transform currentTarget;
    private Vector3[] trailPositionsBuffer;

    void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        body2D = GetComponent<Rigidbody2D>();
        brushCollider = GetComponent<CircleCollider2D>();
        EnsurePlayerBrushSetup();
    }

    void Start()
    {
        if (trail != null)
        {
            trail.emitting = false;
        }

        currentTarget = GetInitialTarget();
        SetPositionByRadius(currentTarget);
    }

    void Update()
    {
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

        bool targetChanged = false;
        if (Input.GetKeyDown(KeyCode.A))
        {
            StepTarget(-1);
            targetChanged = true;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            StepTarget(1);
            targetChanged = true;
        }

        if (targetChanged)
        {
            SetPositionByRadius(currentTarget);
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            fixedRadius += 0.5f;
            if (fixedRadius > 10.0f) fixedRadius = 10.0f;
            SetPositionByRadius(currentTarget);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            fixedRadius -= 0.5f;
            if (fixedRadius < 1.0f) fixedRadius = 1.0f;
            SetPositionByRadius(currentTarget);
        }

        if (trail != null)
        {
            trail.emitting = Input.GetKey(KeyCode.Space);
        }

        EvaluateTrailAccuracy();
    }

    void FixedUpdate()
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

    void SetPositionByRadius(Transform newTarget)
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
        if (trail == null || ScoreManager.Instance == null)
        {
            return;
        }

        if (!trail.emitting)
        {
            return;
        }

        int positionCount = trail.positionCount;
        if (positionCount < 2)
        {
            return;
        }

        if (trailPositionsBuffer == null || trailPositionsBuffer.Length < positionCount)
        {
            trailPositionsBuffer = new Vector3[positionCount];
        }

        int copiedPositions = trail.GetPositions(trailPositionsBuffer);
        if (copiedPositions <= 0)
        {
            return;
        }

        ScoreManager.Instance.EvaluateTrail(trailPositionsBuffer, copiedPositions);
    }

    private void SubmitResultAndLoadScene()
    {
        float accuracy = 0f;
        string progressText = "Accuracy 0% (0/0) | Line 0%";

        if (ScoreManager.Instance != null)
        {
            accuracy = ScoreManager.Instance.GetAccuracy();
            progressText = ScoreManager.Instance.GetProgressText();
        }

        GameManager.StoreResult(accuracy, progressText);
        SceneManager.LoadScene(ResultSceneName);
    }

    private void ClearCurrentTrail()
    {
        if (trail != null)
        {
            trail.Clear();
        }

        trailPositionsBuffer = null;
    }
}
