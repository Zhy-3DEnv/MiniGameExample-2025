using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;

/// <summary>
/// LevelData批量导出工具
/// 支持从SO文件批量导出为CSV文件
/// </summary>
public class LevelDataCSVExporter : EditorWindow
{
    private string inputFolderPath = "Assets/FlappyBrid/Level";
    private string outputFilePath = "";
    private bool includeHeader = true; // 是否包含表头
    
    [MenuItem("Tools/FlappyBird/批量导出关卡数据为CSV")]
    public static void ShowWindow()
    {
        // 重定向到统一工具面板，并切换到导出标签页
        LevelDataTool window = LevelDataTool.ShowWindow();
        if (window != null)
        {
            window.selectedTab = 1; // 切换到导出标签页
        }
    }
    
    void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("关卡数据批量导出工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("从SO文件批量导出为CSV文件，方便在Excel中编辑\n注意：背景颜色不在CSV中导出，需要在SO文件中手动设置", MessageType.Info);
        
        EditorGUILayout.Space();
        
        // 输入文件夹拖拽区域
        EditorGUILayout.LabelField("SO文件夹路径:", GUILayout.Width(120));
        EditorGUILayout.BeginHorizontal();
        
        // 拖拽区域
        Rect folderDropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(folderDropArea, string.IsNullOrEmpty(inputFolderPath) ? "拖拽文件夹到这里\n或点击浏览按钮" : inputFolderPath, EditorStyles.helpBox);
        
        Event folderEvt = Event.current;
        switch (folderEvt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!folderDropArea.Contains(folderEvt.mousePosition))
                    break;
                
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                
                if (folderEvt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    foreach (string draggedPath in DragAndDrop.paths)
                    {
                        // 检查是否是文件夹（在Unity项目中）
                        if (Directory.Exists(draggedPath))
                        {
                            // 转换为Unity相对路径
                            if (draggedPath.StartsWith(Application.dataPath))
                            {
                                inputFolderPath = "Assets" + draggedPath.Substring(Application.dataPath.Length);
                            }
                            else
                            {
                                inputFolderPath = draggedPath;
                            }
                            break;
                        }
                        // 如果是文件，使用其所在文件夹
                        else if (File.Exists(draggedPath))
                        {
                            string folder = Path.GetDirectoryName(draggedPath);
                            if (folder.StartsWith(Application.dataPath))
                            {
                                inputFolderPath = "Assets" + folder.Substring(Application.dataPath.Length);
                            }
                            else
                            {
                                inputFolderPath = folder;
                            }
                            break;
                        }
                    }
                }
                Event.current.Use();
                break;
        }
        
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("选择SO文件夹", inputFolderPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                // 转换为Unity相对路径
                if (path.StartsWith(Application.dataPath))
                {
                    inputFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
                else
                {
                    inputFolderPath = path;
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // 输出文件路径拖拽区域
        EditorGUILayout.LabelField("CSV输出路径:", GUILayout.Width(120));
        EditorGUILayout.BeginHorizontal();
        
        // 拖拽区域
        Rect fileDropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(fileDropArea, string.IsNullOrEmpty(outputFilePath) ? "拖拽CSV文件到这里（作为输出路径）\n或点击浏览按钮选择保存位置" : Path.GetFileName(outputFilePath), EditorStyles.helpBox);
        
        Event fileEvt = Event.current;
        switch (fileEvt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!fileDropArea.Contains(fileEvt.mousePosition))
                    break;
                
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                
                if (fileEvt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    foreach (string draggedPath in DragAndDrop.paths)
                    {
                        if (draggedPath.EndsWith(".csv", System.StringComparison.OrdinalIgnoreCase) || Directory.Exists(Path.GetDirectoryName(draggedPath)))
                        {
                            // 如果是CSV文件，使用其路径；如果是文件夹，生成默认文件名
                            if (draggedPath.EndsWith(".csv", System.StringComparison.OrdinalIgnoreCase))
                            {
                                outputFilePath = draggedPath;
                            }
                            else if (Directory.Exists(draggedPath))
                            {
                                outputFilePath = Path.Combine(draggedPath, "LevelDataExport.csv");
                            }
                            break;
                        }
                    }
                }
                Event.current.Use();
                break;
        }
        
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.SaveFilePanel("保存CSV文件", "", "LevelDataExport", "csv");
            if (!string.IsNullOrEmpty(path))
            {
                outputFilePath = path;
            }
        }
        EditorGUILayout.EndHorizontal();
        
        // 显示完整路径
        if (!string.IsNullOrEmpty(outputFilePath))
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("完整路径:", GUILayout.Width(120));
            EditorGUILayout.SelectableLabel(outputFilePath, EditorStyles.textField, GUILayout.Height(18));
            if (GUILayout.Button("清除", GUILayout.Width(50)))
            {
                outputFilePath = "";
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space();
        
        // 选项
        includeHeader = EditorGUILayout.Toggle("包含表头", includeHeader);
        
        EditorGUILayout.Space();
        
        // 导出按钮
        GUI.enabled = !string.IsNullOrEmpty(inputFolderPath) && Directory.Exists(inputFolderPath) && !string.IsNullOrEmpty(outputFilePath);
        if (GUILayout.Button("开始导出", GUILayout.Height(30)))
        {
            ExportToCSV();
        }
        GUI.enabled = true;
        
        EditorGUILayout.Space();
        
        // 帮助信息
        EditorGUILayout.LabelField("使用说明:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. 选择包含LevelData SO文件的文件夹（默认：Assets/FlappyBrid/Level）\n" +
            "2. 选择CSV文件的保存路径\n" +
            "3. 勾选'包含表头'（推荐）\n" +
            "4. 点击'开始导出'按钮\n\n" +
            "注意：\n" +
            "- 会导出文件夹中所有的LevelData SO文件\n" +
            "- 按照关卡编号排序\n" +
            "- 背景颜色不在CSV中导出，需要在SO文件中手动设置\n" +
            "- 导出的CSV文件使用UTF-8 BOM编码，Excel打开不会乱码",
            MessageType.Info);
    }
    
    void ExportToCSV()
    {
        if (string.IsNullOrEmpty(inputFolderPath) || !Directory.Exists(inputFolderPath))
        {
            EditorUtility.DisplayDialog("错误", "SO文件夹不存在！", "确定");
            return;
        }
        
        if (string.IsNullOrEmpty(outputFilePath))
        {
            EditorUtility.DisplayDialog("错误", "请选择CSV输出路径！", "确定");
            return;
        }
        
        try
        {
            // 查找所有LevelData SO文件
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { inputFolderPath });
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", $"在文件夹 {inputFolderPath} 中未找到LevelData文件！", "确定");
                return;
            }
            
            List<LevelData> levelDataList = new List<LevelData>();
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelData levelData = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (levelData != null)
                {
                    levelDataList.Add(levelData);
                }
            }
            
            // 按关卡编号排序
            levelDataList = levelDataList.OrderBy(ld => ld.levelNumber).ToList();
            
            if (levelDataList.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "未找到有效的LevelData文件！", "确定");
                return;
            }
            
            // 生成CSV内容
            StringBuilder csvContent = new StringBuilder();
            
            // 表头
            if (includeHeader)
            {
                csvContent.AppendLine("关卡编号,关卡名称,目标时间,生成率倍数,移动速度倍数,高度偏移,管道通过分数,完成奖励,关卡描述,使用关卡道具设置,道具生成概率,道具X轴偏移,使用关卡怪物设置,怪物生成概率,怪物X轴偏移,怪物Y轴偏移");
            }
            
            // 数据行
            foreach (LevelData levelData in levelDataList)
            {
                string line = $"{levelData.levelNumber}," +
                    $"\"{EscapeCSV(levelData.levelName)}\"," +
                    $"{levelData.targetTime:F2}," +
                    $"{levelData.spawnRateMultiplier}," +
                    $"{levelData.moveSpeedMultiplier}," +
                    $"{levelData.heightOffset}," +
                    $"{levelData.pipePassScore}," +
                    $"{levelData.completionBonus}," +
                    $"\"{EscapeCSV(levelData.levelDescription)}\"," +
                    $"{(levelData.useLevelItemSettings ? "是" : "否")}," +
                    $"{levelData.itemSpawnChance}," +
                    $"{levelData.itemSpawnOffsetX}," +
                    $"{(levelData.useLevelMonsterSettings ? "是" : "否")}," +
                    $"{levelData.monsterSpawnChance}," +
                    $"{levelData.monsterSpawnOffsetX}," +
                    $"{levelData.monsterSpawnOffsetY}";
                
                csvContent.AppendLine(line);
            }
            
            // 使用UTF-8 with BOM编码写入文件
            byte[] bom = new byte[] { 0xEF, 0xBB, 0xBF };
            byte[] contentBytes = Encoding.UTF8.GetBytes(csvContent.ToString());
            byte[] fileBytes = new byte[bom.Length + contentBytes.Length];
            
            System.Buffer.BlockCopy(bom, 0, fileBytes, 0, bom.Length);
            System.Buffer.BlockCopy(contentBytes, 0, fileBytes, bom.Length, contentBytes.Length);
            
            File.WriteAllBytes(outputFilePath, fileBytes);
            
            EditorUtility.DisplayDialog("导出成功", 
                $"成功导出 {levelDataList.Count} 个关卡数据到：\n{outputFilePath}\n\n" +
                "现在可以用Excel打开该CSV文件进行编辑。", 
                "确定");
            
            // 如果在Unity项目内，刷新资源
            if (outputFilePath.StartsWith(Application.dataPath))
            {
                AssetDatabase.Refresh();
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"导出失败: {e.Message}", "确定");
            Debug.LogError($"导出失败: {e.Message}\n{e.StackTrace}");
        }
    }
    
    /// <summary>
    /// 转义CSV字段中的特殊字符
    /// </summary>
    string EscapeCSV(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        
        // 如果包含逗号、引号或换行符，需要用引号包裹，并转义引号
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            return value.Replace("\"", "\"\""); // 转义引号："" -> """"
        }
        
        return value;
    }
}

