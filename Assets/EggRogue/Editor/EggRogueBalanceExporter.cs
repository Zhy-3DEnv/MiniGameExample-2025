using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using EggRogue;

/// <summary>
/// EggRogue 数值导出工具：
/// 将当前项目中的 LevelData / EnemyData / CharacterData / WeaponData / CardData 导出为 CSV，
/// 作为 Excel 反向编辑的模板。
///
/// 用法：
/// - 菜单：EggRogue/Excel/导出当前配置为 CSV/...
/// - 导出后的 CSV 可以直接在 Excel 中编辑，然后再用导入工具写回 SO。
/// </summary>
public static class EggRogueBalanceExporter
{
    private const string CsvFolder = "Assets/EggRogue/Configs/Excel";

    [MenuItem("EggRogue/Excel/导出当前配置为 CSV/Level-Base")]
    public static void ExportLevelBaseMenu()
    {
        EnsureFolder();
        string path = Path.Combine(CsvFolder, "Level-Base_Export.csv");
        ExportLevelBase(path);
        EditorUtility.DisplayDialog("导出 Level-Base", $"已导出到：\n{path}", "确定");
        AssetDatabase.Refresh();
    }

    [MenuItem("EggRogue/Excel/导出当前配置为 CSV/Level-SpawnMix")]
    public static void ExportLevelSpawnMixMenu()
    {
        EnsureFolder();
        string path = Path.Combine(CsvFolder, "Level-SpawnMix_Export.csv");
        ExportLevelSpawnMix(path);
        EditorUtility.DisplayDialog("导出 Level-SpawnMix", $"已导出到：\n{path}", "确定");
        AssetDatabase.Refresh();
    }

    [MenuItem("EggRogue/Excel/导出当前配置为 CSV/Level-CardWeight")]
    public static void ExportLevelCardWeightMenu()
    {
        EnsureFolder();
        string path = Path.Combine(CsvFolder, "Level-CardWeight_Export.csv");
        ExportLevelCardWeight(path);
        EditorUtility.DisplayDialog("导出 Level-CardWeight", $"已导出到：\n{path}", "确定");
        AssetDatabase.Refresh();
    }

    [MenuItem("EggRogue/Excel/导出当前配置为 CSV/导出怪物 CSV")]
    public static void ExportEnemiesMenu()
    {
        EnsureFolder();
        string path = Path.Combine(CsvFolder, "EggRogue_Enemies_Export.csv");
        ExportEnemies(path);
        EditorUtility.DisplayDialog("导出怪物", $"已导出到：\n{path}", "确定");
        AssetDatabase.Refresh();
    }

    [MenuItem("EggRogue/Excel/导出当前配置为 CSV/导出角色 CSV")]
    public static void ExportCharactersMenu()
    {
        EnsureFolder();
        string path = Path.Combine(CsvFolder, "EggRogue_Characters_Export.csv");
        ExportCharacters(path);
        EditorUtility.DisplayDialog("导出角色", $"已导出到：\n{path}", "确定");
        AssetDatabase.Refresh();
    }

    [MenuItem("EggRogue/Excel/导出当前配置为 CSV/导出武器 CSV")]
    public static void ExportWeaponsMenu()
    {
        EnsureFolder();
        string path = Path.Combine(CsvFolder, "EggRogue_Weapons_Export.csv");
        ExportWeapons(path);
        EditorUtility.DisplayDialog("导出武器", $"已导出到：\n{path}", "确定");
        AssetDatabase.Refresh();
    }

    [MenuItem("EggRogue/Excel/导出当前配置为 CSV/导出卡片 CSV")]
    public static void ExportCardsMenu()
    {
        EnsureFolder();
        string path = Path.Combine(CsvFolder, "EggRogue_Cards_Export.csv");
        ExportCards(path);
        EditorUtility.DisplayDialog("导出卡片", $"已导出到：\n{path}", "确定");
        AssetDatabase.Refresh();
    }

    [MenuItem("EggRogue/Excel/导出当前配置为 CSV/一键导出全部")]
    public static void ExportAllMenu()
    {
        EnsureFolder();
        ExportLevelBase(Path.Combine(CsvFolder, "Level-Base_Export.csv"));
        ExportLevelSpawnMix(Path.Combine(CsvFolder, "Level-SpawnMix_Export.csv"));
        ExportLevelCardWeight(Path.Combine(CsvFolder, "Level-CardWeight_Export.csv"));
        ExportEnemies(Path.Combine(CsvFolder, "EggRogue_Enemies_Export.csv"));
        ExportCharacters(Path.Combine(CsvFolder, "EggRogue_Characters_Export.csv"));
        ExportWeapons(Path.Combine(CsvFolder, "EggRogue_Weapons_Export.csv"));
        ExportCards(Path.Combine(CsvFolder, "EggRogue_Cards_Export.csv"));
        EditorUtility.DisplayDialog("导出全部", $"已导出到：\n{CsvFolder}", "确定");
        AssetDatabase.Refresh();
    }

    private static void EnsureFolder()
    {
        if (!Directory.Exists(CsvFolder))
        {
            Directory.CreateDirectory(CsvFolder);
            AssetDatabase.Refresh();
        }
    }

    private static List<LevelData> GetSortedLevels()
    {
        var levels = new List<LevelData>();
        var db = AssetDatabase.LoadAssetAtPath<LevelDatabase>("Assets/EggRogue/Configs/LevelDatabase.asset");
        if (db != null && db.levels != null)
            levels.AddRange(db.levels);
        else
        {
            string[] guids = AssetDatabase.FindAssets("t:EggRogue.LevelData", new[] { "Assets/EggRogue/Configs/Levels" });
            foreach (string guid in guids)
            {
                var ld = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
                if (ld != null) levels.Add(ld);
            }
            levels.Sort((a, b) => a.levelNumber.CompareTo(b.levelNumber));
        }
        return levels;
    }

    private static void ExportLevelBase(string csvPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("关卡编号,关卡名称,每秒刷怪,最大同时怪物数,最少总怪物,最多总怪物,随机偏移半径,刷怪开始时间,刷怪结束时间,关卡时长,胜利奖励金币,血量倍率,移速倍率,金币倍率");
        var levels = GetSortedLevels();
        var inv = CultureInfo.InvariantCulture;
        foreach (var ld in levels)
        {
            if (ld == null) continue;
            sb.AppendLine(ld.levelNumber + "," + Escape(ld.levelName) + "," + ld.spawnsPerSecond.ToString(inv) + "," +
                ld.maxAliveEnemies + "," + ld.minTotalEnemies + "," + ld.maxTotalEnemies + "," +
                ld.randomOffsetRadius.ToString(inv) + "," + ld.spawnStartTime.ToString(inv) + "," + ld.spawnEndTime.ToString(inv) + "," +
                ld.levelDuration.ToString(inv) + "," + ld.victoryRewardGold + "," +
                ld.enemyHealthMultiplier.ToString(inv) + "," + ld.enemyMoveSpeedMultiplier.ToString(inv) + "," + ld.coinDropMultiplier.ToString(inv));
        }
        File.WriteAllText(csvPath, sb.ToString(), new UTF8Encoding(true));
    }

    private static void ExportLevelSpawnMix(string csvPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("关卡编号,Enemy1Id,Enemy1Weight,Enemy1MaxAlive,Enemy1Start,Enemy1End,Enemy2Id,Enemy2Weight,Enemy2MaxAlive,Enemy2Start,Enemy2End,Enemy3Id,Enemy3Weight,Enemy3MaxAlive,Enemy3Start,Enemy3End");
        var levels = GetSortedLevels();
        var inv = CultureInfo.InvariantCulture;
        foreach (var ld in levels)
        {
            if (ld == null) continue;
            string line = ld.levelNumber.ToString();
            var mix = ld.spawnMix ?? Array.Empty<LevelData.LevelSpawnEntry>();
            for (int i = 0; i < 3; i++)
            {
                if (i < mix.Length && mix[i] != null)
                {
                    var e = mix[i];
                    line += "," + Escape(e.enemyData != null ? e.enemyData.name : "") + "," + e.spawnWeight.ToString(inv) + "," + e.maxAlive + "," + e.spawnTimeStart.ToString(inv) + "," + e.spawnTimeEnd.ToString(inv);
                }
                else
                    line += ",,,,,";
            }
            sb.AppendLine(line);
        }
        File.WriteAllText(csvPath, sb.ToString(), new UTF8Encoding(true));
    }

    private static void ExportLevelCardWeight(string csvPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("关卡编号,W_Lv1,W_Lv2,W_Lv3,W_Lv4,W_Lv5");
        var levels = GetSortedLevels();
        var inv = CultureInfo.InvariantCulture;
        foreach (var ld in levels)
        {
            if (ld == null) continue;
            var w = ld.cardLevelWeights;
            if (w == null || w.Length < 5) w = new float[5];
            sb.AppendLine(ld.levelNumber + "," + (w.Length > 0 ? w[0].ToString(inv) : "0") + "," + (w.Length > 1 ? w[1].ToString(inv) : "0") + "," + (w.Length > 2 ? w[2].ToString(inv) : "0") + "," + (w.Length > 3 ? w[3].ToString(inv) : "0") + "," + (w.Length > 4 ? w[4].ToString(inv) : "0"));
        }
        File.WriteAllText(csvPath, sb.ToString(), new UTF8Encoding(true));
    }

    /// <summary>
    /// 导出所有 EnemyData。
    /// </summary>
    private static void ExportEnemies(string csvPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,名称,描述,基础生命,基础移速,基础伤害,经验值,掉落金币最小,掉落金币最大");

        string[] guids = AssetDatabase.FindAssets("t:EggRogue.EnemyData", new[] { "Assets/EggRogue/Configs/Enemies" });
        var inv = CultureInfo.InvariantCulture;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (enemy == null) continue;

            string id = enemy.name;
            string name = Escape(enemy.enemyName);
            string desc = Escape(enemy.description);

            string line =
                Escape(id) + "," +
                name + "," +
                desc + "," +
                enemy.baseMaxHealth.ToString(inv) + "," +
                enemy.baseMoveSpeed.ToString(inv) + "," +
                enemy.baseDamage.ToString(inv) + "," +
                enemy.xpValue + "," +
                enemy.coinDropMin + "," +
                enemy.coinDropMax;

            sb.AppendLine(line);
        }

        File.WriteAllText(csvPath, sb.ToString(), new UTF8Encoding(true));
    }

    /// <summary>
    /// 导出所有 CharacterData。
    /// </summary>
    private static void ExportCharacters(string csvPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("AssetName,角色名称,描述,基础等级,基础伤害,基础攻速,基础生命,基础移速,基础子弹速度,基础攻击范围,基础拾取范围");

        string[] guids = AssetDatabase.FindAssets("t:EggRogue.CharacterData", new[] { "Assets/EggRogue/Configs/Characters" });
        var inv = CultureInfo.InvariantCulture;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ch = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (ch == null) continue;

            string assetName = ch.name;
            string name = Escape(ch.characterName);
            string desc = Escape(ch.description);

            string line =
                Escape(assetName) + "," +
                name + "," +
                desc + "," +
                ch.baseLevel + "," +
                ch.baseDamage.ToString(inv) + "," +
                ch.baseFireRate.ToString(inv) + "," +
                ch.baseMaxHealth.ToString(inv) + "," +
                ch.baseMoveSpeed.ToString(inv) + "," +
                ch.baseBulletSpeed.ToString(inv) + "," +
                ch.baseAttackRange.ToString(inv) + "," +
                ch.basePickupRange.ToString(inv);

            sb.AppendLine(line);
        }

        File.WriteAllText(csvPath, sb.ToString(), new UTF8Encoding(true));
    }

    /// <summary>
    /// 导出所有 WeaponData。
    /// </summary>
    private static void ExportWeapons(string csvPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("AssetName,WeaponId,名称,类型,等级,基础价格,伤害,攻速,攻击范围,子弹速度,子弹寿命,下一等级Asset");

        string[] guids = AssetDatabase.FindAssets("t:EggRogue.WeaponData", new[] { "Assets/EggRogue/Configs/Weapons" });
        var inv = CultureInfo.InvariantCulture;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var w = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (w == null) continue;

            string assetName = w.name;
            string name = Escape(w.weaponName);
            string type = w.weaponType.ToString(); // Ranged / Melee
            string next = w.nextLevelWeapon != null ? w.nextLevelWeapon.name : "";

            string line =
                Escape(assetName) + "," +
                Escape(w.weaponId) + "," +
                name + "," +
                Escape(type) + "," +
                w.level + "," +
                w.basePrice + "," +
                w.damage.ToString(inv) + "," +
                w.fireRate.ToString(inv) + "," +
                w.attackRange.ToString(inv) + "," +
                w.bulletSpeed.ToString(inv) + "," +
                w.bulletLifeTime.ToString(inv) + "," +
                Escape(next);

            sb.AppendLine(line);
        }

        File.WriteAllText(csvPath, sb.ToString(), new UTF8Encoding(true));
    }

    /// <summary>
    /// 导出所有 CardData 的等级加成表（方案2）。每张卡输出 5 行（level 1-5）。
    /// </summary>
    private static void ExportCards(string csvPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("cardTypeId,level,卡片名称,描述,伤害加成,攻速加成,生命加成,移速加成,子弹速度加成,攻击范围加成,拾取范围加成");

        var cards = new List<CardData>();
        var db = AssetDatabase.LoadAssetAtPath<CardDatabase>("Assets/EggRogue/Configs/CardDatabase.asset");
        if (db != null && db.allCards != null && db.allCards.Length > 0)
            cards.AddRange(db.allCards);
        else
        {
            string[] guids = AssetDatabase.FindAssets("t:EggRogue.CardData", new[] { "Assets/EggRogue/Configs/Cards" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var c = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (c != null) cards.Add(c);
            }
        }

        var inv = CultureInfo.InvariantCulture;
        foreach (var c in cards)
        {
            if (c == null) continue;
            string ctId = Escape(c.cardTypeId);
            string name = Escape(c.cardName);
            string desc = Escape(c.description);

            var bonuses = c.levelBonuses;
            if (bonuses == null || bonuses.Length < 5)
                bonuses = new CardLevelBonus[5];

            for (int lv = 1; lv <= 5; lv++)
            {
                int idx = lv - 1;
                var b = idx < bonuses.Length ? bonuses[idx] : default;
                string line =
                    ctId + "," +
                    lv + "," +
                    name + "," +
                    desc + "," +
                    b.damageBonus.ToString(inv) + "," +
                    b.fireRateBonus.ToString(inv) + "," +
                    b.maxHealthBonus.ToString(inv) + "," +
                    b.moveSpeedBonus.ToString(inv) + "," +
                    b.bulletSpeedBonus.ToString(inv) + "," +
                    b.attackRangeBonus.ToString(inv) + "," +
                    b.pickupRangeBonus.ToString(inv);
                sb.AppendLine(line);
            }
        }

        File.WriteAllText(csvPath, sb.ToString(), new UTF8Encoding(true));
    }

    /// <summary>
    /// 简单的 CSV 字段转义：遇到逗号或引号时，加引号并转义内部引号。
    /// </summary>
    private static string Escape(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        bool needQuote = field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r");
        if (!needQuote) return field;
        string escaped = field.Replace("\"", "\"\"");
        return "\"" + escaped + "\"";
    }
}

