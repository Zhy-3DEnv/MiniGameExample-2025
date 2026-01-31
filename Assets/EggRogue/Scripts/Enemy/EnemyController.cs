using UnityEngine;
using EggRogue;

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
    [Tooltip("移动速度（可在运行时通过 SetMoveSpeed 修改）")]
    [SerializeField] private float moveSpeed = 3f;

    [Tooltip("停止距离（距离玩家多近时停止移动）")]
    [SerializeField] private float stoppingDistance = 1.5f;

    [Header("地面约束")]
    [Tooltip("地面高度（Y轴固定值，确保怪物不脱离地面）")]
    [SerializeField] private float groundHeight = 0f;

    [Tooltip("是否强制锁定Y轴（推荐开启，防止怪物在高度上重叠）")]
    [SerializeField] private bool lockYAxis = true;

    [Header("出场")]
    [Tooltip("出场时长（秒），0 表示不演出、直接可移动攻击")]
    [SerializeField] private float emergenceDuration = 1f;

    [Tooltip("出场时从地面下沉的深度（Y 轴，用于“从地下冒出”等效果）")]
    [SerializeField] private float emergenceOffsetY = 1f;

        [Header("目标")]
        [Tooltip("目标（玩家），如果为空会自动查找")]
        [SerializeField] private Transform target;

        [Header("攻击配置")]
        [Tooltip("接触玩家时造成的伤害（每次接触冷却一次）")]
        [SerializeField] private float contactDamage = 5f;

        [Tooltip("接触攻击冷却时间（秒），避免每帧多次伤害")]
        [SerializeField] private float contactAttackCooldown = 0.5f;

    [Header("掉落")]
    [Tooltip("金币 Prefab（需含 Coin 组件），不填则不掉落")]
    [SerializeField] private GameObject coinPrefab;

    [Tooltip("敌人数据（ScriptableObject）- 如果设置，会覆盖下面的掉落配置")]
    [SerializeField] private EnemyData enemyData;

    [Tooltip("最少掉落金币数（如果 enemyData 未设置则使用此值）")]
    [SerializeField] private int coinDropMin = 1;

    [Tooltip("最多掉落金币数（如果 enemyData 未设置则使用此值）")]
    [SerializeField] private int coinDropMax = 2;

    [Tooltip("掉落随机偏移半径（XZ），避免叠在一起）")]
    [SerializeField] private float coinDropRadius = 0.3f;

        private Health health;
        private Transform _transform;
        private bool isDead = false;
        private bool isPaused = false;
        private float lastContactAttackTime = -999f;

        private bool isEmerging = false;
        private float emergenceElapsed = 0f;
        private float emergenceSurfaceY = 0f;
        private float emergenceStartY = 0f;

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

        // 出场：若配置了出场时长，从地下冒出（以生成点 Y 为地面）
        emergenceSurfaceY = _transform.position.y;
        if (emergenceDuration > 0f && emergenceOffsetY > 0f)
        {
            emergenceStartY = emergenceSurfaceY - emergenceOffsetY;
            Vector3 pos = _transform.position;
            pos.y = emergenceStartY;
            _transform.position = pos;
            isEmerging = true;
            emergenceElapsed = 0f;
        }
        else if (lockYAxis)
        {
            Vector3 pos = _transform.position;
            pos.y = groundHeight;
            _transform.position = pos;
        }
    }

    private void Update()
    {
        if (isDead)
            return;

        // 出场中：只推进冒出动画，不移动；暂停时也不推进
        if (isEmerging)
        {
            if (!isPaused)
                emergenceElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(emergenceElapsed / emergenceDuration);
            Vector3 pos = _transform.position;
            pos.y = Mathf.Lerp(emergenceStartY, emergenceSurfaceY, t);
            _transform.position = pos;
            if (t >= 1f)
            {
                isEmerging = false;
                pos.y = emergenceSurfaceY;
                _transform.position = pos;
            }
            return;
        }

        if (isPaused)
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
        if (isDead)
            return;
        // 出场中 Y 由冒出动画控制，不在这里覆盖
        if (isEmerging)
            return;
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

        // 掉落 1–2 个金币
        DropCoins();

        Destroy(gameObject, 0.1f);
    }

    private void DropCoins()
    {
        if (coinPrefab == null) return;

        // 优先使用 EnemyData SO 的配置
        int min = coinDropMin;
        int max = coinDropMax;
        float radius = coinDropRadius;

        if (enemyData != null)
        {
            min = enemyData.coinDropMin;
            max = enemyData.coinDropMax;
            radius = enemyData.coinDropRadius;
        }

        // 应用关卡倍率（如果存在）
        EggRogue.LevelData levelData = EggRogue.LevelManager.Instance?.GetCurrentLevelData();
        if (levelData != null && levelData.coinDropMultiplier > 0f)
        {
            // 倍率只影响数量，不影响范围
            float mult = levelData.coinDropMultiplier;
            min = Mathf.RoundToInt(min * mult);
            max = Mathf.RoundToInt(max * mult);
        }

        int n = Random.Range(min, max + 1);
        Vector3 basePos = _transform.position;
        for (int i = 0; i < n; i++)
        {
            Vector2 r = Random.insideUnitCircle * radius;
            Vector3 pos = basePos + new Vector3(r.x, 0f, r.y);
            pos.y = groundHeight;
            Instantiate(coinPrefab, pos, Quaternion.identity);
        }
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
    /// 当前敌人对应的 EnemyData（用于按类型统计等）。
    /// </summary>
    public EnemyData EnemyData => enemyData;

    /// <summary>
    /// 获取到玩家的距离
    /// </summary>
    public float GetDistanceToPlayer()
    {
        if (target == null)
            return float.MaxValue;

        return Vector3.Distance(_transform.position, target.position);
    }

    /// <summary>
    /// 设置移动速度（外部调用，例如从配置系统读取）
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    /// <summary>
    /// 设置敌人数据（EnemySpawner 调用）
    /// </summary>
    public void SetEnemyData(EnemyData data)
    {
        enemyData = data;

        // 使用 EnemyData 中的配置覆盖局部默认值（例如接触伤害）
        if (enemyData != null)
        {
            contactDamage = enemyData.baseDamage;
        }
    }

    /// <summary>
    /// 暂停敌人移动（用于结算界面等）
    /// </summary>
    public void PauseMovement()
    {
        isPaused = true;
    }

    /// <summary>
    /// 恢复敌人移动
    /// </summary>
    public void ResumeMovement()
    {
        isPaused = false;
    }

    /// <summary>
    /// 与玩家发生触发器接触时，对玩家造成伤害。
    /// 需要怪物 Collider 勾选 IsTrigger，玩家身上有 Collider + Rigidbody + Health + CharacterController。
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        if (isDead || isPaused || isEmerging)
            return;

        // 通过 CharacterController 判断是否为玩家
        CharacterController playerController = other.GetComponentInParent<CharacterController>();
        if (playerController == null)
            return;

        // 冷却未结束，不重复伤害
        if (Time.time < lastContactAttackTime + contactAttackCooldown)
            return;

        Health playerHealth = playerController.GetComponent<Health>();
        if (playerHealth == null || playerHealth.IsDead)
            return;

        float damageToDeal = Mathf.Max(0f, contactDamage);
        if (damageToDeal <= 0f)
            return;

        playerHealth.TakeDamage(damageToDeal);
        lastContactAttackTime = Time.time;

        Debug.Log($"EnemyController: {gameObject.name} 对玩家造成接触伤害 {damageToDeal}");
    }
}
