using UnityEngine;
using UnityEngine.Splines;

public class SplineGuide : MonoBehaviour
{
    public SplineContainer splineContainer;
    public GameObject checkpointPrefab; // Trigger가 붙은 작은 원형 프리팹
    public int resolution = 20; // 생성할 체크포인트 개수

    void Start()
    {
        var spline = splineContainer.Spline;
        for (int i = 0; i <= resolution; i++)
        {
            float t = (float)i / resolution;
            Vector3 worldPos = splineContainer.EvaluatePosition(t);
            
            GameObject cp = Instantiate(checkpointPrefab, worldPos, Quaternion.identity, transform);
            cp.GetComponent<Checkpoint>().index = i; // 순서 부여
        }
    }
}
