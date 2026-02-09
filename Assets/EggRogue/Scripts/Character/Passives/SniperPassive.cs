using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 狙击手被动 - 提升攻击范围和子弹速度，但降低射速。
    /// </summary>
    [CreateAssetMenu(fileName = "Sniper", menuName = "EggRogue/Passive/Sniper", order = 8)]
    public class SniperPassive : CharacterPassive
    {
        [Header("狙击手设置")]
        [Tooltip("攻击范围提升倍率")]
        public float rangeMultiplier = 1.8f;

        [Tooltip("子弹速度提升倍率")]
        public float bulletSpeedMultiplier = 1.5f;

        [Tooltip("射速降低倍率")]
        public float fireRateMultiplier = 0.6f;

        public override void ModifyStats(CharacterStats stats)
        {
            if (stats == null)
                return;

            stats.CurrentAttackRange *= rangeMultiplier;
            stats.CurrentBulletSpeed *= bulletSpeedMultiplier;
            stats.CurrentFireRate *= fireRateMultiplier;
        }
    }
}
