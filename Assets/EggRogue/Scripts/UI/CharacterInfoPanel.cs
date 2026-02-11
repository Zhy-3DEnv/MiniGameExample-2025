using UnityEngine;
using UnityEngine.UI;
using EggRogue;

/// <summary>
/// 角色信息面板（原属性面板）- 显示玩家等级与角色当前属性数值。
/// 按 ESC 键打开/关闭。
/// </summary>
[ExecuteAlways]
public class CharacterInfoPanel : BaseUIPanel
{
    [Header("UI 引用")]
    [Tooltip("关闭按钮（点击关闭面板）")]
    public Button closeButton;

    [Tooltip("玩家等级文本（格式：Lv.X 当前经验/升级所需，例如：Lv.3 25/45）")]
    public Text playerLevelText;

    [Header("自动生成模式")]
    [Tooltip("属性行容器（例如一个 VerticalLayoutGroup）")]
    public Transform attributesContainer;

    [Tooltip("属性行预制体（需要包含 NameText 和 ValueText 两个 Text 子节点）")]
    public GameObject attributeRowPrefab;

    [Header("已拥有道具（可选）")]
    [Tooltip("道具列表容器（需带 GridLayoutGroup 以网格展示图标，或 VerticalLayoutGroup）")]
    public Transform itemsContainer;

    [Tooltip("编辑状态下显示的临时道具，用于调整布局。运行时使用真实已购买道具")]
    public ItemData[] editorPreviewItems = new ItemData[0];

    [Tooltip("道具槽预制体（PurchasedItems 形式：含 Icon、CountText 子节点）。与商店已购买物品栏一致")]
    public GameObject itemSlotPrefab;

    [Header("道具说明提示（点击图标在图标旁显示）")]
    [Tooltip("说明文本宽度（像素）")]
    public float itemTooltipWidth = 200f;

    [Tooltip("说明文本字体大小")]
    public int itemTooltipFontSize = 12;

    [Tooltip("道具说明提示预制体（可选；留空则使用默认样式）")]
    public RectTransform itemTooltipPrefab;

    private RectTransform _itemTooltipRoot;
    private Text _itemTooltipText;
    private RectTransform _lastTooltipSlot;
    private bool wasActiveOnAwake = false;
    private bool openingViaShow = false;

    private void Awake()
    {
        wasActiveOnAwake = gameObject.activeSelf;
    }

    public override void Show()
    {
        openingViaShow = true;
        base.Show();
    }

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);

        if (playerLevelText == null)
            playerLevelText = transform.Find("PlayerLevelText")?.GetComponent<Text>();

        if (!openingViaShow && wasActiveOnAwake && gameObject.activeSelf)
        {
            gameObject.SetActive(false);
            isVisible = false;
        }
        openingViaShow = false;
    }

    private void Update()
    {
        if (Application.isPlaying)
        {
            if (IsVisible())
                UpdateAttributes();
        }
        else
        {
            if (gameObject.activeInHierarchy)
                UpdateAttributes();
        }
    }

    private void OnEnable()
    {
        if (Application.isPlaying && ItemInventoryManager.Instance != null)
            ItemInventoryManager.Instance.OnItemsChanged += RefreshOwnedItemsWhenNeeded;
        if (!Application.isPlaying && gameObject.activeInHierarchy)
            UpdateAttributes();
    }

    private void OnDisable()
    {
        if (Application.isPlaying && ItemInventoryManager.Instance != null)
            ItemInventoryManager.Instance.OnItemsChanged -= RefreshOwnedItemsWhenNeeded;
    }

    private void RefreshOwnedItemsWhenNeeded()
    {
        if (itemsContainer != null && IsVisible())
            RefreshOwnedItems();
    }

    /// <summary>
    /// 切换面板显示/隐藏
    /// </summary>
    public void TogglePanel()
    {
        if (IsVisible())
            Hide();
        else
            Show();
    }

    private void OnCloseClicked()
    {
        Hide();
    }

    /// <summary>
    /// 更新属性显示（等级、属性列表）。道具列表仅在 OnShow 与背包变化时刷新，避免每帧重建导致点击失效。
    /// </summary>
    private void UpdateAttributes()
    {
        if (attributesContainer != null && attributeRowPrefab != null)
            UpdateAttributesDynamic();

        RefreshPlayerLevel();
        // 不再每帧刷新道具列表，见 RefreshOwnedItemsWhenNeeded / OnShow / OnItemsChanged
    }

    /// <summary>
    /// 刷新已拥有道具列表。编辑状态使用 editorPreviewItems；运行时从 ItemInventoryManager 读取。
    /// </summary>
    private void RefreshOwnedItems()
    {
        if (itemsContainer == null) return;

        for (int i = itemsContainer.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(itemsContainer.GetChild(i).gameObject);
            else
                DestroyImmediate(itemsContainer.GetChild(i).gameObject);
        }

        var stacks = GetItemStacksToDisplay();
        if (stacks == null || stacks.Count == 0) return;

        if (itemSlotPrefab != null)
            RefreshOwnedItemsAsSlots(stacks);
    }

    private System.Collections.Generic.List<(ItemData item, int count)> GetItemStacksToDisplay()
    {
        if (!Application.isPlaying && editorPreviewItems != null && editorPreviewItems.Length > 0)
        {
            var list = new System.Collections.Generic.List<(ItemData, int)>();
            foreach (var item in editorPreviewItems)
            {
                if (item != null)
                    list.Add((item, 1));
            }
            return list;
        }

        var inv = ItemInventoryManager.Instance;
        if (inv == null) return null;

        return inv.GetItemStacks();
    }

    /// <summary>
    /// 使用 PurchasedItems 形式（图标 + 数量）显示道具，支持点击图标弹出说明
    /// </summary>
    private void RefreshOwnedItemsAsSlots(System.Collections.Generic.List<(ItemData item, int count)> stacks)
    {
        EnsureItemsContainerGridLayout();

        foreach (var (item, count) in stacks)
        {
            if (item == null) continue;

            GameObject slot = Instantiate(itemSlotPrefab, itemsContainer);
            var iconTr = slot.transform.Find("Icon");
            var iconImg = iconTr != null ? iconTr.GetComponent<Image>() : slot.GetComponentInChildren<Image>(true);
            if (iconImg != null)
            {
                iconImg.sprite = item.icon;
                iconImg.enabled = item.icon != null;
                iconImg.raycastTarget = false;
            }

            var countTr = slot.transform.Find("CountText");
            var countText = countTr != null ? countTr.GetComponent<Text>() : null;
            if (countText != null)
            {
                countText.gameObject.SetActive(count > 1);
                countText.text = count.ToString();
                GameFont.ApplyTo(countText);
            }

            var btn = slot.GetComponent<Button>();
            if (btn == null)
                btn = slot.AddComponent<Button>();
            // 确保 Button 有可点击目标（否则点击可能无效）
            var slotImage = slot.GetComponent<Image>();
            if (slotImage != null && btn.targetGraphic != slotImage)
                btn.targetGraphic = slotImage;
            var capturedItem = item;
            var slotRt = slot.GetComponent<RectTransform>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnItemSlotClicked(capturedItem, slotRt));
        }
    }

    private void EnsureItemsContainerGridLayout()
    {
        if (itemsContainer == null) return;
        var grid = itemsContainer.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = itemsContainer.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(44, 44);
            grid.spacing = new Vector2(6, 6);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
        }
    }

    private void OnItemSlotClicked(ItemData item, RectTransform slotRect)
    {
        if (item == null || slotRect == null) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Application.isPlaying) Debug.Log($"[CharacterInfoPanel] 点击道具: {item.itemName}, 描述长度: {(ShopItemData.GetItemDescription(item) ?? "").Length}");
#endif

        // 再次点击同一槽位则关闭说明
        if (_itemTooltipRoot != null && _itemTooltipRoot.gameObject.activeSelf && _lastTooltipSlot == slotRect)
        {
            HideItemTooltip();
            _lastTooltipSlot = null;
            return;
        }

        var tooltip = GetOrCreateItemTooltip();
        if (tooltip == null) return;

        string desc = ShopItemData.GetItemDescription(item);
        string displayText = string.IsNullOrEmpty(desc) ? item.itemName : $"{item.itemName}\n{desc}";
        _itemTooltipText.text = displayText;

        // 以面板根（或其父 Canvas）作为 tooltip 父节点，使用世界坐标精确对齐
        RectTransform tooltipParent = GetTooltipParent();
        if (tooltipParent == null) return;
        tooltip.SetParent(tooltipParent, false);
        tooltip.SetAsLastSibling();

        // 一排 4 个：左两格右侧，右两格左侧
        int columnIndex = 0;
        var grid = itemsContainer != null ? itemsContainer.GetComponent<GridLayoutGroup>() : null;
        if (grid != null && itemsContainer != null)
        {
            for (int i = 0; i < itemsContainer.childCount; i++)
            {
                if (itemsContainer.GetChild(i) == slotRect)
                {
                    columnIndex = i % Mathf.Max(1, grid.constraintCount);
                    break;
                }
            }
        }

        int columns = grid != null ? Mathf.Max(1, grid.constraintCount) : 4;
        int half = columns / 2;
        bool showOnRight = columnIndex < half;

        // 槽位四个世界坐标角：0 左下、1 左上、2 右上、3 右下
        Vector3[] corners = new Vector3[4];
        slotRect.GetWorldCorners(corners);

        if (showOnRight)
        {
            // 左两格：描述框左上角对齐到图标右下角
            Vector3 attachWorld = corners[3]; // 右下角
            tooltip.pivot = new Vector2(0f, 1f);
            tooltip.anchorMin = new Vector2(0f, 1f);
            tooltip.anchorMax = new Vector2(0f, 1f);
            tooltip.position = attachWorld;
        }
        else
        {
            // 右两格：描述框右上角对齐到图标左下角
            Vector3 attachWorld = corners[0]; // 左下角
            tooltip.pivot = new Vector2(1f, 1f);
            tooltip.anchorMin = new Vector2(1f, 1f);
            tooltip.anchorMax = new Vector2(1f, 1f);
            tooltip.position = attachWorld;
        }

        _lastTooltipSlot = slotRect;
        tooltip.gameObject.SetActive(true);
    }

    /// <summary>Tooltip 必须挂在 RectTransform 下，否则 UI 布局会错乱、文字可能不显示。</summary>
    private RectTransform GetTooltipParent()
    {
        var rt = transform as RectTransform;
        if (rt != null) return rt;
        return GetComponentInParent<RectTransform>();
    }

    private RectTransform GetOrCreateItemTooltip()
    {
        if (_itemTooltipRoot != null) return _itemTooltipRoot;

        RectTransform parent = GetTooltipParent();
        if (parent == null) return null;

        RectTransform rt;
        Text text;

        if (itemTooltipPrefab != null)
        {
            // 使用自定义预制体
            rt = Instantiate(itemTooltipPrefab, parent);
            text = rt.GetComponentInChildren<Text>(true);
            if (text == null)
            {
                var textTr = rt.Find("Text");
                if (textTr != null) text = textTr.GetComponent<Text>();
            }
        }
        else
        {
            // 默认动态创建样式
            var root = new GameObject("ItemTooltip");
            root.transform.SetParent(parent, false);

            rt = root.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(itemTooltipWidth, 80f);

            // 若父节点是 GridLayoutGroup，忽略布局，避免被限制大小和位置
            var le = root.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            var img = root.AddComponent<Image>();
            img.color = new Color(0.12f, 0.1f, 0.18f, 0.95f);
            img.raycastTarget = false;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(root.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(6f, 6f);
            textRt.offsetMax = new Vector2(-6f, -6f);

            text = textGo.AddComponent<Text>();
            text.fontSize = itemTooltipFontSize;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            GameFont.ApplyTo(text);
        }

        _itemTooltipRoot = rt;
        _itemTooltipText = text;

        rt.gameObject.SetActive(false);
        return rt;
    }

    private void HideItemTooltip()
    {
        if (_itemTooltipRoot != null)
        {
            if (itemsContainer != null)
                _itemTooltipRoot.SetParent(itemsContainer, false);
            _itemTooltipRoot.gameObject.SetActive(false);
        }
        _lastTooltipSlot = null;
    }

    public void HideItemDescriptionPopup()
    {
        HideItemTooltip();
    }

    /// <summary>
    /// 刷新玩家等级显示（格式：Lv.X 当前经验/升级所需，例如 Lv.3 25/45）
    /// </summary>
    private void RefreshPlayerLevel()
    {
        if (playerLevelText == null) return;
        var plm = PlayerLevelManager.Instance;
        if (plm != null)
        {
            int level = plm.CurrentLevel;
            int currentXP = plm.CurrentXP;
            int xpToNext = plm.GetXPToNextLevel();
            playerLevelText.text = $"玩家等级 Lv.{level} {currentXP}/{xpToNext}";
        }
        else
        {
            playerLevelText.text = "玩家等级 Lv.1 0/10";
        }
    }

    /// <summary>
    /// 自动生成属性行（推荐模式）。直接从 CharacterStats 抓取所有角色属性，不依赖 CharacterData 字段顺序。
    /// </summary>
    private void UpdateAttributesDynamic()
    {
        CharacterStats stats = FindObjectOfType<CharacterStats>();
        if (stats == null)
            return;

        // 先按当前背包重新汇总道具加成（护甲/生命恢复/吸血），避免显示为 0
        var itemEffect = stats.GetComponent<ItemEffectManager>();
        if (itemEffect != null)
            itemEffect.RecalculateItemStatBonuses();

        var data = stats.characterData;
        Health health = stats.GetComponent<Health>();

        for (int i = attributesContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(attributesContainer.GetChild(i).gameObject);
        }

        // 固定属性列表：从 CharacterStats 自动抓取，顺序可调
        var entries = new[]
        {
            ("伤害", GetStatDisplay(stats.CurrentDamage, data?.baseDamage ?? 0f)),
            ("生命值", GetHealthDisplay(stats, health)),
            ("生命恢复", GetHealthRegenDisplay(stats)),
            ("护甲", GetArmorDisplay(stats)),
            ("闪避", GetDodgeDisplay(stats)),
            ("生命偷取", GetLifestealDisplay(stats)),
            ("暴击率", GetCritRateDisplay(stats)),
            ("暴击伤害", GetCritDamageDisplay(stats)),
            ("关卡奖励加成", GetRewardBonusDisplay(stats)),
            ("幸运", GetLuckDisplay(stats)),
            ("击退", GetKnockbackDisplay(stats)),
            ("攻击速度", GetStatDisplay(stats.CurrentFireRate, data?.baseFireRate ?? 0f) + " 发/秒"),
            ("移动速度", GetStatDisplay(stats.CurrentMoveSpeed, data?.baseMoveSpeed ?? 0f)),
            ("攻击范围", GetStatDisplay(stats.CurrentAttackRange, data?.baseAttackRange ?? 0f)),
            ("拾取范围", GetStatDisplay(stats.CurrentPickupRange, data?.basePickupRange ?? 0.5f))
        };

        foreach (var (displayName, valueStr) in entries)
        {
            GameObject row = Instantiate(attributeRowPrefab, attributesContainer);
            Text nameText = row.transform.Find("NameText")?.GetComponent<Text>();
            Text valueText = row.transform.Find("ValueText")?.GetComponent<Text>()
                ?? row.transform.Find("VauleText")?.GetComponent<Text>(); // 兼容旧预制体拼写错误

            if (nameText != null)
                nameText.text = displayName;
            if (valueText != null)
                valueText.text = valueStr;
        }
    }

    /// <summary>
    /// 只显示最终数值，不显示基础与加成拆开格式。
    /// </summary>
    private string GetStatDisplay(float current, float baseVal)
    {
        return current.ToString("F1");
    }

    private string GetHealthDisplay(CharacterStats stats, Health health)
    {
        float max = stats.CurrentMaxHealth;
        float current = health != null ? health.CurrentHealth : max;
        return $"{current:F0}/{max:F0}";
    }

    private string GetHealthRegenDisplay(CharacterStats stats)
    {
        if (stats == null) return "0";
        float percent = Mathf.Max(0f, stats.CurrentHealthRegenPercent * 100f);
        if (percent <= 0.001f)
            return "0";

        float perSec = stats.CurrentMaxHealth * stats.CurrentHealthRegenPercent;
        return $"{perSec:F1}/秒 ({percent:F1}%)";
    }

    private string GetLifestealDisplay(CharacterStats stats)
    {
        if (stats == null) return "0%";
        float percent = Mathf.Max(0f, stats.CurrentLifestealPercent * 100f);
        if (percent <= 0.001f)
            return "0%";
        return $"{percent:F1}%";
    }

    private string GetArmorDisplay(CharacterStats stats)
    {
        if (stats == null) return "0%";
        float percent = Mathf.Max(0f, stats.CurrentArmorPercent * 100f);
        if (percent <= 0.001f)
            return "0%";
        return $"{percent:F1}%";
    }

    private string GetDodgeDisplay(CharacterStats stats)
    {
        if (stats == null) return "0%";
        float percent = Mathf.Max(0f, stats.CurrentDodgePercent * 100f);
        if (percent <= 0.001f) return "0%";
        return $"{percent:F1}%";
    }

    private string GetRewardBonusDisplay(CharacterStats stats)
    {
        if (stats == null) return "0";
        return stats.CurrentRewardBonus > 0 ? $"+{stats.CurrentRewardBonus}" : "0";
    }

    private string GetCritRateDisplay(CharacterStats stats)
    {
        if (stats == null) return "0%";
        float percent = Mathf.Max(0f, stats.CurrentCritRatePercent * 100f);
        if (percent <= 0.001f) return "0%";
        return $"{percent:F1}%";
    }

    private string GetCritDamageDisplay(CharacterStats stats)
    {
        if (stats == null) return "120%";
        float mult = stats.CurrentCritDamageMultiplier;
        return $"{(mult * 100f):F0}%";
    }

    private string GetLuckDisplay(CharacterStats stats)
    {
        if (stats == null) return "0";
        return stats.CurrentLuck.ToString("F1");
    }

    private string GetKnockbackDisplay(CharacterStats stats)
    {
        if (stats == null) return "0";
        if (stats.CurrentKnockback <= 0.001f) return "0";
        return stats.CurrentKnockback.ToString("F1");
    }

    protected override void OnShow()
    {
        base.OnShow();

        if (transform.parent != null)
        {
            transform.SetAsLastSibling();
        }

        UpdateAttributes();
        RefreshOwnedItems();

        if (GameplayPauseManager.Instance != null)
        {
            GameplayPauseManager.Instance.RequestPause("CharacterInfoPanel");
        }
    }

    protected override void OnHide()
    {
        base.OnHide();
        HideItemTooltip();

        if (GameplayPauseManager.Instance != null)
        {
            GameplayPauseManager.Instance.RequestResume("CharacterInfoPanel");
        }
    }
}
