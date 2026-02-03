using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 武器选择管理器 - 存储玩家在武器选择界面选中的起始武器。
    /// 仅首次进入游戏时使用，之后武器通过商店获得。
    /// 常驻 PersistentScene。
    /// </summary>
    public class WeaponSelectionManager : MonoBehaviour
    {
        private static WeaponSelectionManager _instance;
        public static WeaponSelectionManager Instance => _instance;

        [Header("配置")]
        [Tooltip("武器数据库")]
        public WeaponDatabase weaponDatabase;

        [Tooltip("默认起始武器（未选择时使用）")]
        public WeaponData defaultStarterWeapon;

        /// <summary>
        /// 玩家选择的起始武器（仅首次进入游戏时有效）
        /// </summary>
        public WeaponData SelectedStarterWeapon { get; private set; }

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

            if (SelectedStarterWeapon == null)
                SelectedStarterWeapon = defaultStarterWeapon;
        }

        /// <summary>
        /// 设置起始武器（武器选择界面调用）
        /// </summary>
        public void SetStarterWeapon(WeaponData weapon)
        {
            SelectedStarterWeapon = weapon;
            Debug.Log($"WeaponSelectionManager: 起始武器 - {(weapon != null ? weapon.weaponName : "null")}");
        }

        /// <summary>
        /// 重置为默认（重新开始游戏时）
        /// </summary>
        public void ResetToDefault()
        {
            SelectedStarterWeapon = defaultStarterWeapon;
        }
    }
}
