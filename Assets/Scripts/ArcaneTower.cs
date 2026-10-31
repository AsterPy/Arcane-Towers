using System.Collections.Generic;
using UnityEngine;

public class ArcaneTower : MonoBehaviour, IDamageable
{
    [Header("Faction & Identity")]
    public bool isPlayer;
    public bool isCore;

    [Header("Stats")]
    public float maxHp = 500f;
    public float hp = 500f;
    public float attackRange = 3.2f;
    public float damage = 35f;
    public float attackInterval = 1f;

    [Header("Visuals & Audio")]
    public Color projectileColor = new Color(1f, 0.9f, 0.2f);

    [Header("Collision & Layout (Configured in Prefab)")]
    public float towerRadius = 1.05f;
    public float healthBarOffsetY = 1.25f;
    public float healthBarWidth = 0.52f;

    private float attackTimer;
    private ArcaneHealthBar healthBar;

    public bool IsPlayer => isPlayer;
    public TargetCategory Category => TargetCategory.Building;
    public bool IsAlive => hp > 0f;
    public float CurrentHp => hp;
    public float MaxHp => maxHp;
    public Transform Transform => transform;

    private void Awake()
    {
        hp = maxHp;
        if (isCore && towerRadius <= 1.05f) towerRadius = 1.45f;
        if (isCore && healthBarOffsetY <= 1.25f) healthBarOffsetY = 1.55f;
        if (isCore && healthBarWidth <= 0.52f) healthBarWidth = 0.75f;
    }

    private void Start()
    {
        Color barColor = isPlayer ? new Color(0.2f, 0.75f, 1f) : new Color(1f, 0.35f, 0.35f);
        healthBar = ArcaneHealthBar.Create(transform, healthBarOffsetY, healthBarWidth, 0.038f, barColor);
        healthBar.SetHealth(hp, maxHp);
    }

    private void Update()
    {
        if (!IsAlive) return;
        if (ArcaneGameManager.Instance != null && ArcaneGameManager.Instance.MatchEnded) return;

        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;

        IDamageable target = FindTarget();
        if (target != null)
        {
            AttackTarget(target);
            attackTimer = attackInterval;
        }
    }

    private IDamageable FindTarget()
    {
        IDamageable best = null;
        float bestDistance = attackRange;

        // Towers target enemy units (Ground or Air)
        foreach (ArcaneUnit unit in FindObjectsByType<ArcaneUnit>())
        {
            if (!unit.IsAlive || unit.IsPlayer == isPlayer) continue;
            float dist = Vector2.Distance(transform.position, unit.transform.position);
            if (dist <= bestDistance)
            {
                bestDistance = dist;
                best = unit;
            }
        }

        return best;
    }

    private void AttackTarget(IDamageable target)
    {
        ArcaneAudio.PlayTowerShot(isCore);
        Color color = isPlayer ? new Color(0.3f, 0.8f, 1f) : new Color(1f, 0.4f, 0.3f);
        ArcaneProjectile.Spawn(transform.position, target, damage, 8f, color, isPlayer);
    }

    public void TakeDamage(float amount)
    {
        if (!IsAlive) return;

        hp = Mathf.Max(0f, hp - amount);
        if (healthBar != null) healthBar.SetHealth(hp, maxHp);

        if (hp <= 0f)
        {
            ArcaneAudio.PlayTowerDestroy();
            gameObject.SetActive(false);
            if (ArcaneGameManager.Instance != null)
            {
                ArcaneGameManager.Instance.OnTowerDestroyed(this);
            }
        }
        else
        {
            ArcaneAudio.PlayTowerHit();
        }
    }

    public void Heal(float amount)
    {
        // Towers are not healed in base rules
    }
}
