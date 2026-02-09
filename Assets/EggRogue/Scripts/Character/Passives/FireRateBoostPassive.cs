using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 射速提升被动 - 提升角色的攻击速度。
    /// </summary>
    [CreateAssetMenu(fileName = "FireRateBoost", menuName = "EggRogue/Passive/Fire Rate Boost", order = 2)]
    public class FireRateBoostPassive : CharacterPassive
    {
        [Header("提升设置")]
        [Tooltip("射速提升倍率。例如 1.5 = 射速 × 1.5")]
        public float multiplier = 1.5f;

        public override void ModifyStats(CharacterStats stats)
        {
            if (stats == null)
                return;

            stats.CurrentFireRate *= multiplier;
        }
    }
}
