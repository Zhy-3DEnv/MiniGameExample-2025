using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡数据库
/// 存储所有关卡的数据配置
/// </summary>
[CreateAssetMenu(fileName = "LevelDatabase", menuName = "FlappyBird/Level Database", order = 0)]
public class LevelDatabase : ScriptableObject
{
    [Header("关卡列表")]
    [Tooltip("所有关卡的数据配置")]
    public List<LevelData> levels = new List<LevelData>();
    
    [Header("默认设置")]
    [Tooltip("如果关卡超出列表，使用默认关卡数据")]
    public LevelData defaultLevelTemplate;
    
    /// <summary>
    /// 获取指定关卡的数据
    /// </summary>
    /// <param name="levelNumber">关卡编号（从1开始）</param>
    /// <returns>关卡数据，如果不存在则返回默认或生成新数据</returns>
    public LevelData GetLevelData(int levelNumber)
    {
        // 检查关卡编号是否有效
        if (levelNumber < 1)
        {
            Debug.LogWarning($"关卡编号无效: {levelNumber}，使用第1关数据");
            levelNumber = 1;
        }
        
        // 尝试从列表中获取
        int index = levelNumber - 1;
        if (index >= 0 && index < levels.Count)
        {
            if (levels[index] != null)
            {
                return levels[index];
            }
        }
        
        // 如果列表中没有，尝试查找匹配的关卡编号
        foreach (var level in levels)
        {
            if (level != null && level.levelNumber == levelNumber)
            {
                return level;
            }
        }
        
        // 如果都没有，使用默认模板创建新关卡数据
        if (defaultLevelTemplate != null)
        {
            LevelData newLevel = CreateInstance<LevelData>();
            newLevel.levelNumber = levelNumber;
            newLevel.levelName = $"关卡{levelNumber}";
            newLevel.targetScore = defaultLevelTemplate.targetScore + (levelNumber - 1) * 10;
            newLevel.backgroundColor = defaultLevelTemplate.backgroundColor;
            newLevel.backgroundSprite = defaultLevelTemplate.backgroundSprite;
            newLevel.backgroundMusic = defaultLevelTemplate.backgroundMusic;
            newLevel.spawnRateMultiplier = defaultLevelTemplate.spawnRateMultiplier;
            newLevel.moveSpeedMultiplier = defaultLevelTemplate.moveSpeedMultiplier;
            newLevel.heightOffset = defaultLevelTemplate.heightOffset;
            newLevel.itemSpawnChance = defaultLevelTemplate.itemSpawnChance;
            newLevel.completionBonus = defaultLevelTemplate.completionBonus;
            
            Debug.Log($"关卡 {levelNumber} 不存在，使用默认模板生成");
            return newLevel;
        }
        
        // 最后的兜底方案
        Debug.LogWarning($"无法获取关卡 {levelNumber} 的数据，返回null");
        return null;
    }
    
    /// <summary>
    /// 获取关卡总数
    /// </summary>
    public int GetLevelCount()
    {
        return levels.Count;
    }
    
    /// <summary>
    /// 检查是否有下一关
    /// </summary>
    public bool HasNextLevel(int currentLevel)
    {
        // 如果有默认模板，认为可以无限关卡
        if (defaultLevelTemplate != null)
        {
            return true;
        }
        
        // 否则检查列表
        return currentLevel < levels.Count;
    }
}


