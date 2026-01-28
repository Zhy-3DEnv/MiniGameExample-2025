using UnityEngine;
using System.Collections;
using EggRogue;

/// <summary>
/// 敌人生成器 - 在场景中按一定频率生成敌人。
/// 
/// 使用方式：
/// 1. 在 GameScene 中创建一个空物体，挂载本脚本，命名为 "EnemySpawner"
/// 2. 在场景中创建若干空物体作为刷怪点，拖到 Spawn Points 数组
/// 3. 创建一个怪物 Prefab（带 EnemyController + Health），拖到 Enemy Prefab 字段
/// 4. 配置生成间隔和最大数量
/// 
/// 后续可以扩展：
/// - 按关卡波次配置（不同关卡刷不同敌人）
/// - 刷怪模板（组合不同类型的敌人）
/// - 难度曲线（随关卡增加敌人数量和强度）
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("生成配置")]
    [Tooltip("敌人 Prefab（需要包含 EnemyController 和 Health 组件）")]
    public GameObject enemyPrefab;

    [Tooltip("敌人数据（ScriptableObject）- 定义敌人的基础属性")]
    public EggRogue.EnemyData enemyData;

    [Tooltip("刷怪点位置列表")]
    public Transform[] spawnPoints;

    [Tooltip("生成间隔（秒）")]
    public float spawnInterval = 3f;

    [Tooltip("最大同时存在的敌人数量")]
    public int maxAliveEnemies = 10;

    [Header("生成范围（可选）")]
    [Tooltip("是否在刷怪点周围随机位置生成（而不是精确在刷怪点）")]
    public bool useRandomOffset = true;

    [Tooltip("随机偏移范围（半径）")]
    public float randomOffsetRadius = 2f;

    [Tooltip("地面高度（Y轴固定值，确保生成的怪物不脱离地面）")]
    public float groundHeight = 0f;

    [Header("随机区域生成")]
    [Tooltip("是否使用平面区域随机生成敌人（忽略刷怪点）")]
    public bool useAreaSpawn = false;

    [Tooltip("随机区域中心位置（通常为场景中心）")]
    public Vector3 areaCenter = Vector3.zero;

    [Tooltip("随机区域尺寸（X,Z 范围），Y 由 groundHeight 控制")]
    public Vector2 areaSize = new Vector2(10f, 10f);

    [Header("可视化")]
    [Tooltip("是否在 Scene 视图中绘制生成区域 Gizmo")]
    public bool drawSpawnAreaGizmo = true;

    private float spawnTimer = 0f;
    private bool isSpawning = true;
    private int spawnedThisLevel = 0;

    /// <summary>
    /// 当前关卡配置（由 LevelManager 设置）。生成时用于血量/移速/金币倍率。
    /// </summary>
    private EggRogue.LevelData currentLevelData;

    /// <summary>
    /// 应用关卡配置（LevelManager 调用）。会覆盖 spawnInterval、maxAliveEnemies、randomOffsetRadius；
    /// 生成敌人时使用 health/moveSpeed/coin 倍率。
    /// </summary>
    public void ApplyLevelData(EggRogue.LevelData data)
    {
        currentLevelData = data;
    }

    /// <summary>
    /// 关卡加载后重置刷怪计时（LevelManager 调用）。
    /// </summary>
    public void ResetForLevel()
    {
        spawnTimer = spawnInterval;
        spawnedThisLevel = 0;
        isSpawning = true;
    }

    private void Start()
    {
        // 验证配置
        if (enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: enemyPrefab 未设置！");
            enabled = false;
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("EnemySpawner: spawnPoints 为空，将在原点生成敌人。");
        }

        // 初始化计时器
        spawnTimer = spawnInterval;
    }

    private void Update()
    {
        if (!isSpawning)
            return;

        // 更新计时器
        spawnTimer -= Time.deltaTime;

        // 如果到了生成时间
        if (spawnTimer <= 0f)
        {
            // 如果关卡配置了最大总生成数，且已达上限，则停止刷怪
            if (currentLevelData != null && currentLevelData.maxTotalEnemies > 0 &&
                spawnedThisLevel >= currentLevelData.maxTotalEnemies)
            {
                isSpawning = false;
                return;
            }

            // 检查当前存活敌人数量
            int currentAliveCount = 0;
            if (EnemyManager.Instance != null)
            {
                currentAliveCount = EnemyManager.Instance.GetAliveEnemyCount();
            }

            // 如果未达到上限，生成新敌人
            // 约定：maxAliveEnemies <= 0 表示“不限制”
            if (maxAliveEnemies <= 0 || currentAliveCount < maxAliveEnemies)
            {
                SpawnEnemy();
            }

            // 重置计时器
            spawnTimer = spawnInterval;
        }
    }

    /// <summary>
    /// 生成一个敌人
    /// </summary>
    private void SpawnEnemy()
    {
        if (enemyPrefab == null)
            return;

        Vector3 spawnPosition = GetRandomSpawnPosition();
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        
        // 记录本关已生成数量
        spawnedThisLevel++;
        
        // 使用 EnemyData SO 配置敌人属性
        if (enemyData != null)
        {
            float baseHealth = enemyData.baseMaxHealth;
            float baseMoveSpeed = enemyData.baseMoveSpeed;
            
            // 应用关卡倍率（如果存在）
            float healthMult = (currentLevelData != null && currentLevelData.enemyHealthMultiplier > 0f)
                ? currentLevelData.enemyHealthMultiplier : 1f;
            float moveMult = (currentLevelData != null && currentLevelData.enemyMoveSpeedMultiplier > 0f)
                ? currentLevelData.enemyMoveSpeedMultiplier : 1f;

            Health health = enemy.GetComponent<Health>();
            if (health != null)
                health.SetMaxHealth(baseHealth * healthMult);

            EnemyController enemyController = enemy.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                enemyController.SetMoveSpeed(baseMoveSpeed * moveMult);
                // 传递 EnemyData 给 EnemyController（用于金币掉落配置）
                enemyController.SetEnemyData(enemyData);
            }
        }
        else
        {
            Debug.LogWarning("EnemySpawner: enemyData 未设置，使用默认值");
        }
    }

    /// <summary>
    /// 获取随机生成位置
    /// </summary>
    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 basePosition;

        if (useAreaSpawn)
        {
            // 在指定区域内随机生成（X,Z 平面，Y 使用 groundHeight）
            float halfX = areaSize.x * 0.5f;
            float halfZ = areaSize.y * 0.5f;
            float x = areaCenter.x + Random.Range(-halfX, halfX);
            float z = areaCenter.z + Random.Range(-halfZ, halfZ);
            basePosition = new Vector3(x, groundHeight, z);
        }
        else
        {
            // 使用刷怪点 + 随机偏移
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                basePosition = randomPoint.position;
            }
            else
            {
                basePosition = Vector3.zero;
            }

            if (useRandomOffset && randomOffsetRadius > 0f)
            {
                Vector2 randomCircle = Random.insideUnitCircle * randomOffsetRadius;
                basePosition += new Vector3(randomCircle.x, 0f, randomCircle.y);
            }

            basePosition.y = groundHeight;
        }

        return basePosition;
    }

    /// <summary>
    /// 开始生成敌人
    /// </summary>
    public void StartSpawning()
    {
        isSpawning = true;
    }

    /// <summary>
    /// 停止生成敌人
    /// </summary>
    public void StopSpawning()
    {
        isSpawning = false;
    }

    /// <summary>
    /// 立即生成一个敌人（用于测试）
    /// </summary>
    public void SpawnEnemyNow()
    {
        SpawnEnemy();
    }

    // 在 Scene 视图中显示刷怪点 / 生成区域（调试用）
    private void OnDrawGizmosSelected()
    {
        if (!drawSpawnAreaGizmo)
            return;

        // 绘制刷怪点
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Gizmos.color = Color.red;
            foreach (var point in spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, 0.5f);
                }
            }
        }

        // 绘制随机区域
        if (useAreaSpawn)
        {
            // 线框矩形表示生成区域（X,Z 平面）
            Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
            Vector3 center = areaCenter;
            center.y = groundHeight;
            Vector3 size = new Vector3(areaSize.x, 0.1f, areaSize.y);
            Gizmos.DrawWireCube(center, size);

            // 画一个小十字标记区域中心
            float crossSize = 0.3f;
            Gizmos.DrawLine(center + Vector3.left * crossSize, center + Vector3.right * crossSize);
            Gizmos.DrawLine(center + Vector3.forward * crossSize, center + Vector3.back * crossSize);
        }
    }
}
