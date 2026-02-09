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

    private RectTransform _itemTooltipRoot;
    private Text _itemTooltipText;
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
        if (!Application.isPlaying && gameObject.activeInHierarchy)
            UpdateAttributes();
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
    /// 更新属性显示
    /// </summary>
    private void UpdateAttributes()
    {
        if (attributesContainer != null && attributeRowPrefab != null)
            UpdateAttributesDynamic();

        RefreshPlayerLevel();
        RefreshOwnedItems();
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

        if (_itemTooltipRoot != null && _itemTooltipRoot.gameObject.activeSelf && _itemTooltipRoot.parent == slotRect)
        {
            HideItemTooltip();
            return;
        }

        HideItemTooltip();

        var tooltip = GetOrCreateItemTooltip();
        if (tooltip == null) return;

        string desc = ShopItemData.GetItemDescription(item);
        _itemTooltipText.text = string.IsNullOrEmpty(desc) ? item.itemName : $"{item.itemName}\n{desc}";

        tooltip.SetParent(slotRect, false);
        tooltip.SetAsLastSibling();
        tooltip.anchorMin = new Vector2(1f, 0.5f);
        tooltip.anchorMax = new Vector2(1f, 0.5f);
        tooltip.pivot = new Vector2(0f, 0.5f);
        tooltip.anchoredPosition = new Vector2(8f, 0f);

        tooltip.gameObject.SetActive(true);
    }

    private RectTransform GetOrCreateItemTooltip()
    {
        if (_itemTooltipRoot != null) return _itemTooltipRoot;

        if (itemsContainer == null) return null;

        var root = new GameObject("ItemTooltip");
        root.transform.SetParent(transform, false);

        var rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(itemTooltipWidth, 80f);

        var img = root.AddComponent<Image>();
        img.color = new Color(0.12f, 0.1f, 0.18f, 0.95f);

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(root.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(6f, 6f);
        textRt.offsetMax = new Vector2(-6f, -6f);

        var text = textGo.AddComponent<Text>();
        text.fontSize = itemTooltipFontSize;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.alignment = TextAnchor.UpperLeft;
        text.color = Color.white;
        if (GameFont.GetDefault() != null) text.font = GameFont.GetDefault();

        _itemTooltipRoot = rt;
        _itemTooltipText = text;

        root.SetActive(false);
        return rt;
    }

    private void HideItemTooltip()
    {
        if (_itemTooltipRoot != null)
        {
            _itemTooltipRoot.SetParent(transform, false);
            _itemTooltipRoot.gameObject.SetActive(false);
        }
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
            ("攻击速度", GetStatDisplay(stats.CurrentFireRate, data?.baseFireRate ?? 0f) + " 发/秒"),
            ("移动速度", GetStatDisplay(stats.CurrentMoveSpeed, data?.baseMoveSpeed ?? 0f)),
            ("子弹速度", GetStatDisplay(stats.CurrentBulletSpeed, data?.baseBulletSpeed ?? 0f)),
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

    private string GetStatDisplay(float current, float baseVal)
    {
        if (Mathf.Approximately(current, baseVal))
            return current.ToString("F1");
        float bonus = current - baseVal;
        string sign = bonus >= 0 ? "+" : "";
        return $"{current:F1} ({sign}{bonus:F1})";
    }

    private string GetHealthDisplay(CharacterStats stats, Health health)
    {
        float max = stats.CurrentMaxHealth;
        float current = health != null ? health.CurrentHealth : max;
        return $"{current:F0}/{max:F0}";
    }

    protected override void OnShow()
    {
        base.OnShow();

        if (transform.parent != null)
        {
            transform.SetAsLastSibling();
        }

        UpdateAttributes();

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
