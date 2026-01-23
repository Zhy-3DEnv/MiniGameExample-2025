using UnityEngine;

/// <summary>
/// 子弹/投射物 - 从玩家武器发射，命中敌人造成伤害。
/// 
/// 使用方式：
/// 1. 创建一个子弹 Prefab（例如一个小球或胶囊体）
/// 2. 在子弹 Prefab 上添加：
///    - 本脚本（Projectile）
///    - Collider（勾选 IsTrigger）
///    - （可选）Rigidbody（勾选 IsKinematic）
/// 3. 在 Inspector 中配置伤害、速度、生命周期
/// 
/// 注意：
/// - 子弹会自动朝 forward 方向移动
/// - 命中敌人时会调用敌人的 Health.TakeDamage()
/// - 超过生命周期或命中目标后自动销毁
/// </summary>
[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    [Header("子弹属性")]
    [Tooltip("伤害值")]
    public float damage = 10f;

    [Tooltip("移动速度")]
    public float speed = 10f;

    [Tooltip("生命周期（秒），超过时间自动销毁")]
    public float lifeTime = 5f;

    [Header("目标设置")]
    [Tooltip("目标标签（例如 'Enemy'）")]
    public string targetTag = "Enemy";

    [Tooltip("是否穿透（穿透模式下不会在命中时销毁）")]
    public bool isPiercing = false;

    private float lifeTimer = 0f;

    private void Start()
    {
        // 确保 Collider 是 Trigger
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"Projectile: {gameObject.name} 的 Collider 应该勾选 IsTrigger！");
        }
    }

    private void Update()
    {
        // 移动子弹
        transform.position += transform.forward * speed * Time.deltaTime;

        // 生命周期倒计时
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 碰撞检测（Trigger 模式）
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // 检查是否是目标
        if (!other.CompareTag(targetTag))
            return;

        // 获取敌人的 Health 组件
        Health enemyHealth = other.GetComponent<Health>();
        if (enemyHealth != null)
        {
            // 造成伤害
            enemyHealth.TakeDamage(damage);
        }

        // 如果不是穿透子弹，命中后销毁
        if (!isPiercing)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 初始化子弹（外部调用，用于设置方向和伤害等）
    /// </summary>
    /// <param name="direction">发射方向</param>
    /// <param name="bulletDamage">伤害值（可选，如果不传则使用默认值）</param>
    public void Initialize(Vector3 direction, float bulletDamage = -1f)
    {
        // 设置朝向
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // 如果指定了伤害值，覆盖默认值
        if (bulletDamage > 0f)
        {
            damage = bulletDamage;
        }
    }
}
