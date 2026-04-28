using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public int index;
    public bool isPassed = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerBrush"))
        {
            isPassed = true;
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.green;
            }
        }
    }

    public float CalculateAccuracy()
    {
        Checkpoint[] cps = GetComponentsInChildren<Checkpoint>();
        
        // 0으로 나누기(Divide by Zero) 오류 방지
        if (cps.Length == 0)
        {
            return 0f;
        }

        int passedCount = 0;
        foreach(Checkpoint cp in cps) if(cp.isPassed) passedCount++;

        return (float)passedCount / cps.Length * 100f;
    }
}