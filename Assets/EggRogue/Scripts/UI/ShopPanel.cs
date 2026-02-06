using UnityEngine;
using UnityEngine.UI;
using EggRogue;

/// <summary>
/// 商店界面 - 选卡完成后必进，可购买武器/道具，也可不购买直接继续。
/// </summary>
public class ShopPanel : BaseUIPanel
{
    [Header("UI 引用")]
    [Tooltip("标题文本")]
    public Text titleText;

    [Tooltip("金币数量文本")]
    public Text goldText;

    [Tooltip("继续按钮（进入下一关）")]
    public Button continueButton;

    [Tooltip("随机按钮（花费金币重新随机商品）")]
    public Button rerollButton;

    [Header("角色武器槽（6 个，商店内显示）")]
    [Tooltip("武器槽显示组件，展示当前装备的 6 把武器，可点击出售、拖拽合并")]
    public ShopWeaponSlotsDisplay weaponSlotsDisplay;

    [Header("已购买物品栏")]
    [Tooltip("已购买道具显示组件")]
    public ShopItemInventoryDisplay itemInventoryDisplay;

    [Header("提示文本（可选）")]
    [Tooltip("购买失败等提示信息")]
    public Text tipText;

    [Tooltip("提示文本显示时长（秒）")]
    public float tipDuration = 1.5f;

    private Coroutine _tipRoutine;

    [Header("商品槽（5 个）")]
    [Tooltip("商品槽 0-4：每个需包含 Icon(Image)、Name(Text)、Price(Text)、BuyButton(Button)")]
    public ShopItemSlotView[] itemSlots = new ShopItemSlotView[5];

    [Header("商品槽自动生成（可选）")]
    [Tooltip("商品槽容器（挂有 Horizontal / Grid Layout 的节点）")]
    public Transform itemSlotsContainer;

    [Tooltip("商品槽基础预制体（内部需包含 Icon / Name / Price / BuyButton 子节点）")]
    public GameObject itemSlotPrefab;

    [Tooltip("自动生成的商品槽数量")]
    public int autoItemSlotCount = 5;

    [System.Serializable]
    public struct ShopItemSlotView
    {
        public Image icon;
        public Text nameText;
        public Text priceText;
        public Button buyButton;
        public Button lockButton;
    }

    /// <summary>
    /// 如果用户只在 Inspector 中指定了一个商品槽预制体和容器，
    /// 这里会在运行时自动克隆出 autoItemSlotCount 个槽位，并填充到 itemSlots 数组。
    /// </summary>
    private void TryAutoBuildItemSlots()
    {
        if (itemSlotPrefab == null || itemSlotsContainer == null)
            return;

        // 已经手工配置好了槽位就不再自动生成，避免覆盖你的现有设置
        if (itemSlots != null && itemSlots.Length > 0 && itemSlots[0].buyButton != null)
            return;

        // 运行时：清空容器下原本手动摆放的子物体（仅用于编辑预览）
        if (Application.isPlaying)
        {
            for (int i = itemSlotsContainer.childCount - 1; i >= 0; i--)
            {
                var child = itemSlotsContainer.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }
        }

        int count = Mathf.Max(1, autoItemSlotCount);
        itemSlots = new ShopItemSlotView[count];

        for (int i = 0; i < count; i++)
        {
            var slotGO = Instantiate(itemSlotPrefab, itemSlotsContainer);
            slotGO.name = $"ItemSlot_{i}";

            // 约定结构：Icon, Name, Price, BuyButton, LockButton（可选）
            var icon = slotGO.transform.Find("Icon")?.GetComponent<Image>();
            var nameText = slotGO.transform.Find("Name")?.GetComponent<Text>();
            var priceText = slotGO.transform.Find("Price")?.GetComponent<Text>();
            var buyButton = slotGO.transform.Find("BuyButton")?.GetComponent<Button>();
            var lockButton = slotGO.transform.Find("LockButton")?.GetComponent<Button>();

            itemSlots[i] = new ShopItemSlotView
            {
                icon = icon,
                nameText = nameText,
                priceText = priceText,
                buyButton = buyButton,
                lockButton = lockButton
            };
        }
    }

    private void Awake()
    {
        TryAutoBuildItemSlots();
    }

    private void Start()
    {
        for (int i = 0; i < itemSlots.Length; i++)
        {
            int index = i;
            if (itemSlots[i].buyButton != null)
            {
                itemSlots[i].buyButton.onClick.RemoveAllListeners();
                itemSlots[i].buyButton.onClick.AddListener(() => OnBuyClicked(index));
            }
            if (itemSlots[i].lockButton != null)
            {
                itemSlots[i].lockButton.onClick.RemoveAllListeners();
                itemSlots[i].lockButton.onClick.AddListener(() => OnLockClicked(index));
            }
        }
        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(OnRerollClicked);
        }
    }

    private void OnEnable()
    {
        if (GoldManager.Instance != null)
            GoldManager.Instance.OnGoldChanged.AddListener(OnGoldChanged);
        if (WeaponInventoryManager.Instance != null)
            WeaponInventoryManager.Instance.OnWeaponsChanged += OnWeaponsChanged;
    }

    private void OnDisable()
    {
        if (GoldManager.Instance != null)
            GoldManager.Instance.OnGoldChanged.RemoveListener(OnGoldChanged);
        if (WeaponInventoryManager.Instance != null)
            WeaponInventoryManager.Instance.OnWeaponsChanged -= OnWeaponsChanged;
    }

    private void OnWeaponsChanged()
    {
        RefreshAll();
    }

    protected override void OnShow()
    {
        base.OnShow();

        // 行业标准做法：进入商店时清理所有伤害飘字，避免它们显示在UI上并阻挡交互
        ClearAllDamagePopups();

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.ResetRerollCountForNewShop();
            ShopManager.Instance.RefreshShop();
        }

        RefreshAll();
        if (titleText != null)
            titleText.text = "商店";
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        if (GameplayPauseManager.Instance != null)
            GameplayPauseManager.Instance.RequestPause("ShopPanel");
    }
    
    /// <summary>
    /// 清理场景中所有活跃的伤害飘字（ScorePopup），避免它们显示在商店UI上并阻挡交互。
    /// 行业标准做法：在进入UI界面（商店/选卡等）时统一清理所有飘字。
    /// </summary>
    private void ClearAllDamagePopups()
    {
        // 查找所有 ScorePopup 组件并销毁
        FlappyBird.ScorePopup[] popups = Object.FindObjectsOfType<FlappyBird.ScorePopup>();
        foreach (var popup in popups)
        {
            if (popup != null && popup.gameObject != null)
            {
                Object.Destroy(popup.gameObject);
            }
        }
    }

    protected override void OnHide()
    {
        base.OnHide();
        if (GameplayPauseManager.Instance != null)
            GameplayPauseManager.Instance.RequestResume("ShopPanel");
    }

    private void OnGoldChanged(int _)
    {
        RefreshAll();
    }

    public void RefreshAll()
    {
        RefreshGoldDisplay();
        RefreshItemSlots();
        RefreshRerollButton();
        if (weaponSlotsDisplay != null)
            weaponSlotsDisplay.Refresh();
        if (itemInventoryDisplay != null)
            itemInventoryDisplay.Refresh();
    }

    private void RefreshGoldDisplay()
    {
        if (goldText == null) return;
        int gold = GoldManager.Instance != null ? GoldManager.Instance.Gold : 0;
        goldText.text = $"金币: {gold}";
    }

    private void RefreshItemSlots()
    {
        var items = ShopManager.Instance != null ? ShopManager.Instance.CurrentItems : null;
        int gold = GoldManager.Instance != null ? GoldManager.Instance.Gold : 0;

        for (int i = 0; i < itemSlots.Length; i++)
        {
            var slot = itemSlots[i];
            bool hasSlot = slot.icon != null || slot.nameText != null || slot.priceText != null || slot.buyButton != null;
            if (!hasSlot) continue;

            ShopItemData item = null;
            if (items != null && i < items.Count)
                item = items[i];

            bool isLocked = ShopManager.Instance != null && ShopManager.Instance.IsLocked(i);

            if (item == null)
            {
                if (slot.icon != null) { slot.icon.enabled = false; slot.icon.sprite = null; }
                if (slot.nameText != null) slot.nameText.text = "-";
                if (slot.priceText != null) slot.priceText.text = "";
                if (slot.buyButton != null)
                {
                    slot.buyButton.gameObject.SetActive(false);
                    slot.buyButton.interactable = false;
                }
                if (slot.lockButton != null)
                    slot.lockButton.gameObject.SetActive(false);
                continue;
            }

            if (slot.icon != null)
            {
                slot.icon.sprite = item.Icon;
                slot.icon.enabled = item.Icon != null;
            }
            if (slot.nameText != null)
                slot.nameText.text = item.DisplayName;
            bool canBuy = gold >= item.Price;

            if (slot.priceText != null)
            {
                slot.priceText.text = $"{item.Price}金币";
                slot.priceText.color = canBuy ? Color.yellow : Color.red;
            }
            if (slot.buyButton != null)
            {
                slot.buyButton.gameObject.SetActive(true);
                slot.buyButton.interactable = canBuy;
            }
            if (slot.lockButton != null)
            {
                slot.lockButton.gameObject.SetActive(true);
                var lockText = slot.lockButton.GetComponentInChildren<Text>();
                if (lockText != null)
                {
                    lockText.text = isLocked ? "已锁" : "锁定";
                    lockText.color = isLocked ? Color.red : Color.white;
                }
                slot.lockButton.interactable = true;
            }
        }
    }

    private void RefreshRerollButton()
    {
        if (rerollButton == null) return;
        int price = ShopManager.Instance != null ? ShopManager.Instance.CurrentRerollPrice : 0;
        int gold = GoldManager.Instance != null ? GoldManager.Instance.Gold : 0;
        bool canAfford = gold >= price;

        var text = rerollButton.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = $"随机 ({price}金币)";
            text.color = canAfford ? Color.white : Color.red;
        }
        rerollButton.interactable = canAfford;
    }

    private void OnBuyClicked(int slotIndex)
    {
        if (ShopManager.Instance == null) return;
        if (ShopManager.Instance.TryPurchase(slotIndex))
        {
            RefreshAll();
            ShowTip("");
            return;
        }

        ShowPurchaseFailTip(ShopManager.Instance.LastFailReason);
    }

    private void OnRerollClicked()
    {
        if (ShopManager.Instance == null) return;
        if (ShopManager.Instance.TryReroll())
            RefreshAll();
    }

    private void OnLockClicked(int slotIndex)
    {
        if (ShopManager.Instance == null) return;
        ShopManager.Instance.ToggleLock(slotIndex);
        RefreshAll();
    }

    private void OnContinueClicked()
    {
        // 保存商店状态（锁定物品等）
        if (ShopManager.Instance != null)
            ShopManager.Instance.SaveLockedItemsForNextShop();

        // 保存玩家血量（如果需要继承到下一关）
        if (LevelManager.Instance != null && !LevelManager.Instance.fullHealOnNextLevel)
        {
            CharacterStats stats = FindObjectOfType<CharacterStats>();
            if (stats != null)
            {
                Health h = stats.GetComponent<Health>();
                if (h != null)
                    LevelManager.Instance.SaveRunStateHealth(h.CurrentHealth, h.maxHealth);
            }
        }

        // 通知 UIManager 开始过渡到下一关（UIManager 负责显示 Loading 和执行加载）
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RequestNextLevelTransition();
        }
        else
        {
            // 兜底：如果没有 UIManager，直接隐藏并加载下一关
            Hide();
            if (LevelManager.Instance != null)
                LevelManager.Instance.NextLevel();
        }
    }

    private void ShowPurchaseFailTip(ShopManager.ShopPurchaseFailReason reason)
    {
        string msg = reason switch
        {
            ShopManager.ShopPurchaseFailReason.NotEnoughGold => "金币不足",
            ShopManager.ShopPurchaseFailReason.WeaponSlotsFull => "武器槽已满，无法购买",
            ShopManager.ShopPurchaseFailReason.ItemInventoryFull => "物品栏已满，无法购买",
            ShopManager.ShopPurchaseFailReason.InventoryMissing => "缺少库存管理器",
            _ => "无法购买"
        };
        ShowTip(msg);
    }

    private void ShowTip(string message)
    {
        if (tipText == null) return;

        if (_tipRoutine != null)
            StopCoroutine(_tipRoutine);

        if (string.IsNullOrEmpty(message))
        {
            tipText.gameObject.SetActive(false);
            return;
        }

        tipText.text = message;
        tipText.gameObject.SetActive(true);
        _tipRoutine = StartCoroutine(HideTipAfterSeconds(tipDuration));
    }

    private System.Collections.IEnumerator HideTipAfterSeconds(float seconds)
    {
        if (seconds <= 0f)
        {
            tipText.gameObject.SetActive(false);
            yield break;
        }
        yield return new WaitForSeconds(seconds);
        if (tipText != null)
            tipText.gameObject.SetActive(false);
    }
}
