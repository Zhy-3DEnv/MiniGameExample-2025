using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using EggRogue;

/// <summary>
/// EggRogue 数值导入工具：
/// 从 Excel 导出的 CSV 表批量更新 LevelData / EnemyData / CharacterData / WeaponData / CardData ScriptableObject。
///
/// 设计思路：
/// - 你在 Excel 里用多个 Sheet 管理数值（Levels / Enemies / Characters / Weapons）
/// - 每个 Sheet 另存为一个 CSV（UTF-8）
/// - 在 Unity 菜单执行：
///     EggRogue/Excel/导入关卡配置(CSV)
///     EggRogue/Excel/导入怪物配置(CSV)
///     EggRogue/Excel/导入角色配置(CSV)
///     EggRogue/Excel/导入武器配置(CSV)
/// - 工具会按照表头列名，把 CSV 中的数据写回对应的 ScriptableObject
///
/// 运行时：游戏仍然只使用现有的 LevelData / EnemyData / CharacterData / WeaponData，不依赖 CSV。
/// </summary>
public static class EggRogueBalanceImporter
{
    #region 公共入口菜单

    [MenuItem("EggRogue/Excel/导入关卡配置(CSV)/Level-Base")]
    public static void ImportLevelBaseMenu()
    {
        string path = EditorUtility.OpenFilePanel("选择 Level-Base CSV 文件", "", "csv");
        if (string.IsNullOrEmpty(path)) return;
        ImportLevelBaseFromCsv(path, "Assets/EggRogue/Configs/Levels");
    }

    [MenuItem("EggRogue/Excel/导入关卡配置(CSV)/Level-SpawnMix")]
    public static void ImportLevelSpawnMixMenu()
    {
        string path = EditorUtility.OpenFilePanel("选择 Level-SpawnMix CSV 文件", "", "csv");
        if (string.IsNullOrEmpty(path)) return;
        ImportLevelSpawnMixFromCsv(path, "Assets/EggRogue/Configs/Levels");
    }

    [MenuItem("EggRogue/Excel/导入关卡配置(CSV)/Level-CardWeight")]
    public static void ImportLevelCardWeightMenu()
    {
        string path = EditorUtility.OpenFilePanel("选择 Level-CardWeight CSV 文件", "", "csv");
        if (string.IsNullOrEmpty(path)) return;
        ImportLevelCardWeightFromCsv(path, "Assets/EggRogue/Configs/Levels");
    }

    [MenuItem("EggRogue/Excel/导入怪物配置(CSV)")]
    public static void ImportEnemiesMenu()
    {
        string path = EditorUtility.OpenFilePanel("选择 Enemies CSV 文件", "", "csv");
        if (string.IsNullOrEmpty(path)) return;
        ImportEnemiesFromCsv(path, "Assets/EggRogue/Configs/Enemies");
    }

    [MenuItem("EggRogue/Excel/导入角色配置(CSV)")]
    public static void ImportCharactersMenu()
    {
        string path = EditorUtility.OpenFilePanel("选择 Characters CSV 文件", "", "csv");
        if (string.IsNullOrEmpty(path)) return;
        ImportCharactersFromCsv(path, "Assets/EggRogue/Configs/Characters");
    }

    [MenuItem("EggRogue/Excel/导入武器配置(CSV)")]
    public static void ImportWeaponsMenu()
    {
        string path = EditorUtility.OpenFilePanel("选择 Weapons CSV 文件", "", "csv");
        if (string.IsNullOrEmpty(path)) return;
        ImportWeaponsFromCsv(path, "Assets/EggRogue/Configs/Weapons");
    }

    [MenuItem("EggRogue/Excel/导入卡片配置(CSV)")]
    public static void ImportCardsMenu()
    {
        string path = EditorUtility.OpenFilePanel("选择 Cards CSV 文件", "", "csv");
        if (string.IsNullOrEmpty(path)) return;
        ImportCardsFromCsv(path, "Assets/EggRogue/Configs/Cards");
    }

    public static void ImportLevelBaseFromCsvPath(string csvPath, string levelsFolder = "Assets/EggRogue/Configs/Levels")
    {
        if (string.IsNullOrEmpty(csvPath)) return;
        ImportLevelBaseFromCsv(csvPath, levelsFolder);
    }

    public static void ImportLevelSpawnMixFromCsvPath(string csvPath, string levelsFolder = "Assets/EggRogue/Configs/Levels")
    {
        if (string.IsNullOrEmpty(csvPath)) return;
        ImportLevelSpawnMixFromCsv(csvPath, levelsFolder);
    }

    public static void ImportLevelCardWeightFromCsvPath(string csvPath, string levelsFolder = "Assets/EggRogue/Configs/Levels")
    {
        if (string.IsNullOrEmpty(csvPath)) return;
        ImportLevelCardWeightFromCsv(csvPath, levelsFolder);
    }

    /// <summary>
    /// 提供给工具面板使用的封装入口：使用指定 CSV 路径导入怪物。
    /// </summary>
    public static void ImportEnemiesFromCsvPath(string csvPath, string enemiesFolder = "Assets/EggRogue/Configs/Enemies")
    {
        if (string.IsNullOrEmpty(csvPath)) return;
        ImportEnemiesFromCsv(csvPath, enemiesFolder);
    }

    /// <summary>
    /// 提供给工具面板使用的封装入口：使用指定 CSV 路径导入角色。
    /// </summary>
    public static void ImportCharactersFromCsvPath(string csvPath, string charactersFolder = "Assets/EggRogue/Configs/Characters")
    {
        if (string.IsNullOrEmpty(csvPath)) return;
        ImportCharactersFromCsv(csvPath, charactersFolder);
    }

    /// <summary>
    /// 提供给工具面板使用的封装入口：使用指定 CSV 路径导入武器。
    /// </summary>
    public static void ImportWeaponsFromCsvPath(string csvPath, string weaponsFolder = "Assets/EggRogue/Configs/Weapons")
    {
        if (string.IsNullOrEmpty(csvPath)) return;
        ImportWeaponsFromCsv(csvPath, weaponsFolder);
    }

    /// <summary>
    /// 提供给工具面板使用的封装入口：使用指定 CSV 路径导入卡片。
    /// </summary>
    public static void ImportCardsFromCsvPath(string csvPath, string cardsFolder = "Assets/EggRogue/Configs/Cards")
    {
        if (string.IsNullOrEmpty(csvPath)) return;
        ImportCardsFromCsv(csvPath, cardsFolder);
    }

    #endregion

    #region 关卡导入

    private static LevelData GetOrCreateLevelData(string levelsFolder, int levelNumber)
    {
        string assetPath = $"{levelsFolder}/Level_{levelNumber:D2}.asset";
        var ld = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
        if (ld == null)
        {
            ld = ScriptableObject.CreateInstance<LevelData>();
            ld.levelNumber = levelNumber;
            AssetDatabase.CreateAsset(ld, assetPath);
        }
        return ld;
    }

    /// <summary>
    /// 从 Level-Base.csv 导入关卡基础数据（不含 spawnMix、cardLevelWeights）。
    /// 表头：关卡编号,关卡名称,每秒刷怪,最大同时怪物数,最少总怪物,最多总怪物,随机偏移半径,刷怪开始时间,刷怪结束时间,关卡时长,胜利奖励金币,血量倍率,移速倍率,金币倍率
    /// </summary>
    private static void ImportLevelBaseFromCsv(string csvPath, string levelsFolder)
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[EggRogueBalanceImporter] 关卡 CSV 文件不存在：{csvPath}");
            return;
        }

        try
        {
            string fileContent = ReadCsvFile(csvPath);
            string[] lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (lines.Length < 2)
            {
                Debug.LogError("[EggRogueBalanceImporter] 关卡 CSV 文件至少需要 2 行（表头 + 数据）。");
                return;
            }

            // 表头解析
            string[] headers = ParseCsvLine(lines[0]);
            var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                string h = headers[i].Trim();
                if (!string.IsNullOrEmpty(h) && !headerIndex.ContainsKey(h))
                    headerIndex[h] = i;
            }

            // 中文表头到字段名的映射
            var fieldMap = new Dictionary<string, string>
            {
                { "关卡编号", "levelNumber" },
                { "关卡名称", "levelName" },
                { "每秒刷怪", "spawnsPerSecond" },
                { "最大同时怪物数", "maxAliveEnemies" },
                { "最少总怪物", "minTotalEnemies" },
                { "最多总怪物", "maxTotalEnemies" },
                { "随机偏移半径", "randomOffsetRadius" },
                { "刷怪开始时间", "spawnStartTime" },
                { "刷怪结束时间", "spawnEndTime" },
                { "关卡时长", "levelDuration" },
                { "胜利奖励金币", "victoryRewardGold" },
                { "血量倍率", "enemyHealthMultiplier" },
                { "移速倍率", "enemyMoveSpeedMultiplier" },
                { "金币倍率", "coinDropMultiplier" }
            };

            // 检查至少有关卡编号
            if (!headerIndex.ContainsKey("关卡编号"))
            {
                Debug.LogError("[EggRogueBalanceImporter] 关卡 CSV 缺少必需表头：关卡编号");
                return;
            }

            if (!AssetDatabase.IsValidFolder(levelsFolder))
            {
                Directory.CreateDirectory(levelsFolder);
                AssetDatabase.Refresh();
            }

            int success = 0;
            int errors = 0;
            List<string> errorLines = new List<string>();

            for (int row = 1; row < lines.Length; row++)
            {
                if (string.IsNullOrWhiteSpace(lines[row]))
                    continue;

                try
                {
                    string[] cols = ParseCsvLine(lines[row]);
                    if (cols.Length == 0) continue;

                    // 关卡编号决定文件名与 levelNumber
                    int levelNumber = 0;
                    {
                        int idx = headerIndex["关卡编号"];
                        string val = GetColumn(cols, idx);
                        if (!int.TryParse(val, out levelNumber) || levelNumber <= 0)
                            throw new Exception($"关卡编号无效：{val}");
                    }

                    LevelData levelData = GetOrCreateLevelData(levelsFolder, levelNumber);

                    // 基础字段
                    foreach (var kv in fieldMap)
                    {
                        if (!headerIndex.TryGetValue(kv.Key, out int idx)) continue;
                        string value = GetColumn(cols, idx);
                        if (string.IsNullOrEmpty(value)) continue;

                        switch (kv.Value)
                        {
                            case "levelNumber":
                                if (int.TryParse(value, out int ln)) levelData.levelNumber = ln;
                                break;
                            case "levelName":
                                levelData.levelName = value;
                                break;
                            case "spawnsPerSecond":
                                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float sps))
                                    levelData.spawnsPerSecond = sps;
                                break;
                            case "maxAliveEnemies":
                                if (int.TryParse(value, out int maxAlive)) levelData.maxAliveEnemies = maxAlive;
                                break;
                            case "minTotalEnemies":
                                if (int.TryParse(value, out int minTotal)) levelData.minTotalEnemies = minTotal;
                                break;
                            case "maxTotalEnemies":
                                if (int.TryParse(value, out int maxTotal)) levelData.maxTotalEnemies = maxTotal;
                                break;
                            case "randomOffsetRadius":
                                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float radius))
                                    levelData.randomOffsetRadius = radius;
                                break;
                            case "spawnStartTime":
                                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float sst))
                                    levelData.spawnStartTime = sst;
                                break;
                            case "spawnEndTime":
                                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float set))
                                    levelData.spawnEndTime = set;
                                break;
                            case "levelDuration":
                                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float dur))
                                    levelData.levelDuration = dur;
                                break;
                            case "victoryRewardGold":
                                if (int.TryParse(value, out int reward)) levelData.victoryRewardGold = reward;
                                break;
                            case "enemyHealthMultiplier":
                                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float hpMul))
                                    levelData.enemyHealthMultiplier = hpMul;
                                break;
                            case "enemyMoveSpeedMultiplier":
                                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float msMul))
                                    levelData.enemyMoveSpeedMultiplier = msMul;
                                break;
                            case "coinDropMultiplier":
                                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float coinMul))
                                    levelData.coinDropMultiplier = coinMul;
                                break;
                        }
                    }

                    EditorUtility.SetDirty(levelData);
                    success++;
                }
                catch (Exception ex)
                {
                    errors++;
                    errorLines.Add($"第 {row + 1} 行: {ex.Message}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string msg = $"[EggRogueBalanceImporter] Level-Base 导入完成：成功 {success} 条，错误 {errors} 条";
            if (errorLines.Count > 0)
            {
                msg += "\n前几条错误：\n" + string.Join("\n", errorLines.GetRange(0, Math.Min(5, errorLines.Count)));
            }
            Debug.Log(msg);
        }
        catch (Exception e)
        {
            Debug.LogError($"[EggRogueBalanceImporter] Level-Base 导入失败：{e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 从 Level-SpawnMix.csv 导入刷怪配方。
    /// 表头：关卡编号,Enemy1Id,Enemy1Weight,Enemy1MaxAlive,Enemy1Start,Enemy1End,Enemy2Id,...,Enemy3Id,...
    /// </summary>
    private static void ImportLevelSpawnMixFromCsv(string csvPath, string levelsFolder)
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[EggRogueBalanceImporter] Level-SpawnMix CSV 不存在：{csvPath}");
            return;
        }
        try
        {
            string fileContent = ReadCsvFile(csvPath);
            string[] lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (lines.Length < 2)
            {
                Debug.LogError("[EggRogueBalanceImporter] Level-SpawnMix CSV 至少需要 2 行。");
                return;
            }
            string[] headers = ParseCsvLine(lines[0]);
            var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                string h = headers[i].Trim();
                if (!string.IsNullOrEmpty(h) && !headerIndex.ContainsKey(h)) headerIndex[h] = i;
            }
            if (!headerIndex.ContainsKey("关卡编号"))
            {
                Debug.LogError("[EggRogueBalanceImporter] Level-SpawnMix 缺少表头：关卡编号");
                return;
            }
            int success = 0;
            for (int row = 1; row < lines.Length; row++)
            {
                if (string.IsNullOrWhiteSpace(lines[row])) continue;
                string[] cols = ParseCsvLine(lines[row]);
                if (cols.Length == 0) continue;
                if (!int.TryParse(GetColumn(cols, headerIndex["关卡编号"]), out int levelNumber) || levelNumber <= 0) continue;

                LevelData levelData = GetOrCreateLevelData(levelsFolder, levelNumber);
                var mix = new List<LevelData.LevelSpawnEntry>();
                for (int i = 1; i <= 3; i++)
                {
                    if (!headerIndex.TryGetValue($"Enemy{i}Id", out int idIdx)) continue;
                    string enemyId = GetColumn(cols, idIdx).Trim();
                    if (string.IsNullOrEmpty(enemyId)) continue;

                    EnemyData enemyData = FindEnemyDataById(enemyId);
                    if (enemyData == null) { Debug.LogWarning($"[Level-SpawnMix] 未找到：{enemyId} 行{row + 1}"); continue; }

                    var entry = new LevelData.LevelSpawnEntry();
                    entry.enemyData = enemyData;
                    if (headerIndex.TryGetValue($"Enemy{i}Weight", out int wIdx) && float.TryParse(GetColumn(cols, wIdx), NumberStyles.Float, CultureInfo.InvariantCulture, out float w)) entry.spawnWeight = w;
                    if (headerIndex.TryGetValue($"Enemy{i}MaxAlive", out int maIdx) && int.TryParse(GetColumn(cols, maIdx), out int ma)) entry.maxAlive = ma;
                    if (headerIndex.TryGetValue($"Enemy{i}Start", out int stIdx) && float.TryParse(GetColumn(cols, stIdx), NumberStyles.Float, CultureInfo.InvariantCulture, out float st)) entry.spawnTimeStart = st;
                    if (headerIndex.TryGetValue($"Enemy{i}End", out int edIdx) && float.TryParse(GetColumn(cols, edIdx), NumberStyles.Float, CultureInfo.InvariantCulture, out float ed)) entry.spawnTimeEnd = ed;
                    mix.Add(entry);
                }
                levelData.spawnMix = mix.ToArray();
                EditorUtility.SetDirty(levelData);
                success++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[EggRogueBalanceImporter] Level-SpawnMix 导入完成：成功 {success} 条");
        }
        catch (Exception e)
        {
            Debug.LogError($"[EggRogueBalanceImporter] Level-SpawnMix 导入失败：{e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 从 Level-CardWeight.csv 导入卡牌等级权重。
    /// 表头：关卡编号,W_Lv1,W_Lv2,W_Lv3,W_Lv4,W_Lv5
    /// </summary>
    private static void ImportLevelCardWeightFromCsv(string csvPath, string levelsFolder)
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[EggRogueBalanceImporter] Level-CardWeight CSV 不存在：{csvPath}");
            return;
        }
        try
        {
            string fileContent = ReadCsvFile(csvPath);
            string[] lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (lines.Length < 2)
            {
                Debug.LogError("[EggRogueBalanceImporter] Level-CardWeight CSV 至少需要 2 行。");
                return;
            }
            string[] headers = ParseCsvLine(lines[0]);
            var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                string h = headers[i].Trim();
                if (!string.IsNullOrEmpty(h) && !headerIndex.ContainsKey(h)) headerIndex[h] = i;
            }
            if (!headerIndex.ContainsKey("关卡编号"))
            {
                Debug.LogError("[EggRogueBalanceImporter] Level-CardWeight 缺少表头：关卡编号");
                return;
            }
            int success = 0;
            var inv = CultureInfo.InvariantCulture;
            for (int row = 1; row < lines.Length; row++)
            {
                if (string.IsNullOrWhiteSpace(lines[row])) continue;
                string[] cols = ParseCsvLine(lines[row]);
                if (cols.Length == 0) continue;
                if (!int.TryParse(GetColumn(cols, headerIndex["关卡编号"]), out int levelNumber) || levelNumber <= 0) continue;

                LevelData levelData = GetOrCreateLevelData(levelsFolder, levelNumber);
                if (levelData.cardLevelWeights == null || levelData.cardLevelWeights.Length < 5)
                    levelData.cardLevelWeights = new float[5];
                for (int i = 1; i <= 5; i++)
                {
                    string key = $"W_Lv{i}";
                    if (headerIndex.TryGetValue(key, out int idx) && float.TryParse(GetColumn(cols, idx), NumberStyles.Float, inv, out float w))
                        levelData.cardLevelWeights[i - 1] = w;
                }
                EditorUtility.SetDirty(levelData);
                success++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[EggRogueBalanceImporter] Level-CardWeight 导入完成：成功 {success} 条");
        }
        catch (Exception e)
        {
            Debug.LogError($"[EggRogueBalanceImporter] Level-CardWeight 导入失败：{e.Message}\n{e.StackTrace}");
        }
    }

    #endregion

    #region 怪物导入

    /// <summary>
    /// 从 CSV 导入 EnemyData。
    /// 约定表头：
    /// - Id            （用于匹配/创建资源名，例如 Enemy01）【必填】
    /// - 名称          -> enemyName
    /// - 描述          -> description
    /// - 基础生命      -> baseMaxHealth
    /// - 基础移速      -> baseMoveSpeed
    /// - 基础伤害      -> baseDamage
    /// - 经验值        -> xpValue
    /// - 掉落金币最小  -> coinDropMin
    /// - 掉落金币最大  -> coinDropMax
    /// </summary>
    private static void ImportEnemiesFromCsv(string csvPath, string enemiesFolder)
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[EggRogueBalanceImporter] 怪物 CSV 文件不存在：{csvPath}");
            return;
        }

        try
        {
            string fileContent = ReadCsvFile(csvPath);
            string[] lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (lines.Length < 2)
            {
                Debug.LogError("[EggRogueBalanceImporter] 怪物 CSV 文件至少需要 2 行（表头 + 数据）。");
                return;
            }

            string[] headers = ParseCsvLine(lines[0]);
            var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                string h = headers[i].Trim();
                if (!string.IsNullOrEmpty(h) && !headerIndex.ContainsKey(h))
                    headerIndex[h] = i;
            }

            if (!headerIndex.ContainsKey("Id"))
            {
                Debug.LogError("[EggRogueBalanceImporter] 怪物 CSV 缺少必需表头：Id");
                return;
            }

            if (!AssetDatabase.IsValidFolder(enemiesFolder))
            {
                Directory.CreateDirectory(enemiesFolder);
                AssetDatabase.Refresh();
            }

            int success = 0;
            int errors = 0;
            List<string> errorLines = new List<string>();

            for (int row = 1; row < lines.Length; row++)
            {
                if (string.IsNullOrWhiteSpace(lines[row]))
                    continue;

                try
                {
                    string[] cols = ParseCsvLine(lines[row]);
                    if (cols.Length == 0) continue;

                    string id = GetColumn(cols, headerIndex["Id"]).Trim();
                    if (string.IsNullOrEmpty(id))
                        throw new Exception("Id 为空");

                    string assetPath = $"{enemiesFolder}/{id}.asset";
                    EnemyData enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(assetPath);
                    bool isNew = false;
                    if (enemy == null)
                    {
                        enemy = ScriptableObject.CreateInstance<EnemyData>();
                        isNew = true;
                    }

                    // 填字段
                    if (headerIndex.TryGetValue("名称", out int nameIdx))
                        enemy.enemyName = GetColumn(cols, nameIdx);

                    if (headerIndex.TryGetValue("描述", out int descIdx))
                        enemy.description = GetColumn(cols, descIdx);

                    if (headerIndex.TryGetValue("基础生命", out int hpIdx))
                    {
                        string v = GetColumn(cols, hpIdx);
                        if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float hp))
                            enemy.baseMaxHealth = hp;
                    }

                    if (headerIndex.TryGetValue("基础移速", out int msIdx))
                    {
                        string v = GetColumn(cols, msIdx);
                        if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float ms))
                            enemy.baseMoveSpeed = ms;
                    }

                    if (headerIndex.TryGetValue("基础伤害", out int dmgIdx))
                    {
                        string v = GetColumn(cols, dmgIdx);
                        if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float dmg))
                            enemy.baseDamage = dmg;
                    }

                    if (headerIndex.TryGetValue("经验值", out int xpIdx))
                    {
                        string v = GetColumn(cols, xpIdx);
                        if (int.TryParse(v, out int xp))
                            enemy.xpValue = xp;
                    }

                    if (headerIndex.TryGetValue("掉落金币最小", out int cminIdx))
                    {
                        string v = GetColumn(cols, cminIdx);
                        if (int.TryParse(v, out int cmin))
                            enemy.coinDropMin = cmin;
                    }

                    if (headerIndex.TryGetValue("掉落金币最大", out int cmaxIdx))
                    {
                        string v = GetColumn(cols, cmaxIdx);
                        if (int.TryParse(v, out int cmax))
                            enemy.coinDropMax = cmax;
                    }

                    // 保存
                    if (isNew)
                        AssetDatabase.CreateAsset(enemy, assetPath);
                    else
                        EditorUtility.SetDirty(enemy);

                    success++;
                }
                catch (Exception ex)
                {
                    errors++;
                    errorLines.Add($"第 {row + 1} 行: {ex.Message}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string msg = $"[EggRogueBalanceImporter] 怪物导入完成：成功 {success} 条，错误 {errors} 条";
            if (errorLines.Count > 0)
            {
                msg += "\n前几条错误：\n" + string.Join("\n", errorLines.GetRange(0, Math.Min(5, errorLines.Count)));
            }
            Debug.Log(msg);
        }
        catch (Exception e)
        {
            Debug.LogError($"[EggRogueBalanceImporter] 怪物导入失败：{e.Message}\n{e.StackTrace}");
        }
    }

    #endregion

    #region 角色导入

    /// <summary>
    /// 从 CSV 导入 CharacterData。
    /// 约定表头：
    /// - AssetName      （用于匹配资源名，例如 Character_爱因斯蛋）【必填】
    /// - 角色名称        -> characterName
    /// - 描述            -> description
    /// - 基础等级        -> baseLevel
    /// - 基础伤害        -> baseDamage
    /// - 基础攻速        -> baseFireRate
    /// - 基础生命        -> baseMaxHealth
    /// - 基础移速        -> baseMoveSpeed
    /// - 基础子弹速度    -> baseBulletSpeed
    /// - 基础攻击范围    -> baseAttackRange
    /// - 基础拾取范围    -> basePickupRange
    /// </summary>
    private static void ImportCharactersFromCsv(string csvPath, string charactersFolder)
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[EggRogueBalanceImporter] 角色 CSV 文件不存在：{csvPath}");
            return;
        }

        try
        {
            string fileContent = ReadCsvFile(csvPath);
            string[] lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (lines.Length < 2)
            {
                Debug.LogError("[EggRogueBalanceImporter] 角色 CSV 文件至少需要 2 行（表头 + 数据）。");
                return;
            }

            string[] headers = ParseCsvLine(lines[0]);
            var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                string h = headers[i].Trim();
                if (!string.IsNullOrEmpty(h) && !headerIndex.ContainsKey(h))
                    headerIndex[h] = i;
            }

            if (!headerIndex.ContainsKey("AssetName"))
            {
                Debug.LogError("[EggRogueBalanceImporter] 角色 CSV 缺少必需表头：AssetName");
                return;
            }

            if (!AssetDatabase.IsValidFolder(charactersFolder))
            {
                Directory.CreateDirectory(charactersFolder);
                AssetDatabase.Refresh();
            }

            int success = 0;
            int errors = 0;
            List<string> errorLines = new List<string>();

            for (int row = 1; row < lines.Length; row++)
            {
                if (string.IsNullOrWhiteSpace(lines[row]))
                    continue;

                try
                {
                    string[] cols = ParseCsvLine(lines[row]);
                    if (cols.Length == 0) continue;

                    string assetName = GetColumn(cols, headerIndex["AssetName"]).Trim();
                    if (string.IsNullOrEmpty(assetName))
                        throw new Exception("AssetName 为空");

                    string assetPath = $"{charactersFolder}/{assetName}.asset";
                    CharacterData character = AssetDatabase.LoadAssetAtPath<CharacterData>(assetPath);
                    if (character == null)
                        throw new Exception($"未找到角色资源：{assetPath}（目前只支持更新已有角色，不自动创建）");

                    if (headerIndex.TryGetValue("角色名称", out int nameIdx))
                        character.characterName = GetColumn(cols, nameIdx);

                    if (headerIndex.TryGetValue("描述", out int descIdx))
                        character.description = GetColumn(cols, descIdx);

                    if (headerIndex.TryGetValue("基础等级", out int lvlIdx))
                    {
                        string v = GetColumn(cols, lvlIdx);
                        if (int.TryParse(v, out int lvl))
                            character.baseLevel = lvl;
                    }

                    if (headerIndex.TryGetValue("基础伤害", out int dmgIdx))
                    {
                        string v = GetColumn(cols, dmgIdx);
                        if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float dmg))
                            character.baseDamage = dmg;
                    }

                    if (headerIndex.TryGetValue("基础攻速", out int frIdx))
                    {
                        string v = GetColumn(cols, frIdx);
                        if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float fr))
                            character.baseFireRate = fr;
                    }

                    if (headerIndex.TryGetValue("基础生命", out int hpIdx))
                    {
                        string v = GetColumn(cols, hpIdx);
                        if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float hp))
                            character.baseMaxHealth = hp;
                    }

                    if (headerIndex.TryGetValue("基础移速", out int msIdx))
                    {
                        string v = GetColumn(cols, msIdx);
                        if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float ms))
                            character.baseMoveSpeed = ms;
                    }

                    if (headerIndex.TryGetValue("基础子弹速度", out int bsIdx))
                    {
                        string v = GetColumn(cols, bsIdx);
                        if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float bs))
                            character.baseBulletSpeed = bs;
                    }

                    if (headerIndex.TryGetValue("基础攻击范围", out int arIdx))
                    {
                        string v = GetColumn(cols, arIdx);
                        if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float ar))
                            character.baseAttackRange = ar;
                    }

                    if (headerIndex.TryGetValue("基础拾取范围", out int prIdx))
                    {
                        string v = GetColumn(cols, prIdx);
                        if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float pr))
                            character.basePickupRange = pr;
                    }

                    EditorUtility.SetDirty(character);
                    success++;
                }
                catch (Exception ex)
                {
                    errors++;
                    errorLines.Add($"第 {row + 1} 行: {ex.Message}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string msg = $"[EggRogueBalanceImporter] 角色导入完成：成功 {success} 条，错误 {errors} 条";
            if (errorLines.Count > 0)
            {
                msg += "\n前几条错误：\n" + string.Join("\n", errorLines.GetRange(0, Math.Min(5, errorLines.Count)));
            }
            Debug.Log(msg);
        }
        catch (Exception e)
        {
            Debug.LogError($"[EggRogueBalanceImporter] 角色导入失败：{e.Message}\n{e.StackTrace}");
        }
    }

    #endregion

    #region 武器导入

    /// <summary>
    /// 从 CSV 导入 WeaponData。
    /// 约定表头：
    /// - AssetName      （用于匹配/创建资源名，例如 Weapon_Gun_Lv1）【必填】
    /// - WeaponId      -> weaponId
    /// - 名称           -> weaponName
    /// - 类型           -> weaponType（Ranged/Melee 或 远程/近战）
    /// - 等级           -> level
    /// - 基础价格       -> basePrice
    /// - 伤害           -> damage
    /// - 攻速           -> fireRate
    /// - 攻击范围       -> attackRange
    /// - 子弹速度       -> bulletSpeed
    /// - 子弹寿命       -> bulletLifeTime
    /// - 下一等级Asset   -> nextLevelWeapon（按资源名查找）
    /// </summary>
    private static void ImportWeaponsFromCsv(string csvPath, string weaponsFolder)
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[EggRogueBalanceImporter] 武器 CSV 文件不存在：{csvPath}");
            return;
        }

        try
        {
            string fileContent = ReadCsvFile(csvPath);
            string[] lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (lines.Length < 2)
            {
                Debug.LogError("[EggRogueBalanceImporter] 武器 CSV 文件至少需要 2 行（表头 + 数据）。");
                return;
            }

            string[] headers = ParseCsvLine(lines[0]);
            var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                string h = headers[i].Trim();
                if (!string.IsNullOrEmpty(h) && !headerIndex.ContainsKey(h))
                    headerIndex[h] = i;
            }

            if (!headerIndex.ContainsKey("AssetName"))
            {
                Debug.LogError("[EggRogueBalanceImporter] 武器 CSV 缺少必需表头：AssetName");
                return;
            }

            if (!AssetDatabase.IsValidFolder(weaponsFolder))
            {
                Directory.CreateDirectory(weaponsFolder);
                AssetDatabase.Refresh();
            }

            int success = 0;
            int errors = 0;
            List<string> errorLines = new List<string>();

            var inv = CultureInfo.InvariantCulture;

            for (int row = 1; row < lines.Length; row++)
            {
                if (string.IsNullOrWhiteSpace(lines[row]))
                    continue;

                try
                {
                    string[] cols = ParseCsvLine(lines[row]);
                    if (cols.Length == 0) continue;

                    string assetName = GetColumn(cols, headerIndex["AssetName"]).Trim();
                    if (string.IsNullOrEmpty(assetName))
                        throw new Exception("AssetName 为空");

                    string assetPath = $"{weaponsFolder}/{assetName}.asset";
                    WeaponData weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(assetPath);
                    bool isNew = false;
                    if (weapon == null)
                    {
                        weapon = ScriptableObject.CreateInstance<WeaponData>();
                        isNew = true;
                    }

                    // weaponId
                    if (headerIndex.TryGetValue("WeaponId", out int idIdx))
                    {
                        string v = GetColumn(cols, idIdx);
                        if (!string.IsNullOrEmpty(v))
                            weapon.weaponId = v;
                    }

                    // 名称
                    if (headerIndex.TryGetValue("名称", out int nameIdx))
                        weapon.weaponName = GetColumn(cols, nameIdx);

                    // 类型
                    if (headerIndex.TryGetValue("类型", out int typeIdx))
                    {
                        string v = GetColumn(cols, typeIdx);
                        if (!string.IsNullOrEmpty(v))
                        {
                            if (v.Equals("Ranged", StringComparison.OrdinalIgnoreCase) || v.Contains("远程"))
                                weapon.weaponType = WeaponType.Ranged;
                            else if (v.Equals("Melee", StringComparison.OrdinalIgnoreCase) || v.Contains("近战"))
                                weapon.weaponType = WeaponType.Melee;
                        }
                    }

                    // 等级
                    if (headerIndex.TryGetValue("等级", out int lvlIdx))
                    {
                        string v = GetColumn(cols, lvlIdx);
                        if (int.TryParse(v, out int lvl))
                            weapon.level = lvl;
                    }

                    // 基础价格
                    if (headerIndex.TryGetValue("基础价格", out int priceIdx))
                    {
                        string v = GetColumn(cols, priceIdx);
                        if (int.TryParse(v, out int price))
                            weapon.basePrice = price;
                    }

                    // 数值属性
                    if (headerIndex.TryGetValue("伤害", out int dmgIdx))
                    {
                        string v = GetColumn(cols, dmgIdx);
                        if (float.TryParse(v, NumberStyles.Float, inv, out float dmg))
                            weapon.damage = dmg;
                    }

                    if (headerIndex.TryGetValue("攻速", out int frIdx))
                    {
                        string v = GetColumn(cols, frIdx);
                        if (float.TryParse(v, NumberStyles.Float, inv, out float fr))
                            weapon.fireRate = fr;
                    }

                    if (headerIndex.TryGetValue("攻击范围", out int arIdx))
                    {
                        string v = GetColumn(cols, arIdx);
                        if (float.TryParse(v, NumberStyles.Float, inv, out float ar))
                            weapon.attackRange = ar;
                    }

                    if (headerIndex.TryGetValue("子弹速度", out int bsIdx))
                    {
                        string v = GetColumn(cols, bsIdx);
                        if (float.TryParse(v, NumberStyles.Float, inv, out float bs))
                            weapon.bulletSpeed = bs;
                    }

                    if (headerIndex.TryGetValue("子弹寿命", out int blIdx))
                    {
                        string v = GetColumn(cols, blIdx);
                        if (float.TryParse(v, NumberStyles.Float, inv, out float bl))
                            weapon.bulletLifeTime = bl;
                    }

                    // 下一等级武器
                    if (headerIndex.TryGetValue("下一等级Asset", out int nextIdx))
                    {
                        string nextName = GetColumn(cols, nextIdx);
                        if (!string.IsNullOrEmpty(nextName))
                        {
                            string nextPath = $"{weaponsFolder}/{nextName}.asset";
                            WeaponData next = AssetDatabase.LoadAssetAtPath<WeaponData>(nextPath);
                            if (next == null)
                            {
                                // 兜底：全局搜一下
                                string[] guids = AssetDatabase.FindAssets($"{nextName} t:EggRogue.WeaponData");
                                foreach (string guid in guids)
                                {
                                    string p = AssetDatabase.GUIDToAssetPath(guid);
                                    next = AssetDatabase.LoadAssetAtPath<WeaponData>(p);
                                    if (next != null) break;
                                }
                            }
                            weapon.nextLevelWeapon = next;
                        }
                    }

                    // 保存
                    if (isNew)
                        AssetDatabase.CreateAsset(weapon, assetPath);
                    else
                        EditorUtility.SetDirty(weapon);

                    success++;
                }
                catch (Exception ex)
                {
                    errors++;
                    errorLines.Add($"第 {row + 1} 行: {ex.Message}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string msg = $"[EggRogueBalanceImporter] 武器导入完成：成功 {success} 条，错误 {errors} 条";
            if (errorLines.Count > 0)
            {
                msg += "\n前几条错误：\n" + string.Join("\n", errorLines.GetRange(0, Math.Min(5, errorLines.Count)));
            }
            Debug.Log(msg);
        }
        catch (Exception e)
        {
            Debug.LogError($"[EggRogueBalanceImporter] 武器导入失败：{e.Message}\n{e.StackTrace}");
        }
    }

    #endregion

    #region 卡片导入

    /// <summary>
    /// 从 CSV 导入 CardData（方案2：等级加成表）。
    /// 约定表头：cardTypeId, level, 卡片名称, 描述, 伤害加成, 攻速加成, 生命加成, 移速加成, 子弹速度加成, 攻击范围加成, 拾取范围加成
    /// 每行对应一个 (cardTypeId, level) 的等级加成；多行相同 cardTypeId 合并为一张卡。
    /// </summary>
    private static void ImportCardsFromCsv(string csvPath, string cardsFolder)
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"[EggRogueBalanceImporter] 卡片 CSV 文件不存在：{csvPath}");
            return;
        }

        try
        {
            string fileContent = ReadCsvFile(csvPath);
            string[] lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (lines.Length < 2)
            {
                Debug.LogError("[EggRogueBalanceImporter] 卡片 CSV 文件至少需要 2 行（表头 + 数据）。");
                return;
            }

            string[] headers = ParseCsvLine(lines[0]);
            var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                string h = headers[i].Trim();
                if (!string.IsNullOrEmpty(h) && !headerIndex.ContainsKey(h))
                    headerIndex[h] = i;
            }

            if (!headerIndex.ContainsKey("cardTypeId") && !headerIndex.ContainsKey("CardTypeId"))
            {
                Debug.LogError("[EggRogueBalanceImporter] 卡片 CSV 缺少必需表头：cardTypeId");
                return;
            }
            if (!headerIndex.ContainsKey("level") && !headerIndex.ContainsKey("等级"))
            {
                Debug.LogError("[EggRogueBalanceImporter] 卡片 CSV 缺少必需表头：level");
                return;
            }

            if (!AssetDatabase.IsValidFolder(cardsFolder))
            {
                Directory.CreateDirectory(cardsFolder);
                AssetDatabase.Refresh();
            }

            var cardByTypeId = new Dictionary<string, CardData>();
            int success = 0;
            int errors = 0;
            List<string> errorLines = new List<string>();
            var inv = CultureInfo.InvariantCulture;

            for (int row = 1; row < lines.Length; row++)
            {
                if (string.IsNullOrWhiteSpace(lines[row]))
                    continue;

                try
                {
                    string[] cols = ParseCsvLine(lines[row]);
                    if (cols.Length == 0) continue;

                    int ctIdx = headerIndex.TryGetValue("cardTypeId", out int c) ? c : headerIndex["CardTypeId"];
                    string cardTypeId = GetColumn(cols, ctIdx).Trim();
                    if (string.IsNullOrEmpty(cardTypeId))
                        throw new Exception("cardTypeId 为空");

                    int lvIdx = headerIndex.TryGetValue("level", out int l) ? l : headerIndex["等级"];
                    if (!int.TryParse(GetColumn(cols, lvIdx), out int lv))
                        throw new Exception("level 无法解析");
                    lv = Mathf.Clamp(lv, 1, 5);

                    string assetPath = $"{cardsFolder}/{cardTypeId}.asset";
                    if (headerIndex.TryGetValue("AssetName", out int anIdx))
                    {
                        string an = GetColumn(cols, anIdx).Trim();
                        if (!string.IsNullOrEmpty(an))
                            assetPath = $"{cardsFolder}/{an}.asset";
                    }

                    if (!cardByTypeId.TryGetValue(cardTypeId, out CardData card))
                    {
                        card = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
                        if (card == null)
                        {
                            card = ScriptableObject.CreateInstance<CardData>();
                            AssetDatabase.CreateAsset(card, assetPath);
                        }
                        card.cardTypeId = cardTypeId;
                        if (card.levelBonuses == null || card.levelBonuses.Length < 5)
                            card.levelBonuses = new CardLevelBonus[5];
                        cardByTypeId[cardTypeId] = card;
                    }

                    if (headerIndex.TryGetValue("卡片名称", out int nameIdx))
                    {
                        string name = GetColumn(cols, nameIdx);
                        if (!string.IsNullOrEmpty(name))
                            card.cardName = name;
                    }
                    if (headerIndex.TryGetValue("描述", out int descIdx))
                    {
                        string desc = GetColumn(cols, descIdx);
                        if (!string.IsNullOrEmpty(desc))
                            card.description = desc;
                    }

                    int idx = lv - 1;
                    ref var bonus = ref card.levelBonuses[idx];
                    bonus.level = lv;
                    if (headerIndex.TryGetValue("伤害加成", out int dmgIdx) && float.TryParse(GetColumn(cols, dmgIdx), NumberStyles.Float, inv, out float dmg))
                        bonus.damageBonus = dmg;
                    if (headerIndex.TryGetValue("攻速加成", out int frIdx) && float.TryParse(GetColumn(cols, frIdx), NumberStyles.Float, inv, out float fr))
                        bonus.fireRateBonus = fr;
                    if (headerIndex.TryGetValue("生命加成", out int hpIdx) && float.TryParse(GetColumn(cols, hpIdx), NumberStyles.Float, inv, out float hp))
                        bonus.maxHealthBonus = hp;
                    if (headerIndex.TryGetValue("移速加成", out int msIdx) && float.TryParse(GetColumn(cols, msIdx), NumberStyles.Float, inv, out float ms))
                        bonus.moveSpeedBonus = ms;
                    if (headerIndex.TryGetValue("子弹速度加成", out int bsIdx) && float.TryParse(GetColumn(cols, bsIdx), NumberStyles.Float, inv, out float bs))
                        bonus.bulletSpeedBonus = bs;
                    if (headerIndex.TryGetValue("攻击范围加成", out int arIdx) && float.TryParse(GetColumn(cols, arIdx), NumberStyles.Float, inv, out float ar))
                        bonus.attackRangeBonus = ar;
                    if (headerIndex.TryGetValue("拾取范围加成", out int prIdx) && float.TryParse(GetColumn(cols, prIdx), NumberStyles.Float, inv, out float pr))
                        bonus.pickupRangeBonus = pr;

                    EditorUtility.SetDirty(card);
                    success++;
                }
                catch (Exception ex)
                {
                    errors++;
                    errorLines.Add($"第 {row + 1} 行: {ex.Message}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var db = AssetDatabase.LoadAssetAtPath<CardDatabase>("Assets/EggRogue/Configs/CardDatabase.asset");
            if (db != null)
            {
                string[] guids = AssetDatabase.FindAssets("t:EggRogue.CardData", new[] { cardsFolder });
                var list = new List<CardData>();
                foreach (string guid in guids)
                {
                    var c = AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(guid));
                    if (c != null) list.Add(c);
                }
                db.allCards = list.ToArray();
                EditorUtility.SetDirty(db);
                AssetDatabase.SaveAssets();
            }

            string msg = $"[EggRogueBalanceImporter] 卡片导入完成：成功 {success} 条，错误 {errors} 条";
            if (errorLines.Count > 0)
                msg += "\n前几条错误：\n" + string.Join("\n", errorLines.GetRange(0, Math.Min(5, errorLines.Count)));
            Debug.Log(msg);
        }
        catch (Exception e)
        {
            Debug.LogError($"[EggRogueBalanceImporter] 卡片导入失败：{e.Message}\n{e.StackTrace}");
        }
    }

    #endregion

    #region 工具方法

    private static string ReadCsvFile(string path)
    {
        byte[] fileBytes = File.ReadAllBytes(path);
        string fileContent;

        // UTF-8 BOM (EF BB BF)
        if (fileBytes.Length >= 3 && fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF)
        {
            fileContent = System.Text.Encoding.UTF8.GetString(fileBytes, 3, fileBytes.Length - 3);
        }
        else
        {
            try
            {
                fileContent = System.Text.Encoding.UTF8.GetString(fileBytes);
            }
            catch
            {
                fileContent = System.Text.Encoding.Default.GetString(fileBytes);
            }
        }

        return fileContent;
    }

    /// <summary>
    /// 安全获取某列内容（越界则返回空字符串）
    /// </summary>
    private static string GetColumn(string[] cols, int index)
    {
        if (index < 0 || index >= cols.Length) return string.Empty;
        return cols[index]?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// 简单 CSV 行解析（支持引号、逗号）。
    /// </summary>
    private static string[] ParseCsvLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string current = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current += '"';
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }

        result.Add(current);
        return result.ToArray();
    }

    /// <summary>
    /// 根据 Id（通常为资源名，如 Enemy01）查找 EnemyData。
    /// </summary>
    private static EnemyData FindEnemyDataById(string id)
    {
        string path = $"Assets/EggRogue/Configs/Enemies/{id}.asset";
        var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        if (data != null) return data;
        // 兜底：全局搜索
        string[] guids = AssetDatabase.FindAssets($"{id} t:EnemyData");
        foreach (string guid in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var d = AssetDatabase.LoadAssetAtPath<EnemyData>(p);
            if (d != null) return d;
        }
        return null;
    }

    #endregion
}

