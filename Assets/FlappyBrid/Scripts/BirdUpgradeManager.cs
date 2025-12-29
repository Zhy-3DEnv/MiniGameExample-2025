using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 小鸟升级管理器
/// 管理升级购买、属性持久化等
/// </summary>
public class BirdUpgradeManager : MonoBehaviour
{
    public static BirdUpgradeManager Instance { get; private set; }
    
    [Header("升级数据配置")]
    [Tooltip("升级配置（包含所有升级项的配置）")]
    public BirdUpgradeConfig upgradeConfig;
    
    [Header("小鸟引用")]
    [Tooltip("小鸟脚本引用")]
    public BirdScript bird;
    
    // 存储每个升级项的购买次数（用于计算价格和跨关卡保持）
    private Dictionary<string, int> upgradePurchaseCount = new Dictionary<string, int>();
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // 初始化购买次数字典
        if (upgradeConfig != null)
        {
            upgradePurchaseCount["Health"] = 0;
            upgradePurchaseCount["BulletDamage"] = 0;
            upgradePurchaseCount["BulletCount"] = 0;
            upgradePurchaseCount["FireSpeed"] = 0;
            upgradePurchaseCount["AttackRange"] = 0;
        }
    }
    
    void Start()
    {
        // 如果没有设置小鸟引用，自动查找
        if (bird == null)
        {
            bird = FindObjectOfType<BirdScript>();
        }
        
        // 应用已购买的升级
        ApplyAllUpgrades();
    }
    
    /// <summary>
    /// 购买升级
    /// </summary>
    /// <param name="upgradeType">升级类型</param>
    /// <param name="currentCoins">当前金币数</param>
    /// <returns>是否购买成功</returns>
    public bool PurchaseUpgrade(BirdUpgradeData.UpgradeType upgradeType, ref int currentCoins)
    {
        if (upgradeConfig == null)
        {
            Debug.LogError("BirdUpgradeManager: 升级配置为空！请设置 upgradeConfig");
            return false;
        }
        
        string upgradeKey = upgradeType.ToString();
        int purchaseCount = GetPurchaseCount(upgradeType);
        
        // 获取配置数据
        int basePrice = 0;
        float priceMultiplier = 0f;
        int valueIncrease = 0;
        float percentIncrease = 0f;
        int maxValue = 0;
        string upgradeName = "";
        
        switch (upgradeType)
        {
            case BirdUpgradeData.UpgradeType.Health:
                basePrice = upgradeConfig.healthBasePrice;
                priceMultiplier = upgradeConfig.healthPriceMultiplier;
                valueIncrease = upgradeConfig.healthValueIncrease;
                maxValue = upgradeConfig.healthMaxValue;
                upgradeName = upgradeConfig.healthUpgradeName;
                break;
            case BirdUpgradeData.UpgradeType.BulletDamage:
                basePrice = upgradeConfig.damageBasePrice;
                priceMultiplier = upgradeConfig.damagePriceMultiplier;
                valueIncrease = upgradeConfig.damageValueIncrease;
                maxValue = upgradeConfig.damageMaxValue;
                upgradeName = upgradeConfig.damageUpgradeName;
                break;
            case BirdUpgradeData.UpgradeType.BulletCount:
                basePrice = upgradeConfig.countBasePrice;
                priceMultiplier = upgradeConfig.countPriceMultiplier;
                valueIncrease = upgradeConfig.countValueIncrease;
                maxValue = upgradeConfig.countMaxValue;
                upgradeName = upgradeConfig.countUpgradeName;
                break;
            case BirdUpgradeData.UpgradeType.FireSpeed:
                basePrice = upgradeConfig.fireSpeedBasePrice;
                priceMultiplier = upgradeConfig.fireSpeedPriceMultiplier;
                valueIncrease = (int)upgradeConfig.fireSpeedValueIncrease;
                percentIncrease = upgradeConfig.fireSpeedPercentIncrease;
                maxValue = 0; // 发射速度使用最小间隔限制
                upgradeName = upgradeConfig.fireSpeedUpgradeName;
                break;
            case BirdUpgradeData.UpgradeType.AttackRange:
                basePrice = upgradeConfig.rangeBasePrice;
                priceMultiplier = upgradeConfig.rangePriceMultiplier;
                valueIncrease = upgradeConfig.rangeValueIncrease;
                maxValue = upgradeConfig.rangeMaxValue;
                upgradeName = upgradeConfig.rangeUpgradeName;
                break;
        }
        
        // 计算价格
        int price = Mathf.RoundToInt(basePrice * (1f + priceMultiplier * purchaseCount));
        
        // 检查是否有足够的金币
        if (currentCoins < price)
        {
            Debug.Log($"BirdUpgradeManager: 金币不足！需要 {price}，当前 {currentCoins}");
            return false;
        }
        
        // 检查是否达到最大值
        if (maxValue > 0)
        {
            float currentValue = purchaseCount * valueIncrease;
            if (currentValue >= maxValue)
            {
                Debug.Log($"BirdUpgradeManager: 已达到最大值 {maxValue}！");
                return false;
            }
        }
        
        // 扣除金币
        currentCoins -= price;
        
        // 增加购买次数
        upgradePurchaseCount[upgradeKey] = purchaseCount + 1;
        
        // 应用升级
        if (bird == null)
        {
            bird = FindObjectOfType<BirdScript>();
        }
        if (bird != null)
        {
            bird.ApplyUpgrade(upgradeType, valueIncrease, percentIncrease);
        }
        
        Debug.Log($"BirdUpgradeManager: 成功购买 {upgradeName}，花费 {price} 金币，剩余 {currentCoins} 金币");
        
        return true;
    }
    
    /// <summary>
    /// 获取升级价格
    /// </summary>
    public int GetUpgradePrice(BirdUpgradeData.UpgradeType upgradeType)
    {
        if (upgradeConfig == null) return 0;
        
        int purchaseCount = GetPurchaseCount(upgradeType);
        int basePrice = 0;
        float priceMultiplier = 0f;
        
        switch (upgradeType)
        {
            case BirdUpgradeData.UpgradeType.Health:
                basePrice = upgradeConfig.healthBasePrice;
                priceMultiplier = upgradeConfig.healthPriceMultiplier;
                break;
            case BirdUpgradeData.UpgradeType.BulletDamage:
                basePrice = upgradeConfig.damageBasePrice;
                priceMultiplier = upgradeConfig.damagePriceMultiplier;
                break;
            case BirdUpgradeData.UpgradeType.BulletCount:
                basePrice = upgradeConfig.countBasePrice;
                priceMultiplier = upgradeConfig.countPriceMultiplier;
                break;
            case BirdUpgradeData.UpgradeType.FireSpeed:
                basePrice = upgradeConfig.fireSpeedBasePrice;
                priceMultiplier = upgradeConfig.fireSpeedPriceMultiplier;
                break;
            case BirdUpgradeData.UpgradeType.AttackRange:
                basePrice = upgradeConfig.rangeBasePrice;
                priceMultiplier = upgradeConfig.rangePriceMultiplier;
                break;
        }
        
        // 价格 = 基础价格 * (1 + 增长倍数 * 购买次数)
        int price = Mathf.RoundToInt(basePrice * (1f + priceMultiplier * purchaseCount));
        
        return price;
    }
    
    /// <summary>
    /// 获取购买次数
    /// </summary>
    public int GetPurchaseCount(BirdUpgradeData.UpgradeType upgradeType)
    {
        string upgradeKey = upgradeType.ToString();
        if (upgradePurchaseCount.ContainsKey(upgradeKey))
        {
            return upgradePurchaseCount[upgradeKey];
        }
        return 0;
    }
    
    /// <summary>
    /// 应用所有已购买的升级（公共方法，供外部调用）
    /// </summary>
    public void ApplyAllUpgrades()
    {
        if (bird == null)
        {
            bird = FindObjectOfType<BirdScript>();
        }
        
        if (bird == null || upgradeConfig == null) return;
        
        // 应用所有升级类型
        ApplyUpgradeByType(BirdUpgradeData.UpgradeType.Health);
        ApplyUpgradeByType(BirdUpgradeData.UpgradeType.BulletDamage);
        ApplyUpgradeByType(BirdUpgradeData.UpgradeType.BulletCount);
        ApplyUpgradeByType(BirdUpgradeData.UpgradeType.FireSpeed);
        ApplyUpgradeByType(BirdUpgradeData.UpgradeType.AttackRange);
    }
    
    /// <summary>
    /// 根据类型应用升级（内部方法）
    /// </summary>
    private void ApplyUpgradeByType(BirdUpgradeData.UpgradeType upgradeType)
    {
        if (bird == null || upgradeConfig == null) return;
        
        int purchaseCount = GetPurchaseCount(upgradeType);
        if (purchaseCount <= 0) return;
        
        int valueIncrease = 0;
        float percentIncrease = 0f;
        
        switch (upgradeType)
        {
            case BirdUpgradeData.UpgradeType.Health:
                valueIncrease = upgradeConfig.healthValueIncrease;
                break;
            case BirdUpgradeData.UpgradeType.BulletDamage:
                valueIncrease = upgradeConfig.damageValueIncrease;
                break;
            case BirdUpgradeData.UpgradeType.BulletCount:
                valueIncrease = upgradeConfig.countValueIncrease;
                break;
            case BirdUpgradeData.UpgradeType.FireSpeed:
                valueIncrease = (int)upgradeConfig.fireSpeedValueIncrease;
                percentIncrease = upgradeConfig.fireSpeedPercentIncrease;
                break;
            case BirdUpgradeData.UpgradeType.AttackRange:
                valueIncrease = upgradeConfig.rangeValueIncrease;
                break;
        }
        
        // 应用升级（根据购买次数）
        for (int i = 0; i < purchaseCount; i++)
        {
            bird.ApplyUpgrade(upgradeType, valueIncrease, percentIncrease);
        }
    }
    
    /// <summary>
    /// 重置所有升级（用于游戏重新开始时）
    /// </summary>
    public void ResetAllUpgrades()
    {
        upgradePurchaseCount.Clear();
        
        Debug.Log("BirdUpgradeManager: 已重置所有升级");
    }
}
