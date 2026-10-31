using System;
using UnityEngine;

[Flags]
public enum TargetCategory
{
    None = 0,
    Ground = 1 << 0,
    Air = 1 << 1,
    Building = 1 << 2
}

public interface IDamageable
{
    bool IsPlayer { get; }
    TargetCategory Category { get; }
    bool IsAlive { get; }
    float CurrentHp { get; }
    float MaxHp { get; }
    Transform Transform { get; }
    void TakeDamage(float amount);
    void Heal(float amount);
}

public enum CardType
{
    Troop,
    Spell
}

public enum SpellType
{
    Damage,
    Heal
}

[System.Serializable]
public class CardInfo
{
    public string id;
    public string cardName;
    public int cost;
    public CardType cardType;
    public Color cardColor = Color.white;
    public Sprite cardSprite;

    [Header("Troop Settings")]
    public float maxHp = 850f;
    public float damage = 90f;
    public float moveSpeed = 0.85f;
    public float attackRange = 0.85f;
    public float attackInterval = 1.2f;
    public TargetCategory unitCategory = TargetCategory.Ground;
    public TargetCategory targetCategories = TargetCategory.Ground | TargetCategory.Building;
    public bool isRanged = false;
    public float projectileSpeed = 6f;

    [Header("Visual & Layout")]
    public float modelScale = 1f;
    public float healthBarOffsetY = 0.70f;
    public float healthBarWidth = 0.44f;

    [Header("Spell Settings")]
    public SpellType spellType = SpellType.Damage;
    public float spellRadius = 2.2f;
    public float spellPower = 400f; // damage or heal amount
}
