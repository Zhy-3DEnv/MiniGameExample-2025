using UnityEngine;
using System.Collections.Generic;

namespace EggRogue
{
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
    }

    /// <summary>
    /// 应用卡片加成到玩家。
    /// </summary>
        public void ApplyCard(CardData card, int star)
        {
            if (card == null)
                return;

            var offer = new CardOffer(card, star);
            selectedCards.Add(offer);

            // 优先使用 CharacterStats（新系统）
            CharacterStats stats = FindObjectOfType<CharacterStats>();
            if (stats != null)
            {
                float beforeMax = stats.CurrentMaxHealth;
                float beforeDamage = stats.CurrentDamage;

                var bonus = card.GetBonusForStar(star);
                stats.ApplyCardBonus(bonus);

                // 如果该卡有护甲减伤加成，则叠加到角色护甲属性（armorPercentBonus 为百分比，例如 10 = 10%）
                if (bonus.armorPercentBonus != 0f)
                {
                    float beforeArmor = stats.CurrentArmorPercent;
                    float addArmor = Mathf.Clamp(bonus.armorPercentBonus / 100f, 0f, 0.8f);
                    stats.CurrentArmorPercent = Mathf.Clamp01(beforeArmor + addArmor);
                    Debug.Log(
                        $"CardManager: {card.cardName} ★{star} 护甲减伤 {beforeArmor * 100f:F1}% -> {stats.CurrentArmorPercent * 100f:F1}%");
                }

                Debug.Log(
                    $"CardManager: 通过 CharacterStats 应用卡片 {card.cardName} ★{star} 加成。" +
                    $" MaxHealth: {beforeMax} -> {stats.CurrentMaxHealth}, Damage: {beforeDamage} -> {stats.CurrentDamage}");
                return;
            }

            Debug.LogWarning("CardManager: 未找到 CharacterStats，使用兼容旧系统路径应用卡片加成。");

            var legacyBonus = card.GetBonusForStar(star);
            PlayerCombatController legacyCombat = FindObjectOfType<PlayerCombatController>();
            if (legacyCombat != null)
            {
                if (legacyBonus.damageBonus != 0f)
                    legacyCombat.SetDamage(legacyCombat.damagePerShot + legacyBonus.damageBonus);
                if (legacyBonus.fireRateBonus != 0f)
                    legacyCombat.SetFireRate(legacyCombat.fireRate + legacyBonus.fireRateBonus);
                if (legacyBonus.attackRangeBonus != 0f)
                    legacyCombat.SetAttackRange(legacyCombat.attackRange + legacyBonus.attackRangeBonus);
            }

            Health legacyHealth = FindObjectOfType<Health>();
            if (legacyHealth != null && legacyBonus.maxHealthBonus != 0f)
            {
                legacyHealth.SetMaxHealth(legacyHealth.maxHealth + legacyBonus.maxHealthBonus);
                legacyHealth.FullHeal();
            }

            CharacterController legacyCharacter = FindObjectOfType<CharacterController>();
            if (legacyCharacter != null && legacyBonus.moveSpeedBonus != 0f)
                legacyCharacter.SetMoveSpeed(legacyCharacter.moveSpeed + legacyBonus.moveSpeedBonus);

            Debug.Log($"CardManager: 已应用卡片 {card.cardName} ★{star} 的加成（兼容模式）");
        }

    /// <summary>
    /// 清除所有已选择的卡片（用于重新开始游戏）。
    /// </summary>
    public void ClearAllCards()
    {
        selectedCards.Clear();
    }

    /// <summary>
    /// 计算所有卡片的累计加成（用于显示与初始化）。
    /// </summary>
    public void GetTotalBonuses(out float totalDamage, out float totalFireRate, out float totalMaxHealth,
        out float totalMoveSpeed, out float totalAttackRange, out float totalPickupRange,
        out float totalArmorPercent, out float totalDodgePercent, out int totalReward, out float totalCritRate,
        out float totalCritDamageMult, out float totalLuck, out float totalKnockback)
    {
        totalDamage = 0f;
        totalFireRate = 0f;
        totalMaxHealth = 0f;
        totalMoveSpeed = 0f;
        totalAttackRange = 0f;
        totalPickupRange = 0f;
        totalArmorPercent = 0f;
        totalDodgePercent = 0f;
        totalReward = 0;
        totalCritRate = 0f;
        totalCritDamageMult = 0f;
        totalLuck = 0f;
        totalKnockback = 0f;

        foreach (var offer in selectedCards)
        {
            if (offer.card == null) continue;
            var b = offer.card.GetBonusForStar(offer.star);
            totalDamage += b.damageBonus;
            totalFireRate += b.fireRateBonus;
            totalMaxHealth += b.maxHealthBonus;
            totalMoveSpeed += b.moveSpeedBonus;
            totalAttackRange += b.attackRangeBonus;
            totalPickupRange += b.pickupRangeBonus;
            totalArmorPercent += b.armorPercentBonus;
            totalDodgePercent += b.dodgePercentBonus;
            totalReward += b.rewardBonus;
            totalCritRate += b.critRatePercentBonus;
            totalCritDamageMult += b.critDamageMultiplierBonus;
            totalLuck += b.luckBonus;
            totalKnockback += b.knockbackBonus;
        }
    }

}
}