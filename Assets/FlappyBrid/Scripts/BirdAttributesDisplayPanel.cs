using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 小鸟属性显示面板
/// 统一的属性显示组件，可以在HUD、商店等不同场景中复用
/// 作为预制体使用，会自动注册到 logicManager
/// </summary>
public class BirdAttributesDisplayPanel : MonoBehaviour
{
    [Header("属性显示文本")]
    [Tooltip("显示小鸟血量的文本（格式：HP: x/y）")]
    public Text birdHPText;
    [Tooltip("显示子弹伤害的文本")]
    public Text bulletDamageText;
    [Tooltip("显示子弹数量的文本")]
    public Text bulletCountText;
    [Tooltip("显示发射速度的文本（显示发射间隔，越小越快）")]
    public Text fireSpeedText;
    [Tooltip("显示攻击范围的文本")]
    public Text attackRangeText;
    
    private logicManager logicManager;
    
    void Awake()
    {
        // 自动注册到 logicManager
        logicManager = FindObjectOfType<logicManager>();
        if (logicManager != null)
        {
            logicManager.RegisterAttributesPanel(this);
        }
    }
    
    void OnDestroy()
    {
        // 注销
        if (logicManager != null)
        {
            logicManager.UnregisterAttributesPanel(this);
        }
    }
    
    /// <summary>
    /// 更新属性显示（从 BirdUpgradeManager 读取，单一数据源）
    /// </summary>
    public void UpdateAttributes()
    {
        if (BirdUpgradeManager.Instance == null) return;
        
        // 获取小鸟当前血量（从 BirdScript 读取，因为血量会变化）
        BirdScript bird = FindObjectOfType<BirdScript>();
        int currentHP = bird != null ? bird.GetCurrentHP() : BirdUpgradeManager.Instance.GetCurrentMaxHP();
        
        // 更新血量显示（从 BirdUpgradeManager 读取最大血量）
        if (birdHPText != null)
        {
            int maxHP = BirdUpgradeManager.Instance.GetCurrentMaxHP();
            birdHPText.text = $"生命值: {currentHP}/{maxHP}";
        }
        
        // 更新子弹伤害显示（从 BirdUpgradeManager 读取）
        if (bulletDamageText != null)
        {
            bulletDamageText.text = $"伤害: {BirdUpgradeManager.Instance.GetCurrentBulletDamage()}";
        }
        
        // 更新子弹数量显示（从 BirdUpgradeManager 读取）
        if (bulletCountText != null)
        {
            bulletCountText.text = $"子弹数: {BirdUpgradeManager.Instance.GetCurrentBulletCount()}";
        }
        
        // 更新发射速度显示（从 BirdUpgradeManager 读取）
        if (fireSpeedText != null)
        {
            float fireInterval = BirdUpgradeManager.Instance.GetCurrentFireInterval();
            fireSpeedText.text = $"发射间隔: {fireInterval:F2}s";
        }
        
        // 更新攻击范围显示（从 BirdUpgradeManager 读取）
        if (attackRangeText != null)
        {
            float attackRange = BirdUpgradeManager.Instance.GetCurrentAttackRange();
            attackRangeText.text = $"攻击范围: {attackRange:F1}";
        }
    }
}

