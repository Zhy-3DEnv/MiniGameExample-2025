using UnityEngine;
using UnityEngine.UI;
using EggRogue;
using System.Collections.Generic;

/// <summary>
/// 商店内显示的已购买物品栏，同类道具显示数量。
/// </summary>
[ExecuteAlways]
public class ShopItemInventoryDisplay : MonoBehaviour
{
    [Tooltip("物品槽容器（HorizontalLayoutGroup）")]
    public Transform container;

    [Header("滚动显示（可选）")]
    [Tooltip("如需上下滚动预览，请指定 ScrollRect")]
    public ScrollRect scrollRect;

    [Tooltip("自动设置 ScrollRect 的 Content/滚动方向")]
    public bool autoSetupScroll = true;

    [Tooltip("自动为容器添加布局（Grid + ContentSizeFitter）")]
    public bool autoSetupLayout = true;

    [Tooltip("使用网格布局展示道具")]
    public bool useGridLayout = true;

    [Tooltip("网格列数")]
    public int gridColumnCount = 5;

    [Tooltip("网格单元大小（勾选「覆盖」时生效，否则保留容器上已有的 GridLayoutGroup.cellSize）")]
    public Vector2 gridCellSize = new Vector2(44, 44);

    [Tooltip("是否用上方 gridCellSize 覆盖容器的 cellSize；不勾选则保留编辑器/预制体中的设置")]
    public bool overrideCellSize = false;

    [Tooltip("网格间距")]
    public Vector2 gridSpacing = new Vector2(6, 6);

    [Header("道具说明（点击槽位显示）")]
    [Tooltip("说明框宽度（像素），仅在未指定预制体时生效")]
    public float itemTooltipWidth = 200f;
    [Tooltip("说明文字字号，仅在未指定预制体时生效")]
    public int itemTooltipFontSize = 12;
    [Tooltip("道具说明提示预制体（可选；留空则使用默认样式）")]
    public RectTransform itemTooltipPrefab;

    private readonly List<GameObject> _slotInstances = new List<GameObject>();
    private RectTransform _itemTooltipRoot;
    private Text _itemTooltipText;
    private RectTransform _lastTooltipSlot;

    private void OnEnable()
    {
        EnsureLayoutAndScroll();
        if (ItemInventoryManager.Instance != null)
            ItemInventoryManager.Instance.OnItemsChanged += Refresh;
    }

    private void OnDisable()
    {
        if (ItemInventoryManager.Instance != null)
            ItemInventoryManager.Instance.OnItemsChanged -= Refresh;
    }

    private void OnValidate()
    {
        EnsureLayoutAndScroll();
    }

    public void Refresh()
    {
        if (container == null) return;

        // 如果已有 tooltip 实例，先安全销毁，避免残留多个提示框
        if (_itemTooltipRoot != null)
        {
            if (_itemTooltipRoot.gameObject != null)
            {
                if (Application.isPlaying)
                    Destroy(_itemTooltipRoot.gameObject);
                else
                    DestroyImmediate(_itemTooltipRoot.gameObject);
            }
            _itemTooltipRoot = null;
            _itemTooltipText = null;
            _lastTooltipSlot = null;
        }

        // 清空容器下所有子物体（包括编辑器放置的预览道具、以及可能存在的 tooltip）
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i);
            if (child == null) continue;
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        _slotInstances.Clear();
        _itemTooltipRoot = null;
        _itemTooltipText = null;
        _lastTooltipSlot = null;

        var inv = ItemInventoryManager.Instance ?? FindObjectOfType<ItemInventoryManager>();
        if (inv == null) return;

        var stacks = inv.GetItemStacks();
        if (stacks == null) return;

        int index = 0;
        foreach (var (item, count) in stacks)
        {
            if (item == null) continue;
            var slot = CreateItemSlot(item, count);
            slot.transform.SetParent(container, false);
            _slotInstances.Add(slot);

            // 为槽位添加点击事件，根据列索引决定提示框在左还是在右
            var slotRt = slot.GetComponent<RectTransform>();
            var bg = slot.GetComponent<Image>();
            var btn = slot.GetComponent<Button>();
            if (btn == null) btn = slot.AddComponent<Button>();
            if (bg != null && btn.targetGraphic != bg) btn.targetGraphic = bg;

            int columnIndex = gridColumnCount > 0 ? (index % gridColumnCount) : index;
            var capturedItem = item;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnItemSlotClicked(capturedItem, slotRt, columnIndex));

            index++;
        }

        // 刷新后固定从顶部显示，避免出现“自动上移”
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    private void EnsureLayoutAndScroll()
    {
        if (container == null) return;

        if (autoSetupLayout)
            EnsureLayout();

        if (autoSetupScroll)
            EnsureScroll();
    }

    private void EnsureLayout()
    {
        if (!useGridLayout) return;

        var grid = container.GetComponent<GridLayoutGroup>();
        if (grid == null)
            grid = container.gameObject.AddComponent<GridLayoutGroup>();

        if (overrideCellSize)
            grid.cellSize = gridCellSize;
        grid.spacing = gridSpacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, gridColumnCount);
        grid.childAlignment = TextAnchor.UpperLeft;

        var hlg = container.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) hlg.enabled = false;
        var vlg = container.GetComponent<VerticalLayoutGroup>();
        if (vlg != null) vlg.enabled = false;

        var fitter = container.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = container.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void EnsureScroll()
    {
        if (scrollRect == null) return;
        if (container == null) return;

        var contentRect = container as RectTransform;
        if (contentRect == null) return;
        if (scrollRect.content != contentRect)
            scrollRect.content = contentRect;

        scrollRect.vertical = true;
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Content 锚点贴顶、向下生长，避免未排满时内容“上移”
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
    }

    private GameObject CreateItemSlot(ItemData item, int count)
    {
        var slot = new GameObject("ItemSlot");
        var rt = slot.AddComponent<RectTransform>();
        var layout = slot.AddComponent<LayoutElement>();
        layout.preferredWidth = 44;
        layout.preferredHeight = 44;
        var bg = slot.AddComponent<Image>();
        bg.color = new Color(0.25f, 0.2f, 0.3f, 0.9f);

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(slot.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;
        var icon = iconGo.AddComponent<Image>();
        icon.sprite = item.icon;
        icon.enabled = item.icon != null;
        icon.raycastTarget = false;

        var countGo = new GameObject("CountText");
        countGo.transform.SetParent(slot.transform, false);
        var countRt = countGo.AddComponent<RectTransform>();
        countRt.anchorMin = new Vector2(0f, 0f);
        countRt.anchorMax = new Vector2(1f, 0.35f);
        countRt.offsetMin = Vector2.zero;
        countRt.offsetMax = Vector2.zero;
        var countT = countGo.AddComponent<Text>();
        countT.text = count.ToString();
        countT.alignment = TextAnchor.LowerRight;
        countT.fontSize = 24;
        countT.color = Color.white;
        GameFont.ApplyTo(countT);

        return slot;
    }

    private void OnItemSlotClicked(ItemData item, RectTransform slotRect, int columnIndex)
    {
        if (item == null || slotRect == null) return;

        // 若已存在其它槽位的 tooltip，先关闭它，保证互斥
        if (_itemTooltipRoot != null && _itemTooltipRoot.gameObject.activeSelf && _lastTooltipSlot != null && _lastTooltipSlot != slotRect)
        {
            HideItemTooltip();
        }

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

        // 以 ScrollRect 的 Viewport 作为 tooltip 父节点，在可见区域内对齐
        RectTransform tooltipParent = null;
        if (scrollRect != null && scrollRect.viewport != null)
            tooltipParent = scrollRect.viewport;
        else
            tooltipParent = container as RectTransform;
        if (tooltipParent == null) return;

        // 先挂到父节点，再用世界坐标精确对齐（position 是以 pivot 为基准的世界坐标）
        tooltip.SetParent(tooltipParent, false);
        tooltip.SetAsLastSibling();

        // 简单列逻辑：左半部分（前 2 列）显示在右侧，右半部分（后 2 列）显示在左侧
        int columns = Mathf.Max(1, gridColumnCount);
        int half = columns / 2;
        bool showOnRight = columnIndex < half;

        // 槽位四个世界坐标角：0 左下、1 左上、2 右上、3 右下
        Vector3[] corners = new Vector3[4];
        slotRect.GetWorldCorners(corners);

        if (showOnRight)
        {
            // 左两列：描述框左上角对齐到图标右下角
            Vector3 attachWorld = corners[3]; // 右下角
            tooltip.pivot = new Vector2(0f, 1f);      // 左上
            tooltip.anchorMin = new Vector2(0f, 1f);
            tooltip.anchorMax = new Vector2(0f, 1f);
            tooltip.position = attachWorld;          // 直接用世界坐标，对齐 pivot
        }
        else
        {
            // 右两列：描述框右上角对齐到图标左下角
            Vector3 attachWorld = corners[0]; // 左下角
            tooltip.pivot = new Vector2(1f, 1f);      // 右上
            tooltip.anchorMin = new Vector2(1f, 1f);
            tooltip.anchorMax = new Vector2(1f, 1f);
            tooltip.position = attachWorld;          // 直接用世界坐标，对齐 pivot
        }

        _lastTooltipSlot = slotRect;
        tooltip.gameObject.SetActive(true);
    }

    private RectTransform GetOrCreateItemTooltip()
    {
        if (_itemTooltipRoot != null) return _itemTooltipRoot;

        RectTransform parent = container as RectTransform;
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
            if (container != null)
                _itemTooltipRoot.SetParent(container, false);
            _itemTooltipRoot.gameObject.SetActive(false);
        }
        _lastTooltipSlot = null;
    }
}
