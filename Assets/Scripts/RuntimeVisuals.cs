using System.Collections.Generic;
using UnityEngine;

public static class RuntimeSpriteFactory
{
    private const string LogoResourcesPath = "KWC-logo_png";
    private static Sprite whiteSprite;
    private static Sprite circleSprite;
    private static Sprite logoSprite;
    private static readonly Dictionary<string, Sprite> roundedRectSprites = new Dictionary<string, Sprite>();

    public static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null)
        {
            return whiteSprite;
        }

        Texture2D texture = Texture2D.whiteTexture;
        whiteSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        whiteSprite.name = "RuntimeWhiteSprite";
        whiteSprite.hideFlags = HideFlags.HideAndDontSave;
        return whiteSprite;
    }

    public static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
        {
            return circleSprite;
        }

        const int textureSize = 256;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.name = "RuntimeCircleTexture";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color32[] pixels = new Color32[textureSize * textureSize];
        float center = (textureSize - 1) * 0.5f;
        float radius = center - 1f;
        float edgeWidth = 1.5f;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                float alpha = 0f;

                if (distance < radius)
                {
                    float t = Mathf.Clamp01((radius - distance) / edgeWidth);
                    alpha = t * t * (3f - (2f * t));
                }

                pixels[(y * textureSize) + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        circleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            100f);
        circleSprite.name = "RuntimeCircleSprite";
        circleSprite.hideFlags = HideFlags.HideAndDontSave;
        return circleSprite;
    }

    public static Sprite GetLogoSprite()
    {
        if (logoSprite != null)
        {
            return logoSprite;
        }

        Sprite[] sprites = Resources.LoadAll<Sprite>(LogoResourcesPath);
        if (sprites != null && sprites.Length > 0)
        {
            Sprite largestSprite = GetLargestSprite(sprites);
            if (largestSprite == null)
            {
                logoSprite = GetWhiteSprite();
                return logoSprite;
            }

            Texture2D texture = largestSprite.texture;
            if (texture != null)
            {
                Rect combinedRect = GetCombinedSpriteRect(sprites, largestSprite.rect);
                return CreateLogoSprite(texture, combinedRect, largestSprite.pixelsPerUnit);
            }

            logoSprite = largestSprite;
            return logoSprite;
        }

        Texture2D loadedTexture = Resources.Load<Texture2D>(LogoResourcesPath);
        if (loadedTexture != null)
        {
            return CreateLogoSprite(
                loadedTexture,
                new Rect(0f, 0f, loadedTexture.width, loadedTexture.height),
                100f);
        }

        logoSprite = GetWhiteSprite();
        return logoSprite;
    }

    public static Sprite GetRoundedRectSprite(int width, int height, float cornerRadius)
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
        cornerRadius = Mathf.Clamp(cornerRadius, 0f, Mathf.Min(width, height) * 0.5f);

        string cacheKey = $"{width}x{height}_r{cornerRadius:0.##}";
        if (roundedRectSprites.TryGetValue(cacheKey, out Sprite cachedSprite) && cachedSprite != null)
        {
            return cachedSprite;
        }

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = $"RuntimeRoundedRectTexture_{cacheKey}";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color32[] pixels = new Color32[width * height];
        float centerX = (width - 1) * 0.5f;
        float centerY = (height - 1) * 0.5f;
        float edgeWidth = 1.5f;

        for (int y = 0; y < height; y++)
        {
            float py = y - centerY;

            for (int x = 0; x < width; x++)
            {
                float px = x - centerX;
                float qx = Mathf.Abs(px) - ((width - 1) * 0.5f - cornerRadius);
                float qy = Mathf.Abs(py) - ((height - 1) * 0.5f - cornerRadius);

                float outsideX = Mathf.Max(qx, 0f);
                float outsideY = Mathf.Max(qy, 0f);
                float distance = Mathf.Sqrt((outsideX * outsideX) + (outsideY * outsideY)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - cornerRadius;

                float alpha = distance <= 0f
                    ? 1f
                    : 1f - Mathf.SmoothStep(0f, edgeWidth, Mathf.Min(distance, edgeWidth));

                pixels[(y * width) + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            100f);
        sprite.name = $"RuntimeRoundedRectSprite_{cacheKey}";
        sprite.hideFlags = HideFlags.HideAndDontSave;

        roundedRectSprites[cacheKey] = sprite;
        return sprite;
    }

    private static Sprite GetLargestSprite(IEnumerable<Sprite> sprites)
    {
        Sprite largestSprite = null;
        float largestArea = 0f;

        foreach (Sprite sprite in sprites)
        {
            if (sprite == null)
            {
                continue;
            }

            float area = sprite.rect.width * sprite.rect.height;
            if (largestSprite == null || area > largestArea)
            {
                largestSprite = sprite;
                largestArea = area;
            }
        }

        return largestSprite;
    }

    private static Rect GetCombinedSpriteRect(IEnumerable<Sprite> sprites, Rect fallbackRect)
    {
        bool hasValidRect = false;
        float minX = 0f;
        float minY = 0f;
        float maxX = 0f;
        float maxY = 0f;

        foreach (Sprite sprite in sprites)
        {
            if (sprite == null)
            {
                continue;
            }

            Rect rect = sprite.rect;
            if (!hasValidRect)
            {
                minX = rect.xMin;
                minY = rect.yMin;
                maxX = rect.xMax;
                maxY = rect.yMax;
                hasValidRect = true;
                continue;
            }

            minX = Mathf.Min(minX, rect.xMin);
            minY = Mathf.Min(minY, rect.yMin);
            maxX = Mathf.Max(maxX, rect.xMax);
            maxY = Mathf.Max(maxY, rect.yMax);
        }

        return hasValidRect
            ? Rect.MinMaxRect(minX, minY, maxX, maxY)
            : fallbackRect;
    }

    private static Sprite CreateLogoSprite(Texture2D texture, Rect textureRect, float pixelsPerUnit)
    {
        logoSprite = Sprite.Create(
            texture,
            textureRect,
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit > 0f ? pixelsPerUnit : 100f);
        logoSprite.name = "RuntimeKWCLogoSprite";
        logoSprite.hideFlags = HideFlags.HideAndDontSave;
        return logoSprite;
    }
}

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
        fillMaskRect.sizeDelta = new Vector2(innerWidth * normalized, innerHeight);
    }
}
