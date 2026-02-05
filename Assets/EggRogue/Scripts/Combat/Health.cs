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

    [Header("受击反馈")]
    [Tooltip("是否启用受击后短暂无敌（防止一瞬间被打空血量）")]
    public bool useInvincibility = false;

    [Tooltip("受击后无敌时间（秒）")]
    public float invincibleDuration = 0.5f;

    [Tooltip("受击飘字的预制体（可选，需带有 FlappyBird.ScorePopup 组件）")]
    public GameObject damagePopupPrefab;

    private float invincibleUntilTime = 0f;

    private void Awake()
    {
        // 初始化：当前生命值 = 最大生命值
        currentHealth = maxHealth;
    }

    /// <summary>
    /// 伤害修正回调： (原始伤害, 伤害来源) => 修正后伤害。用于护盾减伤等。
    /// </summary>
    public System.Func<float, GameObject, float> DamageModifier;

    /// <summary>
    /// 受到伤害时且来源非空时的回调，用于荆棘反弹等。
    /// </summary>
    public System.Action<float, GameObject> OnDamageFrom;

    /// <summary>
    /// 造成伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    /// <param name="damageSource">伤害来源（如敌人 GameObject，用于荆棘反弹）</param>
    public void TakeDamage(float damage, GameObject damageSource = null)
    {
        if (IsDead)
            return;

        if (useInvincibility && Time.time < invincibleUntilTime)
            return;

        if (damage <= 0f)
            return;

        if (DamageModifier != null)
            damage = DamageModifier(damage, damageSource);

        if (damage <= 0f)
            return;

        // 扣除生命值
        currentHealth -= damage;
        currentHealth = Mathf.Max(0f, currentHealth); // 确保不会小于 0

        // 设置新的无敌结束时间
        if (useInvincibility)
        {
            invincibleUntilTime = Time.time + invincibleDuration;
        }

        // 触发受击事件
        OnTakeDamage?.Invoke(damage);

        // 受击飘字（如果配置了预制体）
        if (damagePopupPrefab != null)
        {
            try
            {
                GameObject popup = Instantiate(damagePopupPrefab);

                // 兼容 FlappyBird.ScorePopup 的接口
                var popupScript = popup.GetComponent<FlappyBird.ScorePopup>();
                if (popupScript != null)
                {
                    // 位置：略高于当前对象
                    Vector3 worldPos = transform.position + Vector3.up * 1.0f;
                    popupScript.SetPosition(worldPos);
                    popupScript.SetDamage(Mathf.RoundToInt(damage));
                }
                else
                {
                    Destroy(popup);
                }
            }
            catch
            {
                // 如果预制体缺组件，避免在受击时抛异常影响游戏流程
            }
        }

        if (IsDead)
            OnDeath?.Invoke();

        if (damageSource != null && OnDamageFrom != null)
            OnDamageFrom.Invoke(damage, damageSource);
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
    /// 直接设置当前生命值（用于跨关恢复等）。会被钳制到 [0, maxHealth]。
    /// </summary>
    public void SetCurrentHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);
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
