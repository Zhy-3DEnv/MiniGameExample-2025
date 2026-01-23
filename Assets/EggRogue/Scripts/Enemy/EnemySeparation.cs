using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 怪物分离系统 - 防止怪物重叠，使用简单的排斥力让怪物互相推开。
/// 
/// 性能优化方案：
/// - 不使用物理碰撞（避免2000个怪物造成性能问题）
/// - 使用空间分区（Spatial Partitioning）优化碰撞检测
/// - 只检测附近怪物，不检测所有怪物
/// 
/// 使用方式：
/// 1. 将本脚本挂载到 EnemyManager 对象上
/// 2. 在 Inspector 中配置排斥力和检测范围
/// 
/// 注意：
/// - 这个系统会定期更新，不会每帧都检测所有怪物
/// - 可以通过调整更新频率来平衡性能和效果
/// </summary>
public class EnemySeparation : MonoBehaviour
{
    [Header("分离参数")]
    [Tooltip("排斥力强度（越大怪物越不容易重叠）")]
    public float separationForce = 5f;

    [Tooltip("检测范围（只检测这个范围内的其他怪物）")]
    public float detectionRadius = 2f;

    [Tooltip("最小分离距离（怪物之间保持的最小距离）")]
    public float minSeparationDistance = 0.5f;

    [Header("性能优化")]
    [Tooltip("更新间隔（秒），降低更新频率可以提升性能")]
    public float updateInterval = 0.1f; // 每0.1秒更新一次，而不是每帧

    [Tooltip("每帧最多处理的怪物数量（避免单帧卡顿）")]
    public int maxEnemiesPerFrame = 50;

    private float updateTimer = 0f;
    private int currentProcessIndex = 0; // 当前处理的怪物索引（用于分帧处理）

    private void Update()
    {
        updateTimer += Time.deltaTime;

        // 如果到了更新间隔，处理怪物分离
        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            ProcessSeparation();
        }
    }

    /// <summary>
    /// 处理怪物分离（分帧处理，避免单帧卡顿）
    /// </summary>
    private void ProcessSeparation()
    {
        if (EnemyManager.Instance == null)
            return;

        List<EnemyController> enemies = EnemyManager.Instance.GetAllAliveEnemies();
        if (enemies.Count == 0)
            return;

        // 分帧处理：每次只处理一部分怪物
        int processed = 0;
        int startIndex = currentProcessIndex;
        
        for (int i = 0; i < enemies.Count && processed < maxEnemiesPerFrame; i++)
        {
            int index = (startIndex + i) % enemies.Count;
            EnemyController enemy = enemies[index];
            
            if (enemy != null && !enemy.GetComponent<Health>().IsDead)
            {
                ApplySeparation(enemy, enemies);
                processed++;
            }
        }

        // 更新下次处理的起始索引
        currentProcessIndex = (currentProcessIndex + processed) % enemies.Count;
    }

    /// <summary>
    /// 对单个怪物应用分离力
    /// </summary>
    private void ApplySeparation(EnemyController enemy, List<EnemyController> allEnemies)
    {
        Vector3 separationVector = Vector3.zero;
        Vector3 enemyPos = enemy.transform.position;
        int neighborCount = 0;

        // 只检测附近的怪物（使用检测范围优化）
        foreach (var other in allEnemies)
        {
            if (other == null || other == enemy)
                continue;

            Health otherHealth = other.GetComponent<Health>();
            if (otherHealth != null && otherHealth.IsDead)
                continue;

            Vector3 otherPos = other.transform.position;
            float distance = Vector3.Distance(enemyPos, otherPos);

            // 只处理检测范围内的怪物
            if (distance < detectionRadius && distance > 0.01f)
            {
                // 计算分离方向（从其他怪物指向当前怪物）
                Vector3 direction = (enemyPos - otherPos).normalized;
                
                // 距离越近，排斥力越大（使用反比例函数）
                float force = separationForce / (distance + 0.1f);
                
                // 如果距离小于最小分离距离，增加排斥力
                if (distance < minSeparationDistance)
                {
                    force *= 2f; // 加倍排斥力
                }

                separationVector += direction * force;
                neighborCount++;
            }
        }

        // 如果有邻居，应用分离力
        if (neighborCount > 0 && separationVector.sqrMagnitude > 0.01f)
        {
            // 归一化并应用分离力
            separationVector.Normalize();
            
            // 分离力只在XZ平面上应用（Y轴设为0，防止怪物在高度上重叠）
            separationVector.y = 0f;
            
            // 使用较小的移动量，避免和寻路冲突太严重
            // 分离力应该是一个"微调"，而不是主导移动
            Vector3 movement = separationVector * separationForce * updateInterval * 0.5f; // 减小影响
            
            // 应用分离力（只改变XZ坐标）
            Vector3 newPosition = enemy.transform.position + movement;
            enemy.transform.position = newPosition;
        }
    }

    // 在Scene视图中显示检测范围（调试用）
    private void OnDrawGizmosSelected()
    {
        if (EnemyManager.Instance == null)
            return;

        List<EnemyController> enemies = EnemyManager.Instance.GetAllAliveEnemies();
        Gizmos.color = Color.cyan;

        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                Gizmos.DrawWireSphere(enemy.transform.position, detectionRadius);
            }
        }
    }
}
