using UnityEngine;

/// <summary>
/// 怪物控制器 - 控制怪物的移动和基础行为。
/// 
/// 使用方式：
/// 1. 将本脚本挂载到怪物 Prefab 上
/// 2. 怪物 Prefab 需要：
///    - Health 组件（用于受伤和死亡）
///    - Collider（用于子弹碰撞检测）
///    - （可选）Rigidbody（如果使用物理碰撞）
/// 3. 在 Inspector 中配置移动速度和停止距离
/// 
/// 注意：
/// - 怪物会自动寻找场景中的玩家（通过 Tag "Player" 或 CharacterController）
/// - 怪物死亡时会自动从 EnemyManager 中注销
/// </summary>
[RequireComponent(typeof(Health))]
public class EnemyController : MonoBehaviour
{
    [Header("移动配置")]
    [Tooltip("移动速度")]
    [SerializeField] private float moveSpeed = 3f;

    [Tooltip("停止距离（距离玩家多近时停止移动）")]
    [SerializeField] private float stoppingDistance = 1.5f;

    [Header("地面约束")]
    [Tooltip("地面高度（Y轴固定值，确保怪物不脱离地面）")]
    [SerializeField] private float groundHeight = 0f;

    [Tooltip("是否强制锁定Y轴（推荐开启，防止怪物在高度上重叠）")]
    [SerializeField] private bool lockYAxis = true;

    [Header("目标")]
    [Tooltip("目标（玩家），如果为空会自动查找")]
    [SerializeField] private Transform target;

    private Health health;
    private Transform _transform;
    private bool isDead = false;

    private void Awake()
    {
        // 缓存 Transform 引用（性能优化）
        _transform = transform;

        // 获取 Health 组件
        health = GetComponent<Health>();
        if (health == null)
        {
            Debug.LogError($"EnemyController: {gameObject.name} 需要 Health 组件！");
        }

        // 订阅死亡事件
        if (health != null)
        {
            health.OnDeath.AddListener(OnEnemyDeath);
        }
    }

    private void Start()
    {
        // 自动查找玩家目标
        if (target == null)
        {
            FindPlayerTarget();
        }

        // 注册到 EnemyManager（如果存在）
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RegisterEnemy(this);
        }

        // 初始化时锁定Y轴（如果启用）
        if (lockYAxis)
        {
            Vector3 pos = _transform.position;
            pos.y = groundHeight;
            _transform.position = pos;
        }
    }

    private void Update()
    {
        // 如果已死亡，不处理移动
        if (isDead)
            return;

        // 如果目标丢失，尝试重新查找
        if (target == null)
        {
            FindPlayerTarget();
            return;
        }

        // 计算到玩家的距离
        float distanceToTarget = Vector3.Distance(_transform.position, target.position);

        // 如果距离大于停止距离，朝玩家移动
        if (distanceToTarget > stoppingDistance)
        {
            Vector3 direction = (target.position - _transform.position).normalized;
            direction.y = 0f; // 保持在地面上（XZ平面移动）

            // 移动怪物（只在XZ平面移动）
            Vector3 movement = direction * moveSpeed * Time.deltaTime;
            Vector3 newPosition = _transform.position + movement;
            
            // 强制锁定Y轴（防止怪物在高度上重叠）
            if (lockYAxis)
            {
                newPosition.y = groundHeight;
            }
            
            _transform.position = newPosition;

            // （可选）让怪物面向移动方向
            if (direction.sqrMagnitude > 0.01f)
            {
                _transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    /// <summary>
    /// LateUpdate：在所有Update之后执行，确保Y轴锁定不会被其他系统覆盖
    /// </summary>
    private void LateUpdate()
    {
        // 如果已死亡，不处理
        if (isDead)
            return;

        // 强制锁定Y轴（防止怪物在高度上重叠）
        // 这个在LateUpdate中执行，确保即使其他系统修改了位置，Y轴也会被锁定
        if (lockYAxis)
        {
            Vector3 pos = _transform.position;
            pos.y = groundHeight;
            _transform.position = pos;
        }
    }

    /// <summary>
    /// 查找玩家目标
    /// 注意：此方法使用了性能开销较大的 Find 方法，应避免在 Update 中频繁调用
    /// </summary>
    private void FindPlayerTarget()
    {
        // 方法1：通过 Tag 查找
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
            return;
        }

        // 方法2：通过 CharacterController 查找
        CharacterController playerController = FindObjectOfType<CharacterController>();
        if (playerController != null)
        {
            target = playerController.transform;
            return;
        }

        // 如果都找不到，打印警告
        Debug.LogWarning($"EnemyController: {gameObject.name} 未找到玩家目标，请确保场景中有 Tag 为 'Player' 的对象或 CharacterController。");
    }

    /// <summary>
    /// 怪物死亡时的回调
    /// </summary>
    private void OnEnemyDeath()
    {
        isDead = true;

        // 从 EnemyManager 注销
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.UnregisterEnemy(this);
        }

        // 可以在这里播放死亡动画、特效、掉落奖励等
        // 暂时直接销毁
        Destroy(gameObject, 0.1f); // 延迟一点销毁，方便播放特效
    }

    private void OnDestroy()
    {
        // 确保从 EnemyManager 注销（防止重复注销）
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.UnregisterEnemy(this);
        }
    }

    /// <summary>
    /// 获取到玩家的距离
    /// </summary>
    public float GetDistanceToPlayer()
    {
        if (target == null)
            return float.MaxValue;

        return Vector3.Distance(_transform.position, target.position);
    }
}
