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

    private readonly List<GameObject> _slotInstances = new List<GameObject>();

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

        // 清空容器下所有子物体（包括编辑器放置的预览道具）
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

        var inv = ItemInventoryManager.Instance ?? FindObjectOfType<ItemInventoryManager>();
        if (inv == null) return;

        var stacks = inv.GetItemStacks();
        if (stacks == null) return;

        foreach (var (item, count) in stacks)
        {
            if (item == null) continue;
            var slot = CreateItemSlot(item, count);
            slot.transform.SetParent(container, false);
            _slotInstances.Add(slot);
        }
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
        if (contentRect != null && scrollRect.content != contentRect)
            scrollRect.content = contentRect;

        scrollRect.vertical = true;
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
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
}
