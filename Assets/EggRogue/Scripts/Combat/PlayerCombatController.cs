using UnityEngine;

/// <summary>
/// 玩家战斗控制器 - 自动瞄准最近的敌人并发射子弹。
/// 
/// 使用方式：
/// 1. 将本脚本挂载到玩家对象上（与 CharacterController 同级）
/// 2. 创建一个子弹 Prefab，拖到 Bullet Prefab 字段
/// 3. 在玩家对象下创建一个空物体作为 Fire Point（枪口位置），拖到 Fire Point 字段
/// 4. 配置攻击范围、射速、伤害等参数
/// 
/// 后续可以扩展：
/// - 武器系统（从 WeaponConfig 读取属性）
/// - 多武器切换
/// - 技能系统
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerCombatController : MonoBehaviour
{
    [Header("武器配置")]
    [Tooltip("子弹 Prefab")]
    public GameObject bulletPrefab;

    [Tooltip("发射点（枪口位置）")]
    public Transform firePoint;

    [Tooltip("攻击范围（搜索敌人的最大距离）")]
    public float attackRange = 10f;

    [Tooltip("攻击速度（每秒发射几发子弹，例如：2 = 每秒2发）")]
    public float fireRate = 2f;

    [Tooltip("单发伤害")]
    public float damagePerShot = 10f;

    [Tooltip("子弹移动速度（子弹飞行速度，单位：米/秒，例如：20 = 每秒20米）")]
    public float bulletSpeed = 20f;

    [Header("自动瞄准")]
    [Tooltip("是否自动瞄准最近的敌人")]
    public bool autoAim = true;

    [Header("朝向")]
    [Tooltip("角色是否始终朝向射击方向（与枪口一致）")]
    public bool faceFiringDirection = true;

    [Tooltip("朝向插值速度（0=瞬时转向，越大转向越平滑）")]
    public float faceRotationSpeed = 0f;

    private float fireCooldown = 0f;
    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        // 如果没有设置 Fire Point，尝试自动查找或创建
        if (firePoint == null)
        {
            // 查找名为 "FirePoint" 的子物体
            Transform found = transform.Find("FirePoint");
            if (found != null)
            {
                firePoint = found;
            }
            else
            {
                // 创建一个默认的 Fire Point（在玩家前方）
                GameObject fp = new GameObject("FirePoint");
                fp.transform.SetParent(transform);
                fp.transform.localPosition = new Vector3(0f, 0.5f, 0.5f); // 稍微在玩家前方和上方
                firePoint = fp.transform;
            }
        }
    }

    private void Update()
    {
        // 更新射击冷却
        if (fireCooldown > 0f)
        {
            fireCooldown -= Time.deltaTime;
        }

        // 角色始终朝向最近敌人（与射击方向一致），枪为子物体会一起旋转
        if (faceFiringDirection)
        {
            UpdateFacing();
        }

        // 如果冷却完成，尝试射击
        if (fireCooldown <= 0f && autoAim)
        {
            TryFire();
        }
    }

    /// <summary>
    /// 更新角色朝向。锁敌时朝向最近敌人；非锁敌时朝向键盘/摇杆移动方向。
    /// 枪为角色子物体会一起旋转。
    /// </summary>
    private void UpdateFacing()
    {
        Vector3 direction = Vector3.zero;

        // 锁敌：有敌人在攻击范围内 → 朝向敌人
        EnemyController targetEnemy = null;
        if (EnemyManager.Instance != null)
        {
            targetEnemy = EnemyManager.Instance.GetClosestEnemy(transform.position, attackRange);
        }

        if (targetEnemy != null)
        {
            direction = (targetEnemy.transform.position - transform.position).normalized;
            direction.y = 0f;
        }
        else
        {
            // 非锁敌：朝向键盘/摇杆移动方向
            Vector2 moveInput = characterController.GetMoveInput();
            if (moveInput.sqrMagnitude > 0.01f)
            {
                direction = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
            }
        }

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(direction);

        if (faceRotationSpeed <= 0f)
        {
            transform.rotation = targetRot;
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * faceRotationSpeed);
        }
    }

    /// <summary>
    /// 尝试开火（自动瞄准最近的敌人）
    /// </summary>
    private void TryFire()
    {
        // 检查是否有子弹 Prefab
        if (bulletPrefab == null)
        {
            Debug.LogWarning("PlayerCombatController: bulletPrefab 未设置！");
            return;
        }

        // 查找最近的敌人
        EnemyController targetEnemy = null;
        if (EnemyManager.Instance != null)
        {
            targetEnemy = EnemyManager.Instance.GetClosestEnemy(transform.position, attackRange);
        }

        // 如果没有敌人，不射击
        if (targetEnemy == null)
            return;

        // 计算射击方向
        Vector3 direction = (targetEnemy.transform.position - firePoint.position).normalized;
        direction.y = 0f; // 保持水平（XZ平面）

        // 发射子弹
        FireBullet(direction);
    }

    /// <summary>
    /// 发射子弹
    /// </summary>
    /// <param name="direction">发射方向</param>
    public void FireBullet(Vector3 direction)
    {
        if (bulletPrefab == null || firePoint == null)
            return;

        // 实例化子弹
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        // 初始化子弹（设置方向、伤害和速度）
        Projectile projectile = bullet.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(direction, damagePerShot, bulletSpeed);
        }
        else
        {
            // 如果没有 Projectile 组件，至少设置朝向
            bullet.transform.rotation = Quaternion.LookRotation(direction);
        }

        // 重置冷却时间
        fireCooldown = 1f / fireRate;
    }

    /// <summary>
    /// 设置射速（外部调用，例如从属性系统读取）
    /// </summary>
    public void SetFireRate(float newFireRate)
    {
        fireRate = Mathf.Max(0.1f, newFireRate); // 防止除零
    }

    /// <summary>
    /// 设置伤害（外部调用，例如从属性系统读取）
    /// </summary>
    public void SetDamage(float newDamage)
    {
        damagePerShot = newDamage;
    }

    /// <summary>
    /// 设置攻击范围（外部调用）
    /// </summary>
    public void SetAttackRange(float newRange)
    {
        attackRange = newRange;
    }

    /// <summary>
    /// 设置子弹移动速度（外部调用）
    /// </summary>
    public void SetBulletSpeed(float newSpeed)
    {
        bulletSpeed = Mathf.Max(0.1f, newSpeed);
    }
}
