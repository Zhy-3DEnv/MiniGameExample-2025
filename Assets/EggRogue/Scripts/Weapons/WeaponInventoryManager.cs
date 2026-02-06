using UnityEngine;
using System.Collections.Generic;

namespace EggRogue
{
    /// <summary>
    /// 武器库存管理器 - 管理角色当前携带的 6 个武器槽。
    /// 常驻 PersistentScene，跨关卡持久化。
    /// </summary>
    public class WeaponInventoryManager : MonoBehaviour
    {
        private static WeaponInventoryManager _instance;
        public static WeaponInventoryManager Instance => _instance;

        public const int MaxSlots = 6;

        [Header("起始武器配置")]
        [Tooltip("首次进入游戏时自动给玩家的起始武器数量（从槽位0开始连续填充），范围 1-6。用于在测试/正式之间快速切换。")]
        [Range(1, MaxSlots)]
        public int starterWeaponCount = 1;

        [Header("槽位填充顺序")]
        [Tooltip("购买/初始化时依次尝试的槽位索引，用于控制左右平衡等布阵。\n" +
                 "例如默认值 {0,3,1,4,2,5} 表示：先填 0，再填 3，然后 1,4,2,5，使左右大致对称。")]
        public int[] fillOrder = new int[] { 0, 3, 1, 4, 2, 5 };

        private readonly WeaponData[] _slots = new WeaponData[MaxSlots];

        /// <summary>武器槽变化时触发</summary>
        public event System.Action OnWeaponsChanged;

        public int SlotCount => MaxSlots;

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

        /// <summary>
        /// 获取指定槽位的武器
        /// </summary>
        public WeaponData GetWeaponAt(int index)
        {
            if (index < 0 || index >= MaxSlots) return null;
            return _slots[index];
        }

        /// <summary>
        /// 按配置的填充顺序遍历槽位索引；若配置非法/不完整，则自动补齐 0..MaxSlots-1。
        /// 用于控制“先填哪些槽位”，从而在角色左右做均衡排布。
        /// </summary>
        private IEnumerable<int> GetSlotIterationOrder()
        {
            bool[] used = new bool[MaxSlots];

            if (fillOrder != null && fillOrder.Length > 0)
            {
                int len = Mathf.Min(fillOrder.Length, MaxSlots);
                for (int i = 0; i < len; i++)
                {
                    int idx = fillOrder[i];
                    if (idx < 0 || idx >= MaxSlots) continue;
                    if (used[idx]) continue;
                    used[idx] = true;
                    yield return idx;
                }
            }

            // 补齐未覆盖的槽位，保证始终遍历到 0..MaxSlots-1
            for (int i = 0; i < MaxSlots; i++)
            {
                if (!used[i])
                    yield return i;
            }
        }

        /// <summary>
        /// 设置指定槽位的武器
        /// </summary>
        public void SetWeaponAt(int index, WeaponData weapon)
        {
            if (index < 0 || index >= MaxSlots) return;
            _slots[index] = weapon;
            OnWeaponsChanged?.Invoke();
        }

        /// <summary>当前是否没有任何武器</summary>
        public bool IsCompletelyEmpty
        {
            get
            {
                for (int i = 0; i < MaxSlots; i++)
                    if (_slots[i] != null) return false;
                return true;
            }
        }

        /// <summary>是否有空槽</summary>
        public bool HasEmptySlot
        {
            get
            {
                for (int i = 0; i < MaxSlots; i++)
                    if (_slots[i] == null) return true;
                return false;
            }
        }

        /// <summary>是否完全没有任何武器</summary>
        public bool IsEmpty
        {
            get
            {
                for (int i = 0; i < MaxSlots; i++)
                    if (_slots[i] != null) return false;
                return true;
            }
        }

        /// <summary>第一个空槽索引，无空槽返回 -1</summary>
        public int GetFirstEmptySlotIndex()
        {
            foreach (int idx in GetSlotIterationOrder())
            {
                if (_slots[idx] == null) return idx;
            }
            return -1;
        }

        /// <summary>
        /// 添加武器到第一个空槽，返回是否成功
        /// </summary>
        public bool TryAddWeapon(WeaponData weapon)
        {
            if (weapon == null) return false;
            foreach (int idx in GetSlotIterationOrder())
            {
                if (_slots[idx] == null)
                {
                    _slots[idx] = weapon;
                    OnWeaponsChanged?.Invoke();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 移除指定槽位武器，返回被移除的武器
        /// </summary>
        public WeaponData RemoveWeaponAt(int index)
        {
            if (index < 0 || index >= MaxSlots) return null;
            var old = _slots[index];
            _slots[index] = null;
            if (old != null) OnWeaponsChanged?.Invoke();
            return old;
        }

        /// <summary>
        /// 从 WeaponSelectionManager 初始化起始武器（首次进入游戏调用）。
        /// 根据 starterWeaponCount 决定首发武器数量：从槽位 0 开始连续填充。
        /// </summary>
        public void InitializeFromStarterWeapon()
        {
            ClearAll();
            var starter = WeaponSelectionManager.Instance != null ? WeaponSelectionManager.Instance.SelectedStarterWeapon : null;
            if (starter != null)
            {
                int count = Mathf.Clamp(starterWeaponCount, 1, MaxSlots);
                int filled = 0;
                foreach (int idx in GetSlotIterationOrder())
                {
                    _slots[idx] = starter;
                    filled++;
                    if (filled >= count) break;
                }
                OnWeaponsChanged?.Invoke();
            }
        }

        /// <summary>
        /// 清空所有槽位
        /// </summary>
        public void ClearAll()
        {
            for (int i = 0; i < MaxSlots; i++)
                _slots[i] = null;
        }

        /// <summary>
        /// 获取所有非空武器的只读列表
        /// </summary>
        public IReadOnlyList<WeaponData> GetAllWeapons()
        {
            var list = new List<WeaponData>();
            for (int i = 0; i < MaxSlots; i++)
            {
                if (_slots[i] != null)
                    list.Add(_slots[i]);
            }
            return list;
        }
    }
}
