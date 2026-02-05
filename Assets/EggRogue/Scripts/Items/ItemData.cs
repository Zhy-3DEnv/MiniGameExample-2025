using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 道具效果类型
    /// </summary>
    public enum ItemEffectType
    {
        None,
        LightningTower,  // 电塔：范围内每3秒雷击随机敌人，伤害=角色50%最大血量
        IcePack,         // 冰袋：范围内敌人移速-30%
        Burning,         // 火箭：攻击附带燃烧，2伤害/秒，持续5秒
        ShieldGenerator, // 护盾发生器：受到伤害减少15%
        HealthRegen,     // 生命恢复器：每秒恢复最大血量1%
        CritChip,        // 暴击芯片：10%概率造成2倍伤害
        Lifesteal,       // 吸血戒指：造成伤害的8%转化为治疗
        Poison,          // 毒瓶：攻击附带毒，3伤害/秒，持续4秒
        SpeedBoots,      // 加速靴：移速+20%
        Thorns,          // 荆棘甲：受到攻击时反弹20%伤害
        Magnet           // 磁铁：拾取范围+effectValue米（可叠加）
    }

    /// <summary>
    /// 道具数据（ScriptableObject）- 定义道具属性与效果。
    /// </summary>
    [CreateAssetMenu(fileName = "Item_01", menuName = "EggRogue/Item Data", order = 6)]
    public class ItemData : ScriptableObject
    {
        [Header("基础信息")]
        public string itemId = "item_01";
        public string itemName = "道具";
        [TextArea(2, 4)]
        public string description = "道具描述";
        public Sprite icon;

        [Header("商店")]
        [Tooltip("基础售价")]
        public int basePrice = 30;

        [Header("效果配置")]
        public ItemEffectType effectType = ItemEffectType.None;

        [Tooltip("电塔：范围(米)")]
        public float effectRange = 8f;

        [Tooltip("电塔：间隔(秒)；燃烧/毒：持续(秒)")]
        public float effectDuration = 3f;

        [Tooltip("电塔：伤害=最大血量*此值；燃烧/毒：每秒伤害")]
        public float effectValue = 0.5f;

        [Tooltip("护盾减伤%/暴击概率%/吸血%/荆棘反弹% 等")]
        public float effectPercent = 15f;
    }
}
