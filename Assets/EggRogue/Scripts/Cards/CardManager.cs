using UnityEngine;
using System.Collections.Generic;
using EggRogue;

/// <summary>
/// 卡片管理器 - 管理玩家已选择的卡片，应用属性加成。
/// 常驻 PersistentScene，与 LevelManager 配合。
/// </summary>
public class CardManager : MonoBehaviour
{
    private static CardManager _instance;
    public static CardManager Instance => _instance;

    [Header("已选择卡片")]
    [Tooltip("玩家已选择的卡片列表（用于存档/显示）")]
    private List<CardOffer> selectedCards = new List<CardOffer>();

    /// <summary>
    /// 已选择的卡片列表（只读）。
    /// </summary>
    public IReadOnlyList<CardOffer> SelectedCards => selectedCards;

    private void Awake()
    {
        // 确保 CardManager 常驻并且只保留一份实例
        GameObject rootGO = transform.root != null ? transform.root.gameObject : gameObject;

        if (_instance != null && _instance != this)
        {
            Destroy(rootGO);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(rootGO);
    }

    /// <summary>
    /// 应用卡片加成到玩家。
    /// </summary>
    public void ApplyCard(CardData card, int level)
    {
        if (card == null)
            return;

        var offer = new CardOffer(card, level);
        selectedCards.Add(offer);

        // 优先使用 CharacterStats（新系统）
        CharacterStats stats = FindObjectOfType<CharacterStats>();
        if (stats != null)
        {
            float beforeMax = stats.CurrentMaxHealth;
            float beforeDamage = stats.CurrentDamage;

            stats.ApplyCardBonus(card, level);

            Debug.Log(
                $"CardManager: 通过 CharacterStats 应用卡片 {card.cardName} Lv{level} 加成。" +
                $" MaxHealth: {beforeMax} -> {stats.CurrentMaxHealth}, Damage: {beforeDamage} -> {stats.CurrentDamage}");
            return;
        }

        Debug.LogWarning("CardManager: 未找到 CharacterStats，使用兼容旧系统路径应用卡片加成。");

        var bonus = card.GetBonusForLevel(level);
        PlayerCombatController combat = FindObjectOfType<PlayerCombatController>();
        if (combat != null)
        {
            if (bonus.damageBonus != 0f)
                combat.SetDamage(combat.damagePerShot + bonus.damageBonus);
            if (bonus.fireRateBonus != 0f)
                combat.SetFireRate(combat.fireRate + bonus.fireRateBonus);
            if (bonus.bulletSpeedBonus != 0f)
                combat.SetBulletSpeed(combat.bulletSpeed + bonus.bulletSpeedBonus);
            if (bonus.attackRangeBonus != 0f)
                combat.SetAttackRange(combat.attackRange + bonus.attackRangeBonus);
        }

        Health health = FindObjectOfType<Health>();
        if (health != null && bonus.maxHealthBonus != 0f)
        {
            health.SetMaxHealth(health.maxHealth + bonus.maxHealthBonus);
            health.FullHeal();
        }

        CharacterController character = FindObjectOfType<CharacterController>();
        if (character != null && bonus.moveSpeedBonus != 0f)
            character.SetMoveSpeed(character.moveSpeed + bonus.moveSpeedBonus);

        Debug.Log($"CardManager: 已应用卡片 {card.cardName} Lv{level} 的加成（兼容模式）");
    }

    /// <summary>
    /// 清除所有已选择的卡片（用于重新开始游戏）。
    /// </summary>
    public void ClearAllCards()
    {
        selectedCards.Clear();
    }

    /// <summary>
    /// 计算所有卡片的累计加成（用于显示）。
    /// </summary>
    public void GetTotalBonuses(out float totalDamage, out float totalFireRate, out float totalMaxHealth,
        out float totalMoveSpeed, out float totalBulletSpeed, out float totalAttackRange, out float totalPickupRange)
    {
        totalDamage = 0f;
        totalFireRate = 0f;
        totalMaxHealth = 0f;
        totalMoveSpeed = 0f;
        totalBulletSpeed = 0f;
        totalAttackRange = 0f;
        totalPickupRange = 0f;

        foreach (var offer in selectedCards)
        {
            if (offer.card == null) continue;
            var b = offer.card.GetBonusForLevel(offer.level);
            totalDamage += b.damageBonus;
            totalFireRate += b.fireRateBonus;
            totalMaxHealth += b.maxHealthBonus;
            totalMoveSpeed += b.moveSpeedBonus;
            totalBulletSpeed += b.bulletSpeedBonus;
            totalAttackRange += b.attackRangeBonus;
            totalPickupRange += b.pickupRangeBonus;
        }
    }
}
