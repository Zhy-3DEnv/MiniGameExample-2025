using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 攻击力修改倍率被动 - 所有攻击力变化（卡片加成等）都会乘以倍率。
    /// 例如：倍率 1.5，基础攻击 10，卡片 +5 → 最终攻击 = 10 + 5×1.5 = 17.5
    /// </summary>
    [CreateAssetMenu(fileName = "DamageMultiplier", menuName = "EggRogue/Passive/Damage Multiplier", order = 0)]
    public class DamageMultiplierPassive : CharacterPassive
    {
        [Header("倍率设置")]
        [Tooltip("攻击力修改倍率。例如 1.5 = 所有攻击力变化（卡片加成等）× 1.5")]
        public float multiplier = 1.5f;

        public override void ModifyStats(CharacterStats stats)
        {
            if (stats == null || stats.characterData == null)
                return;

            // 计算"基础值 → 当前值"的增量（来自卡片等）
            float baseDamage = stats.characterData.baseDamage;
            float delta = stats.CurrentDamage - baseDamage;

            // 将增量乘以倍率，再加回基础值
            stats.CurrentDamage = baseDamage + delta * multiplier;
        }
    }
}
