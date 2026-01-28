using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

/// <summary>
/// Excel 转 CSV 工具 - Unity 编辑器工具。
/// 
/// 使用方式：
/// 1. 在 Excel 中创建表格，格式如下：
///    | Key          | Value | Type  | Desc     |
///    |--------------|-------|-------|----------|
///    | PlayerDamage | 10    | float | 玩家伤害 |
///    | EnemyHealth  | 20    | float | 怪物血量 |
/// 
/// 2. 在 Excel 中另存为 CSV（逗号分隔）
/// 3. 将 CSV 文件放到 Assets/EggRogue/Config/ 目录
/// 4. 在 Unity 菜单选择：Tools > EggRogue > 转换 CSV 配置
/// 
/// 或者直接使用本工具：Tools > EggRogue > 选择 Excel 文件并转换
/// </summary>
public class ExcelToCSVConverter : EditorWindow
{
    private string excelFilePath = "";
    private string outputPath = "Assets/EggRogue/StreamingAssets/gameconfig.csv";
    private string templatePath = "";

    [MenuItem("EggRogue/Excel 转 CSV 配置工具")]
    public static void ShowWindow()
    {
        var window = GetWindow<ExcelToCSVConverter>("Excel 转 CSV");
        window.InitializeTemplatePath();
    }

    private void InitializeTemplatePath()
    {
        // 查找模板文件路径（优先 StreamingAssets，其次 Config 目录）
        string[] searchPaths = {
            "Assets/EggRogue/StreamingAssets/gameconfig.csv",
            "Assets/EggRogue/Config/gameconfig.csv",
            "Assets/EggRogue/StreamingAssets/gameconfig_template.csv",
            "Assets/EggRogue/Config/gameconfig_template.csv"
        };

        foreach (string path in searchPaths)
        {
            if (File.Exists(path))
            {
                templatePath = path;
                // 如果 Excel 路径为空，使用模板路径作为默认值
                if (string.IsNullOrEmpty(excelFilePath))
                {
                    excelFilePath = Path.GetFullPath(path);
                }
                break;
            }
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Excel 转 CSV 配置工具", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "使用说明：\n" +
            "1. 点击\"创建 CSV 模板\"生成模板文件\n" +
            "2. 在 Excel 中打开模板，修改参数后另存为 CSV\n" +
            "3. 选择 CSV 文件并转换到 StreamingAssets 目录",
            MessageType.Info);

        GUILayout.Space(10);

        // 创建模板按钮
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("创建 CSV 模板", GUILayout.Height(30)))
        {
            CreateCSVTemplate();
        }
        if (!string.IsNullOrEmpty(templatePath) && File.Exists(templatePath))
        {
            EditorGUILayout.LabelField($"模板: {templatePath}", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        EditorGUILayout.LabelField("CSV 文件路径：");
        EditorGUILayout.BeginHorizontal();
        excelFilePath = EditorGUILayout.TextField(excelFilePath);
        if (GUILayout.Button("选择文件", GUILayout.Width(80)))
        {
            // 如果模板路径存在，默认打开模板所在目录
            string defaultDir = "";
            if (!string.IsNullOrEmpty(templatePath) && File.Exists(templatePath))
            {
                defaultDir = Path.GetDirectoryName(Path.GetFullPath(templatePath));
            }

            string path = EditorUtility.OpenFilePanel("选择 CSV 文件", defaultDir, "csv");
            if (!string.IsNullOrEmpty(path))
            {
                excelFilePath = path;
            }
        }
        if (GUILayout.Button("使用模板", GUILayout.Width(80)))
        {
            if (!string.IsNullOrEmpty(templatePath) && File.Exists(templatePath))
            {
                excelFilePath = Path.GetFullPath(templatePath);
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "模板文件不存在，请先创建模板", "确定");
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        EditorGUILayout.LabelField("输出 CSV 路径（StreamingAssets）：");
        EditorGUILayout.BeginHorizontal();
        outputPath = EditorGUILayout.TextField(outputPath);
        if (GUILayout.Button("使用默认", GUILayout.Width(80)))
        {
            outputPath = "Assets/EggRogue/StreamingAssets/gameconfig.csv";
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(20);

        if (GUILayout.Button("转换", GUILayout.Height(30)))
        {
            ConvertExcelToCSV();
        }

        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "注意：如果 Excel 文件是 .xlsx 格式，需要先在 Excel 中另存为 CSV。\n" +
            "Unity 无法直接读取 .xlsx 文件。",
            MessageType.Warning);
    }

    private void ConvertExcelToCSV()
    {
        if (string.IsNullOrEmpty(excelFilePath))
        {
            EditorUtility.DisplayDialog("错误", "请选择 Excel 文件", "确定");
            return;
        }

        if (!File.Exists(excelFilePath))
        {
            EditorUtility.DisplayDialog("错误", "文件不存在", "确定");
            return;
        }

        try
        {
            // 如果是 CSV，直接复制；如果是 Excel，提示用户先转换
            string ext = Path.GetExtension(excelFilePath).ToLower();
            if (ext == ".xlsx" || ext == ".xls")
            {
                EditorUtility.DisplayDialog("提示",
                    "Unity 无法直接读取 Excel 文件。\n" +
                    "请先在 Excel 中：\n" +
                    "1. 打开文件\n" +
                    "2. 文件 > 另存为\n" +
                    "3. 格式选择 'CSV (逗号分隔)(*.csv)'\n" +
                    "4. 保存后使用 CSV 文件",
                    "确定");
                return;
            }

            if (ext == ".csv")
            {
                // 确保输出目录存在
                string outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // 复制 CSV 文件
                File.Copy(excelFilePath, outputPath, true);
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("成功", $"CSV 文件已复制到：\n{outputPath}", "确定");
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"转换失败：{e.Message}", "确定");
        }
    }

    /// <summary>
    /// 创建 CSV 模板文件
    /// </summary>
    private void CreateCSVTemplate()
    {
        string templateDir = "Assets/EggRogue/StreamingAssets";
        string templateFile = Path.Combine(templateDir, "gameconfig.csv");

        // 确保目录存在
        if (!Directory.Exists(templateDir))
        {
            Directory.CreateDirectory(templateDir);
        }

        // 创建模板内容
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Key,Value,Type,Desc");
        sb.AppendLine("PlayerDamage,10,float,玩家单发伤害");
        sb.AppendLine("PlayerAttackRange,10,float,玩家攻击范围");
        sb.AppendLine("PlayerFireRate,2,float,玩家射速（每秒几发）");
        sb.AppendLine("EnemyHealth,20,float,怪物最大生命值");
        sb.AppendLine("EnemyMoveSpeed,3,float,怪物移动速度");
        sb.AppendLine("CoinDropMin,1,int,最少掉落金币数");
        sb.AppendLine("CoinDropMax,2,int,最多掉落金币数");

        try
        {
            File.WriteAllText(templateFile, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();

            templatePath = templateFile;
            excelFilePath = Path.GetFullPath(templateFile);

            EditorUtility.DisplayDialog("成功", 
                $"CSV 模板已创建：\n{templateFile}\n\n" +
                $"你可以：\n" +
                $"1. 在 Excel 中打开此文件\n" +
                $"2. 修改参数后另存为 CSV\n" +
                $"3. 使用本工具转换到 StreamingAssets", 
                "确定");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"创建模板失败：{e.Message}", "确定");
        }
    }
}
