using UnityEngine;

/// <summary>
/// 子弹/投射物 - 从玩家武器发射，命中敌人造成伤害。
/// 
/// 使用方式：
/// 1. 创建子弹 Prefab，添加：Projectile、Collider(IsTrigger)、Rigidbody(IsKinematic)
/// 2. Unity 要求：OnTriggerEnter 至少一方有 Rigidbody，故子弹必须有 kinematic Rigidbody
/// 3. 在 Inspector 配置伤害、速度、生命周期、Target Tag = Enemy
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("子弹属性")]
    [Tooltip("伤害值")]
    public float damage = 10f;

    [Tooltip("移动速度（子弹飞行速度，单位：米/秒）")]
    public float speed = 20f;

    [Tooltip("生命周期（秒），超过时间自动销毁")]
    public float lifeTime = 5f;

    [Header("目标设置")]
    [Tooltip("目标标签（例如 'Enemy'）")]
    public string targetTag = "Enemy";

    [Tooltip("是否穿透（穿透模式下不会在命中时销毁）")]
    public bool isPiercing = false;

    private float lifeTimer = 0f;
    private bool hit;

    private void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
    }

    private void Update()
    {
        if (hit) return;

        transform.position += transform.forward * speed * Time.deltaTime;
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
        if (hit) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Projectile] OnTriggerEnter: other={other.name}, tag={other.tag}, layer={other.gameObject.layer}");
#endif

        if (!other.CompareTag(targetTag))
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Projectile] 忽略：Tag 不匹配（期望 '{targetTag}'）");
#endif
            return;
        }

        Health enemyHealth = other.GetComponentInParent<Health>();
        if (enemyHealth == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Projectile] 忽略：{other.name} 及其父节点无 Health 组件");
#endif
            return;
        }
        if (enemyHealth.IsDead)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Projectile] 忽略：敌人已死亡");
#endif
            return;
        }

        enemyHealth.TakeDamage(damage);

        if (!isPiercing)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Projectile] 命中 {other.name}，销毁子弹");
#endif
            hit = true;
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 初始化子弹（外部调用，用于设置方向和伤害等）
    /// </summary>
    /// <param name="direction">发射方向</param>
    /// <param name="bulletDamage">
    /// 传入的伤害值；传入负数表示“不修改当前伤害”，传入 0 也会生效（可用于完全关闭伤害）。
    /// </param>
    /// <param name="bulletSpeed">
    /// 传入的移动速度；传入负数表示“不修改当前速度”，传入 0 会被 Clamp 为一个很小的正数。
    /// </param>
    /// <param name="bulletLifeTime">
    /// 传入的飞行时间（秒）；传入负数表示“不修改当前值”。
    /// </param>
    public void Initialize(Vector3 direction, float bulletDamage = -1f, float bulletSpeed = -1f, float bulletLifeTime = -1f)
    {
        // 设置朝向
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // 如果传入的伤害值非负，则覆盖默认值（允许设置为0）
        if (bulletDamage >= 0f)
        {
            damage = bulletDamage;
        }

        // 如果传入的速度非负，则覆盖默认值（0 会被后续逻辑 Clamp）
        if (bulletSpeed >= 0f)
        {
            speed = Mathf.Max(0.1f, bulletSpeed);
        }

        // 如果传入的飞行时间非负，则覆盖默认值（由武器配置）
        if (bulletLifeTime >= 0f)
        {
            lifeTime = Mathf.Max(0.1f, bulletLifeTime);
        }
    }

    /// <summary>
    /// 设置子弹移动速度（外部调用）
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        speed = Mathf.Max(0.1f, newSpeed);
    }
}
