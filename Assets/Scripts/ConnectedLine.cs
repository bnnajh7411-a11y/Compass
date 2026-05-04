using UnityEngine;

[DisallowMultipleComponent]
public class ConnectedLine : MonoBehaviour
{
    [SerializeField] private Rotate rotate;
    private Transform satellite;

    private void Awake()
    {
        if (rotate == null)
        {
            rotate = Object.FindFirstObjectByType<Rotate>();
        }

        satellite = rotate != null ? rotate.transform : null;
    }

    private void LateUpdate()
    {
        if (rotate == null)
        {
            return;
        }

        Transform currentTarget = rotate.CurrentTargetTransform;
        if (currentTarget == null || satellite == null)
        {
            return;
        }

        float distance = Vector3.Distance(currentTarget.position, satellite.position);

        transform.position = (currentTarget.position + satellite.position) / 2.0f;

        transform.localScale = new Vector3(distance, transform.localScale.y, transform.localScale.z);

        Vector3 direction = satellite.position - currentTarget.position;
        transform.right = direction;
    }
}
