using UnityEngine;

[DisallowMultipleComponent]
public class Checkpoint : MonoBehaviour
{
    public int index;
    public bool isPassed = false;

    private SpriteRenderer spriteRenderer;
    private SplineGuide owningGuide;
    private Color defaultColor = Color.white;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            defaultColor = spriteRenderer.color;
        }
    }

    public void Initialize(int checkpointIndex, SplineGuide guide)
    {
        index = checkpointIndex;
        owningGuide = guide;
        isPassed = false;
        SetColor(defaultColor);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerBrush"))
        {
            MarkPassed();
        }
    }

    public void MarkPassed()
    {
        if (isPassed)
        {
            return;
        }

        isPassed = true;
        SetColor(Color.green);
        ScoreManager.Instance?.Refresh();
    }

    public float CalculateAccuracy()
    {
        if (owningGuide != null)
        {
            return owningGuide.CalculateAccuracy();
        }

        if (ScoreManager.Instance != null)
        {
            return ScoreManager.Instance.GetAccuracy();
        }

        return isPassed ? 100f : 0f;
    }

    private void SetColor(Color color)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }
}
