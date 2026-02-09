using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 生命值转攻击力被动 - 根据当前最大生命值提供攻击力加成。
    /// 例如：比例 0.2（即 1/5），生命 50 → 攻击力 +10
    /// </summary>
    [CreateAssetMenu(fileName = "HealthToDamage", menuName = "EggRogue/Passive/Health To Damage", order = 1)]
    public class HealthToDamagePassive : CharacterPassive
    {
        [Header("转化设置")]
        [Tooltip("生命值转攻击力比例。例如 0.2 = 最大生命值 / 5")]
        public float ratio = 0.2f;

        public override void ModifyStats(CharacterStats stats)
        {
            if (stats == null)
                return;

            // 攻击力 += 最大生命值 × 比例
            float bonus = stats.CurrentMaxHealth * ratio;
            stats.CurrentDamage += bonus;
        }
    }
}
