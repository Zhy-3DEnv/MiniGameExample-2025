using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 坦克被动 - 大幅提升生命值，但降低移动速度。
    /// </summary>
    [CreateAssetMenu(fileName = "Tank", menuName = "EggRogue/Passive/Tank", order = 6)]
    public class TankPassive : CharacterPassive
    {
        [Header("坦克设置")]
        [Tooltip("生命值提升倍率。例如 2.0 = 最大生命值 × 2")]
        public float healthMultiplier = 2.0f;

        [Tooltip("移速降低倍率。例如 0.7 = 移速 × 0.7")]
        public float speedMultiplier = 0.7f;

        public override void ModifyStats(CharacterStats stats)
        {
            if (stats == null)
                return;

            stats.CurrentMaxHealth *= healthMultiplier;
            stats.CurrentMoveSpeed *= speedMultiplier;
        }
    }
}
