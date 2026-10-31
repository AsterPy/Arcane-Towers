using UnityEngine;

public class ArcaneHealthBar : MonoBehaviour
{
    private Transform fillTransform;
    private SpriteRenderer fillRenderer;
    private float maxHp = 100f;
    private float currentHp = 100f;
    private float initialWidth = 1f;

    public static ArcaneHealthBar Create(Transform parent, float offsetY, float width, float height, Color fillColor)
    {
        GameObject barObj = new GameObject("HealthBar");
        barObj.transform.SetParent(parent, false);
        barObj.transform.localPosition = new Vector3(0, offsetY, 0);

        // Outer Dark Border (Gives crisp, high-definition contrast)
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(barObj.transform, false);
        borderObj.transform.localScale = new Vector3(width + 0.02f, height + 0.015f, 1f);
        SpriteRenderer borderSr = borderObj.AddComponent<SpriteRenderer>();
        borderSr.sprite = GetDefaultSprite();
        borderSr.color = new Color(0.02f, 0.02f, 0.04f, 0.95f);
        borderSr.sortingOrder = 21;

        // Dark Background (Empty HP)
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(barObj.transform, false);
        bgObj.transform.localScale = new Vector3(width, height, 1f);
        SpriteRenderer bgSr = bgObj.AddComponent<SpriteRenderer>();
        bgSr.sprite = GetDefaultSprite();
        bgSr.color = new Color(0.12f, 0.12f, 0.15f, 0.9f);
        bgSr.sortingOrder = 22;

        // Health Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barObj.transform, false);
        fillObj.transform.localScale = new Vector3(width, height, 1f);
        SpriteRenderer fillSr = fillObj.AddComponent<SpriteRenderer>();
        fillSr.sprite = GetDefaultSprite();
        fillSr.color = fillColor;
        fillSr.sortingOrder = 23;

        ArcaneHealthBar bar = barObj.AddComponent<ArcaneHealthBar>();
        bar.fillTransform = fillObj.transform;
        bar.fillRenderer = fillSr;
        bar.initialWidth = width;
        return bar;
    }

    public void SetHealth(float current, float max)
    {
        maxHp = Mathf.Max(1f, max);
        currentHp = Mathf.Clamp(current, 0f, maxHp);

        if (fillTransform != null)
        {
            float percent = currentHp / maxHp;
            fillTransform.localScale = new Vector3(initialWidth * percent, fillTransform.localScale.y, 1f);
            float shift = (initialWidth * (1f - percent)) * 0.5f;
            fillTransform.localPosition = new Vector3(-shift, 0f, 0f);
        }
    }

    private static Sprite GetDefaultSprite()
    {
        Texture2D tex = Texture2D.whiteTexture;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1f);
    }
}
