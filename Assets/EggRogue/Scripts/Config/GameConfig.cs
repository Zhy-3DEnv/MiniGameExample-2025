using UnityEngine;

/// <summary>
/// 游戏运行时配置数据 - 可在运行时修改并保存到本地文件。
/// 
/// 使用方式：
/// 1. 在 Unity 中创建 GameConfig ScriptableObject（Assets/EggRogue/Config/GameConfig.asset）
/// 2. 设置默认值
/// 3. 运行时通过 ConfigManager 加载/保存配置
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "EggRogue/Game Config", order = 1)]
public class GameConfig : ScriptableObject
{
    [Header("玩家战斗配置")]
    [Tooltip("玩家单发伤害")]
    public float playerDamage = 10f;

    [Tooltip("玩家攻击范围")]
    public float playerAttackRange = 10f;

    [Tooltip("玩家射速（每秒几发）")]
    public float playerFireRate = 2f;

    [Header("怪物配置")]
    [Tooltip("怪物最大生命值")]
    public float enemyMaxHealth = 20f;

    [Tooltip("怪物移动速度")]
    public float enemyMoveSpeed = 3f;

    [Header("其他配置")]
    [Tooltip("金币掉落数量（最少）")]
    public int coinDropMin = 1;

    [Tooltip("金币掉落数量（最多）")]
    public int coinDropMax = 2;
}
