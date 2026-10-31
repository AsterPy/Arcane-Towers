using UnityEngine;

public class ArcaneProjectile : MonoBehaviour
{
    private Transform targetTransform;
    private IDamageable targetDamageable;
    private Vector3 lastTargetPos;
    private float damage;
    private float speed = 7f;
    private bool isPlayer;

    public static void Spawn(Vector3 startPos, IDamageable target, float dmg, float projSpeed, Color color, bool playerSource)
    {
        GameObject go = new GameObject("Projectile");
        go.transform.position = startPos;
        go.transform.localScale = new Vector3(0.2f, 0.2f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        Texture2D tex = Texture2D.whiteTexture;
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        sr.color = color;
        sr.sortingOrder = 8;

        ArcaneProjectile proj = go.AddComponent<ArcaneProjectile>();
        proj.targetDamageable = target;
        proj.targetTransform = target != null ? target.Transform : null;
        proj.lastTargetPos = target != null && target.Transform != null ? target.Transform.position : startPos;
        proj.damage = dmg;
        proj.speed = projSpeed;
        proj.isPlayer = playerSource;
    }

    private void Update()
    {
        if (targetTransform != null)
        {
            lastTargetPos = targetTransform.position;
        }

        transform.position = Vector3.MoveTowards(transform.position, lastTargetPos, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, lastTargetPos) <= 0.15f)
        {
            HitTarget();
        }
    }

    private void HitTarget()
    {
        if (targetDamageable != null && targetDamageable.IsAlive)
        {
            targetDamageable.TakeDamage(damage);
        }
        Destroy(gameObject);
    }
}
