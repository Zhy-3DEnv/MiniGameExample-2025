using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 狂战士被动 - 提升伤害和攻速，但降低攻击范围。
    /// </summary>
    [CreateAssetMenu(fileName = "Berserker", menuName = "EggRogue/Passive/Berserker", order = 7)]
    public class BerserkerPassive : CharacterPassive
    {
        [Header("狂战士设置")]
        [Tooltip("伤害提升倍率")]
        public float damageMultiplier = 1.5f;

        [Tooltip("攻速提升倍率")]
        public float fireRateMultiplier = 1.3f;

        [Tooltip("攻击范围降低倍率")]
        public float rangeMultiplier = 0.6f;

        public override void ModifyStats(CharacterStats stats)
        {
            if (stats == null)
                return;

            stats.CurrentDamage *= damageMultiplier;
            stats.CurrentFireRate *= fireRateMultiplier;
            stats.CurrentAttackRange *= rangeMultiplier;
        }
    }
}
