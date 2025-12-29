using UnityEngine;

/// <summary>
/// 子弹脚本
/// 控制子弹的移动、伤害和碰撞检测
/// </summary>
public class BulletScript : MonoBehaviour
{
    [Header("子弹设置")]
    [Tooltip("子弹移动速度")]
    public float moveSpeed = 10f;
    
    [Tooltip("子弹伤害值")]
    public int damage = 1;
    
    [Tooltip("销毁区域（X坐标大于此值时销毁）")]
    public float destroyZone = 20f;
    
    [Header("追踪设置")]
    [Tooltip("是否自动追踪怪物（true=追踪最近的怪物，false=直线移动）")]
    public bool autoTrackMonster = true;
    
    [Tooltip("追踪范围（子弹会自动寻找此范围内的怪物）")]
    [Range(5f, 50f)]
    public float trackRange = 20f;
    
    [Tooltip("追踪更新间隔（秒，值越小追踪越精确但性能消耗越大）")]
    [Range(0.01f, 0.5f)]
    public float trackUpdateInterval = 0.1f;
    
    [Tooltip("怪物检测图层")]
    public LayerMask monsterLayer;
    
    private bool hasHit = false;  // 是否已击中目标
    private Transform targetMonster = null;  // 目标怪物
    private float trackTimer = 0f;  // 追踪更新计时器
    private Vector2 moveDirection = Vector2.right;  // 移动方向
    
    void Start()
    {
        // 如果没有设置怪物图层，尝试自动检测
        if (monsterLayer.value == 0)
        {
            // 默认使用所有图层（除了子弹自己的图层）
            monsterLayer = ~LayerMask.GetMask("Default");
        }
        
        // 初始方向向右
        moveDirection = Vector2.right;
        
        // 如果启用自动追踪，立即寻找目标
        if (autoTrackMonster)
        {
            FindNearestMonster();
        }
    }
    
    void Update()
    {
        // 如果已击中目标，不移动
        if (hasHit) return;
        
        // 只有在游戏进行中时才移动
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying())
        {
            return;
        }
        
        // 如果启用自动追踪，定期更新目标
        if (autoTrackMonster)
        {
            trackTimer += Time.deltaTime;
            if (trackTimer >= trackUpdateInterval)
            {
                FindNearestMonster();
                trackTimer = 0f;
            }
            
            // 如果有目标，朝向目标移动
            if (targetMonster != null)
            {
                Vector2 direction = (targetMonster.position - transform.position).normalized;
                moveDirection = direction;
            }
        }
        
        // 移动子弹（朝向目标方向）
        Vector3 oldPosition = transform.position;
        transform.position = transform.position + (Vector3)(moveDirection * moveSpeed * Time.deltaTime);
        
        // 使用距离检测作为备用碰撞检测（不依赖物理系统）
        CheckMonsterCollisionByDistance();
        
        // 旋转子弹朝向移动方向（可选，让子弹看起来更自然）
        if (moveDirection != Vector2.zero)
        {
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
        
        // 检查是否超出销毁区域
        if (transform.position.x > destroyZone || transform.position.x < -destroyZone)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 使用距离检测怪物碰撞（备用方法，不依赖物理系统）
    /// </summary>
    private void CheckMonsterCollisionByDistance()
    {
        if (hasHit) return;
        
        // 检测半径（子弹的碰撞范围）
        float collisionRadius = 0.5f; // 可以根据子弹大小调整
        
        // 查找所有怪物
        MonsterScript[] allMonsters = FindObjectsOfType<MonsterScript>();
        
        foreach (MonsterScript monster in allMonsters)
        {
            if (monster == null || monster.gameObject == null) continue;
            
            // 计算距离
            float distance = Vector2.Distance(transform.position, monster.transform.position);
            
            // 如果距离小于碰撞半径，判定为击中
            if (distance <= collisionRadius)
            {
                HitMonster(monster);
                return; // 只击中一个怪物
            }
        }
    }
    
    /// <summary>
    /// 寻找最近的怪物作为目标
    /// </summary>
    private void FindNearestMonster()
    {
        // 使用圆形区域检测怪物
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, trackRange, monsterLayer);
        
        Transform nearestMonster = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Collider2D collider in colliders)
        {
            MonsterScript monster = collider.GetComponent<MonsterScript>();
            if (monster != null)
            {
                // 只追踪前方的怪物（X坐标大于子弹）
                if (monster.transform.position.x > transform.position.x)
                {
                    float distance = Vector2.Distance(transform.position, monster.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestMonster = monster.transform;
                    }
                }
            }
        }
        
        targetMonster = nearestMonster;
    }
    
    /// <summary>
    /// 碰撞检测（使用Unity的触发器系统）
    /// </summary>
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;
        
        // 检查是否是怪物
        MonsterScript monster = collision.GetComponent<MonsterScript>();
        if (monster != null)
        {
            Debug.Log($"BulletScript: OnTriggerEnter2D 检测到怪物碰撞 - 怪物: {monster.gameObject.name}");
            // 击中怪物
            HitMonster(monster);
        }
    }
    
    /// <summary>
    /// 碰撞检测（使用Unity的碰撞系统，非触发器）
    /// </summary>
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit) return;
        
        // 检查是否是怪物
        MonsterScript monster = collision.gameObject.GetComponent<MonsterScript>();
        if (monster != null)
        {
            Debug.Log($"BulletScript: OnCollisionEnter2D 检测到怪物碰撞 - 怪物: {monster.gameObject.name}");
            // 击中怪物
            HitMonster(monster);
        }
    }
    
    /// <summary>
    /// 击中怪物
    /// </summary>
    private void HitMonster(MonsterScript monster)
    {
        if (hasHit) return;
        if (monster == null || monster.gameObject == null) return;
        
        hasHit = true;
        
        Debug.Log($"BulletScript: 子弹击中怪物！怪物名称: {monster.gameObject.name}, 子弹伤害: {damage}, 怪物当前血量: {monster.GetCurrentHP()}/{monster.GetMaxHP()}");
        
        // 对怪物造成伤害
        monster.TakeDamage(damage);
        
        // 销毁子弹（可以添加击中特效）
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 设置子弹伤害值
    /// </summary>
    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }
    
    /// <summary>
    /// 设置子弹速度
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }
    
    void OnDrawGizmosSelected()
    {
        // 在Scene视图中显示追踪范围
        if (autoTrackMonster)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, trackRange);
            
            // 如果有目标，绘制到目标的线
            if (targetMonster != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, targetMonster.position);
            }
        }
    }
}

