using UnityEngine;
using EggRogue;

/// <summary>
/// 角色属性管理器 - 管理角色的所有属性（基础值 + 卡片加成）。
/// 挂载在角色对象上，引用 CharacterData 作为基础配置。
/// </summary>
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerCombatController))]
public class CharacterStats : MonoBehaviour
{
    [Header("角色配置")]
    [Tooltip("角色数据（ScriptableObject）")]
    public CharacterData characterData;

    // 当前属性（运行时，包含卡片加成，只读）
    // 注意：属性（property）不能使用 [Header] 和 [Tooltip]，所以这些属性在 Inspector 中不会显示
    // 可以通过 AttributePanel 查看当前属性值
    public float CurrentDamage { get; private set; }
    public float CurrentFireRate { get; private set; }
    public float CurrentMaxHealth { get; private set; }
    public float CurrentMoveSpeed { get; private set; }
    public float CurrentBulletSpeed { get; private set; }
    public float CurrentAttackRange { get; private set; }

    private Health health;
    private CharacterController characterController;
    private PlayerCombatController combatController;

    private void Awake()
    {
        health = GetComponent<Health>();
        characterController = GetComponent<CharacterController>();
        combatController = GetComponent<PlayerCombatController>();
    }

    private void Start()
    {
        // 初始化属性（从 CharacterData 读取基础值）
        InitializeStats();
    }

    /// <summary>
    /// 初始化属性（从 CharacterData 读取基础值）
    /// </summary>
    public void InitializeStats()
    {
        if (characterData == null)
        {
            Debug.LogWarning("CharacterStats: characterData 未设置，使用默认值");
            return;
        }

        // 设置基础值
        CurrentDamage = characterData.baseDamage;
        CurrentFireRate = characterData.baseFireRate;
        CurrentMaxHealth = characterData.baseMaxHealth;
        CurrentMoveSpeed = characterData.baseMoveSpeed;
        CurrentBulletSpeed = characterData.baseBulletSpeed;
        CurrentAttackRange = characterData.baseAttackRange;

            // 如果有卡片管理器，叠加当前已选卡片的总加成（用于跨关卡继承成长）
            if (CardManager.Instance != null)
            {
                CardManager.Instance.GetTotalBonuses(
                    out float bonusDamage,
                    out float bonusFireRate,
                    out float bonusMaxHealth,
                    out float bonusMoveSpeed,
                    out float bonusBulletSpeed,
                    out float bonusAttackRange);

                CurrentDamage      += bonusDamage;
                CurrentFireRate    += bonusFireRate;
                CurrentMaxHealth   += bonusMaxHealth;
                CurrentMoveSpeed   += bonusMoveSpeed;
                CurrentBulletSpeed += bonusBulletSpeed;
                CurrentAttackRange += bonusAttackRange;
            }

        // 应用到组件
        ApplyStatsToComponents();
    }

    /// <summary>
    /// 应用属性到各个组件
    /// </summary>
    private void ApplyStatsToComponents()
    {
        if (combatController != null)
        {
            combatController.SetDamage(CurrentDamage);
            combatController.SetFireRate(CurrentFireRate);
            combatController.SetBulletSpeed(CurrentBulletSpeed);
            combatController.SetAttackRange(CurrentAttackRange);
        }

        if (health != null)
        {
            health.SetMaxHealth(CurrentMaxHealth);
            health.FullHeal();
        }

        if (characterController != null)
        {
            characterController.SetMoveSpeed(CurrentMoveSpeed);
        }
    }

    /// <summary>
    /// 应用卡片加成（CardManager 调用）
    /// </summary>
    public void ApplyCardBonus(CardData card)
    {
        if (card == null) return;

        // 累加卡片加成
        CurrentDamage += card.damageBonus;
        CurrentFireRate += card.fireRateBonus;
        CurrentMaxHealth += card.maxHealthBonus;
        CurrentMoveSpeed += card.moveSpeedBonus;
        CurrentBulletSpeed += card.bulletSpeedBonus;
        CurrentAttackRange += card.attackRangeBonus;

        // 重新应用到组件
        ApplyStatsToComponents();
    }

    /// <summary>
    /// 获取基础属性（用于显示，不包含卡片加成）
    /// </summary>
    public void GetBaseStats(out float damage, out float fireRate, out float maxHealth,
        out float moveSpeed, out float bulletSpeed, out float attackRange)
    {
        if (characterData != null)
        {
            damage = characterData.baseDamage;
            fireRate = characterData.baseFireRate;
            maxHealth = characterData.baseMaxHealth;
            moveSpeed = characterData.baseMoveSpeed;
            bulletSpeed = characterData.baseBulletSpeed;
            attackRange = characterData.baseAttackRange;
        }
        else
        {
            damage = 0f;
            fireRate = 0f;
            maxHealth = 0f;
            moveSpeed = 0f;
            bulletSpeed = 0f;
            attackRange = 0f;
        }
    }

    /// <summary>
    /// 获取卡片累计加成（用于显示）
    /// </summary>
    public void GetCardBonuses(out float damage, out float fireRate, out float maxHealth,
        out float moveSpeed, out float bulletSpeed, out float attackRange)
    {
        if (characterData == null)
        {
            damage = 0f;
            fireRate = 0f;
            maxHealth = 0f;
            moveSpeed = 0f;
            bulletSpeed = 0f;
            attackRange = 0f;
            return;
        }

        // 当前值 - 基础值 = 卡片加成
        damage = CurrentDamage - characterData.baseDamage;
        fireRate = CurrentFireRate - characterData.baseFireRate;
        maxHealth = CurrentMaxHealth - characterData.baseMaxHealth;
        moveSpeed = CurrentMoveSpeed - characterData.baseMoveSpeed;
        bulletSpeed = CurrentBulletSpeed - characterData.baseBulletSpeed;
        attackRange = CurrentAttackRange - characterData.baseAttackRange;
    }
}
