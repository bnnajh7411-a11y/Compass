using UnityEngine;

public static class RuntimeSpriteFactory
{
    private static Sprite whiteSprite;

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
}
