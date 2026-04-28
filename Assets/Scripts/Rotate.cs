using UnityEngine;

public class Rotate : MonoBehaviour
{
    public GameObject target1; 
    public GameObject target2; 
    public float orbitSpeed = 50.0f; 
    public float fixedRadius = 3.0f;

    private TrailRenderer trail;
    private Transform currentTarget;

    void Start()
    {

        trail = GetComponent<TrailRenderer>();
        trail.emitting = false;

        currentTarget = target1.transform;
        SetPositionByRadius(currentTarget);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            currentTarget = (currentTarget == target1.transform) ? target2.transform : target1.transform;
            SetPositionByRadius(currentTarget);
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            currentTarget = (currentTarget == target1.transform) ? target2.transform : target1.transform;
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

        if (currentTarget != null)
        {

            transform.RotateAround(currentTarget.position, Vector3.forward, orbitSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.Space))
        {
            trail.emitting = true;
        }
        else
        {
            trail.emitting = false;
        }
    }

    void SetPositionByRadius(Transform newTarget)
    {
        if (newTarget == null) return;
        Vector3 direction = (transform.position - newTarget.position).normalized;

        if (direction == Vector3.zero) direction = Vector3.right;
        transform.position = newTarget.position + (direction * fixedRadius);
    }
}