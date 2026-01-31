using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 移速提升被动 - 提升角色的移动速度。
    /// </summary>
    [CreateAssetMenu(fileName = "MoveSpeedBoost", menuName = "EggRogue/Passive/Move Speed Boost", order = 3)]
    public class MoveSpeedBoostPassive : CharacterPassive
    {
        [Header("提升设置")]
        [Tooltip("移速提升倍率。例如 1.3 = 移速 × 1.3")]
        public float multiplier = 1.3f;

        public override void ModifyStats(CharacterStats stats)
        {
            if (stats == null)
                return;

            stats.CurrentMoveSpeed *= multiplier;
        }
    }
}
