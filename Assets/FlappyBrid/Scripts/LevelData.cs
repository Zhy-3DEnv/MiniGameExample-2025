using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 金币产出控制曲线类型
/// </summary>
public enum CoinControlCurveType
{
    Linear,   // 线性衰减
    Smooth,   // 平滑衰减（使用平滑曲线）
    Fast      // 快速衰减
}

/// <summary>
/// 单个关卡的数据配置
/// </summary>
[CreateAssetMenu(fileName = "NewLevel", menuName = "FlappyBird/Level Data", order = 1)]
public class LevelData : ScriptableObject
{
    [Header("关卡基本信息")]
    [Tooltip("关卡编号")]
    public int levelNumber = 1;
    
    [Tooltip("关卡名称")]
    public string levelName = "关卡1";
    
    [Header("关卡目标")]
    [Tooltip("完成关卡所需的目标时间（秒），如果为0则自动计算：15 + (关卡编号-1) * 5")]
    public float targetTime = 0f;
    
    [Header("背景环境设置")]
    [Tooltip("背景颜色（如果没有背景图片）")]
    public Color backgroundColor = Color.cyan;
    
    [Tooltip("背景图片（可选）")]
    public Sprite backgroundSprite;
    
    [Tooltip("背景音乐（可选）")]
    public AudioClip backgroundMusic;
    
    [Header("游戏难度设置")]
    [Tooltip("生成率（数值越大生成的越多，相对于基础生成率的倍数）")]
    [Range(0.1f, 3f)]
    public float spawnRateMultiplier = 1f;
    
    [Tooltip("移动速度（数值越大移动的越快，相对于基础速度的倍数）")]
    [Range(0.5f, 3f)]
    public float moveSpeedMultiplier = 1f;
    
    [Tooltip("高度偏移（数值越大高度偏移越大，管道生成位置的Y轴随机范围）")]
    [Range(1f, 20f)]
    public float heightOffset = 10f;
    
    [Tooltip("通过管道后获得的分数")]
    public int pipePassScore = 1;
    
    [Header("道具生成设置")]
    [Tooltip("是否使用关卡的道具生成设置（如果为false，则使用PipeSpawner的默认设置）")]
    public bool useLevelItemSettings = true;
    
    [Tooltip("道具生成概率（0-1之间，0.3表示30%概率生成道具）\n这是第一层概率：决定是否生成道具")]
    [Range(0f, 1f)]
    public float itemSpawnChance = 0.3f;
    
    [Tooltip("道具相对管道的X轴偏移（可以放在管道中间位置）")]
    public float itemSpawnOffsetX = 0f;
    
    [Tooltip("道具类型列表\n每个道具的Spawn Weight控制相对生成概率\n实际概率 = (该道具权重/总权重) × Item Spawn Chance\n如果列表为空，则使用PipeSpawner的默认道具列表")]
    public List<ItemData> itemTypes = new List<ItemData>();
    
    [Header("关卡奖励")]
    [Tooltip("完成关卡后获得的额外分数")]
    public int completionBonus = 0;
    
    [Header("金币产出控制")]
    [Tooltip("关卡金币产出下限（玩家至少能获得的金币数量）\n只统计道具和怪物掉落的金币，不包括管道通过分数")]
    public int minCoins = 10;
    
    [Tooltip("关卡金币产出上限（玩家最多能获得的金币数量）\n只统计道具和怪物掉落的金币，不包括管道通过分数\n当接近上限时，道具和怪物的生成概率会逐渐降低")]
    public int maxCoins = 50;
    
    [Tooltip("开始控制产出的比例（0-1之间）\n例如：0.7 表示当产出达到上限的70%时开始降低生成概率\n0.5 表示达到上限的50%时开始控制\n1.0 表示达到上限时才完全停止生成")]
    [Range(0f, 1f)]
    public float coinControlStartRatio = 0.7f;
    
    [Tooltip("产出控制曲线类型\nLinear: 线性衰减（均匀降低）\nSmooth: 平滑衰减（开始慢，接近上限时快速降低）\nFast: 快速衰减（开始快速降低）")]
    public CoinControlCurveType coinControlCurve = CoinControlCurveType.Smooth;
    
    [Header("怪物生成设置")]
    [Tooltip("是否使用关卡的怪物生成设置（如果为false，则使用PipeSpawner的默认设置）")]
    public bool useLevelMonsterSettings = true;
    
    [Tooltip("怪物生成概率（0-1之间，0.2表示20%概率生成怪物）\n这是第一层概率：决定是否生成怪物")]
    [Range(0f, 1f)]
    public float monsterSpawnChance = 0.2f;
    
    [Tooltip("怪物相对管道的X轴偏移（可以放在管道附近位置）")]
    public float monsterSpawnOffsetX = 0f;
    
    [Tooltip("怪物相对管道的Y轴偏移（可以放在管道上方或下方）")]
    public float monsterSpawnOffsetY = 0f;
    
    [Tooltip("怪物类型列表\n每个怪物的Spawn Weight控制相对生成概率\n实际概率 = (该怪物权重/总权重) × Monster Spawn Chance\n如果列表为空，则使用PipeSpawner的默认怪物列表")]
    public List<MonsterData> monsterTypes = new List<MonsterData>();
    
    [Header("关卡描述")]
    [TextArea(2, 4)]
    [Tooltip("关卡描述（用于UI显示）")]
    public string levelDescription = "";
}

