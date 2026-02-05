#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using EggRogue;

/// <summary>
/// 创建 10 个功能性道具 + ItemDatabase
/// </summary>
public static class ItemDatabaseSetup
{
    private const string ConfigDir = "Assets/EggRogue/Configs";
    private const string ItemsDir = ConfigDir + "/Items";

    [MenuItem("EggRogue/创建道具数据库和10个道具")]
    public static void CreateItemDatabaseAndItems()
    {
        if (!AssetDatabase.IsValidFolder("Assets/EggRogue"))
            AssetDatabase.CreateFolder("Assets", "EggRogue");
        if (!AssetDatabase.IsValidFolder("Assets/EggRogue/Configs"))
            AssetDatabase.CreateFolder("Assets/EggRogue", "Configs");
        if (!AssetDatabase.IsValidFolder(ItemsDir))
            AssetDatabase.CreateFolder(ConfigDir, "Items");

        var items = new ItemData[10];

        // 1. 电塔
        items[0] = CreateItem("item_lightning", "电塔", "范围内每3秒雷击随机敌人，造成角色50%最大血量伤害",
            ItemEffectType.LightningTower, 60, 8f, 3f, 0.5f, 0);

        // 2. 冰袋
        items[1] = CreateItem("item_ice", "冰袋", "范围内敌人移速降低30%",
            ItemEffectType.IcePack, 45, 6f, 0, 0, 30);

        // 3. 火箭（燃烧）
        items[2] = CreateItem("item_burning", "火箭", "攻击附带燃烧，2伤害/秒，持续5秒",
            ItemEffectType.Burning, 50, 0, 5f, 2f, 0);

        // 4. 护盾发生器
        items[3] = CreateItem("item_shield", "护盾发生器", "受到伤害减少15%",
            ItemEffectType.ShieldGenerator, 55, 0, 0, 0, 15);

        // 5. 生命恢复器
        items[4] = CreateItem("item_regen", "生命恢复器", "每秒恢复最大血量1%",
            ItemEffectType.HealthRegen, 40, 0, 0, 0, 1);

        // 6. 暴击芯片
        items[5] = CreateItem("item_crit", "暴击芯片", "10%概率造成2倍伤害",
            ItemEffectType.CritChip, 70, 0, 0, 0, 10);

        // 7. 吸血戒指
        items[6] = CreateItem("item_lifesteal", "吸血戒指", "造成伤害的8%转化为治疗",
            ItemEffectType.Lifesteal, 65, 0, 0, 0, 8);

        // 8. 毒瓶
        items[7] = CreateItem("item_poison", "毒瓶", "攻击附带毒，3伤害/秒，持续4秒",
            ItemEffectType.Poison, 48, 0, 4f, 3f, 0);

        // 9. 加速靴
        items[8] = CreateItem("item_speed", "加速靴", "移速+20%",
            ItemEffectType.SpeedBoots, 35, 0, 0, 0, 20);

        // 10. 荆棘甲
        items[9] = CreateItem("item_thorns", "荆棘甲", "受到攻击时反弹20%伤害",
            ItemEffectType.Thorns, 58, 0, 0, 0, 20);

        for (int i = 0; i < items.Length; i++)
        {
            string path = $"{ItemsDir}/Item_{items[i].itemId}.asset";
            AssetDatabase.CreateAsset(items[i], path);
        }

        var db = ScriptableObject.CreateInstance<ItemDatabase>();
        db.items = items;
        AssetDatabase.CreateAsset(db, $"{ConfigDir}/ItemDatabase.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var sm = Object.FindObjectOfType<ShopManager>();
        if (sm != null)
        {
            sm.itemDatabase = db;
            EditorUtility.SetDirty(sm);
        }

        Debug.Log("[ItemDatabaseSetup] 已创建 10 个道具和 ItemDatabase。");
        EditorUtility.DisplayDialog("完成", "已创建 10 个功能性道具和 ItemDatabase。\n请在 ShopManager 上绑定 ItemDatabase。", "确定");
    }

    private static ItemData CreateItem(string id, string name, string desc,
        ItemEffectType type, int price, float range, float duration, float value, float percent)
    {
        var item = ScriptableObject.CreateInstance<ItemData>();
        item.itemId = id;
        item.itemName = name;
        item.description = desc;
        item.basePrice = price;
        item.effectType = type;
        item.effectRange = range;
        item.effectDuration = duration;
        item.effectValue = value;
        item.effectPercent = percent;
        return item;
    }
}
#endif
