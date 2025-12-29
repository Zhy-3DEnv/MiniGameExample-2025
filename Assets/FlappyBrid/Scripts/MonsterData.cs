using UnityEngine;

/// <summary>
/// 怪物数据配置
/// 用于定义怪物的属性
/// </summary>
[System.Serializable]
public class MonsterData
{
    [Header("怪物设置")]
    [Tooltip("怪物预制体")]
    public GameObject monsterPrefab;  // 怪物预制体
    
    [Tooltip("怪物名称（用于调试和识别）")]
    public string monsterName = "";   // 怪物名称
    
    [Header("血量设置")]
    [Tooltip("怪物最大血量")]
    public int maxHP = 10;            // 最大血量
    
    [Header("掉落设置")]
    [Tooltip("怪物死亡后掉落的金币数量")]
    public int dropCoins = 5;         // 掉落金币数量
    
    [Header("生成概率设置")]
    [Range(0f, 1f)]
    [Tooltip("生成权重（相对权重，数值越大越容易生成）")]
    public float spawnWeight = 1f;    // 生成权重（用于控制不同怪物的生成概率）
    
    [Header("移动设置")]
    [Tooltip("是否跟随管道移动（true=跟随管道速度，false=使用自定义速度）")]
    public bool followPipeMovement = true;  // 是否跟随管道移动
    
    [Tooltip("自定义移动速度（仅在followPipeMovement=false时使用）")]
    public float customMoveSpeed = 5f;      // 自定义移动速度
    
    // 注意：实际生成概率 = (spawnWeight / 所有怪物权重总和) * monsterSpawnChance
}

