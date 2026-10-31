using System.Collections.Generic;
using UnityEngine;

public static class ArcaneVisuals
{
    private static Sprite shadowSprite;
    private static readonly Dictionary<string, Sprite[]> frameCache = new Dictionary<string, Sprite[]>();

    public static Sprite LoadSpriteSafe(string path)
    {
#if UNITY_EDITOR
        Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex != null)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        Sprite sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp != null) return sp;

        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        if (assets != null && assets.Length > 0)
        {
            Sprite best = null;
            float maxArea = -1f;
            foreach (var obj in assets)
            {
                if (obj is Sprite s)
                {
                    float area = s.rect.width * s.rect.height;
                    if (area > maxArea)
                    {
                        maxArea = area;
                        best = s;
                    }
                }
            }
            if (best != null) return best;
        }
#endif
        return null;
    }

    public static string GetFolderForUnit(string unitId)
    {
        switch (unitId.ToLower())
        {
            case "knight":
            case "warrior":
                return "warrior";
            case "archer":
                return "archer";
            case "golem":
                return "golem";
            case "drone":
                return "drone";
            default:
                return unitId.ToLower();
        }
    }

    public static Sprite[] GetUnitAnimationFrames(string unitId, string actionPrefix, bool isPlayer)
    {
        string folder = GetFolderForUnit(unitId);
        string side = isPlayer ? "from" : "to";
        string cacheKey = $"{folder}_{actionPrefix}_{side}";

        if (frameCache.TryGetValue(cacheKey, out Sprite[] cached) && cached != null && cached.Length > 0 && cached[0] != null)
        {
            return cached;
        }

        string path1 = $"Assets/Sprites/cards/{folder}/{actionPrefix}_{side}_1.png";
        string path2 = $"Assets/Sprites/cards/{folder}/{actionPrefix}_{side}_2.png";

        Sprite f1 = LoadSpriteSafe(path1);
        Sprite f2 = LoadSpriteSafe(path2);

        if (f1 != null && f2 != null)
        {
            Sprite[] frames = new Sprite[] { f1, f2 };
            frameCache[cacheKey] = frames;
            return frames;
        }
        else if (f1 != null)
        {
            Sprite[] frames = new Sprite[] { f1, f1 };
            frameCache[cacheKey] = frames;
            return frames;
        }
        else if (f2 != null)
        {
            Sprite[] frames = new Sprite[] { f2, f2 };
            frameCache[cacheKey] = frames;
            return frames;
        }

        return null;
    }

    public static Sprite[] GetUnitWalkFrames(string unitId, bool isPlayer) => GetUnitAnimationFrames(unitId, "run", isPlayer);
    public static Sprite[] GetUnitAttackFrames(string unitId, bool isPlayer) => GetUnitAnimationFrames(unitId, "attack", isPlayer);
    public static Sprite[] GetUnitDeathFrames(string unitId, bool isPlayer) => GetUnitAnimationFrames(unitId, "die", isPlayer);
    public static Sprite[] GetGolemWalkFrames(bool isPlayer) => GetUnitWalkFrames("golem", isPlayer);

    public static Sprite GetShadowSprite()
    {
        if (shadowSprite == null)
            shadowSprite = GenerateShadowTexture(64, 32);
        return shadowSprite;
    }

    public static Sprite GetUnitSprite(string unitId, bool isPlayer)
    {
        Sprite[] walk = GetUnitWalkFrames(unitId, isPlayer);
        if (walk != null && walk.Length > 0 && walk[0] != null)
            return walk[0];

        return GetFallbackSprite(isPlayer ? new Color(0.2f, 0.7f, 1f) : new Color(1f, 0.3f, 0.35f));
    }

    public static void ApplyUnitVisuals(ArcaneUnit unit, string unitId, bool isPlayer)
    {
        Sprite[] walk = GetUnitWalkFrames(unitId, isPlayer);
        Sprite[] attack = GetUnitAttackFrames(unitId, isPlayer);
        Sprite[] die = GetUnitDeathFrames(unitId, isPlayer);

        unit.walkFrames = walk;
        unit.attackFrames = attack;
        unit.deathFrames = die;
        unit.frameDuration = 0.28f;

        SpriteRenderer sr = unit.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (walk != null && walk.Length > 0 && walk[0] != null)
                sr.sprite = walk[0];
            else
                sr.sprite = GetUnitSprite(unitId, isPlayer);

            sr.color = Color.white;
            sr.sortingOrder = unit.category == TargetCategory.Air ? 6 : 3;
        }

        // Soft ground shadow
        Transform existingShadow = unit.transform.Find("Shadow");
        if (existingShadow == null)
        {
            GameObject shadowObj = new GameObject("Shadow");
            shadowObj.transform.SetParent(unit.transform, false);
            float shadowY = unit.category == TargetCategory.Air ? -0.7f : -0.38f;
            shadowObj.transform.localPosition = new Vector3(0, shadowY, 0);

            SpriteRenderer shadowSr = shadowObj.AddComponent<SpriteRenderer>();
            shadowSr.sprite = GetShadowSprite();
            shadowSr.sortingOrder = 1;
            shadowObj.transform.localScale = new Vector3(0.9f, 0.54f, 1f);
        }
    }

    private static Sprite GetFallbackSprite(Color c)
    {
        Texture2D tex = Texture2D.whiteTexture;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite GenerateShadowTexture(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float rx = width * 0.5f;
        float ry = height * 0.5f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = (x - rx) / rx;
                float dy = (y - ry) / ry;
                float d = dx * dx + dy * dy;
                if (d <= 1f)
                {
                    float alpha = Mathf.SmoothStep(0.45f, 0f, Mathf.Sqrt(d));
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 64f);
    }
}
