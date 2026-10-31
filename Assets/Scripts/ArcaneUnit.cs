using System.Collections.Generic;
using UnityEngine;

public class ArcaneUnit : MonoBehaviour, IDamageable
{
    [Header("Faction & Identity")]
    public bool isPlayer;
    public string unitId = "warrior";
    public string unitName = "Knight";
    public TargetCategory category = TargetCategory.Ground;
    public TargetCategory attackableTargets = TargetCategory.Ground | TargetCategory.Building;

    [Header("Stats")]
    public float maxHp = 120f;
    public float hp = 120f;
    public float moveSpeed = 0.85f;
    public float attackRange = 0.85f;
    public float detectionRange = 3.5f;
    public float damage = 22f;
    public float attackInterval = 1f;

    [Header("Combat Type")]
    public bool isRanged = false;
    public float projectileSpeed = 7f;

    [Header("Visual & Layout Offsets")]
    public float barOffsetY = 0.70f;
    public float barWidth = 0.44f;

    [Header("Animation")]
    public Sprite[] walkFrames;
    public Sprite[] attackFrames;
    public Sprite[] deathFrames;
    public float frameDuration = 0.28f;

    private float attackTimer;
    private IDamageable currentTarget;
    private ArcaneHealthBar healthBar;
    private SpriteRenderer spriteRenderer;
    private float animTimer;
    private int currentFrameIndex;
    private bool isAttacking = false;
    private bool isDying = false;

    public bool IsPlayer => isPlayer;
    public TargetCategory Category => category;
    public bool IsAlive => hp > 0f && !isDying;
    public float CurrentHp => hp;
    public float MaxHp => maxHp;
    public Transform Transform => transform;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        hp = maxHp;
    }

    private void Start()
    {
        Color barColor = isPlayer ? new Color(0.25f, 0.88f, 0.45f) : new Color(1f, 0.35f, 0.35f);
        healthBar = ArcaneHealthBar.Create(transform, barOffsetY, barWidth, 0.032f, barColor);
        healthBar.SetHealth(hp, maxHp);
    }

    private static Transform cachedRiver;
    private static Transform cachedBridgeLeft;
    private static Transform cachedBridgeRight;
    private static float cachedRiverY = 0f;
    private static float cachedLeftBridgeX = -2.7f;
    private static float cachedRightBridgeX = 2.7f;
    private static bool navCached = false;

    private static void EnsureNavCached()
    {
        if (navCached && cachedRiver != null && cachedBridgeLeft != null && cachedBridgeRight != null) return;

        GameObject r = GameObject.Find("River_Water");
        if (r != null)
        {
            cachedRiver = r.transform;
            cachedRiverY = cachedRiver.position.y;
        }

        GameObject bl = GameObject.Find("Bridge_Left");
        if (bl != null)
        {
            cachedBridgeLeft = bl.transform;
            cachedLeftBridgeX = cachedBridgeLeft.position.x;
        }

        GameObject br = GameObject.Find("Bridge_Right");
        if (br != null)
        {
            cachedBridgeRight = br.transform;
            cachedRightBridgeX = cachedBridgeRight.position.x;
        }

        navCached = true;
    }

    public Vector3 GetNavWaypoint(Vector3 targetPos)
    {
        // Air units fly straight across water
        if (category == TargetCategory.Air)
        {
            return targetPos;
        }

        EnsureNavCached();

        Vector3 curr = transform.position;
        float riverCenterY = cachedRiver != null ? cachedRiver.position.y : cachedRiverY;
        float riverMinY = riverCenterY - 1.4f;
        float riverMaxY = riverCenterY + 1.4f;

        float leftBridgeX = cachedBridgeLeft != null ? cachedBridgeLeft.position.x : cachedLeftBridgeX;
        float rightBridgeX = cachedBridgeRight != null ? cachedBridgeRight.position.x : cachedRightBridgeX;

        // Pick nearest bridge
        float bridgeX = Mathf.Abs(curr.x - leftBridgeX) < Mathf.Abs(curr.x - rightBridgeX) ? leftBridgeX : rightBridgeX;

        // Case 1: Unit is south of river
        if (curr.y < riverMinY)
        {
            if (targetPos.y > riverMinY)
            {
                // Target is across river -> must go through bridge
                if (Mathf.Abs(curr.x - bridgeX) > 0.35f)
                {
                    return new Vector3(bridgeX, riverMinY - 0.25f, 0f);
                }
                else
                {
                    return new Vector3(bridgeX, riverMaxY + 0.35f, 0f);
                }
            }
            return targetPos;
        }

        // Case 2: Unit is north of river
        if (curr.y > riverMaxY)
        {
            if (targetPos.y < riverMaxY)
            {
                // Target is across river south -> must go through bridge
                if (Mathf.Abs(curr.x - bridgeX) > 0.35f)
                {
                    return new Vector3(bridgeX, riverMaxY + 0.25f, 0f);
                }
                else
                {
                    return new Vector3(bridgeX, riverMinY - 0.35f, 0f);
                }
            }
            return targetPos;
        }

        // Case 3: Unit is on bridge / crossing river zone
        if (targetPos.y > curr.y)
        {
            return new Vector3(bridgeX, riverMaxY + 0.35f, 0f);
        }
        else
        {
            return new Vector3(bridgeX, riverMinY - 0.35f, 0f);
        }
    }

    private void Update()
    {
        if (isDying || !IsAlive) return;
        if (ArcaneGameManager.Instance != null && ArcaneGameManager.Instance.MatchEnded) return;

        attackTimer -= Time.deltaTime;

        if (isAttacking) return;

        // Verify current target or find a new one
        if (currentTarget == null || !currentTarget.IsAlive || !IsTargetValid(currentTarget))
        {
            currentTarget = FindNearestTarget();
        }

        if (currentTarget != null)
        {
            float distToTarget = Vector2.Distance(transform.position, currentTarget.Transform.position);
            float effectiveRange = attackRange;
            if (currentTarget.Category == TargetCategory.Building)
            {
                ArcaneTower targetTower = currentTarget as ArcaneTower;
                float towerRadius = targetTower != null ? targetTower.towerRadius : 1.05f;
                effectiveRange += towerRadius;
            }

            if (distToTarget <= effectiveRange)
            {
                // In range: perform attack
                if (attackTimer <= 0f)
                {
                    PerformAttack(currentTarget);
                    attackTimer = attackInterval;
                }
                else
                {
                    ResetWalkAnimation();
                }
                PreventTowerPenetration();
                return;
            }
            else
            {
                // Move towards target through bridges (avoid water)
                Vector3 targetPoint = currentTarget.Transform.position;
                if (currentTarget.Category == TargetCategory.Building)
                {
                    Vector2 dir = ((Vector2)targetPoint - (Vector2)transform.position).normalized;
                    ArcaneTower targetTower = currentTarget as ArcaneTower;
                    float towerRadius = targetTower != null ? targetTower.towerRadius : 1.05f;
                    targetPoint = (Vector2)targetPoint - dir * towerRadius;
                }

                Vector3 moveWaypoint = GetNavWaypoint(targetPoint);
                transform.position = Vector2.MoveTowards(transform.position, moveWaypoint, moveSpeed * Time.deltaTime);
                PreventTowerPenetration();
                UpdateWalkAnimation();
                return;
            }
        }

        // No targets in detection range -> follow lane towards objective through bridges
        Vector3 laneDestination = GetLaneDestination();
        Vector3 laneWaypoint = GetNavWaypoint(laneDestination);
        transform.position = Vector2.MoveTowards(transform.position, laneWaypoint, moveSpeed * Time.deltaTime);
        PreventTowerPenetration();
        UpdateWalkAnimation();
    }

    private void UpdateWalkAnimation()
    {
        if (walkFrames == null || walkFrames.Length <= 1) return;
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;

        animTimer += Time.deltaTime;
        if (animTimer >= frameDuration)
        {
            animTimer = 0f;
            currentFrameIndex = (currentFrameIndex + 1) % walkFrames.Length;
            spriteRenderer.sprite = walkFrames[currentFrameIndex];
        }
    }

    private void ResetWalkAnimation()
    {
        if (walkFrames != null && walkFrames.Length > 0)
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && currentFrameIndex != 0)
            {
                currentFrameIndex = 0;
                spriteRenderer.sprite = walkFrames[0];
            }
            animTimer = 0f;
        }
    }

    private bool IsTargetValid(IDamageable target)
    {
        if (target == null || !target.IsAlive || target.IsPlayer == isPlayer)
            return false;

        return (target.Category & attackableTargets) != 0;
    }

    private IDamageable FindNearestTarget()
    {
        IDamageable best = null;
        float bestDistance = detectionRange;

        // 1. Search enemy units first (if our targets allow them)
        if ((attackableTargets & (TargetCategory.Ground | TargetCategory.Air)) != 0)
        {
            foreach (ArcaneUnit unit in FindObjectsByType<ArcaneUnit>())
            {
                if (!IsTargetValid(unit)) continue;
                float dist = Vector2.Distance(transform.position, unit.transform.position);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    best = unit;
                }
            }
        }

        // 2. If no units in range, or we also target buildings, check enemy towers
        if ((attackableTargets & TargetCategory.Building) != 0)
        {
            foreach (ArcaneTower tower in FindObjectsByType<ArcaneTower>())
            {
                if (!IsTargetValid(tower)) continue;
                float dist = Vector2.Distance(transform.position, tower.transform.position);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    best = tower;
                }
            }
        }

        return best;
    }

    private void PerformAttack(IDamageable target)
    {
        if (attackFrames != null && attackFrames.Length > 0)
        {
            StartCoroutine(AttackRoutine(target));
        }
        else
        {
            ExecuteDamageOrProjectile(target);
        }
    }

    private System.Collections.IEnumerator AttackRoutine(IDamageable target)
    {
        isAttacking = true;
        for (int i = 0; i < attackFrames.Length; i++)
        {
            if (spriteRenderer != null && attackFrames[i] != null)
            {
                spriteRenderer.sprite = attackFrames[i];
            }
            if (i == attackFrames.Length - 1 && target != null && target.IsAlive)
            {
                ExecuteDamageOrProjectile(target);
            }
            yield return new WaitForSeconds(frameDuration);
        }
        isAttacking = false;
        ResetWalkAnimation();
    }

    private void ExecuteDamageOrProjectile(IDamageable target)
    {
        if (target == null || !target.IsAlive) return;

        ArcaneAudio.PlayAttack(unitId);

        if (isRanged)
        {
            Color projColor = isPlayer ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.6f, 0.2f);
            ArcaneProjectile.Spawn(transform.position, target, damage, projectileSpeed, projColor, isPlayer);
        }
        else
        {
            target.TakeDamage(damage);
        }
    }

    private void PreventTowerPenetration()
    {
        // Ground units must never penetrate inside or walk over any living tower
        if (category == TargetCategory.Air) return;

        foreach (ArcaneTower tower in FindObjectsByType<ArcaneTower>())
        {
            if (tower == null || !tower.IsAlive) continue;
            float radius = tower.towerRadius;
            Vector2 diff = (Vector2)transform.position - (Vector2)tower.transform.position;
            float dist = diff.magnitude;
            if (dist < radius && dist > 0.001f)
            {
                transform.position = (Vector2)tower.transform.position + diff.normalized * radius;
            }
        }
    }

    private Vector3 GetLaneDestination()
    {
        // Choose lane based on current X position (-2.7 or +2.7)
        float laneX = transform.position.x < 0f ? -2.7f : 2.7f;

        // Enemy tower target on this lane (destination is base of tower)
        ArcaneTower laneTower = FindLaneTower(laneX);
        if (laneTower != null && laneTower.IsAlive)
        {
            float stopY = isPlayer ? laneTower.transform.position.y - 1.05f : laneTower.transform.position.y + 1.05f;
            return new Vector3(laneTower.transform.position.x, stopY, 0f);
        }

        // If lane tower is destroyed, target enemy core
        ArcaneTower core = FindEnemyCore();
        if (core != null && core.IsAlive)
        {
            float stopY = isPlayer ? core.transform.position.y - 1.45f : core.transform.position.y + 1.45f;
            return new Vector3(core.transform.position.x, stopY, 0f);
        }

        float endY = isPlayer ? 5.8f : -5.8f;
        return new Vector3(laneX, endY, 0f);
    }

    private ArcaneTower FindLaneTower(float laneX)
    {
        foreach (ArcaneTower tower in FindObjectsByType<ArcaneTower>())
        {
            if (tower.isPlayer != isPlayer && !tower.isCore && Mathf.Abs(tower.transform.position.x - laneX) < 1.0f)
            {
                return tower;
            }
        }
        return null;
    }

    private ArcaneTower FindEnemyCore()
    {
        foreach (ArcaneTower tower in FindObjectsByType<ArcaneTower>())
        {
            if (tower.isPlayer != isPlayer && tower.isCore)
            {
                return tower;
            }
        }
        return null;
    }

    public void TakeDamage(float amount)
    {
        if (isDying || !IsAlive) return;

        hp = Mathf.Max(0f, hp - amount);
        if (healthBar != null) healthBar.SetHealth(hp, maxHp);

        if (hp <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDying) return;
        isDying = true;
        hp = 0f;

        ArcaneAudio.PlayDie();

        StopAllCoroutines();

        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
        }

        Transform sh = transform.Find("Shadow");
        if (sh != null)
        {
            sh.gameObject.SetActive(false);
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        StartCoroutine(DeathRoutine());
    }

    private System.Collections.IEnumerator DeathRoutine()
    {
        // Play death animation frames (works for drone, archer, warrior, golem)
        if (deathFrames != null && deathFrames.Length > 0)
        {
            for (int i = 0; i < deathFrames.Length; i++)
            {
                if (spriteRenderer != null && deathFrames[i] != null)
                {
                    spriteRenderer.sprite = deathFrames[i];
                }
                yield return new WaitForSeconds(frameDuration * 1.25f);
            }
        }

        // Smooth fade-out before destroying
        if (spriteRenderer != null)
        {
            float elapsed = 0f;
            float fadeTime = 0.35f;
            Color startCol = spriteRenderer.color;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeTime);
                spriteRenderer.color = Color.Lerp(startCol, new Color(startCol.r, startCol.g, startCol.b, 0f), t);
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(0.2f);
        }

        Destroy(gameObject);
    }

    public void Heal(float amount)
    {
        if (!IsAlive) return;

        hp = Mathf.Min(maxHp, hp + amount);
        if (healthBar != null) healthBar.SetHealth(hp, maxHp);
    }
}
