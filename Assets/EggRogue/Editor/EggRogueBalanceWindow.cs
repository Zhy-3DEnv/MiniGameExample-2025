using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// EggRogue 数值配置工具面板。
///
/// 功能：
/// - 一键生成 CSV 模板（Levels / Enemies / Characters / Weapons / Cards），方便在 Excel 中编辑。
/// - 配置 CSV 路径。
/// - 单独导入关卡 / 怪物 / 角色 / 武器 / 卡片，或一键导入全部。
/// - 一键导出全部（所有 SO → CSV）。
///
/// 注意：
/// - 运行时游戏仍然只使用 ScriptableObject，不依赖 CSV。
/// - 建议将 CSV 放在项目内，例如：Assets/EggRogue/Configs/Excel/ 目录。
/// </summary>
public class EggRogueBalanceWindow : EditorWindow
{
    private const string DefaultCsvFolder = "Assets/EggRogue/Configs/Excel";

    private string _levelBaseCsvPath = DefaultCsvFolder + "/Level-Base.csv";
    private string _levelSpawnMixCsvPath = DefaultCsvFolder + "/Level-SpawnMix.csv";
    private string _levelCardWeightCsvPath = DefaultCsvFolder + "/Level-CardWeight.csv";
    private string _enemiesCsvPath = DefaultCsvFolder + "/EggRogue_Enemies.csv";
    private string _charactersCsvPath = DefaultCsvFolder + "/EggRogue_Characters.csv";
    private string _weaponsCsvPath = DefaultCsvFolder + "/EggRogue_Weapons.csv";
    private string _cardsCsvPath = DefaultCsvFolder + "/EggRogue_Cards.csv";

    [MenuItem("EggRogue/Excel/数值配置面板")]
    public static void ShowWindow()
    {
        var win = GetWindow<EggRogueBalanceWindow>("EggRogue 数值配置");
        win.minSize = new Vector2(500, 320);
    }

    private void OnEnable()
    {
        // 尝试从 EditorPrefs 读取上次使用的路径
        _levelBaseCsvPath = EditorPrefs.GetString("EggRogue.LevelBaseCsvPath", _levelBaseCsvPath);
        _levelSpawnMixCsvPath = EditorPrefs.GetString("EggRogue.LevelSpawnMixCsvPath", _levelSpawnMixCsvPath);
        _levelCardWeightCsvPath = EditorPrefs.GetString("EggRogue.LevelCardWeightCsvPath", _levelCardWeightCsvPath);
        _enemiesCsvPath = EditorPrefs.GetString("EggRogue.EnemiesCsvPath", _enemiesCsvPath);
        _charactersCsvPath = EditorPrefs.GetString("EggRogue.CharactersCsvPath", _charactersCsvPath);
        _weaponsCsvPath = EditorPrefs.GetString("EggRogue.WeaponsCsvPath", _weaponsCsvPath);
        _cardsCsvPath = EditorPrefs.GetString("EggRogue.CardsCsvPath", _cardsCsvPath);
    }

    private void OnDisable()
    {
        EditorPrefs.SetString("EggRogue.LevelBaseCsvPath", _levelBaseCsvPath);
        EditorPrefs.SetString("EggRogue.LevelSpawnMixCsvPath", _levelSpawnMixCsvPath);
        EditorPrefs.SetString("EggRogue.LevelCardWeightCsvPath", _levelCardWeightCsvPath);
        EditorPrefs.SetString("EggRogue.EnemiesCsvPath", _enemiesCsvPath);
        EditorPrefs.SetString("EggRogue.CharactersCsvPath", _charactersCsvPath);
        EditorPrefs.SetString("EggRogue.WeaponsCsvPath", _weaponsCsvPath);
        EditorPrefs.SetString("EggRogue.CardsCsvPath", _cardsCsvPath);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("EggRogue 数值配置（Excel → CSV → ScriptableObject）", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        DrawTemplateSection();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("CSV 路径配置", EditorStyles.boldLabel);

        DrawCsvPathField("Level-Base", ref _levelBaseCsvPath);
        DrawCsvPathField("Level-SpawnMix", ref _levelSpawnMixCsvPath);
        DrawCsvPathField("Level-CardWeight", ref _levelCardWeightCsvPath);
        DrawCsvPathField("怪物 CSV", ref _enemiesCsvPath);
        DrawCsvPathField("角色 CSV", ref _charactersCsvPath);
        DrawCsvPathField("武器 CSV", ref _weaponsCsvPath);
        DrawCsvPathField("卡片 CSV", ref _cardsCsvPath);

        EditorGUILayout.Space();
        DrawImportButtons();
        EditorGUILayout.Space();
        DrawExportButtons();
    }

    private void DrawTemplateSection()
    {
        EditorGUILayout.LabelField("模板生成", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "生成 CSV 模板后：\n" +
            "1. 用 Excel 打开模板（UTF-8，不会乱码）\n" +
            "2. 在对应 Sheet 中填写 Levels / Enemies / Characters / Weapons / Cards\n" +
            "3. 另存为 CSV 后，回到本面板执行导入",
            MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("生成 CSV 模板（Levels / Enemies / Characters / Weapons / Cards）", GUILayout.Height(30)))
        {
            GenerateAllTemplates();
        }
        if (GUILayout.Button("打开模板目录", GUILayout.Width(120)))
        {
            if (!Directory.Exists(DefaultCsvFolder))
            {
                Directory.CreateDirectory(DefaultCsvFolder);
                AssetDatabase.Refresh();
            }
            EditorUtility.RevealInFinder(DefaultCsvFolder);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawCsvPathField(string label, ref string path)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(80));
        path = EditorGUILayout.TextField(path);
        if (GUILayout.Button("选择", GUILayout.Width(60)))
        {
            string dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                dir = DefaultCsvFolder;
            string selected = EditorUtility.OpenFilePanel("选择 " + label + " CSV", dir, "csv");
            if (!string.IsNullOrEmpty(selected))
            {
                path = selected;
            }
        }
        if (GUILayout.Button("Ping", GUILayout.Width(50)))
        {
            var assetPath = ToProjectPath(path);
            if (!string.IsNullOrEmpty(assetPath))
            {
                var obj = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
                if (obj != null)
                    EditorGUIUtility.PingObject(obj);
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawImportButtons()
    {
        EditorGUILayout.LabelField("导入操作", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("导入 Level-Base", GUILayout.Height(25)))
        {
            ImportLevelBase();
        }
        if (GUILayout.Button("导入 Level-SpawnMix", GUILayout.Height(25)))
        {
            ImportLevelSpawnMix();
        }
        if (GUILayout.Button("导入 Level-CardWeight", GUILayout.Height(25)))
        {
            ImportLevelCardWeight();
        }
        if (GUILayout.Button("导入怪物", GUILayout.Height(25)))
        {
            ImportEnemies();
        }
        if (GUILayout.Button("导入角色", GUILayout.Height(25)))
        {
            ImportCharacters();
        }
        if (GUILayout.Button("导入武器", GUILayout.Height(25)))
        {
            ImportWeapons();
        }
        if (GUILayout.Button("导入卡片", GUILayout.Height(25)))
        {
            ImportCards();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        if (GUILayout.Button("一键导入全部（关卡 + 怪物 + 角色 + 武器 + 卡片）", GUILayout.Height(30)))
        {
            ImportAll();
        }
    }

    private void DrawExportButtons()
    {
        EditorGUILayout.LabelField("导出操作", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("将当前 ScriptableObject 导出为 CSV，导出到 " + DefaultCsvFolder + " 目录。", MessageType.None);
        if (GUILayout.Button("一键导出全部（所有 SO → CSV）", GUILayout.Height(30)))
        {
            EggRogueBalanceExporter.ExportAllMenu();
        }
    }

    #region 导入封装

    private void ImportLevelBase()
    {
        if (!CheckCsvExists(_levelBaseCsvPath, "Level-Base CSV")) return;
        EggRogueBalanceImporter.ImportLevelBaseFromCsvPath(_levelBaseCsvPath);
    }

    private void ImportLevelSpawnMix()
    {
        if (!CheckCsvExists(_levelSpawnMixCsvPath, "Level-SpawnMix CSV")) return;
        EggRogueBalanceImporter.ImportLevelSpawnMixFromCsvPath(_levelSpawnMixCsvPath);
    }

    private void ImportLevelCardWeight()
    {
        if (!CheckCsvExists(_levelCardWeightCsvPath, "Level-CardWeight CSV")) return;
        EggRogueBalanceImporter.ImportLevelCardWeightFromCsvPath(_levelCardWeightCsvPath);
    }

    private void ImportEnemies()
    {
        if (!CheckCsvExists(_enemiesCsvPath, "怪物 CSV")) return;
        EggRogueBalanceImporter.ImportEnemiesFromCsvPath(_enemiesCsvPath);
    }

    private void ImportCharacters()
    {
        if (!CheckCsvExists(_charactersCsvPath, "角色 CSV")) return;
        EggRogueBalanceImporter.ImportCharactersFromCsvPath(_charactersCsvPath);
    }

    private void ImportAll()
    {
        ImportEnemies();
        ImportLevelBase();
        ImportLevelSpawnMix();
        ImportLevelCardWeight();
        ImportCharacters();
        ImportWeapons();
        ImportCards();
    }

    private void ImportWeapons()
    {
        if (!CheckCsvExists(_weaponsCsvPath, "武器 CSV")) return;
        EggRogueBalanceImporter.ImportWeaponsFromCsvPath(_weaponsCsvPath);
    }

    private void ImportCards()
    {
        if (!CheckCsvExists(_cardsCsvPath, "卡片 CSV")) return;
        EggRogueBalanceImporter.ImportCardsFromCsvPath(_cardsCsvPath);
    }

    private bool CheckCsvExists(string path, string label)
    {
        if (string.IsNullOrEmpty(path))
        {
            EditorUtility.DisplayDialog("错误", $"{label} 路径为空，请先选择 CSV 文件。", "确定");
            return false;
        }

        if (!File.Exists(path))
        {
            EditorUtility.DisplayDialog("错误", $"{label} 不存在：\n{path}", "确定");
            return false;
        }
        return true;
    }

    #endregion

    #region 模板生成

    private void GenerateAllTemplates()
    {
        if (!Directory.Exists(DefaultCsvFolder))
        {
            Directory.CreateDirectory(DefaultCsvFolder);
            AssetDatabase.Refresh();
        }

        GenerateLevelBaseTemplate();
        GenerateLevelSpawnMixTemplate();
        GenerateLevelCardWeightTemplate();
        GenerateEnemiesTemplate();
        GenerateCharactersTemplate();
        GenerateWeaponsTemplate();
        GenerateCardsTemplate();

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("成功", "CSV 模板已生成到：\n" + DefaultCsvFolder, "确定");
    }

    private void GenerateLevelBaseTemplate()
    {
        string path = DefaultCsvFolder + "/Level-Base.csv";
        var sb = new StringBuilder();
        sb.AppendLine("关卡编号,关卡名称,每秒刷怪,最大同时怪物数,最少总怪物,最多总怪物,随机偏移半径,刷怪开始时间,刷怪结束时间,关卡时长,胜利奖励金币,血量倍率,移速倍率,金币倍率");
        sb.AppendLine("1,第1关,5,0,15,500,2,0,0,20,10,1,1,1");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        _levelBaseCsvPath = path;
    }

    private void GenerateLevelSpawnMixTemplate()
    {
        string path = DefaultCsvFolder + "/Level-SpawnMix.csv";
        var sb = new StringBuilder();
        sb.AppendLine("关卡编号,Enemy1Id,Enemy1Weight,Enemy1MaxAlive,Enemy1Start,Enemy1End,Enemy2Id,Enemy2Weight,Enemy2MaxAlive,Enemy2Start,Enemy2End,Enemy3Id,Enemy3Weight,Enemy3MaxAlive,Enemy3Start,Enemy3End");
        sb.AppendLine("1,Enemy01,1,0,0,0,,,,,,,,,,,");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        _levelSpawnMixCsvPath = path;
    }

    private void GenerateLevelCardWeightTemplate()
    {
        string path = DefaultCsvFolder + "/Level-CardWeight.csv";
        var sb = new StringBuilder();
        sb.AppendLine("关卡编号,W_Lv1,W_Lv2,W_Lv3,W_Lv4,W_Lv5");
        sb.AppendLine("1,10,4,2,1,0");
        sb.AppendLine("5,4,6,6,3,1");
        sb.AppendLine("10,2,3,5,6,4");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        _levelCardWeightCsvPath = path;
    }

    private void GenerateEnemiesTemplate()
    {
        string path = DefaultCsvFolder + "/EggRogue_Enemies.csv";
        var sb = new StringBuilder();
        sb.AppendLine("Id,名称,描述,基础生命,基础移速,基础伤害,经验值,掉落金币最小,掉落金币最大");
        sb.AppendLine("Enemy01,基础怪,基础敌人,20,2,1,1,1,1");
        sb.AppendLine("Enemy02,快速怪,移动更快的敌人,20,3,1,2,1,2");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        _enemiesCsvPath = path;
    }

    private void GenerateCharactersTemplate()
    {
        string path = DefaultCsvFolder + "/EggRogue_Characters.csv";
        var sb = new StringBuilder();
        sb.AppendLine("AssetName,角色名称,描述,基础等级,基础伤害,基础攻速,基础生命,基础移速,基础子弹速度,基础攻击范围,基础拾取范围");
        sb.AppendLine("Character_爱因斯蛋,爱因斯蛋,远程型角色,1,11,1.6,85,8,22,2,1.5");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        _charactersCsvPath = path;
    }

    private void GenerateWeaponsTemplate()
    {
        string path = DefaultCsvFolder + "/EggRogue_Weapons.csv";
        var sb = new StringBuilder();
        sb.AppendLine("AssetName,WeaponId,名称,类型,等级,基础价格,伤害,攻速,攻击范围,子弹速度,子弹寿命,下一等级Asset");
        sb.AppendLine("Weapon_Gun_Lv1,gun_lv1,手枪,Ranged,1,50,10,2,10,20,5,Weapon_Gun_Lv2");
        sb.AppendLine("Weapon_Knife_Lv1,knife_lv1,小刀,Melee,1,40,12,1.5,2,0,0,Weapon_Knife_Lv2");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        _weaponsCsvPath = path;
    }

    private void GenerateCardsTemplate()
    {
        string path = DefaultCsvFolder + "/EggRogue_Cards.csv";
        var sb = new StringBuilder();
        sb.AppendLine("AssetName,cardTypeId,level,卡片名称,描述,伤害加成,攻速加成,生命加成,移速加成,子弹速度加成,攻击范围加成,拾取范围加成");
        sb.AppendLine("Card_力量提升_Lv1,力量提升,1,力量提升 Lv1,伤害 +5,5,0,0,0,0,0,0");
        sb.AppendLine("Card_攻速提升_Lv1,攻速提升,1,攻速提升 Lv1,攻击速度 +1,0,1,0,0,0,0,0");
        sb.AppendLine("Card_生命提升_Lv1,生命提升,1,生命提升 Lv1,最大生命值 +20,0,0,20,0,0,0,0");
        sb.AppendLine("Card_移速提升_Lv1,移速提升,1,移速提升 Lv1,移动速度 +1,0,0,0,1,0,0,0");
        sb.AppendLine("Card_全面强化_Lv1,全面强化,1,全面强化 Lv1,伤害+3 攻速+0.5 生命+15,3,0.5,15,0,0,0,0");
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        _cardsCsvPath = path;
    }

    #endregion

    #region 辅助

    private static string ToProjectPath(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath)) return null;
        fullPath = fullPath.Replace("\\", "/");
        string dataPath = Application.dataPath.Replace("\\", "/");
        if (fullPath.StartsWith(dataPath))
        {
            return "Assets" + fullPath.Substring(dataPath.Length);
        }
        return null;
    }

    #endregion
}

