using UnityEngine;
using EggRogue;

/// <summary>
/// 角色属性管理器 - 管理角色的所有属性（基础值 + 卡片加成）。
/// 挂载在角色对象上，引用 CharacterData 作为基础配置。
/// </summary>
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(CharacterController))]
public class CharacterStats : MonoBehaviour
{
    [Header("角色配置")]
    [Tooltip("角色数据（ScriptableObject）。若为空，会尝试从 CharacterSelectionManager 获取选中角色")]
    public CharacterData characterData;

    // 当前属性（运行时，包含卡片加成，只读）
    // 注意：属性（property）不能使用 [Header] 和 [Tooltip]，所以这些属性在 Inspector 中不会显示
    // 可以通过 CharacterInfoPanel 查看当前属性值
    public float CurrentDamage { get; set; }
    public float CurrentFireRate { get; set; }
    public float CurrentMaxHealth { get; set; }
    public float CurrentMoveSpeed { get; set; }
    public float CurrentBulletSpeed { get; set; }
    public float CurrentAttackRange { get; set; }
    public float CurrentPickupRange { get; set; }

    private Health health;
    private CharacterController characterController;
    private PlayerCombatController combatController;

    private void Awake()
    {
        health = GetComponent<Health>();
        characterController = GetComponent<CharacterController>();
        combatController = GetComponent<PlayerCombatController>();
        // 若使用 WeaponController， combatController 可能被禁用，但引用仍可用（用于卡片加成等）
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
        CurrentPickupRange = characterData.basePickupRange;

        // 如果有卡片管理器，叠加当前已选卡片的总加成（用于跨关卡继承成长）
        if (CardManager.Instance != null)
        {
            CardManager.Instance.GetTotalBonuses(
                out float bonusDamage,
                out float bonusFireRate,
                out float bonusMaxHealth,
                out float bonusMoveSpeed,
                out float bonusBulletSpeed,
                out float bonusAttackRange,
                out float bonusPickupRange);

            CurrentDamage += bonusDamage;
            CurrentFireRate += bonusFireRate;
            CurrentMaxHealth += bonusMaxHealth;
            CurrentMoveSpeed += bonusMoveSpeed;
            CurrentBulletSpeed += bonusBulletSpeed;
            CurrentAttackRange += bonusAttackRange;
            CurrentPickupRange += bonusPickupRange;
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
    /// 应用卡片加成（CardManager 调用）。按等级加成表取值并应用。
    /// </summary>
    public void ApplyCardBonus(CardData card, int level)
    {
        if (card == null) return;
        ApplyCardBonus(card.GetBonusForLevel(level));
    }

    /// <summary>
    /// 应用卡片等级加成（内部使用）。
    /// </summary>
    public void ApplyCardBonus(CardLevelBonus bonus)
    {
        float oldDamage = CurrentDamage;
        float oldFireRate = CurrentFireRate;
        float oldMaxHealth = CurrentMaxHealth;
        float oldMoveSpeed = CurrentMoveSpeed;
        float oldBulletSpeed = CurrentBulletSpeed;
        float oldAttackRange = CurrentAttackRange;

        CurrentDamage += bonus.damageBonus;
        CurrentFireRate += bonus.fireRateBonus;
        CurrentMaxHealth += bonus.maxHealthBonus;
        CurrentMoveSpeed += bonus.moveSpeedBonus;
        CurrentBulletSpeed += bonus.bulletSpeedBonus;
        CurrentAttackRange += bonus.attackRangeBonus;
        CurrentPickupRange += bonus.pickupRangeBonus;

        Debug.Log(
            $"CharacterStats: ApplyCardBonus Lv{bonus.level} " +
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
        out float moveSpeed, out float bulletSpeed, out float attackRange, out float pickupRange)
    {
        if (characterData != null)
        {
            damage = characterData.baseDamage;
            fireRate = characterData.baseFireRate;
            maxHealth = characterData.baseMaxHealth;
            moveSpeed = characterData.baseMoveSpeed;
            bulletSpeed = characterData.baseBulletSpeed;
            attackRange = characterData.baseAttackRange;
            pickupRange = characterData.basePickupRange;
        }
        else
        {
            damage = 0f;
            fireRate = 0f;
            maxHealth = 0f;
            moveSpeed = 0f;
            bulletSpeed = 0f;
            attackRange = 0f;
            pickupRange = 0.5f;
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
        out float moveSpeed, out float bulletSpeed, out float attackRange, out float pickupRange)
    {
        if (characterData == null)
        {
            damage = 0f;
            fireRate = 0f;
            maxHealth = 0f;
            moveSpeed = 0f;
            bulletSpeed = 0f;
            attackRange = 0f;
            pickupRange = 0f;
            return;
        }

        // 当前值 - 基础值 = 卡片加成
        damage = CurrentDamage - characterData.baseDamage;
        fireRate = CurrentFireRate - characterData.baseFireRate;
        maxHealth = CurrentMaxHealth - characterData.baseMaxHealth;
        moveSpeed = CurrentMoveSpeed - characterData.baseMoveSpeed;
        bulletSpeed = CurrentBulletSpeed - characterData.baseBulletSpeed;
        attackRange = CurrentAttackRange - characterData.baseAttackRange;
        pickupRange = CurrentPickupRange - characterData.basePickupRange;
    }

    /// <summary>
    /// 计算当前角色使用一把武器时的「基础攻击伤害」。
    ///
    /// 设计目标：
    /// - 角色成长（等级、卡牌、被动）统一体现在 CurrentDamage 上；
    /// - 武器本身的数值体现在 WeaponData.damage 上；
    /// - 不同角色可以通过调整系数，让“更吃武器”或“更吃自身数值”的风格清晰可控。
    /// </summary>
    /// <param name="weapon">当前攻击使用的武器（可空）</param>
    public float GetBaseAttackDamage(WeaponData weapon)
    {
        // 角色当前伤害（已包含：角色基础值 + 卡片加成 + 被动修正）
        float charBase = CurrentDamage;

        // 武器伤害（来自 WeaponData）
        float weaponBase = weapon != null ? weapon.damage : 0f;

        // 角色与武器的权重系数：后续可按角色类型、被动等做差异化
        float charFactor = 1f;
        float weaponFactor = 1f;

        // 示例：如果以后想做“武器依赖型角色”，可以这样写：
        // if (characterData != null && characterData.characterName == "武器大师")
        // {
        //     charFactor = 0.5f;
        //     weaponFactor = 1.5f;
        // }

        float dmg = charBase * charFactor + weaponBase * weaponFactor;

        // 玻璃大炮等“最终总伤害倍率”类被动：在所有加成叠好之后再做一次整体倍率
        if (characterData != null && characterData.passiveAbilities != null)
        {
            foreach (var passive in characterData.passiveAbilities)
            {
                if (passive is GlassCannonPassive glass)
                {
                    dmg *= glass.damageMultiplier;
                }
            }
        }

        return Mathf.Max(0f, dmg);
    }

    /// <summary>
    /// 计算当前角色使用一把武器时的「基础攻击速度」（每秒攻击次数）。
    ///
    /// 设计目标：
    /// - 角色的攻速成长统一反映在 CurrentFireRate；
    /// - 武器本身的攻速在 WeaponData.fireRate；
    /// - 通过系数控制：有的角色更吃自身攻速，有的更吃武器攻速。
    /// </summary>
    public float GetBaseFireRate(WeaponData weapon)
    {
        float charBase = CurrentFireRate;                 // 角色当前攻速（含等级/卡牌/被动）
        float weaponBase = weapon != null ? weapon.fireRate : 0f; // 武器自身攻速

        float charFactor = 1f;
        float weaponFactor = 1f;

        // 将来可按角色区分风格：
        // if (characterData != null && characterData.characterName == "快速射手")
        // {
        //     charFactor = 1.2f;
        //     weaponFactor = 0.8f;
        // }

        float rate = charBase * charFactor + weaponBase * weaponFactor;
        return Mathf.Max(0.1f, rate); // 防止除零
    }

        /// <summary>
        /// 在 Scene 视图中显示角色当前攻击范围（黄圈）和拾取范围（青圈），用于调试。
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (GetComponent<DebugDrawController>() != null) return;
            if (characterData == null && _instanceNotInitialized())
                return;

            Vector3 center = transform.position;
            int segments = 32;

            // 攻击范围（黄色）- 编辑模式下用 characterData 基础值
            float attackRange = Application.isPlaying ? CurrentAttackRange : (characterData != null ? characterData.baseAttackRange : 0f);
            if (attackRange > 0f)
            {
                Gizmos.color = Color.yellow;
                Vector3 prev = center + new Vector3(attackRange, 0f, 0f);
                for (int i = 1; i <= segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    Vector3 next = center + new Vector3(Mathf.Cos(angle) * attackRange, 0f, Mathf.Sin(angle) * attackRange);
                    Gizmos.DrawLine(prev, next);
                    prev = next;
                }
            }

            // 拾取范围（青色）
            float pickupRange = Application.isPlaying
                ? Mathf.Max(0.1f, CurrentPickupRange + EggRogue.ItemEffectManager.GetPickupRangeBonus())
                : (characterData != null ? characterData.basePickupRange : 0.5f);
            if (pickupRange > 0f)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.8f);
                Vector3 prev = center + new Vector3(pickupRange, 0f, 0f);
                for (int i = 1; i <= segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    Vector3 next = center + new Vector3(Mathf.Cos(angle) * pickupRange, 0f, Mathf.Sin(angle) * pickupRange);
                    Gizmos.DrawLine(prev, next);
                    prev = next;
                }
            }
        }

        // 简单判断：在编辑器下未运行且尚未初始化 stats 时不画圈
        private bool _instanceNotInitialized()
        {
            return Application.isPlaying == false && CurrentAttackRange <= 0.0001f;
        }
}
