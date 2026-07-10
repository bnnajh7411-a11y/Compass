using UnityEngine;

[DisallowMultipleComponent]
public class ConnectedLine : MonoBehaviour
{
    [SerializeField] private Rotate rotate;

    private void Awake()
    {
        if (rotate == null)
        {
            rotate = Object.FindFirstObjectByType<Rotate>();
        }
        enabled = rotate != null;
    }

    private void LateUpdate()
    {
        if (rotate == null)
        {
            return;
        }

        Transform currentTarget = rotate.CurrentTargetTransform;
        if (currentTarget == null)
        {
            return;
        }

        Vector3 currentTargetPosition = currentTarget.position;
        Vector3 satellitePosition = rotate.transform.position;
        Vector3 direction = satellitePosition - currentTargetPosition;

        transform.position = currentTargetPosition + (direction * 0.5f);
        transform.right = direction;

        Vector3 localScale = transform.localScale;
        localScale.x = direction.magnitude;
        transform.localScale = localScale;
    }
}
