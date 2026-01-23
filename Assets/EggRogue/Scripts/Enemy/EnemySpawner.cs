using UnityEngine;
using System.Collections;

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

    private float spawnTimer = 0f;
    private bool isSpawning = true;

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
            // 检查当前存活敌人数量
            int currentAliveCount = 0;
            if (EnemyManager.Instance != null)
            {
                currentAliveCount = EnemyManager.Instance.GetAliveEnemyCount();
            }

            // 如果未达到上限，生成新敌人
            if (currentAliveCount < maxAliveEnemies)
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

        // 选择一个刷怪点
        Vector3 spawnPosition = GetRandomSpawnPosition();

        // 实例化敌人
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        
        // 可以在这里设置敌人的初始属性（例如根据关卡难度调整血量）
        // Health health = enemy.GetComponent<Health>();
        // if (health != null) { health.SetMaxHealth(...); }
    }

    /// <summary>
    /// 获取随机生成位置
    /// </summary>
    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 basePosition;

        // 如果有刷怪点，随机选一个；否则在原点
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            basePosition = randomPoint.position;
        }
        else
        {
            basePosition = Vector3.zero;
        }

        // 如果需要随机偏移
        if (useRandomOffset && randomOffsetRadius > 0f)
        {
            Vector2 randomCircle = Random.insideUnitCircle * randomOffsetRadius;
            basePosition += new Vector3(randomCircle.x, 0f, randomCircle.y);
        }

        // 强制设置Y轴为地面高度（防止怪物在高度上重叠）
        basePosition.y = groundHeight;

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

    // 在Scene视图中显示刷怪点（调试用）
    private void OnDrawGizmosSelected()
    {
        if (spawnPoints != null)
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
    }
}
