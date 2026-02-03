using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 武器类型：远程（枪）与近战（刀）
    /// </summary>
    public enum WeaponType
    {
        Ranged,  // 枪，发射子弹
        Melee    // 刀，近战挥砍
    }

    /// <summary>
    /// 武器数据（ScriptableObject）。定义单把武器的属性与表现。
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon_Gun_Lv1", menuName = "EggRogue/Weapon Data", order = 5)]
    public class WeaponData : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("唯一标识")]
        public string weaponId = "gun_lv1";

        [Tooltip("显示名称")]
        public string weaponName = "手枪";

        [Tooltip("武器图标")]
        public Sprite icon;

        [Tooltip("武器类型")]
        public WeaponType weaponType = WeaponType.Ranged;

        [Tooltip("等级 1-5，用于合成")]
        public int level = 1;

        [Tooltip("基础售价（商店购买/出售 80%）")]
        public int basePrice = 50;

        [Header("武器属性")]
        [Tooltip("单次伤害")]
        public float damage = 10f;

        [Tooltip("攻击频率（次/秒）")]
        public float fireRate = 2f;

        [Tooltip("攻击范围")]
        public float attackRange = 10f;

        [Tooltip("子弹速度（远程有效）")]
        public float bulletSpeed = 20f;

        [Tooltip("子弹飞行时间（秒），超时自动销毁；远程有效")]
        public float bulletLifeTime = 5f;

        [Header("预制体")]
        [Tooltip("子弹预制体（远程）")]
        public GameObject bulletPrefab;

        [Tooltip("近战挥砍判定预制体（近战，Phase 4 实现）")]
        public GameObject meleeHitPrefab;

        [Tooltip("武器模型预制体")]
        public GameObject modelPrefab;

        [Header("合成（Phase 3）")]
        [Tooltip("2 把同级合成后的下一级武器")]
        public WeaponData nextLevelWeapon;
    }
}
