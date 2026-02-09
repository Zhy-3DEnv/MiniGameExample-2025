using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 怪物分离系统 - 防止怪物重叠，使用简单的排斥力让怪物互相推开。
///
/// 性能优化方案：
/// - 不使用物理碰撞（避免2000个怪物造成性能问题）
/// - 只检测附近怪物，不检测所有怪物
///
/// 防抖动设计：
/// - 每帧施加小幅分离力（使用 Time.deltaTime），避免之前「每 0.1 秒瞬时位移」造成的抖动
/// - 在 LateUpdate 中执行，确保在 EnemyController 移动之后应用，且与渲染同帧
///
/// 使用方式：
/// 1. 将本脚本挂载到 EnemyManager 对象上
/// 2. 在 Inspector 中配置排斥力和检测范围
/// </summary>
public class EnemySeparation : MonoBehaviour
{
    [Header("分离参数")]
    [Tooltip("排斥力强度（越大怪物越不容易重叠）")]
    public float separationForce = 2f;

    [Tooltip("检测范围（只检测这个范围内的其他怪物）")]
    public float detectionRadius = 2f;

    [Tooltip("最小分离距离（怪物之间保持的最小距离）")]
    public float minSeparationDistance = 0.5f;

    [Header("性能优化")]
    [Tooltip("每帧最多处理的怪物数量（0=不限制，建议 50-150 平衡性能与流畅度）")]
    public int maxEnemiesPerFrame = 100;

    private int _currentProcessIndex;

    /// <summary>
    /// 在 LateUpdate 中执行，确保在 EnemyController.Update 之后应用分离，并与渲染同帧，减少抖动。
    /// </summary>
    private void LateUpdate()
    {
        if (EnemyManager.Instance == null)
            return;

        List<EnemyController> enemies = EnemyManager.Instance.GetAllAliveEnemies();
        if (enemies.Count == 0)
            return;

        int processed = 0;
        int limit = maxEnemiesPerFrame > 0 ? maxEnemiesPerFrame : enemies.Count;
        int startIndex = _currentProcessIndex;

        for (int i = 0; i < enemies.Count && processed < limit; i++)
        {
            int index = (startIndex + i) % enemies.Count;
            EnemyController enemy = enemies[index];

            if (enemy != null && !enemy.GetComponent<Health>().IsDead)
            {
                ApplySeparation(enemy, enemies);
                processed++;
            }
        }

        _currentProcessIndex = (startIndex + processed) % Mathf.Max(1, enemies.Count);
    }

    /// <summary>
    /// 对单个怪物应用分离力（每帧小幅位移，避免瞬时跳跃）
    /// </summary>
    private void ApplySeparation(EnemyController enemy, List<EnemyController> allEnemies)
    {
        Vector3 separationVector = Vector3.zero;
        Vector3 enemyPos = enemy.transform.position;

        foreach (var other in allEnemies)
        {
            if (other == null || other == enemy)
                continue;

            Health otherHealth = other.GetComponent<Health>();
            if (otherHealth != null && otherHealth.IsDead)
                continue;

            Vector3 otherPos = other.transform.position;
            float distance = Vector3.Distance(enemyPos, otherPos);

            if (distance < detectionRadius && distance > 0.01f)
            {
                Vector3 direction = (enemyPos - otherPos).normalized;
                float force = separationForce / (distance + 0.1f);
                if (distance < minSeparationDistance)
                    force *= 2f;
                separationVector += direction * force;
            }
        }

        if (separationVector.sqrMagnitude > 0.01f)
        {
            separationVector.Normalize();
            separationVector.y = 0f;

            // 每帧小幅位移（关键：使用 Time.deltaTime），避免瞬时跳跃造成的抖动
            float moveAmount = separationForce * Time.deltaTime * 0.5f;
            Vector3 movement = separationVector * moveAmount;
            enemy.transform.position = enemy.transform.position + movement;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (EnemyManager.Instance == null)
            return;

        List<EnemyController> enemies = EnemyManager.Instance.GetAllAliveEnemies();
        Gizmos.color = Color.cyan;
        foreach (var enemy in enemies)
        {
            if (enemy != null)
                Gizmos.DrawWireSphere(enemy.transform.position, detectionRadius);
        }
    }
}
