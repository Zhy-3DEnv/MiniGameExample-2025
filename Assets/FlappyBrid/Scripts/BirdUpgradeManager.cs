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
    
    // 存储初始属性值（从小鸟获取，只获取一次）
    [Header("初始属性值（只获取一次）")]
    private int initialMaxHP = 0; // 0 表示未初始化
    private int initialBulletDamage = 0;
    private int initialBulletCount = 0;
    private float initialFireInterval = 0f;
    private float initialAttackRange = 0f;
    private bool initialValuesSaved = false; // 标记是否已保存初始值
    
    // 存储当前属性值（作为单一数据源）
    [Header("当前属性值（单一数据源）")]
    [Tooltip("当前最大血量")]
    public int currentMaxHP = 1;
    [Tooltip("当前子弹伤害")]
    public int currentBulletDamage = 1;
    [Tooltip("当前子弹数量")]
    public int currentBulletCount = 1;
    [Tooltip("当前发射间隔（秒，越小越快）")]
    public float currentFireInterval = 1f;
    [Tooltip("当前攻击范围")]
    public float currentAttackRange = 10f;
    
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
        
        // 如果没有设置小鸟引用，自动查找并初始化属性值
        if (bird == null)
        {
            bird = FindObjectOfType<BirdScript>();
        }
        if (bird != null)
        {
            // 从小鸟获取初始属性值
            InitializeAttributeValues();
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
        
        // 检查是否达到最大值（基于初始值 + 购买次数 * 增加值）
        if (maxValue > 0)
        {
            // 获取初始值
            int initialValue = 0;
            switch (upgradeType)
            {
                case BirdUpgradeData.UpgradeType.Health:
                    initialValue = bird != null ? bird.maxHP : currentMaxHP;
                    break;
                case BirdUpgradeData.UpgradeType.BulletDamage:
                    initialValue = bird != null ? bird.bulletDamage : currentBulletDamage;
                    break;
                case BirdUpgradeData.UpgradeType.BulletCount:
                    initialValue = bird != null ? bird.bulletCount : currentBulletCount;
                    break;
                case BirdUpgradeData.UpgradeType.AttackRange:
                    initialValue = Mathf.RoundToInt(bird != null ? bird.attackRange : currentAttackRange);
                    break;
            }
            
            // 计算当前值（初始值 + 已购买次数 * 增加值）
            float currentValue = initialValue + purchaseCount * valueIncrease;
            if (currentValue >= maxValue)
            {
                Debug.Log($"BirdUpgradeManager: 已达到最大值 {maxValue}！当前值: {currentValue}");
                return false;
            }
        }
        
        // 扣除金币
        currentCoins -= price;
        
        // 增加购买次数
        upgradePurchaseCount[upgradeKey] = purchaseCount + 1;
        
        // 直接更新属性值（单一数据源）
        UpdateAttributeValue(upgradeType, valueIncrease, percentIncrease);
        
        // 同步到小鸟
        SyncAttributesToBird();
        
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
    /// 初始化属性值（从小鸟获取初始值，只获取一次）
    /// </summary>
    private void InitializeAttributeValues()
    {
        if (bird == null) return;
        
        // 只在第一次初始化时保存初始值
        if (!initialValuesSaved)
        {
            initialMaxHP = bird.maxHP;
            initialBulletDamage = bird.bulletDamage;
            initialBulletCount = bird.bulletCount;
            initialFireInterval = bird.fireInterval;
            initialAttackRange = bird.attackRange;
            initialValuesSaved = true;
            
            Debug.Log($"BirdUpgradeManager: 保存初始属性值 - maxHP={initialMaxHP}, bulletDamage={initialBulletDamage}, bulletCount={initialBulletCount}, fireInterval={initialFireInterval}, attackRange={initialAttackRange}");
        }
        
        // 重置当前属性值到初始值（基于保存的初始值，而不是从bird获取）
        currentMaxHP = initialMaxHP;
        currentBulletDamage = initialBulletDamage;
        currentBulletCount = initialBulletCount;
        currentFireInterval = initialFireInterval;
        currentAttackRange = initialAttackRange;
        
        Debug.Log($"BirdUpgradeManager: 重置到初始属性值 - maxHP={currentMaxHP}, bulletDamage={currentBulletDamage}, bulletCount={currentBulletCount}, fireInterval={currentFireInterval}, attackRange={currentAttackRange}");
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
        
        // 先重置到初始属性值
        InitializeAttributeValues();
        
        // 根据购买次数重新计算所有属性值
        int purchaseCount;
        int valueIncrease;
        float percentIncrease;
        
        // 血量
        purchaseCount = GetPurchaseCount(BirdUpgradeData.UpgradeType.Health);
        if (purchaseCount > 0)
        {
            valueIncrease = upgradeConfig.healthValueIncrease;
            currentMaxHP = currentMaxHP + valueIncrease * purchaseCount;
        }
        
        // 子弹伤害
        purchaseCount = GetPurchaseCount(BirdUpgradeData.UpgradeType.BulletDamage);
        if (purchaseCount > 0)
        {
            valueIncrease = upgradeConfig.damageValueIncrease;
            currentBulletDamage = currentBulletDamage + valueIncrease * purchaseCount;
        }
        
        // 子弹数量
        purchaseCount = GetPurchaseCount(BirdUpgradeData.UpgradeType.BulletCount);
        if (purchaseCount > 0)
        {
            valueIncrease = upgradeConfig.countValueIncrease;
            currentBulletCount = currentBulletCount + valueIncrease * purchaseCount;
            if (currentBulletCount > 5) currentBulletCount = 5; // 限制最大数量
        }
        
        // 发射速度
        purchaseCount = GetPurchaseCount(BirdUpgradeData.UpgradeType.FireSpeed);
        if (purchaseCount > 0)
        {
            valueIncrease = (int)upgradeConfig.fireSpeedValueIncrease;
            percentIncrease = upgradeConfig.fireSpeedPercentIncrease;
            if (percentIncrease > 0)
            {
                // 百分比减少
                for (int i = 0; i < purchaseCount; i++)
                {
                    currentFireInterval = Mathf.Max(0.1f, currentFireInterval * (1f - percentIncrease));
                }
            }
            else
            {
                // 固定值减少
                currentFireInterval = Mathf.Max(0.1f, currentFireInterval - valueIncrease * 0.1f * purchaseCount);
            }
        }
        
        // 攻击范围
        purchaseCount = GetPurchaseCount(BirdUpgradeData.UpgradeType.AttackRange);
        if (purchaseCount > 0)
        {
            valueIncrease = upgradeConfig.rangeValueIncrease;
            currentAttackRange = currentAttackRange + valueIncrease * purchaseCount;
            if (currentAttackRange > 50f) currentAttackRange = 50f; // 限制最大范围
        }
        
        // 同步到小鸟
        SyncAttributesToBird();
    }
    
    /// <summary>
    /// 更新属性值（购买升级时调用）
    /// </summary>
    private void UpdateAttributeValue(BirdUpgradeData.UpgradeType upgradeType, int valueIncrease, float percentIncrease)
    {
        switch (upgradeType)
        {
            case BirdUpgradeData.UpgradeType.Health:
                currentMaxHP += valueIncrease;
                break;
            case BirdUpgradeData.UpgradeType.BulletDamage:
                currentBulletDamage += valueIncrease;
                break;
            case BirdUpgradeData.UpgradeType.BulletCount:
                currentBulletCount += valueIncrease;
                if (currentBulletCount > 5) currentBulletCount = 5;
                break;
            case BirdUpgradeData.UpgradeType.FireSpeed:
                if (percentIncrease > 0)
                {
                    currentFireInterval = Mathf.Max(0.1f, currentFireInterval * (1f - percentIncrease));
                }
                else
                {
                    currentFireInterval = Mathf.Max(0.1f, currentFireInterval - valueIncrease * 0.1f);
                }
                break;
            case BirdUpgradeData.UpgradeType.AttackRange:
                currentAttackRange += valueIncrease;
                if (currentAttackRange > 50f) currentAttackRange = 50f;
                break;
        }
    }
    
    /// <summary>
    /// 将属性值同步到小鸟
    /// </summary>
    public void SyncAttributesToBird()
    {
        if (bird == null)
        {
            bird = FindObjectOfType<BirdScript>();
        }
        
        if (bird == null) return;
        
        bird.maxHP = currentMaxHP;
        bird.SetCurrentHP(currentMaxHP); // 同步时恢复满血
        bird.bulletDamage = currentBulletDamage;
        bird.bulletCount = currentBulletCount;
        bird.fireInterval = currentFireInterval;
        bird.attackRange = currentAttackRange;
        
        Debug.Log($"BirdUpgradeManager: 已同步属性到小鸟 - maxHP={currentMaxHP}, bulletDamage={currentBulletDamage}, bulletCount={currentBulletCount}, fireInterval={currentFireInterval}, attackRange={currentAttackRange}");
    }
    
    /// <summary>
    /// 重置所有升级（用于游戏重新开始时）
    /// </summary>
    public void ResetAllUpgrades()
    {
        upgradePurchaseCount.Clear();
        
        // 重置属性值到初始值
        if (bird != null)
        {
            // 重置标记，允许重新保存初始值
            initialValuesSaved = false;
            InitializeAttributeValues();
            SyncAttributesToBird();
        }
        
        Debug.Log("BirdUpgradeManager: 已重置所有升级");
    }
    
    /// <summary>
    /// 获取当前最大血量
    /// </summary>
    public int GetCurrentMaxHP()
    {
        return currentMaxHP;
    }
    
    /// <summary>
    /// 获取当前子弹伤害
    /// </summary>
    public int GetCurrentBulletDamage()
    {
        return currentBulletDamage;
    }
    
    /// <summary>
    /// 获取当前子弹数量
    /// </summary>
    public int GetCurrentBulletCount()
    {
        return currentBulletCount;
    }
    
    /// <summary>
    /// 获取当前发射间隔
    /// </summary>
    public float GetCurrentFireInterval()
    {
        return currentFireInterval;
    }
    
    /// <summary>
    /// 获取当前攻击范围
    /// </summary>
    public float GetCurrentAttackRange()
    {
        return currentAttackRange;
    }
}
