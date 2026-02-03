using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 角色被动能力基类 - 定义角色特殊能力的统一接口。
    /// 每个具体能力继承此类并实现 ModifyStats 方法。
    /// </summary>
    public abstract class CharacterPassive : ScriptableObject
    {
        [Header("能力信息")]
        [Tooltip("能力名称（用于 UI 显示）")]
        public string abilityName = "特殊能力";

        [Tooltip("能力描述")]
        [TextArea(2, 4)]
        public string description = "描述";

        [Tooltip("能力图标（可选）")]
        public Sprite icon;

        /// <summary>
        /// 修改角色属性。在 CharacterStats 计算最终属性时调用。
        /// 可以读取 stats 的当前值，并修改它们。
        /// </summary>
        /// <param name="stats">角色属性管理器，可读写 Current* 属性</param>
        public abstract void ModifyStats(CharacterStats stats);
    }
}
