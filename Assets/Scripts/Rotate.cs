using UnityEngine;

public class Rotate : MonoBehaviour
{
    public GameObject target1; // 중심이 될 오브젝트1
    public GameObject target2; // 중심이 될 오브젝트2
    public float orbitSpeed = 50.0f; // 회전 속도
    public float fixedRadius = 3.0f;

    private TrailRenderer trail;
    private Transform currentTarget;

    void Start()
    {

        // 오브젝트에 붙어있는 TrailRenderer를 가져옵니다.
        trail = GetComponent<TrailRenderer>();
        // 시작할 때는 선이 그려지지 않게 설정
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

        if (currentTarget != null)
        {
            // RotateAround(중심점, 회전축, 회전각도)
            // Vector3.up은 Y축을 기준으로 회전하게 합니다.
            transform.RotateAround(currentTarget.position, Vector3.forward, orbitSpeed * Time.deltaTime);
        }

        // 2. 스페이스바 입력 감지
        if (Input.GetKey(KeyCode.Space))
        {
            trail.emitting = true;  // 누르는 동안 선 생성
        }
        else
        {
            trail.emitting = false; // 떼면 선 생성 중단
        }
    }

    void SetPositionByRadius(Transform newTarget)
    {
        if (newTarget == null) return;

        // 1. 중심에서 현재 오브젝트를 바라보는 방향 벡터를 구함
        Vector3 direction = (transform.position - newTarget.position).normalized;

        // 만약 오브젝트와 중심이 완전히 같은 위치에 있다면 방향을 잡을 수 없으므로 기본 방향 설정
        if (direction == Vector3.zero) direction = Vector3.right;

        // 2. 새로운 중심 위치 + (방향 * 고정 반지름)으로 위치 이동
        transform.position = newTarget.position + (direction * fixedRadius);
    }
}