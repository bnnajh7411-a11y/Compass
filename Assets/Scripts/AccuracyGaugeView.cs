using UnityEngine;

[DisallowMultipleComponent]
public sealed class AccuracyGaugeView : MonoBehaviour
{
    private RectTransform fillMaskRect;
    private float innerWidth;
    private float innerHeight;

    public void Configure(RectTransform fillMaskRect, float innerWidth, float innerHeight)
    {
        this.fillMaskRect = fillMaskRect;
        this.innerWidth = Mathf.Max(0f, innerWidth);
        this.innerHeight = Mathf.Max(0f, innerHeight);

        SetAccuracy(0f);
    }

    public void SetAccuracy(float accuracy)
    {
        if (fillMaskRect == null)
        {
            return;
        }

        float normalized = Mathf.Clamp01(accuracy / 100f);
        float width = innerWidth * normalized;

        fillMaskRect.sizeDelta = new Vector2(width, innerHeight);
    }
}
