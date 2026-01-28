using UnityEngine;

namespace EggRogue
{
/// <summary>
/// 关卡数据库（ScriptableObject）。持有所有关卡配置，供 LevelManager 使用。
/// </summary>
[CreateAssetMenu(fileName = "LevelDatabase", menuName = "EggRogue/Level Database", order = 1)]
public class LevelDatabase : ScriptableObject
{
    [Tooltip("关卡列表（按关卡编号顺序）")]
    public LevelData[] levels = new LevelData[0];

    /// <summary>
    /// 根据关卡编号获取配置；找不到则返回 null。
    /// </summary>
    public LevelData GetLevel(int levelNumber)
    {
        if (levels == null || levels.Length == 0)
            return null;
        foreach (var lv in levels)
        {
            if (lv != null && lv.levelNumber == levelNumber)
                return lv;
        }
        return null;
    }

    /// <summary>
    /// 根据索引获取关卡（0-based）；越界返回 null。
    /// </summary>
    public LevelData GetLevelByIndex(int index)
    {
        if (levels == null || index < 0 || index >= levels.Length)
            return null;
        return levels[index];
    }

    /// <summary>
    /// 总关卡数。
    /// </summary>
    public int TotalLevelCount => levels != null ? levels.Length : 0;
}
}
