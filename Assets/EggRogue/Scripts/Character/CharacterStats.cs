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
    [Tooltip("角色数据（ScriptableObject）。若为空，会尝试从 CharacterSelectionManager 获取选中角色")]
    public CharacterData characterData;

    // 当前属性（运行时，包含卡片加成，只读）
    // 注意：属性（property）不能使用 [Header] 和 [Tooltip]，所以这些属性在 Inspector 中不会显示
    // 可以通过 AttributePanel 查看当前属性值
    public float CurrentDamage { get; set; }
    public float CurrentFireRate { get; set; }
    public float CurrentMaxHealth { get; set; }
    public float CurrentMoveSpeed { get; set; }
    public float CurrentBulletSpeed { get; set; }
    public float CurrentAttackRange { get; set; }

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
        // 若未设置 characterData，尝试从 CharacterSelectionManager 获取玩家选择的角色
        if (characterData == null && CharacterSelectionManager.Instance != null)
        {
            characterData = CharacterSelectionManager.Instance.SelectedCharacter;
            Debug.Log($"CharacterStats: 从 CharacterSelectionManager 获取角色 - {(characterData != null ? characterData.characterName : "null")}");
        }

        if (characterData == null)
        {
            Debug.LogWarning("CharacterStats: characterData 未设置且未从 CharacterSelectionManager 获取到角色，使用默认值");
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

        // 应用角色被动能力（在卡片加成之后，最终应用到组件之前）
        ApplyPassiveAbilities();

        // 应用到组件（先按满血初始化）
        ApplyStatsToComponents();

        // 单局继承：若上一关保存了血量，恢复当前血量而非满血
        if (LevelManager.Instance != null && LevelManager.Instance.TryGetRunStateHealth(out float savedCurrent, out float _))
        {
            if (health != null)
                health.SetCurrentHealth(Mathf.Clamp(savedCurrent, 0f, CurrentMaxHealth));
        }
    }

    /// <summary>
    /// 应用属性到各个组件
    /// </summary>
    /// <param name="fullHeal">
    /// true  = 将生命值直接回满（用于初始化 / 重新开始关卡）；
    /// false = 按“最大生命值增量”来调整当前生命值（用于选卡加成：例如 15/20 +5 → 20/25）。
    /// </param>
    private void ApplyStatsToComponents(bool fullHeal = true)
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
            if (fullHeal)
            {
                // 初始化场景时：直接设定最大生命并回满
                health.SetMaxHealth(CurrentMaxHealth);
                health.FullHeal();
            }
            else
            {
                // 选卡等成长场景：希望表现为
                //   旧： current / max
                //   新： (current + Δmax) / (max + Δmax) （同时不超过新最大值）
                float oldCurrent = health.CurrentHealth;
                float oldMax = health.maxHealth;
                float newMax = CurrentMaxHealth;
                float bonusMax = newMax - oldMax;

                // 先根据 Health 自身逻辑调整最大生命值（保持血量百分比不变）
                health.SetMaxHealth(newMax);

                // 目标当前生命值：在旧生命值基础上增加与最大生命值相同的增量，再受新最大值上限限制
                float targetCurrent = Mathf.Min(oldCurrent + bonusMax, newMax);

                // 通过 Heal / TakeDamage 调整到目标生命值，保持事件/状态一致
                float delta = targetCurrent - health.CurrentHealth;
                if (delta > 0f)
                    health.Heal(delta);
                else if (delta < 0f)
                    health.TakeDamage(-delta);
            }
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
        
        float oldDamage = CurrentDamage;
        float oldFireRate = CurrentFireRate;
        float oldMaxHealth = CurrentMaxHealth;
        float oldMoveSpeed = CurrentMoveSpeed;
        float oldBulletSpeed = CurrentBulletSpeed;
        float oldAttackRange = CurrentAttackRange;

        // 累加卡片加成
        CurrentDamage      += card.damageBonus;
        CurrentFireRate    += card.fireRateBonus;
        CurrentMaxHealth   += card.maxHealthBonus;
        CurrentMoveSpeed   += card.moveSpeedBonus;
        CurrentBulletSpeed += card.bulletSpeedBonus;
        CurrentAttackRange += card.attackRangeBonus;

        Debug.Log(
            $"CharacterStats: ApplyCardBonus({card.cardName}) " +
            $"Damage {oldDamage}->{CurrentDamage}, FireRate {oldFireRate}->{CurrentFireRate}, " +
            $"MaxHealth {oldMaxHealth}->{CurrentMaxHealth}, MoveSpeed {oldMoveSpeed}->{CurrentMoveSpeed}, " +
            $"BulletSpeed {oldBulletSpeed}->{CurrentBulletSpeed}, AttackRange {oldAttackRange}->{CurrentAttackRange}");

        // 重新应用到组件，但不强制回满生命值，而是按“最大生命值增量”调整当前生命
        ApplyStatsToComponents(fullHeal: false);
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
    /// 应用角色被动能力。在基础值 + 卡片加成之后，最终应用到组件之前调用。
    /// </summary>
    private void ApplyPassiveAbilities()
    {
        if (characterData == null || characterData.passiveAbilities == null)
            return;

        foreach (var passive in characterData.passiveAbilities)
        {
            if (passive != null)
            {
                passive.ModifyStats(this);
            }
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
