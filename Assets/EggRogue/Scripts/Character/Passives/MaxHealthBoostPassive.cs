using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 最大生命值提升被动 - 提升角色的最大生命值。
    /// </summary>
    [CreateAssetMenu(fileName = "MaxHealthBoost", menuName = "EggRogue/Passive/Max Health Boost", order = 4)]
    public class MaxHealthBoostPassive : CharacterPassive
    {
        [Header("提升设置")]
        [Tooltip("生命值提升倍率。例如 1.5 = 最大生命值 × 1.5")]
        public float multiplier = 1.5f;

        public override void ModifyStats(CharacterStats stats)
        {
            if (stats == null)
                return;

            stats.CurrentMaxHealth *= multiplier;
        }
    }
}
