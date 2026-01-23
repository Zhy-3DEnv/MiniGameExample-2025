using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 通用生命值组件 - 所有可受伤的物体（角色、怪物）都可以挂这个组件。
/// 
/// 使用方式：
/// 1. 将本脚本挂载到需要生命值的对象上（玩家、怪物等）
/// 2. 在 Inspector 中设置最大生命值
/// 3. 外部通过 TakeDamage() 方法造成伤害
/// 4. 生命值归零时会触发 OnDeath 事件
/// 
/// 后续可以扩展：
/// - 护盾值（Shield）
/// - 无敌时间（Invincible）
/// - 伤害类型（物理/魔法/火焰等）
/// </summary>
public class Health : MonoBehaviour
{
    [Header("生命值配置")]
    [Tooltip("最大生命值")]
    public float maxHealth = 100f;

    [Tooltip("当前生命值（运行时自动计算）")]
    [SerializeField]
    private float currentHealth;

    [Header("事件")]
    [Tooltip("死亡时触发的事件（可以用于播放特效、掉落奖励等）")]
    public UnityEvent OnDeath;

    [Tooltip("受到伤害时触发的事件（可以用于播放受击特效、音效等）")]
    public UnityEvent<float> OnTakeDamage; // 参数：受到的伤害值

    /// <summary>
    /// 当前生命值（只读）
    /// </summary>
    public float CurrentHealth => currentHealth;

    /// <summary>
    /// 是否已死亡
    /// </summary>
    public bool IsDead => currentHealth <= 0f;

    /// <summary>
    /// 生命值百分比（0 到 1）
    /// </summary>
    public float HealthPercent => maxHealth > 0 ? currentHealth / maxHealth : 0f;

    private void Awake()
    {
        // 初始化：当前生命值 = 最大生命值
        currentHealth = maxHealth;
    }

    /// <summary>
    /// 造成伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(float damage)
    {
        if (IsDead)
            return; // 已经死了，不再处理伤害

        if (damage <= 0f)
            return; // 无效伤害

        // 扣除生命值
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth); // 确保不会小于 0

        // 触发受击事件
        OnTakeDamage?.Invoke(damage);

        // 如果生命值归零，触发死亡事件
        if (IsDead)
        {
            OnDeath?.Invoke();
        }
    }

    /// <summary>
    /// 治疗（恢复生命值）
    /// </summary>
    /// <param name="healAmount">治疗量</param>
    public void Heal(float healAmount)
    {
        if (IsDead)
            return; // 已经死了，无法治疗

        if (healAmount <= 0f)
            return;

        currentHealth += healAmount;
        currentHealth = Mathf.Min(maxHealth, currentHealth); // 确保不超过最大值
    }

    /// <summary>
    /// 设置最大生命值（并自动调整当前生命值）
    /// </summary>
    /// <param name="newMaxHealth">新的最大生命值</param>
    public void SetMaxHealth(float newMaxHealth)
    {
        if (newMaxHealth <= 0f)
            return;

        float healthPercent = HealthPercent; // 保存当前生命值百分比
        maxHealth = newMaxHealth;
        currentHealth = maxHealth * healthPercent; // 按比例调整当前生命值
    }

    /// <summary>
    /// 完全恢复生命值
    /// </summary>
    public void FullHeal()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// 立即死亡（用于特殊机制，如陷阱、即死技能等）
    /// </summary>
    public void Kill()
    {
        if (IsDead)
            return;

        currentHealth = 0f;
        OnDeath?.Invoke();
    }
}
