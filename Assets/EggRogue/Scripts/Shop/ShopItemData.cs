using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 商店商品类型
    /// </summary>
    public enum ShopItemType
    {
        Weapon,
        Item  // Phase 2 后续扩展
    }

    /// <summary>
    /// 商店商品数据（运行时，由 ShopManager 生成）
    /// </summary>
    public class ShopItemData
    {
        public ShopItemType ItemType { get; private set; }
        public WeaponData WeaponData { get; private set; }
        public ItemData ItemData { get; private set; }
        public int Price { get; private set; }
        public Sprite Icon => ItemType == ShopItemType.Weapon ? (WeaponData?.icon) : (ItemData?.icon);
        public string DisplayName => ItemType == ShopItemType.Weapon ? (WeaponData?.weaponName ?? "未知") : (ItemData?.itemName ?? "未知");

        public static ShopItemData CreateWeapon(WeaponData weapon, int price)
        {
            if (weapon == null) return null;
            return new ShopItemData
            {
                ItemType = ShopItemType.Weapon,
                WeaponData = weapon,
                ItemData = null,
                Price = price
            };
        }

        public static ShopItemData CreateItem(ItemData item, int price)
        {
            if (item == null) return null;
            return new ShopItemData
            {
                ItemType = ShopItemType.Item,
                WeaponData = null,
                ItemData = item,
                Price = price
            };
        }

        /// <summary>出售价格（武器基础价 × 80%；道具不可出售）</summary>
        public int GetSellPrice()
        {
            if (ItemType == ShopItemType.Weapon && WeaponData != null)
                return Mathf.Max(0, (int)(WeaponData.basePrice * 0.8f));
            return 0;
        }

        /// <summary>
        /// 获取功能描述或数值（道具：效果描述；武器：伤害+攻速）
        /// </summary>
        public string GetDescriptionOrStats()
        {
            if (ItemType == ShopItemType.Item && ItemData != null)
                return GetItemEffectDescription(ItemData);
            if (ItemType == ShopItemType.Weapon && WeaponData != null)
                return $"伤害: {WeaponData.damage:F0}\n攻速: {WeaponData.fireRate:F1} 发/秒";
            return "";
        }

        /// <summary>
        /// 根据 ItemData 获取道具功能描述（供角色面板等复用）
        /// </summary>
        public static string GetItemDescription(ItemData item)
        {
            if (item == null) return "";
            return GetItemEffectDescription(item);
        }

        private static string GetItemEffectDescription(ItemData item)
        {
            if (!string.IsNullOrEmpty(item.description))
                return item.description;

            switch (item.effectType)
            {
                case ItemEffectType.CritChip:
                    return $"{item.effectPercent:F0}% 概率造成 2 倍伤害";
                case ItemEffectType.Lifesteal:
                    return $"造成伤害的 {item.effectPercent:F0}% 转化为治疗";
                case ItemEffectType.ShieldGenerator:
                    return $"受到伤害减少 {item.effectPercent:F0}%";
                case ItemEffectType.Thorns:
                    return $"受到攻击时反弹 {item.effectPercent:F0}% 伤害";
                case ItemEffectType.IcePack:
                    return $"范围内敌人移速 -{item.effectPercent:F0}%";
                case ItemEffectType.Burning:
                    return $"攻击附带燃烧，{item.effectValue:F0} 伤害/秒，持续 {item.effectDuration:F0} 秒";
                case ItemEffectType.Poison:
                    return $"攻击附带毒，{item.effectValue:F0} 伤害/秒，持续 {item.effectDuration:F0} 秒";
                case ItemEffectType.HealthRegen:
                    return $"每秒恢复最大血量 {item.effectPercent:F0}%";
                case ItemEffectType.SpeedBoots:
                    return $"移速 +{item.effectPercent:F0}%";
                case ItemEffectType.Magnet:
                    return $"拾取范围 +{item.effectValue:F1} 米";
                case ItemEffectType.LightningTower:
                    return $"范围内每 {item.effectDuration:F0} 秒雷击随机敌人，伤害=最大血量×{item.effectValue:P0}";
                default:
                    return "";
            }
        }
    }
}
