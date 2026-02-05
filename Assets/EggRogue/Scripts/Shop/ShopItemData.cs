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
    }
}
