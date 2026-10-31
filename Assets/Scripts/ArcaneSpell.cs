using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcaneSpell : MonoBehaviour
{
    public static void CastFireball(Vector3 center, float radius, float damage, bool isPlayer)
    {
        ArcaneAudio.PlaySpell(SpellType.Damage);
        CreateSpellVisual(center, radius, new Color(1f, 0.45f, 0.1f, 0.7f), 0.5f);

        // Deal damage to all enemy damageables in radius
        List<IDamageable> targets = GetAllDamageables();
        foreach (IDamageable d in targets)
        {
            if (d.IsAlive && d.IsPlayer != isPlayer)
            {
                if (Vector2.Distance(center, d.Transform.position) <= radius)
                {
                    d.TakeDamage(damage);
                }
            }
        }
    }

    public static void CastHeal(Vector3 center, float radius, float healAmount, bool isPlayer)
    {
        ArcaneAudio.PlaySpell(SpellType.Heal);
        CreateSpellVisual(center, radius, new Color(0.2f, 1f, 0.4f, 0.65f), 0.7f);

        // Heal friendly units only (not towers/buildings as per rules)
        List<IDamageable> targets = GetAllDamageables();
        foreach (IDamageable d in targets)
        {
            if (d.IsAlive && d.IsPlayer == isPlayer && d.Category != TargetCategory.Building)
            {
                if (Vector2.Distance(center, d.Transform.position) <= radius)
                {
                    d.Heal(healAmount);
                }
            }
        }
    }

    private static List<IDamageable> GetAllDamageables()
    {
        List<IDamageable> list = new List<IDamageable>();
        foreach (ArcaneUnit u in Object.FindObjectsByType<ArcaneUnit>())
        {
            list.Add(u);
        }
        foreach (ArcaneTower t in Object.FindObjectsByType<ArcaneTower>())
        {
            list.Add(t);
        }
        return list;
    }

    private static void CreateSpellVisual(Vector3 center, float radius, Color color, float duration)
    {
        GameObject fx = new GameObject("SpellFX");
        fx.transform.position = center;
        fx.transform.localScale = Vector3.zero;

        SpriteRenderer sr = fx.AddComponent<SpriteRenderer>();
        sr.sprite = GetCircleSprite();
        sr.color = color;
        sr.sortingOrder = 9;

        ArcaneSpell spell = fx.AddComponent<ArcaneSpell>();
        spell.StartCoroutine(spell.AnimateFX(radius, duration));
    }

    private IEnumerator AnimateFX(float targetRadius, float duration)
    {
        float elapsed = 0f;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color startCol = sr != null ? sr.color : Color.white;
        float targetScale = targetRadius * 2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float currentScale = Mathf.Lerp(0f, targetScale, Mathf.Sin(t * Mathf.PI * 0.5f));
            transform.localScale = new Vector3(currentScale, currentScale, 1f);

            if (sr != null)
            {
                Color c = startCol;
                c.a = Mathf.Lerp(startCol.a, 0f, t);
                sr.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private static Sprite cachedCircleSprite;
    private static Sprite GetCircleSprite()
    {
        if (cachedCircleSprite == null)
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size * 0.5f;
            Vector2 center = new Vector2(radius, radius);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = dist <= radius ? Mathf.Clamp01((radius - dist) / 2f) : 0f;
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            tex.Apply();
            cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
        return cachedCircleSprite;
    }
}
