#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 创建 CardDatabase 与默认 CardData 资源。
/// 菜单：EggRogue → 创建 CardDatabase 与默认卡片
/// </summary>
public static class CardDatabaseSetup
{
    private const string ConfigsDir = "Assets/EggRogue/Configs";
    private const string CardsDir = ConfigsDir + "/Cards";

    [MenuItem("EggRogue/创建 CardDatabase 与默认卡片")]
    public static void CreateCardDatabaseAndCards()
    {
        if (!AssetDatabase.IsValidFolder("Assets/EggRogue"))
        {
            Debug.LogError("CardDatabaseSetup: Assets/EggRogue 不存在。");
            return;
        }
        if (!AssetDatabase.IsValidFolder("Assets/EggRogue/Configs"))
            AssetDatabase.CreateFolder("Assets/EggRogue", "Configs");
        if (!AssetDatabase.IsValidFolder(CardsDir))
            AssetDatabase.CreateFolder(ConfigsDir, "Cards");

        EggRogue.CardDatabase db = ScriptableObject.CreateInstance<EggRogue.CardDatabase>();
        System.Collections.Generic.List<EggRogue.CardData> cards = new System.Collections.Generic.List<EggRogue.CardData>();

        // 创建默认卡片
        cards.Add(CreateCard("力量提升", "伤害 +5", 5f, 0f, 0f, 0f, 0f, 0f));
        cards.Add(CreateCard("攻速提升", "攻击速度 +1", 0f, 1f, 0f, 0f, 0f, 0f));
        cards.Add(CreateCard("生命提升", "最大生命值 +20", 0f, 0f, 20f, 0f, 0f, 0f));
        cards.Add(CreateCard("移速提升", "移动速度 +1", 0f, 0f, 0f, 1f, 0f, 0f));
        cards.Add(CreateCard("子弹加速", "子弹速度 +10", 0f, 0f, 0f, 0f, 10f, 0f));
        cards.Add(CreateCard("射程提升", "攻击范围 +2", 0f, 0f, 0f, 0f, 0f, 2f));
        cards.Add(CreateCard("全面强化", "伤害 +3, 攻速 +0.5, 生命 +15", 3f, 0.5f, 15f, 0f, 0f, 0f));
        cards.Add(CreateCard("极速射击", "攻击速度 +2, 子弹速度 +5", 0f, 2f, 0f, 0f, 5f, 0f));

        foreach (var card in cards)
        {
            string path = $"{CardsDir}/{card.cardName}.asset";
            AssetDatabase.CreateAsset(card, path);
        }

        db.allCards = cards.ToArray();
        AssetDatabase.CreateAsset(db, ConfigsDir + "/CardDatabase.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("CardDatabaseSetup: 已创建 CardDatabase 与默认卡片。在 CardSelectionPanel 上指定 CardDatabase 引用。");
    }

    private static EggRogue.CardData CreateCard(string name, string desc, float damage, float fireRate,
        float health, float moveSpeed, float bulletSpeed, float attackRange)
    {
        var card = ScriptableObject.CreateInstance<EggRogue.CardData>();
        card.cardTypeId = name;
        card.cardName = name;
        card.description = desc;
        card.levelBonuses = new EggRogue.CardLevelBonus[5];
        // 默认只填等级1的加成
        card.levelBonuses[0] = new EggRogue.CardLevelBonus
        {
            level = 1,
            damageBonus = damage,
            fireRateBonus = fireRate,
            maxHealthBonus = health,
            moveSpeedBonus = moveSpeed,
            bulletSpeedBonus = bulletSpeed,
            attackRangeBonus = attackRange,
            pickupRangeBonus = 0f
        };
        return card;
    }

    /// <summary>
    /// 将旧卡片的平铺加成字段迁移到 levelBonuses[0]。适用于从旧格式升级的项目。
    /// </summary>
    [MenuItem("EggRogue/迁移卡片到等级表格式")]
    public static void MigrateCardsToLevelBonuses()
    {
        string[] guids = AssetDatabase.FindAssets("t:EggRogue.CardData", new[] { CardsDir });
        int migrated = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var card = AssetDatabase.LoadAssetAtPath<EggRogue.CardData>(path);
            if (card == null) continue;

            var so = new SerializedObject(card);
            var damageProp = so.FindProperty("damageBonus");
            var fireRateProp = so.FindProperty("fireRateBonus");
            var maxHpProp = so.FindProperty("maxHealthBonus");
            var moveSpeedProp = so.FindProperty("moveSpeedBonus");
            var bulletSpeedProp = so.FindProperty("bulletSpeedBonus");
            var attackRangeProp = so.FindProperty("attackRangeBonus");
            var pickupRangeProp = so.FindProperty("pickupRangeBonus");

            if (damageProp == null) continue;

            float d = damageProp.floatValue, fr = fireRateProp.floatValue, hp = maxHpProp.floatValue;
            float ms = moveSpeedProp.floatValue, bs = bulletSpeedProp.floatValue, ar = attackRangeProp.floatValue, pr = pickupRangeProp.floatValue;

            if (d == 0f && fr == 0f && hp == 0f && ms == 0f && bs == 0f && ar == 0f && pr == 0f)
                continue;

            if (card.levelBonuses == null || card.levelBonuses.Length < 5)
                card.levelBonuses = new EggRogue.CardLevelBonus[5];

            card.levelBonuses[0] = new EggRogue.CardLevelBonus
            {
                level = 1,
                damageBonus = d,
                fireRateBonus = fr,
                maxHealthBonus = hp,
                moveSpeedBonus = ms,
                bulletSpeedBonus = bs,
                attackRangeBonus = ar,
                pickupRangeBonus = pr
            };
            EditorUtility.SetDirty(card);
            migrated++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"CardDatabaseSetup: 迁移完成，共 {migrated} 张卡片已写入 levelBonuses[0]。");
    }
}
#endif
