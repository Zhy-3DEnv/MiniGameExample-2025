using UnityEngine;
using System.Collections.Generic;

namespace EggRogue
{
    /// <summary>
    /// 道具库存管理器 - 存储玩家购买的道具。
    /// 常驻 PersistentScene，跨关卡持久化。
    /// </summary>
    public class ItemInventoryManager : MonoBehaviour
    {
        private static ItemInventoryManager _instance;
        public static ItemInventoryManager Instance => _instance;

        public const int MaxSlots = 50;

        private readonly List<ItemData> _items = new List<ItemData>();

        /// <summary>道具变化时触发（用于 UI 刷新）</summary>
        public event System.Action OnItemsChanged;

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

        public bool TryAddItem(ItemData item)
        {
            if (item == null || _items.Count >= MaxSlots) return false;
            _items.Add(item);
            OnItemsChanged?.Invoke();
            return true;
        }

        public void RemoveItem(ItemData item)
        {
            if (_items.Remove(item))
                OnItemsChanged?.Invoke();
        }

        /// <summary>获取道具堆叠列表（同一道具合并显示数量）</summary>
        public List<(ItemData item, int count)> GetItemStacks()
        {
            var dict = new Dictionary<string, (ItemData, int)>();
            foreach (var item in _items)
            {
                if (item == null) continue;
                if (dict.TryGetValue(item.itemId, out var existing))
                    dict[item.itemId] = (existing.Item1, existing.Item2 + 1);
                else
                    dict[item.itemId] = (item, 1);
            }
            var result = new List<(ItemData, int)>();
            foreach (var kv in dict)
                result.Add(kv.Value);
            return result;
        }

        public bool HasItem(ItemEffectType effectType)
        {
            foreach (var item in _items)
            {
                if (item != null && item.effectType == effectType)
                    return true;
            }
            return false;
        }

        public ItemData GetFirstItemOfType(ItemEffectType effectType)
        {
            foreach (var item in _items)
            {
                if (item != null && item.effectType == effectType)
                    return item;
            }
            return null;
        }

        /// <summary>获取指定效果类型的道具数量（用于效果叠加）</summary>
        public int GetItemCount(ItemEffectType effectType)
        {
            int count = 0;
            foreach (var item in _items)
            {
                if (item != null && item.effectType == effectType)
                    count++;
            }
            return count;
        }

        public IReadOnlyList<ItemData> GetAllItems() => _items;

        public int ItemCount => _items.Count;

        public void ClearAll()
        {
            _items.Clear();
            OnItemsChanged?.Invoke();
        }
    }
}
