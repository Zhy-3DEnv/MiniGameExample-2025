using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 升级按钮脚本
/// 处理升级按钮的显示和点击
/// </summary>
public class UpgradeButtonScript : MonoBehaviour
{
    [Header("UI组件")]
    [Tooltip("升级名称文本")]
    public Text nameText;
    [Tooltip("升级描述文本")]
    public Text descriptionText;
    [Tooltip("价格文本")]
    public Text priceText;
    [Tooltip("购买按钮")]
    public Button purchaseButton;
    
    private BirdUpgradeData.UpgradeType upgradeType;
    private string upgradeName;
    private string description;
    private logicManager logicManager;
    
    /// <summary>
    /// 初始化升级按钮（只设置数据，不修改 RectTransform）
    /// </summary>
    public void Initialize(BirdUpgradeData.UpgradeType type, string name, string desc, logicManager logic)
    {
        upgradeType = type;
        upgradeName = name;
        description = desc;
        logicManager = logic;
        
        // 更新UI显示
        UpdateUI();
        
        // 绑定购买按钮
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(OnPurchaseClicked);
        }
    }
    
    /// <summary>
    /// 更新UI显示
    /// </summary>
    public void UpdateUI()
    {
        // 更新名称
        if (nameText != null)
        {
            nameText.text = upgradeName;
        }
        
        // 更新描述
        if (descriptionText != null)
        {
            descriptionText.text = description;
        }
        
        // 更新价格
        if (priceText != null && BirdUpgradeManager.Instance != null)
        {
            int price = BirdUpgradeManager.Instance.GetUpgradePrice(upgradeType);
            priceText.text = $"价格: {price} 金币";
            
            // 检查是否可以购买
            if (logicManager != null && logicManager.totalCoins < price)
            {
                priceText.color = Color.red; // 金币不足时显示红色
            }
            else
            {
                priceText.color = Color.white;
            }
        }
        
        // 更新按钮状态
        if (purchaseButton != null)
        {
            bool canPurchase = CanPurchase();
            purchaseButton.interactable = canPurchase;
        }
    }
    
    /// <summary>
    /// 检查是否可以购买
    /// </summary>
    private bool CanPurchase()
    {
        if (BirdUpgradeManager.Instance == null || logicManager == null)
            return false;
        
        int price = BirdUpgradeManager.Instance.GetUpgradePrice(upgradeType);
        
        // 检查金币
        if (logicManager.totalCoins < price)
            return false;
        
        // 检查最大值（需要从配置中获取）
        // 这里可以添加检查是否达到最大值的逻辑
        
        return true;
    }
    
    /// <summary>
    /// 购买按钮点击事件
    /// </summary>
    private void OnPurchaseClicked()
    {
        if (logicManager != null)
        {
            logicManager.PurchaseUpgrade(upgradeType);
            UpdateUI(); // 更新UI（价格可能变化）
        }
    }
}
