using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 调试绘制的全局开关，支持 PlayerPrefs 持久化。
    /// 每种调试类型可单独在设置面板中开关。
    /// </summary>
    public static class DebugDrawSettings
    {
        private const string PrefsPrefix = "Settings_DebugDraw_";

        private static bool? _attackRangeCached;
        private static bool? _weaponRangeCached;
        private static bool? _pickupRangeCached;

        /// <summary>是否显示基础攻击范围圈</summary>
        public static bool AttackRangeVisible
        {
            get => _attackRangeCached ?? (PlayerPrefs.GetInt(PrefsPrefix + "AttackRange", 0) != 0);
            set => SetCached(ref _attackRangeCached, PrefsPrefix + "AttackRange", value, nameof(AttackRangeVisible));
        }

        /// <summary>是否显示每把武器射程线</summary>
        public static bool WeaponRangeVisible
        {
            get => _weaponRangeCached ?? (PlayerPrefs.GetInt(PrefsPrefix + "WeaponRange", 0) != 0);
            set => SetCached(ref _weaponRangeCached, PrefsPrefix + "WeaponRange", value, nameof(WeaponRangeVisible));
        }

        /// <summary>是否显示拾取范围圈</summary>
        public static bool PickupRangeVisible
        {
            get => _pickupRangeCached ?? (PlayerPrefs.GetInt(PrefsPrefix + "PickupRange", 0) != 0);
            set => SetCached(ref _pickupRangeCached, PrefsPrefix + "PickupRange", value, nameof(PickupRangeVisible));
        }

        /// <summary>是否有任意调试显示开启</summary>
        public static bool AnyVisible => AttackRangeVisible || WeaponRangeVisible || PickupRangeVisible;

        private static void SetCached(ref bool? cache, string key, bool value, string eventKey)
        {
            if (cache == value) return;
            cache = value;
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
            OnSettingChanged?.Invoke(eventKey, value);
        }

        /// <summary>任一设置变化时触发，参数：设置名、新值</summary>
        public static event System.Action<string, bool> OnSettingChanged;

        /// <summary>兼容旧版：等于 AttackRangeVisible，设置时同时设 AttackRangeVisible</summary>
        [System.Obsolete("请使用 AttackRangeVisible 或各类型单独开关")]
        public static bool Visible
        {
            get => AttackRangeVisible;
            set => AttackRangeVisible = value;
        }

        /// <summary>获取指定类型的开关状态</summary>
        public static bool GetVisible(string key)
        {
            switch (key)
            {
                case nameof(AttackRangeVisible): return AttackRangeVisible;
                case nameof(WeaponRangeVisible): return WeaponRangeVisible;
                case nameof(PickupRangeVisible): return PickupRangeVisible;
                default: return false;
            }
        }

        /// <summary>设置指定类型的开关状态</summary>
        public static void SetVisible(string key, bool value)
        {
            switch (key)
            {
                case nameof(AttackRangeVisible): AttackRangeVisible = value; break;
                case nameof(WeaponRangeVisible): WeaponRangeVisible = value; break;
                case nameof(PickupRangeVisible): PickupRangeVisible = value; break;
            }
        }
    }
}
