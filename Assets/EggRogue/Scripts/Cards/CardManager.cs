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
    private List<CardData> selectedCards = new List<CardData>();

    /// <summary>
    /// 已选择的卡片列表（只读）。
    /// </summary>
    public IReadOnlyList<CardData> SelectedCards => selectedCards;

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
    public void ApplyCard(CardData card)
    {
        if (card == null)
            return;

        selectedCards.Add(card);

        // 优先使用 CharacterStats（新系统）
        CharacterStats stats = FindObjectOfType<CharacterStats>();
        if (stats != null)
        {
            float beforeMax = stats.CurrentMaxHealth;
            float beforeDamage = stats.CurrentDamage;

            stats.ApplyCardBonus(card);

            Debug.Log(
                $"CardManager: 通过 CharacterStats 应用卡片 {card.cardName} 加成。" +
                $" MaxHealth: {beforeMax} -> {stats.CurrentMaxHealth}, Damage: {beforeDamage} -> {stats.CurrentDamage}");
            return;
        }

        Debug.LogWarning("CardManager: 未找到 CharacterStats，使用兼容旧系统路径应用卡片加成。");

        // 兼容旧系统（如果没有 CharacterStats，直接修改组件）
        PlayerCombatController combat = FindObjectOfType<PlayerCombatController>();
        if (combat != null)
        {
            if (card.damageBonus != 0f)
            {
                float newDamage = combat.damagePerShot + card.damageBonus;
                combat.SetDamage(newDamage);
            }
            if (card.fireRateBonus != 0f)
            {
                float newFireRate = combat.fireRate + card.fireRateBonus;
                combat.SetFireRate(newFireRate);
            }
            if (card.bulletSpeedBonus != 0f)
            {
                float newBulletSpeed = combat.bulletSpeed + card.bulletSpeedBonus;
                combat.SetBulletSpeed(newBulletSpeed);
            }
            if (card.attackRangeBonus != 0f)
            {
                float newAttackRange = combat.attackRange + card.attackRangeBonus;
                combat.SetAttackRange(newAttackRange);
            }
        }

        Health health = FindObjectOfType<Health>();
        if (health != null && card.maxHealthBonus != 0f)
        {
            health.SetMaxHealth(health.maxHealth + card.maxHealthBonus);
            health.FullHeal();
        }

        CharacterController character = FindObjectOfType<CharacterController>();
        if (character != null && card.moveSpeedBonus != 0f)
        {
            character.SetMoveSpeed(character.moveSpeed + card.moveSpeedBonus);
        }

        Debug.Log($"CardManager: 已应用卡片 {card.cardName} 的加成（兼容模式）");
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
        out float totalMoveSpeed, out float totalBulletSpeed, out float totalAttackRange)
    {
        totalDamage = 0f;
        totalFireRate = 0f;
        totalMaxHealth = 0f;
        totalMoveSpeed = 0f;
        totalBulletSpeed = 0f;
        totalAttackRange = 0f;

        foreach (var card in selectedCards)
        {
            if (card == null) continue;
            totalDamage += card.damageBonus;
            totalFireRate += card.fireRateBonus;
            totalMaxHealth += card.maxHealthBonus;
            totalMoveSpeed += card.moveSpeedBonus;
            totalBulletSpeed += card.bulletSpeedBonus;
            totalAttackRange += card.attackRangeBonus;
        }
    }
}
