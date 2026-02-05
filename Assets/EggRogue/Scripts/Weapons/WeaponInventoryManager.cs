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
            for (int i = 0; i < MaxSlots; i++)
                if (_slots[i] == null) return i;
            return -1;
        }

        /// <summary>
        /// 添加武器到第一个空槽，返回是否成功
        /// </summary>
        public bool TryAddWeapon(WeaponData weapon)
        {
            if (weapon == null) return false;
            for (int i = 0; i < MaxSlots; i++)
            {
                if (_slots[i] == null)
                {
                    _slots[i] = weapon;
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
        /// 从 WeaponSelectionManager 初始化起始武器（首次进入游戏调用）
        /// 测试用：填满 6 个槽位为同一把起始武器。
        /// </summary>
        public void InitializeFromStarterWeapon()
        {
            ClearAll();
            var starter = WeaponSelectionManager.Instance != null ? WeaponSelectionManager.Instance.SelectedStarterWeapon : null;
            if (starter != null)
            {
                for (int i = 0; i < MaxSlots; i++)
                    _slots[i] = starter;
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
