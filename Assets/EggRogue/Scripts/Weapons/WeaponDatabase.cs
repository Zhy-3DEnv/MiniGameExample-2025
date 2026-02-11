using UnityEngine;
using System.Collections.Generic;

namespace EggRogue
{
    /// <summary>
    /// 武器数据库（ScriptableObject）。持有所有武器配置，供武器选择、商店等使用。
    /// 商店支持按关卡进度用 weaponLevelWeights 控制各等级武器出现权重。
    /// </summary>
    public class WeaponDatabase : ScriptableObject
    {
        [Tooltip("所有“基础武器种类”（通常只填 Lv1，例如 Gun_Lv1、Knife_Lv1）")]
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
        /// 随机获取若干把武器（用于商店刷新，可重复）。不考虑等级权重。
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

        /// <summary>
        /// 按等级权重随机获取若干把武器（用于商店）。
        /// 设计：
        /// 1）先在 levelWeights[0]~[4] 中随机一个目标等级（1~5，权重为 weaponLevelWeights）；
        /// 2）再从 weapons[] 中随机选一个“基础武器种类”（通常是 Lv1）；
        /// 3）沿着该武器的 nextLevelWeapon 链向上走，直到达到目标等级或链末尾。
        /// 若 levelWeights 为 null 或长度为 0，则退化为 GetRandomWeapons(count)（仅按种类随机，不区分等级）。
        /// luck &gt; 0 时高等级权重提升，更容易刷出高等级武器。
        /// </summary>
        public WeaponData[] GetRandomWeaponsWithLevelWeights(int count, float[] levelWeights, float luck = 0f)
        {
            if (weapons == null || weapons.Length == 0 || count <= 0)
                return new WeaponData[0];

            if (levelWeights == null || levelWeights.Length == 0)
                return GetRandomWeapons(count);

            var result = new WeaponData[count];
            for (int i = 0; i < count; i++)
            {
                // 1. 按 levelWeights（叠加 luck 时高等级权重提升）抽一个目标等级（1~5）
                int targetLevel = 1;
                {
                    float total = 0f;
                    for (int lv = 0; lv < Mathf.Min(5, levelWeights.Length); lv++)
                    {
                        float w = levelWeights[lv];
                        if (luck > 0.0001f)
                            w *= (1f + luck * 0.1f * (lv + 1));
                        if (w > 0f) total += w;
                    }
                    if (total <= 0f)
                    {
                        targetLevel = 1;
                    }
                    else
                    {
                        float r = Random.Range(0f, total);
                        for (int lv = 0; lv < Mathf.Min(5, levelWeights.Length); lv++)
                        {
                            float w = levelWeights[lv];
                            if (luck > 0.0001f)
                                w *= (1f + luck * 0.1f * (lv + 1));
                            if (w <= 0f) continue;
                            r -= w;
                            if (r <= 0f)
                            {
                                targetLevel = lv + 1;
                                break;
                            }
                        }
                    }
                }

                // 2. 从基础武器种类中随机选一个
                WeaponData baseWeapon = weapons[Random.Range(0, weapons.Length)];
                if (baseWeapon == null)
                {
                    result[i] = null;
                    continue;
                }

                // 3. 沿 nextLevelWeapon 链向上走，直到接近目标等级或链末尾
                WeaponData current = baseWeapon;
                int safety = 10; // 防止错误配置导致死循环
                while (safety-- > 0 && current != null && current.level < targetLevel && current.nextLevelWeapon != null)
                {
                    current = current.nextLevelWeapon;
                }

                // 如果链太短（例如只到 Lv3 但目标是 Lv5），就用链的最后一级 current
                result[i] = current ?? baseWeapon;
            }
            return result;
        }
    }
}
