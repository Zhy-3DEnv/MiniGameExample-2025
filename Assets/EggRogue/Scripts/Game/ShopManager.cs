using UnityEngine;
using System.Collections.Generic;

namespace EggRogue
{
    /// <summary>
    /// 商店管理器 - 管理商店刷新、购买、出售、随机重刷等。
    /// 常驻 PersistentScene。
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        private static ShopManager _instance;
        public static ShopManager Instance => _instance;

        [Header("配置")]
        [Tooltip("武器数据库")]
        public WeaponDatabase weaponDatabase;

        [Tooltip("道具数据库")]
        public ItemDatabase itemDatabase;

        [Tooltip("商品数量")]
        public int shopSlotCount = 5;

        [Tooltip("随机重刷基础价格")]
        public int rerollBasePrice = 10;

        [Tooltip("每次重刷价格递增")]
        public int rerollPriceIncrement = 10;

        private readonly ShopItemData[] _currentItems = new ShopItemData[5];
        private readonly bool[] _lockedSlots = new bool[5];
        private int _rerollCount;
        private readonly List<ShopItemData> _persistedItemsForNextShop = new List<ShopItemData>();

        /// <summary>当前商品列表（只读）</summary>
        public IReadOnlyList<ShopItemData> CurrentItems => _currentItems;

        /// <summary>当前随机重刷价格</summary>
        public int CurrentRerollPrice => rerollBasePrice + _rerollCount * rerollPriceIncrement;

        private void Awake()
        {
            GameObject rootGO = transform.root != null ? transform.root.gameObject : gameObject;
            if (_instance != null && _instance != this)
            {
                Destroy(rootGO);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(rootGO);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        /// <summary>
        /// 进入新商店时调用（ShopPanel.OnShow），重置随机价格与锁定
        /// </summary>
        public void ResetRerollCountForNewShop()
        {
            _rerollCount = 0;
            for (int i = 0; i < _lockedSlots.Length; i++)
                _lockedSlots[i] = false;
        }

        /// <summary>切换指定槽位锁定状态（锁定后随机时不会替换）</summary>
        public void ToggleLock(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _lockedSlots.Length) return;
            if (_currentItems[slotIndex] == null) return;
            _lockedSlots[slotIndex] = !_lockedSlots[slotIndex];
        }

        /// <summary>指定槽位是否已锁定</summary>
        public bool IsLocked(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _lockedSlots.Length) return false;
            return _lockedSlots[slotIndex];
        }

        /// <summary>
        /// 离开商店时调用，将本轮锁定的商品保存到下一关商店（下次进入时展示但已解锁）
        /// </summary>
        public void SaveLockedItemsForNextShop()
        {
            var toSave = new List<(int slotIndex, ShopItemData item)>();
            for (int i = 0; i < _currentItems.Length; i++)
            {
                if (_lockedSlots[i] && _currentItems[i] != null)
                {
                    toSave.Add((i, _currentItems[i]));
                    Debug.Log($"[ShopManager] 离开商店时保存锁定: 槽位{i} -> {_currentItems[i].DisplayName} ({_currentItems[i].ItemType})");
                }
            }
            if (toSave.Count > 0)
                Debug.Log($"[ShopManager] 共保存 {toSave.Count} 个锁定商品到下一关");
            _persistedItemsForNextShop.Clear();
            foreach (var (slot, item) in toSave)
                _persistedItemsForNextShop.Add(item);
            PlayerRunState.Instance?.SetPersistedShopItems(toSave);
        }

        /// <summary>
        /// 刷新商店商品。每个槽位随机武器或道具，位置随机。
        /// </summary>
        /// <param name="keepLockedSlots">true=保留已锁定槽位内容；false=全部刷新（新商店、空商店免费刷新）</param>
        public void RefreshShop(bool keepLockedSlots = false)
        {
            int total = Mathf.Min(shopSlotCount, _currentItems.Length);
            var slotsToFill = new List<int>();

            for (int i = 0; i < total; i++)
            {
                if (keepLockedSlots && _lockedSlots[i] && _currentItems[i] != null)
                    continue;
                slotsToFill.Add(i);
                _currentItems[i] = null;
            }

            var fromRunState = PlayerRunState.Instance?.GetAndClearPersistedShopItems();
            var itemsToRestore = (fromRunState != null && fromRunState.Count > 0)
                ? fromRunState
                : BuildPersistedFromLegacy();
            if (itemsToRestore.Count > 0)
                Debug.Log($"[ShopManager] RefreshShop: 待恢复上轮锁定商品数={itemsToRestore.Count}, keepLockedSlots={keepLockedSlots}");
            if (!keepLockedSlots && itemsToRestore.Count > 0)
            {
                foreach (var (slotIndex, item) in itemsToRestore)
                {
                    if (slotIndex >= 0 && slotIndex < _currentItems.Length)
                    {
                        _currentItems[slotIndex] = item;
                        slotsToFill.Remove(slotIndex);
                        Debug.Log($"[ShopManager] 下次进入商店恢复锁定商品: 槽位{slotIndex} <- {item.DisplayName}");
                    }
                }
                _persistedItemsForNextShop.Clear();
                Debug.Log($"[ShopManager] 共恢复 {itemsToRestore.Count} 个上轮锁定商品（已解锁，保持原槽位）");
            }

            Shuffle(slotsToFill);

            bool hasWeapons = weaponDatabase != null && weaponDatabase.weapons != null && weaponDatabase.weapons.Length > 0;
            bool hasItems = itemDatabase != null && itemDatabase.items != null && itemDatabase.items.Length > 0;

            foreach (int idx in slotsToFill)
            {
                bool pickWeapon = (!hasItems || (hasWeapons && Random.value < 0.5f));
                if (pickWeapon && hasWeapons)
                {
                    var w = weaponDatabase.GetRandomWeapons(1);
                    if (w != null && w.Length > 0 && w[0] != null)
                        _currentItems[idx] = ShopItemData.CreateWeapon(w[0], w[0].basePrice);
                }
                else if (hasItems)
                {
                    var it = itemDatabase.GetRandomItems(1);
                    if (it != null && it.Length > 0 && it[0] != null)
                        _currentItems[idx] = ShopItemData.CreateItem(it[0], it[0].basePrice);
                }
                else if (hasWeapons)
                {
                    var w = weaponDatabase.GetRandomWeapons(1);
                    if (w != null && w.Length > 0 && w[0] != null)
                        _currentItems[idx] = ShopItemData.CreateWeapon(w[0], w[0].basePrice);
                }
            }
        }

        private List<(int slotIndex, ShopItemData item)> BuildPersistedFromLegacy()
        {
            var result = new List<(int, ShopItemData)>();
            for (int i = 0; i < _persistedItemsForNextShop.Count; i++)
                result.Add((i, _persistedItemsForNextShop[i]));
            return result;
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var t = list[i];
                list[i] = list[j];
                list[j] = t;
            }
        }

        private bool IsShopEmpty()
        {
            for (int i = 0; i < _currentItems.Length; i++)
            {
                if (_currentItems[i] != null) return false;
            }
            return true;
        }

        /// <summary>清除本次商店中所有槽位的锁定状态（不影响下轮商店的跨关保存）</summary>
        private void ClearAllLocksForCurrentShop()
        {
            for (int i = 0; i < _lockedSlots.Length; i++)
                _lockedSlots[i] = false;
        }

        /// <summary>
        /// 随机重刷（花费金币），已锁定槽位保留
        /// </summary>
        public bool TryReroll()
        {
            // 先清理“空槽但仍标记为锁定”的情况：
            // 例如：玩家锁定后购买该物品，槽位已为空，再次手动随机时，
            // 新刷出的物品不应继承之前的锁定状态。
            for (int i = 0; i < _lockedSlots.Length; i++)
            {
                if (_lockedSlots[i] && _currentItems[i] == null)
                    _lockedSlots[i] = false;
            }

            int price = CurrentRerollPrice;
            if (GoldManager.Instance == null || !GoldManager.Instance.SpendGold(price))
                return false;

            _rerollCount++;
            RefreshShop(keepLockedSlots: true);
            return true;
        }

        public enum ShopPurchaseFailReason
        {
            None,
            InvalidItem,
            NotEnoughGold,
            WeaponSlotsFull,
            InventoryMissing,
            ItemInventoryFull
        }

        public ShopPurchaseFailReason LastFailReason { get; private set; }

        /// <summary>
        /// 购买武器商品。优先填满空格，无空格时才尝试与同等级武器合并。
        /// </summary>
        public bool TryPurchaseWeapon(int slotIndex)
        {
            LastFailReason = ShopPurchaseFailReason.None;
            if (slotIndex < 0 || slotIndex >= _currentItems.Length)
            {
                LastFailReason = ShopPurchaseFailReason.InvalidItem;
                return false;
            }

            var item = _currentItems[slotIndex];
            if (item == null || item.ItemType != ShopItemType.Weapon || item.WeaponData == null)
            {
                LastFailReason = ShopPurchaseFailReason.InvalidItem;
                return false;
            }

            var inv = WeaponInventoryManager.Instance;
            if (inv == null)
            {
                LastFailReason = ShopPurchaseFailReason.InventoryMissing;
                return false;
            }

            int price = item.Price;
            if (GoldManager.Instance == null || !GoldManager.Instance.SpendGold(price))
            {
                LastFailReason = ShopPurchaseFailReason.NotEnoughGold;
                return false;
            }

            if (inv.HasEmptySlot)
            {
                inv.TryAddWeapon(item.WeaponData);
                _currentItems[slotIndex] = null;
                if (IsShopEmpty())
                {
                    ClearAllLocksForCurrentShop();
                    RefreshShop(keepLockedSlots: false);
                }
                return true;
            }

            int mergeSlot = FindMergeTargetSlot(inv, item.WeaponData);
            if (mergeSlot >= 0)
            {
                inv.SetWeaponAt(mergeSlot, item.WeaponData.nextLevelWeapon);
                _currentItems[slotIndex] = null;
                if (IsShopEmpty())
                {
                    ClearAllLocksForCurrentShop();
                    RefreshShop(keepLockedSlots: false);
                }
                return true;
            }

            // 无空槽且无可合并同等级武器，退还金币
            GoldManager.Instance.AddGold(price);
            LastFailReason = ShopPurchaseFailReason.WeaponSlotsFull;
            return false;
        }

        /// <summary>
        /// 出售武器槽位上的武器，获得 80% 基础价。
        /// </summary>
        public bool TrySellWeapon(int weaponSlotIndex)
        {
            var inv = WeaponInventoryManager.Instance;
            if (inv == null || weaponSlotIndex < 0 || weaponSlotIndex >= WeaponInventoryManager.MaxSlots)
                return false;

            var weapon = inv.RemoveWeaponAt(weaponSlotIndex);
            if (weapon == null) return false;

            int sellPrice = ShopItemData.CreateWeapon(weapon, weapon.basePrice).GetSellPrice();
            if (sellPrice > 0 && GoldManager.Instance != null)
                GoldManager.Instance.AddGold(sellPrice);
            return true;
        }

        /// <summary>
        /// 合并武器：将 fromSlot 拖到 toSlot，两把同等级同类型武器合并为高一级。
        /// </summary>
        public bool TryMergeWeapons(int fromSlot, int toSlot)
        {
            var inv = WeaponInventoryManager.Instance;
            if (inv == null || fromSlot == toSlot) return false;
            if (fromSlot < 0 || fromSlot >= WeaponInventoryManager.MaxSlots) return false;
            if (toSlot < 0 || toSlot >= WeaponInventoryManager.MaxSlots) return false;

            var wFrom = inv.GetWeaponAt(fromSlot);
            var wTo = inv.GetWeaponAt(toSlot);
            if (wFrom == null || wTo == null) return false;
            if (wFrom != wTo || wFrom.nextLevelWeapon == null) return false;

            inv.SetWeaponAt(fromSlot, null);
            inv.SetWeaponAt(toSlot, wFrom.nextLevelWeapon);
            return true;
        }

        /// <summary>
        /// 购买道具商品
        /// </summary>
        public bool TryPurchaseItem(int slotIndex)
        {
            LastFailReason = ShopPurchaseFailReason.None;
            if (slotIndex < 0 || slotIndex >= _currentItems.Length)
            {
                LastFailReason = ShopPurchaseFailReason.InvalidItem;
                return false;
            }

            var shopItem = _currentItems[slotIndex];
            if (shopItem == null || shopItem.ItemType != ShopItemType.Item || shopItem.ItemData == null)
            {
                LastFailReason = ShopPurchaseFailReason.InvalidItem;
                return false;
            }

            if (GoldManager.Instance == null)
            {
                LastFailReason = ShopPurchaseFailReason.NotEnoughGold;
                return false;
            }

            int price = shopItem.Price;
            // 先尝试扣款，失败则直接返回（金币不足）
            if (!GoldManager.Instance.SpendGold(price))
            {
                LastFailReason = ShopPurchaseFailReason.NotEnoughGold;
                return false;
            }

            var inv = ItemInventoryManager.Instance;
            if (inv == null)
            {
                // 兜底：若没有道具库存管理器，退还金币并报错
                GoldManager.Instance.AddGold(price);
                Debug.LogError("[ShopManager] TryPurchaseItem 失败：场景中没有 ItemInventoryManager 实例。");
                LastFailReason = ShopPurchaseFailReason.InventoryMissing;
                return false;
            }

            if (!inv.TryAddItem(shopItem.ItemData))
            {
                // 背包已满等情况：退还金币
                GoldManager.Instance.AddGold(price);
                Debug.LogWarning("[ShopManager] TryPurchaseItem 失败：物品栏已满或道具无效，已退还金币。");
                LastFailReason = ShopPurchaseFailReason.ItemInventoryFull;
                return false;
            }

            _currentItems[slotIndex] = null;
            if (IsShopEmpty())
            {
                ClearAllLocksForCurrentShop();
                RefreshShop(keepLockedSlots: false);
            }
            return true;
        }

        /// <summary>
        /// 尝试购买商品（自动判断武器/道具）
        /// </summary>
        public bool TryPurchase(int slotIndex)
        {
            LastFailReason = ShopPurchaseFailReason.None;
            var item = slotIndex >= 0 && slotIndex < _currentItems.Length ? _currentItems[slotIndex] : null;
            if (item == null)
            {
                LastFailReason = ShopPurchaseFailReason.InvalidItem;
                return false;
            }
            if (item.ItemType == ShopItemType.Weapon)
                return TryPurchaseWeapon(slotIndex);
            return TryPurchaseItem(slotIndex);
        }

        private int FindMergeTargetSlot(WeaponInventoryManager inv, WeaponData incoming)
        {
            if (inv == null || incoming == null || incoming.nextLevelWeapon == null)
                return -1;
            for (int i = 0; i < WeaponInventoryManager.MaxSlots; i++)
            {
                var w = inv.GetWeaponAt(i);
                if (w == incoming)
                    return i;
            }
            return -1;
        }
    }
}
