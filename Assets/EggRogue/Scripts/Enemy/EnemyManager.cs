using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人管理器 - 统一管理场景中所有存活的敌人。
/// 
/// 使用方式：
/// 1. 在场景中创建一个空物体，挂载本脚本，命名为 "EnemyManager"
/// 2. 敌人生成时会自动注册，死亡时会自动注销
/// 3. 玩家战斗系统可以通过 GetClosestEnemy() 查找最近的敌人
/// 
/// 注意：
/// - 采用单例模式，方便全局访问
/// - 场景切换时自动清理列表
/// </summary>
public class EnemyManager : MonoBehaviour
{
    private static EnemyManager _instance;
    public static EnemyManager Instance => _instance;

    private List<EnemyController> aliveEnemies = new List<EnemyController>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// <summary>
    /// 注册敌人（敌人生成时调用）
    /// </summary>
    public void RegisterEnemy(EnemyController enemy)
    {
        if (enemy != null && !aliveEnemies.Contains(enemy))
        {
            aliveEnemies.Add(enemy);
        }
    }

    /// <summary>
    /// 注销敌人（敌人死亡时调用）
    /// </summary>
    public void UnregisterEnemy(EnemyController enemy)
    {
        if (enemy != null)
        {
            aliveEnemies.Remove(enemy);
        }
    }

    /// <summary>
    /// 获取最近的敌人
    /// </summary>
    /// <param name="fromPosition">起始位置（通常是玩家位置）</param>
    /// <param name="maxDistance">最大搜索距离（如果为 0 或负数，不限制距离）</param>
    /// <returns>最近的敌人，如果没有则返回 null</returns>
    public EnemyController GetClosestEnemy(Vector3 fromPosition, float maxDistance = 0f)
    {
        EnemyController closest = null;
        float closestDistance = float.MaxValue;

        // 清理已死亡的敌人引用（防止内存泄漏）
        aliveEnemies.RemoveAll(e => e == null);

        foreach (var enemy in aliveEnemies)
        {
            if (enemy == null)
                continue;
            
            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth != null && enemyHealth.IsDead)
                continue;

            float distance = Vector3.Distance(fromPosition, enemy.transform.position);

            // 如果设置了最大距离，只考虑范围内的敌人
            if (maxDistance > 0f && distance > maxDistance)
                continue;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy;
            }
        }

        return closest;
    }

    /// <summary>
    /// 获取所有存活的敌人
    /// </summary>
    public List<EnemyController> GetAllAliveEnemies()
    {
        // 清理已死亡的敌人引用
        aliveEnemies.RemoveAll(e => e == null);
        return new List<EnemyController>(aliveEnemies);
    }

    /// <summary>
    /// 获取存活敌人数量
    /// </summary>
    public int GetAliveEnemyCount()
    {
        aliveEnemies.RemoveAll(e => e == null);
        return aliveEnemies.Count;
    }

    /// <summary>
    /// 清除所有敌人（用于关卡重置等）
    /// </summary>
    public void ClearAllEnemies()
    {
        foreach (var enemy in aliveEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        aliveEnemies.Clear();
    }
}
