using UnityEngine;

public class ConnectedLine : MonoBehaviour
{
    public Transform target1;
    public Transform target2;
    public Transform satellite;

    private Transform currentTarget;

	void Start()
    {
        currentTarget = target1 != null ? target1 : target2;
    }

	void Update()
    {
        if (target1 == null || target2 == null || satellite == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            currentTarget = (currentTarget == target1) ? target2 : target1;
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            currentTarget = (currentTarget == target2) ? target1 : target2;
        }

        if (currentTarget != null && satellite != null)
        {
            float distance = Vector3.Distance(currentTarget.position, satellite.position);

            transform.position = (currentTarget.position + satellite.position) / 2.0f;

            transform.localScale = new Vector3(distance, transform.localScale.y, transform.localScale.z);

            Vector3 direction = satellite.position - currentTarget.position;
            transform.right = direction;
        }
    }
}
