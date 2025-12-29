using UnityEngine;

/// <summary>
/// 小鸟升级配置数据库
/// 在一个SO文件中配置所有升级属性
/// </summary>
[CreateAssetMenu(fileName = "BirdUpgradeConfig", menuName = "FlappyBird/Bird Upgrade Config", order = 1)]
public class BirdUpgradeConfig : ScriptableObject
{
    [Header("血量升级配置")]
    [Tooltip("升级项名称")]
    public string healthUpgradeName = "血量提升";
    [Tooltip("升级项描述")]
    [TextArea(2, 4)]
    public string healthDescription = "增加小鸟的最大血量";
    [Tooltip("基础价格")]
    public int healthBasePrice = 10;
    [Tooltip("价格增长倍数")]
    [Range(0.1f, 2f)]
    public float healthPriceMultiplier = 0.5f;
    [Tooltip("每次升级增加的血量")]
    public int healthValueIncrease = 1;
    [Tooltip("最大值限制（0表示无限制）")]
    public int healthMaxValue = 0;
    
    [Header("子弹伤害升级配置")]
    [Tooltip("升级项名称")]
    public string damageUpgradeName = "伤害提升";
    [Tooltip("升级项描述")]
    [TextArea(2, 4)]
    public string damageDescription = "增加子弹的伤害值";
    [Tooltip("基础价格")]
    public int damageBasePrice = 15;
    [Tooltip("价格增长倍数")]
    [Range(0.1f, 2f)]
    public float damagePriceMultiplier = 0.5f;
    [Tooltip("每次升级增加的伤害")]
    public int damageValueIncrease = 1;
    [Tooltip("最大值限制（0表示无限制）")]
    public int damageMaxValue = 0;
    
    [Header("子弹数量升级配置")]
    [Tooltip("升级项名称")]
    public string countUpgradeName = "子弹数量";
    [Tooltip("升级项描述")]
    [TextArea(2, 4)]
    public string countDescription = "增加同时发射的子弹数量";
    [Tooltip("基础价格")]
    public int countBasePrice = 20;
    [Tooltip("价格增长倍数")]
    [Range(0.1f, 2f)]
    public float countPriceMultiplier = 0.5f;
    [Tooltip("每次升级增加的子弹数量")]
    public int countValueIncrease = 1;
    [Tooltip("最大值限制（0表示无限制，建议设置为5）")]
    public int countMaxValue = 5;
    
    [Header("发射速度升级配置")]
    [Tooltip("升级项名称")]
    public string fireSpeedUpgradeName = "发射速度";
    [Tooltip("升级项描述")]
    [TextArea(2, 4)]
    public string fireSpeedDescription = "减少发射间隔，提高发射速度";
    [Tooltip("基础价格")]
    public int fireSpeedBasePrice = 25;
    [Tooltip("价格增长倍数")]
    [Range(0.1f, 2f)]
    public float fireSpeedPriceMultiplier = 0.5f;
    [Tooltip("每次升级减少的发射间隔（秒）")]
    [Range(0f, 1f)]
    public float fireSpeedValueIncrease = 0.1f;
    [Tooltip("每次升级增加的百分比（0表示不使用百分比）")]
    [Range(0f, 1f)]
    public float fireSpeedPercentIncrease = 0.1f;
    [Tooltip("最小发射间隔（秒）")]
    [Range(0.1f, 1f)]
    public float fireSpeedMinInterval = 0.1f;
    
    [Header("攻击范围升级配置")]
    [Tooltip("升级项名称")]
    public string rangeUpgradeName = "攻击范围";
    [Tooltip("升级项描述")]
    [TextArea(2, 4)]
    public string rangeDescription = "增加自动攻击的范围";
    [Tooltip("基础价格")]
    public int rangeBasePrice = 30;
    [Tooltip("价格增长倍数")]
    [Range(0.1f, 2f)]
    public float rangePriceMultiplier = 0.5f;
    [Tooltip("每次升级增加的范围")]
    public int rangeValueIncrease = 5;
    [Tooltip("最大值限制（0表示无限制，建议设置为50）")]
    public int rangeMaxValue = 50;
    
    /// <summary>
    /// 获取指定类型的升级配置（转换为BirdUpgradeData格式）
    /// </summary>
    public BirdUpgradeData.UpgradeType GetUpgradeType()
    {
        // 这个方法用于兼容性，实际不需要
        return BirdUpgradeData.UpgradeType.Health;
    }
    
    /// <summary>
    /// 创建升级数据对象（用于兼容现有系统）
    /// </summary>
    public BirdUpgradeData CreateUpgradeData(BirdUpgradeData.UpgradeType type)
    {
        // 这个方法用于从配置创建升级数据，但我们可以直接使用配置
        return null;
    }
}

