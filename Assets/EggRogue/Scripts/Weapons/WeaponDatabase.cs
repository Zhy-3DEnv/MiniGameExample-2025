using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 武器数据库（ScriptableObject）。持有所有武器配置，供武器选择、商店等使用。
    /// </summary>
    public class WeaponDatabase : ScriptableObject
    {
        [Tooltip("所有武器")]
        public WeaponData[] weapons;

        /// <summary>
        /// 根据 ID 获取武器
        /// </summary>
        public WeaponData GetWeaponById(string id)
        {
            if (weapons == null) return null;
            foreach (var w in weapons)
            {
                if (w != null && w.weaponId == id)
                    return w;
            }
            return null;
        }

        /// <summary>
        /// 获取索引位置的武器
        /// </summary>
        public WeaponData GetWeaponByIndex(int index)
        {
            if (weapons == null || index < 0 || index >= weapons.Length)
                return null;
            return weapons[index];
        }

        /// <summary>
        /// 获取用于首次武器选择的默认池（枪 + 刀等）
        /// </summary>
        public WeaponData[] GetStarterWeaponPool()
        {
            if (weapons == null || weapons.Length == 0) return new WeaponData[0];
            return weapons;
        }

        /// <summary>
        /// 随机获取若干把武器（用于商店刷新，可重复）
        /// </summary>
        public WeaponData[] GetRandomWeapons(int count)
        {
            if (weapons == null || weapons.Length == 0 || count <= 0)
                return new WeaponData[0];

            var result = new WeaponData[count];
            for (int i = 0; i < count; i++)
            {
                int idx = Random.Range(0, weapons.Length);
                result[i] = weapons[idx];
            }
            return result;
        }
    }
}
