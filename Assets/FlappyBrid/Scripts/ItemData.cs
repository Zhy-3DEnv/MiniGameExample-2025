using UnityEngine;

[System.Serializable]
public class ItemData
{
    [Header("道具设置")]
    [Tooltip("道具预制体")]
    public GameObject itemPrefab;  // 道具预制体
    [Tooltip("该道具的分数值")]
    public int scoreValue = 2;     // 该道具的分数值
    [Header("生成概率设置")]
    [Range(0f, 1f)]
    [Tooltip("生成权重（相对权重，数值越大越容易生成）")]
    public float spawnWeight = 1f; // 生成权重（用于控制不同道具的生成概率）
    [Header("可选设置")]
    [Tooltip("道具名称（用于调试和识别）")]
    public string itemName = "";   // 道具名称（用于调试）
    [Header("分数显示颜色")]
    [Tooltip("该道具的分数显示颜色（如果为空则使用默认颜色）")]
    public Color scoreColor = Color.white; // 分数显示颜色
    
    // 注意：实际生成概率 = (spawnWeight / 所有道具权重总和) * itemSpawnChance
    // 例如：如果itemSpawnChance=0.3，该道具权重=1.0，总权重=2.0
    // 那么该道具的实际生成概率 = (1.0/2.0) * 0.3 = 0.15 (15%)
}

