using System;
using System.Collections.Generic;

/// <summary>
/// 游戏配置数据类 - 用于 CSV 序列化/反序列化。
/// 对应 Excel 表格的每一行数据。
/// </summary>
[Serializable]
public class GameConfigData
{
    public string key;      // 配置项名称（例如 "PlayerDamage"）
    public string value;    // 配置值（字符串形式，需要根据类型转换）
    public string type;     // 数据类型（"float", "int", "string"）
    public string desc;     // 描述（可选）

    public GameConfigData() { }

    public GameConfigData(string key, string value, string type = "float", string desc = "")
    {
        this.key = key;
        this.value = value;
        this.type = type;
        this.desc = desc;
    }
}

/// <summary>
/// CSV 配置数据容器
/// </summary>
[Serializable]
public class GameConfigDataList
{
    public List<GameConfigData> configs = new List<GameConfigData>();
}
