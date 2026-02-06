using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 玻璃大炮被动 - 大幅提升伤害，但降低最大生命值。
    /// </summary>
    [CreateAssetMenu(fileName = "GlassCannon", menuName = "EggRogue/Passive/Glass Cannon", order = 5)]
    public class GlassCannonPassive : CharacterPassive
    {
        [Header("玻璃大炮设置")]
        [Tooltip("伤害提升倍率。例如 2.0 = 伤害 × 2")]
        public float damageMultiplier = 2.0f;

        [Tooltip("生命值降低倍率。例如 0.5 = 最大生命值 × 0.5")]
        public float healthMultiplier = 0.5f;

        public override void ModifyStats(CharacterStats stats)
        {
            if (stats == null)
                return;

            // 玻璃大炮设计约定：
            // - 只在“最终伤害结算”时成倍放大（在 CharacterStats.GetBaseAttackDamage 里统一处理）；
            // - 这里仅负责降低最大生命值。
            stats.CurrentMaxHealth *= healthMultiplier;
        }
    }
}
