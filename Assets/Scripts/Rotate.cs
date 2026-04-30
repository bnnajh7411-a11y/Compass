using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class Rotate : MonoBehaviour
{
    public GameObject target1;
    public GameObject target2;
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
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))
        {
            ToggleTarget();
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
        if (target1 != null)
        {
            return target1.transform;
        }

        if (target2 != null)
        {
            return target2.transform;
        }

        return null;
    }

    private void ToggleTarget()
    {
        if (target1 == null || target2 == null)
        {
            return;
        }

        if (currentTarget == target1.transform)
        {
            currentTarget = target2.transform;
        }
        else
        {
            currentTarget = target1.transform;
        }
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
}
