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
    /// <summary>
    /// 本关的单一敌人生成条目（用于多怪物生成配方）。
    /// </summary>
    [System.Serializable]
    public class LevelSpawnEntry
    {
        [Tooltip("本条目对应的敌人数据（EnemyData）。如果为空，则使用 EnemySpawner 上配置的默认 EnemyData。")]
        public EnemyData enemyData;

        [Tooltip("本条目对应的敌人 Prefab。如果为空，则使用 EnemySpawner 上配置的默认 Enemy Prefab。")]
        public GameObject enemyPrefab;

        [Tooltip("权重，用于在多种敌人中按比例随机选择。建议为正数，0 或负数会被忽略。")]
        public float spawnWeight = 1f;

        [Tooltip("该敌人类型在本关内的同屏上限（0 = 不单独限制，仅受 maxAliveEnemies 控制）。")]
        public int maxAlive = 0;

        [Tooltip("生成频率缩放（可选），通常与 spawnWeight 相乘使用。1 = 不额外缩放。")]
        public float spawnRateScale = 1f;

        [Header("时间窗口（可选）")]
        [Tooltip("本条目参与刷怪的起始时间（相对本关开始，秒）。0 或更小表示不限制开始时间。")]
        public float spawnTimeStart = 0f;

        [Tooltip("本条目参与刷怪的结束时间（相对本关开始，秒）。0 或更小表示不限制结束时间。")]
        public float spawnTimeEnd = 0f;
    }

    [Header("关卡信息")]
    [Tooltip("关卡编号（1-based）")]
    public int levelNumber = 1;

    [Tooltip("关卡名称（用于 UI/日志）")]
    public string levelName = "第1关";

    [Header("多怪物生成配方")]
    [Tooltip("本关可生成的敌人类型列表及其配比。为空时，EnemySpawner 将沿用当前的单一敌人配置逻辑。")]
    public LevelSpawnEntry[] spawnMix;

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

    [Header("刷怪时间窗口")]
    [Tooltip("本关开始后多少秒开始刷怪。通常为 0，表示一开局就可以刷怪。")]
    public float spawnStartTime = 0f;

    [Tooltip("本关开始后多少秒停止刷怪。0 或小于等于开始时间表示默认使用关卡总时长 levelDuration。")]
    public float spawnEndTime = 0f;

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
