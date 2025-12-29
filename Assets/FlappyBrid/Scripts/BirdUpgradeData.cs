using UnityEngine;

/// <summary>
/// 小鸟升级项数据配置
/// 定义每个升级项的价格、效果等
/// </summary>
[CreateAssetMenu(fileName = "NewBirdUpgrade", menuName = "FlappyBird/Bird Upgrade Data", order = 1)]
public class BirdUpgradeData : ScriptableObject
{
    [Header("升级项基本信息")]
    [Tooltip("升级项名称")]
    public string upgradeName = "血量提升";
    
    [Tooltip("升级项描述")]
    [TextArea(2, 4)]
    public string description = "增加小鸟的最大血量";
    
    [Header("价格设置")]
    [Tooltip("基础价格（第一次购买的价格）")]
    public int basePrice = 10;
    
    [Tooltip("价格增长倍数（每次购买后价格 = 基础价格 * (1 + 增长倍数 * 购买次数)）")]
    [Range(0.1f, 2f)]
    public float priceMultiplier = 0.5f;
    
    [Header("效果设置")]
    [Tooltip("升级类型")]
    public UpgradeType upgradeType;
    
    [Tooltip("每次升级增加的值（血量、伤害等）")]
    public int valueIncrease = 1;
    
    [Tooltip("每次升级增加的百分比（用于速度等，0表示不使用百分比）")]
    [Range(0f, 1f)]
    public float percentIncrease = 0f;
    
    [Tooltip("最大值限制（0表示无限制）")]
    public int maxValue = 0;
    
    public enum UpgradeType
    {
        Health,          // 血量
        BulletDamage,    // 子弹伤害
        BulletCount,     // 子弹数量
        FireSpeed,       // 发射速度（减少发射间隔）
        AttackRange      // 攻击范围
    }
}

