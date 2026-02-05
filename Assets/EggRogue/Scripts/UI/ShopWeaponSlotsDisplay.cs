using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using EggRogue;

/// <summary>
/// 商店内显示的 6 个武器槽，支持点击出售、拖拽合并。
/// </summary>
public class ShopWeaponSlotsDisplay : MonoBehaviour
{
    [System.Serializable]
    public struct WeaponSlotView
    {
        public RectTransform root;
        public Image icon;
        public Text nameText;
        public Text levelText;
        public Button sellButton;
    }

    public WeaponSlotView[] slots = new WeaponSlotView[6];

    [Header("自动生成（可选）")]
    [Tooltip("武器槽容器（默认使用自身 Transform）")]
    public Transform slotsContainer;

    [Tooltip("武器槽预制体，如 Shop-WeaponSlot.prefab")]
    public GameObject weaponSlotPrefab;

    [Tooltip("自动生成的槽位数量")]
    public int autoSlotCount = 6;

    private int _draggingSlot = -1;
    private Canvas _rootCanvas;
    private Canvas _dragCanvas;
    private RectTransform _dragIcon;
    private Image _dragIconImage;
    private Image _dragSourceImage;
    private Color _dragSourceOriginalColor = Color.white;

    private void Awake()
    {
        TryAutoBuildWeaponSlots();
    }

    private void Start()
    {
        _rootCanvas = GetComponentInParent<Canvas>();

        for (int i = 0; i < slots.Length; i++)
        {
            int idx = i;
            if (slots[i].sellButton != null)
            {
                slots[i].sellButton.onClick.RemoveAllListeners();
                slots[i].sellButton.onClick.AddListener(() => OnSellClicked(idx));
            }
            SetupDragDrop(i);
        }
    }

    private void SetupDragDrop(int slotIndex)
    {
        if (slotIndex >= slots.Length || slots[slotIndex].root == null) return;
        var trigger = slots[slotIndex].root.GetComponent<EventTrigger>();
        if (trigger == null) trigger = slots[slotIndex].root.gameObject.AddComponent<EventTrigger>();

        if (trigger.triggers == null)
            trigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();

        var beginDrag = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
        beginDrag.callback.AddListener(evt => OnBeginDrag(slotIndex, evt as PointerEventData));
        trigger.triggers.Add(beginDrag);

        var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        drag.callback.AddListener(evt => OnDrag(slotIndex, evt as PointerEventData));
        trigger.triggers.Add(drag);

        var endDrag = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
        endDrag.callback.AddListener(evt => OnEndDrag(slotIndex, evt as PointerEventData));
        trigger.triggers.Add(endDrag);

        var drop = new EventTrigger.Entry { eventID = EventTriggerType.Drop };
        drop.callback.AddListener(_ => OnDrop(slotIndex));
        trigger.triggers.Add(drop);
    }

    private void OnBeginDrag(int slotIndex, PointerEventData eventData)
    {
        if (WeaponInventoryManager.Instance?.GetWeaponAt(slotIndex) == null) return;
        _draggingSlot = slotIndex;

        if (eventData == null) return;

        EnsureDragIcon();
        if (_dragCanvas != null) _dragCanvas.gameObject.SetActive(true);
        if (_dragIcon != null) _dragIcon.SetAsLastSibling();
        var slotIcon = slots[slotIndex].icon;
        if (slotIcon != null)
        {
            _dragSourceImage = slotIcon;
            _dragSourceOriginalColor = slotIcon.color;
            slotIcon.color = new Color(_dragSourceOriginalColor.r, _dragSourceOriginalColor.g, _dragSourceOriginalColor.b, 0.35f);
        }

        if (_dragIconImage != null)
        {
            _dragIconImage.sprite = slotIcon != null ? slotIcon.sprite : null;
            _dragIconImage.color = Color.white;
            _dragIcon.gameObject.SetActive(true);
            if (slotIcon != null)
            {
                var rect = slotIcon.rectTransform.rect;
                _dragIcon.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rect.width);
                _dragIcon.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rect.height);
            }
            _dragIconImage.SetNativeSize();
            UpdateDragIconPosition(eventData);
        }
    }

    private void OnDrag(int slotIndex, PointerEventData eventData)
    {
        if (_draggingSlot < 0 || eventData == null || _dragIcon == null || !_dragIcon.gameObject.activeSelf) return;
        UpdateDragIconPosition(eventData);
    }

    private void OnEndDrag(int slotIndex, PointerEventData eventData)
    {
        if (_dragIcon != null)
            _dragIcon.gameObject.SetActive(false);
        if (_dragCanvas != null)
            _dragCanvas.gameObject.SetActive(false);
        if (_dragSourceImage != null)
        {
            _dragSourceImage.color = _dragSourceOriginalColor;
            _dragSourceImage = null;
        }
        _draggingSlot = -1;
    }

    private void OnDrop(int targetSlot)
    {
        if (_draggingSlot < 0) return;
        if (_draggingSlot == targetSlot)
        {
            _draggingSlot = -1;
            return;
        }

        bool handled = false;
        var inv = WeaponInventoryManager.Instance;
        if (inv != null)
        {
            var fromWeapon = inv.GetWeaponAt(_draggingSlot);
            var toWeapon = inv.GetWeaponAt(targetSlot);
            if (fromWeapon != null && toWeapon == null)
            {
                inv.SetWeaponAt(_draggingSlot, null);
                inv.SetWeaponAt(targetSlot, fromWeapon);
                handled = true;
            }
        }

        if (!handled && ShopManager.Instance != null && ShopManager.Instance.TryMergeWeapons(_draggingSlot, targetSlot))
        {
            handled = true;
        }

        if (handled)
        {
            Refresh();
            var panel = GetComponentInParent<ShopPanel>();
            if (panel != null)
                panel.RefreshAll();
        }
        _draggingSlot = -1;
        if (_dragIcon != null)
            _dragIcon.gameObject.SetActive(false);
        if (_dragCanvas != null)
            _dragCanvas.gameObject.SetActive(false);
        if (_dragSourceImage != null)
        {
            _dragSourceImage.color = _dragSourceOriginalColor;
            _dragSourceImage = null;
        }
    }

    private void OnSellClicked(int slotIndex)
    {
        if (ShopManager.Instance == null) return;
        if (ShopManager.Instance.TrySellWeapon(slotIndex))
        {
            Refresh();
            var panel = GetComponentInParent<ShopPanel>();
            if (panel != null) panel.RefreshAll();
        }
    }

    public void Refresh()
    {
        var inv = WeaponInventoryManager.Instance;
        for (int i = 0; i < slots.Length && i < 6; i++)
        {
            var slot = slots[i];
            var weapon = inv != null ? inv.GetWeaponAt(i) : null;

            if (weapon == null)
            {
                if (slot.icon != null) { slot.icon.enabled = false; slot.icon.sprite = null; }
                if (slot.nameText != null) slot.nameText.text = "空";
                if (slot.levelText != null) slot.levelText.text = "";
                if (slot.sellButton != null) slot.sellButton.gameObject.SetActive(false);
            }
            else
            {
                if (slot.icon != null) { slot.icon.sprite = weapon.icon; slot.icon.enabled = weapon.icon != null; }
                if (slot.nameText != null) slot.nameText.text = weapon.weaponName;
                if (slot.levelText != null) slot.levelText.text = $"Lv.{weapon.level}";
                if (slot.sellButton != null)
                {
                    slot.sellButton.gameObject.SetActive(true);
                    int sellPrice = ShopItemData.CreateWeapon(weapon, weapon.basePrice).GetSellPrice();
                    var txt = slot.sellButton.GetComponentInChildren<Text>();
                    if (txt != null) txt.text = $"出售({sellPrice})";
                }
            }
        }
    }

    private void TryAutoBuildWeaponSlots()
    {
        if (weaponSlotPrefab == null) return;

        var container = slotsContainer != null ? slotsContainer : transform;
        if (container == null) return;

        // 运行时：清空容器下原本手动摆放的子物体（仅用于编辑预览），再用预制体生成
        if (Application.isPlaying)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                var child = container.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }
        }

        int count = Mathf.Clamp(autoSlotCount, 1, 6);
        slots = new WeaponSlotView[count];

        for (int i = 0; i < count; i++)
        {
            var slotGO = Instantiate(weaponSlotPrefab, container);
            slotGO.name = $"WeaponSlot_{i}";

            var icon = slotGO.transform.Find("Icon")?.GetComponent<Image>();
            var nameText = slotGO.transform.Find("NameText")?.GetComponent<Text>();
            var levelText = slotGO.transform.Find("LevelText")?.GetComponent<Text>();
            var sellButton = slotGO.transform.Find("SellButton")?.GetComponent<Button>();
            var root = slotGO.GetComponent<RectTransform>();
            if (root == null) root = slotGO.AddComponent<RectTransform>();

            slots[i] = new WeaponSlotView
            {
                root = root,
                icon = icon,
                nameText = nameText,
                levelText = levelText,
                sellButton = sellButton
            };
        }
    }

    private void EnsureDragIcon()
    {
        if (_dragIcon != null) return;
        if (_rootCanvas == null) _rootCanvas = GetComponentInParent<Canvas>();
        if (_rootCanvas == null) return;

        var dragCanvasGo = new GameObject("WeaponDragCanvas", typeof(Canvas), typeof(CanvasGroup));
        dragCanvasGo.transform.SetParent(_rootCanvas.transform, false);
        dragCanvasGo.transform.SetAsLastSibling();

        _dragCanvas = dragCanvasGo.GetComponent<Canvas>();
        _dragCanvas.renderMode = _rootCanvas.renderMode;
        _dragCanvas.worldCamera = _rootCanvas.worldCamera;
        _dragCanvas.sortingLayerID = _rootCanvas.sortingLayerID;
        _dragCanvas.sortingOrder = _rootCanvas.sortingOrder + 100;
        _dragCanvas.overrideSorting = true;

        var group = dragCanvasGo.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        _dragIcon = new GameObject("WeaponDragIcon", typeof(RectTransform)).GetComponent<RectTransform>();
        _dragIcon.SetParent(dragCanvasGo.transform, false);
        _dragIcon.pivot = new Vector2(0.5f, 0.5f);

        _dragIconImage = _dragIcon.gameObject.AddComponent<Image>();
        _dragIconImage.raycastTarget = false;
        _dragIconImage.preserveAspect = true;

        dragCanvasGo.SetActive(false);
    }

    private void UpdateDragIconPosition(PointerEventData eventData)
    {
        if (_dragIcon == null) return;
        RectTransform canvasRect = _rootCanvas != null ? _rootCanvas.transform as RectTransform : null;
        Vector2 localPos;
        Camera cam = _rootCanvas != null && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? _rootCanvas.worldCamera : null;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, cam, out localPos))
            _dragIcon.anchoredPosition = localPos;
    }
}
