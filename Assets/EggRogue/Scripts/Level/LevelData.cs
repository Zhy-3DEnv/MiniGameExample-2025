using UnityEngine;

namespace EggRogue
{
/// <summary>
/// 关卡配置数据（ScriptableObject）。
/// 用于配置单关的刷怪、难度等，可与 CSV 基础配置叠加。
/// </summary>
[CreateAssetMenu(fileName = "Level_01", menuName = "EggRogue/Level Data", order = 0)]
public class LevelData : ScriptableObject
{
    [Header("关卡信息")]
    [Tooltip("关卡编号（1-based）")]
    public int levelNumber = 1;

    [Tooltip("关卡名称（用于 UI/日志）")]
    public string levelName = "第1关";

    [Header("刷怪配置")]
    [Tooltip("每秒刷怪数量（例如：0.5 = 每2秒刷1个，1.0 = 每秒刷1个，2.0 = 每秒刷2个）")]
    public float spawnsPerSecond = 0.33f;

    [Tooltip("最大同时存在的敌人数量（0 = 不限制）")]
    public int maxAliveEnemies = 10;

    [Tooltip("本关期望最少生成的敌人总数（0 = 不限制）")]
    public int minTotalEnemies = 0;

    [Tooltip("本关最多生成的敌人总数（0 = 不限制）")]
    public int maxTotalEnemies = 0;

    [Tooltip("刷怪点随机偏移半径（0=不偏移）")]
    public float randomOffsetRadius = 2f;

    [Header("关卡时长")]
    [Tooltip("关卡时长（秒），时间到且玩家存活则胜利")]
    public float levelDuration = 30f;

    [Header("胜利奖励")]
    [Tooltip("胜利奖励金币数")]
    public int victoryRewardGold = 50;

    [Header("难度系数（可选，1=不变）")]
    [Tooltip("怪物血量倍率（相对 EnemyData.baseMaxHealth）")]
    public float enemyHealthMultiplier = 1f;

    [Tooltip("怪物移速倍率（相对 EnemyData.baseMoveSpeed）")]
    public float enemyMoveSpeedMultiplier = 1f;

    [Tooltip("金币掉落倍率（相对 EnemyData.coinDropMin/Max）")]
    public float coinDropMultiplier = 1f;
}
}
