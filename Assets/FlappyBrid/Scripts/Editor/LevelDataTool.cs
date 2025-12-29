using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Reflection;
using System.Text;

/// <summary>
/// LevelData工具面板
/// 整合导入和导出功能
/// </summary>
public class LevelDataTool : EditorWindow
{
    // 导入相关
    private string csvFilePath = "";
    private string outputFolderPath = "Assets/FlappyBrid/Level";
    private bool updateExisting = true;
    private bool createNew = true;
    
    // 导出相关
    private string exportInputFolderPath = "Assets/FlappyBrid/Level";
    private string exportOutputFilePath = "";
    private bool includeHeader = true;
    
    // UI状态
    public int selectedTab = 0; // 0=导入, 1=导出（public以便从其他菜单项访问）
    private string[] tabNames = { "导入CSV", "导出CSV" };
    
    // CSV列名映射
    private Dictionary<string, string> columnMapping = new Dictionary<string, string>
    {
        { "关卡编号", "levelNumber" },
        { "关卡名称", "levelName" },
        { "目标分数", "targetScore" },
        { "生成率倍数", "spawnRateMultiplier" },
        { "移动速度倍数", "moveSpeedMultiplier" },
        { "高度偏移", "heightOffset" },
        { "管道通过分数", "pipePassScore" },
        { "完成奖励", "completionBonus" },
        { "关卡描述", "levelDescription" },
        { "使用关卡道具设置", "useLevelItemSettings" },
        { "道具生成概率", "itemSpawnChance" },
        { "道具X轴偏移", "itemSpawnOffsetX" }
    };
    
    [MenuItem("Tools/FlappyBird/关卡数据工具")]
    public static LevelDataTool ShowWindow()
    {
        LevelDataTool window = GetWindow<LevelDataTool>("关卡数据工具");
        window.minSize = new Vector2(600, 500);
        return window;
    }
    
    void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("关卡数据工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("支持CSV文件的导入和导出\n注意：背景颜色不在CSV中，需要在SO文件中手动设置", MessageType.Info);
        
        EditorGUILayout.Space();
        
        // 标签页
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
        
        EditorGUILayout.Space();
        
        // 根据选中的标签页显示不同的内容
        switch (selectedTab)
        {
            case 0:
                DrawImportTab();
                break;
            case 1:
                DrawExportTab();
                break;
        }
    }
    
    void DrawImportTab()
    {
        EditorGUILayout.LabelField("从CSV文件导入关卡数据", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        // CSV文件拖拽区域
        EditorGUILayout.LabelField("CSV文件:", GUILayout.Width(100));
        EditorGUILayout.BeginHorizontal();
        
        Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, string.IsNullOrEmpty(csvFilePath) ? "拖拽CSV文件到这里\n或点击浏览按钮" : Path.GetFileName(csvFilePath), EditorStyles.helpBox);
        
        Event evt = Event.current;
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    break;
                
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    foreach (string draggedPath in DragAndDrop.paths)
                    {
                        if (draggedPath.EndsWith(".csv", System.StringComparison.OrdinalIgnoreCase))
                        {
                            csvFilePath = draggedPath;
                            break;
                        }
                    }
                }
                Event.current.Use();
                break;
        }
        
        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFilePanel("选择CSV文件", "", "csv");
            if (!string.IsNullOrEmpty(path))
            {
                csvFilePath = path;
            }
        }
        EditorGUILayout.EndHorizontal();
        
        if (!string.IsNullOrEmpty(csvFilePath))
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("文件路径:", GUILayout.Width(100));
            EditorGUILayout.SelectableLabel(csvFilePath, EditorStyles.textField, GUILayout.Height(18));
            if (GUILayout.Button("清除", GUILayout.Width(50)))
            {
                csvFilePath = "";
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space();
        
        // 输出文件夹拖拽区域
        EditorGUILayout.LabelField("输出文件夹:", GUILayout.Width(100));
        EditorGUILayout.BeginHorizontal();
        
        Rect folderDropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(folderDropArea, string.IsNullOrEmpty(outputFolderPath) ? "拖拽文件夹到这里\n或点击选择按钮" : outputFolderPath, EditorStyles.helpBox);
        
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
                        if (Directory.Exists(draggedPath))
                        {
                            if (draggedPath.StartsWith(Application.dataPath))
                            {
                                outputFolderPath = "Assets" + draggedPath.Substring(Application.dataPath.Length);
                            }
                            else
                            {
                                outputFolderPath = draggedPath;
                            }
                            break;
                        }
                        else if (File.Exists(draggedPath))
                        {
                            string folder = Path.GetDirectoryName(draggedPath);
                            if (folder.StartsWith(Application.dataPath))
                            {
                                outputFolderPath = "Assets" + folder.Substring(Application.dataPath.Length);
                            }
                            else
                            {
                                outputFolderPath = folder;
                            }
                            break;
                        }
                    }
                }
                Event.current.Use();
                break;
        }
        
        if (GUILayout.Button("选择", GUILayout.Width(60)))
        {
            string path = EditorUtility.SaveFolderPanel("选择输出文件夹", outputFolderPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    outputFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
                else
                {
                    outputFolderPath = path;
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // 选项
        updateExisting = EditorGUILayout.Toggle("更新已存在的SO文件", updateExisting);
        createNew = EditorGUILayout.Toggle("创建新的SO文件", createNew);
        
        EditorGUILayout.Space();
        
        // 导入按钮
        GUI.enabled = !string.IsNullOrEmpty(csvFilePath) && File.Exists(csvFilePath);
        if (GUILayout.Button("开始导入", GUILayout.Height(30)))
        {
            ImportFromCSV();
        }
        GUI.enabled = true;
        
        EditorGUILayout.Space();
        
        // 帮助信息
        EditorGUILayout.LabelField("导入说明:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. 拖拽或选择CSV文件\n" +
            "2. 拖拽或选择输出文件夹\n" +
            "3. 配置导入选项\n" +
            "4. 点击'开始导入'\n\n" +
            "注意：\n" +
            "- CSV第一行必须是表头\n" +
            "- 关卡编号用于匹配已存在的SO文件\n" +
            "- 背景颜色不在CSV中导入，需要在SO文件中手动设置",
            MessageType.Info);
    }
    
    void DrawExportTab()
    {
        EditorGUILayout.LabelField("导出关卡数据为CSV文件", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        // 输入文件夹拖拽区域
        EditorGUILayout.LabelField("SO文件夹路径:", GUILayout.Width(120));
        EditorGUILayout.BeginHorizontal();
        
        Rect folderDropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(folderDropArea, string.IsNullOrEmpty(exportInputFolderPath) ? "拖拽文件夹到这里\n或点击浏览按钮" : exportInputFolderPath, EditorStyles.helpBox);
        
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
                        if (Directory.Exists(draggedPath))
                        {
                            if (draggedPath.StartsWith(Application.dataPath))
                            {
                                exportInputFolderPath = "Assets" + draggedPath.Substring(Application.dataPath.Length);
                            }
                            else
                            {
                                exportInputFolderPath = draggedPath;
                            }
                            break;
                        }
                        else if (File.Exists(draggedPath))
                        {
                            string folder = Path.GetDirectoryName(draggedPath);
                            if (folder.StartsWith(Application.dataPath))
                            {
                                exportInputFolderPath = "Assets" + folder.Substring(Application.dataPath.Length);
                            }
                            else
                            {
                                exportInputFolderPath = folder;
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
            string path = EditorUtility.OpenFolderPanel("选择SO文件夹", exportInputFolderPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    exportInputFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
                else
                {
                    exportInputFolderPath = path;
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // 输出文件路径拖拽区域
        EditorGUILayout.LabelField("CSV输出路径:", GUILayout.Width(120));
        EditorGUILayout.BeginHorizontal();
        
        Rect fileDropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(fileDropArea, string.IsNullOrEmpty(exportOutputFilePath) ? "拖拽CSV文件到这里（作为输出路径）\n或点击浏览按钮选择保存位置" : Path.GetFileName(exportOutputFilePath), EditorStyles.helpBox);
        
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
                        if (draggedPath.EndsWith(".csv", System.StringComparison.OrdinalIgnoreCase))
                        {
                            exportOutputFilePath = draggedPath;
                            break;
                        }
                        else if (Directory.Exists(draggedPath))
                        {
                            exportOutputFilePath = Path.Combine(draggedPath, "LevelDataExport.csv");
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
                exportOutputFilePath = path;
            }
        }
        EditorGUILayout.EndHorizontal();
        
        if (!string.IsNullOrEmpty(exportOutputFilePath))
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("完整路径:", GUILayout.Width(120));
            EditorGUILayout.SelectableLabel(exportOutputFilePath, EditorStyles.textField, GUILayout.Height(18));
            if (GUILayout.Button("清除", GUILayout.Width(50)))
            {
                exportOutputFilePath = "";
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space();
        
        // 选项
        includeHeader = EditorGUILayout.Toggle("包含表头", includeHeader);
        
        EditorGUILayout.Space();
        
        // 导出按钮
        GUI.enabled = !string.IsNullOrEmpty(exportInputFolderPath) && Directory.Exists(exportInputFolderPath) && !string.IsNullOrEmpty(exportOutputFilePath);
        if (GUILayout.Button("开始导出", GUILayout.Height(30)))
        {
            ExportToCSV();
        }
        GUI.enabled = true;
        
        EditorGUILayout.Space();
        
        // 帮助信息
        EditorGUILayout.LabelField("导出说明:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. 拖拽或选择包含LevelData SO文件的文件夹\n" +
            "2. 拖拽或选择CSV文件的保存路径\n" +
            "3. 配置导出选项\n" +
            "4. 点击'开始导出'\n\n" +
            "注意：\n" +
            "- 会导出文件夹中所有的LevelData SO文件\n" +
            "- 按照关卡编号排序\n" +
            "- 背景颜色不在CSV中导出，需要在SO文件中手动设置\n" +
            "- 导出的CSV文件使用UTF-8 BOM编码，Excel打开不会乱码",
            MessageType.Info);
    }
    
    // ========== 导入功能 ==========
    
    void ImportFromCSV()
    {
        if (string.IsNullOrEmpty(csvFilePath) || !File.Exists(csvFilePath))
        {
            EditorUtility.DisplayDialog("错误", "CSV文件不存在！", "确定");
            return;
        }
        
        try
        {
            // 读取CSV文件（支持UTF-8 with BOM）
            byte[] fileBytes = File.ReadAllBytes(csvFilePath);
            string fileContent;
            
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
            
            string[] lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
            if (lines.Length < 2)
            {
                EditorUtility.DisplayDialog("错误", "CSV文件至少需要2行（表头+数据）！", "确定");
                return;
            }
            
            // 解析表头
            string[] headers = ParseCSVLine(lines[0]);
            Dictionary<int, string> headerMap = new Dictionary<int, string>();
            for (int i = 0; i < headers.Length; i++)
            {
                string header = headers[i].Trim();
                if (columnMapping.ContainsKey(header))
                {
                    headerMap[i] = columnMapping[header];
                }
            }
            
            if (headerMap.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "未找到有效的表头！请检查CSV文件第一行。", "确定");
                return;
            }
            
            // 确保输出文件夹存在
            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }
            
            int successCount = 0;
            int updateCount = 0;
            int createCount = 0;
            int errorCount = 0;
            List<string> errors = new List<string>();
            
            // 解析数据行
            for (int rowIndex = 1; rowIndex < lines.Length; rowIndex++)
            {
                if (string.IsNullOrWhiteSpace(lines[rowIndex]))
                    continue;
                
                try
                {
                    string[] values = ParseCSVLine(lines[rowIndex]);
                    if (values.Length == 0)
                        continue;
                    
                    LevelData levelData = null;
                    int levelNumber = 0;
                    
                    // 查找关卡编号
                    if (headerMap.ContainsKey(0) && headerMap[0] == "levelNumber")
                    {
                        if (int.TryParse(values[0], out levelNumber))
                        {
                            string existingPath = $"{outputFolderPath}/{levelNumber:D2}.asset";
                            if (File.Exists(existingPath))
                            {
                                if (updateExisting)
                                {
                                    levelData = AssetDatabase.LoadAssetAtPath<LevelData>(existingPath);
                                    if (levelData == null)
                                    {
                                        levelData = ScriptableObject.CreateInstance<LevelData>();
                                    }
                                    updateCount++;
                                }
                            }
                            else
                            {
                                if (createNew)
                                {
                                    levelData = ScriptableObject.CreateInstance<LevelData>();
                                    createCount++;
                                }
                            }
                        }
                    }
                    else
                    {
                        levelNumber = rowIndex;
                        if (createNew)
                        {
                            levelData = ScriptableObject.CreateInstance<LevelData>();
                            createCount++;
                        }
                    }
                    
                    if (levelData == null)
                        continue;
                    
                    // 填充数据
                    FillLevelDataFromCSV(levelData, headerMap, values);
                    
                    // 保存SO文件
                    string assetPath = $"{outputFolderPath}/{levelNumber:D2}.asset";
                    if (updateExisting && File.Exists(assetPath))
                    {
                        EditorUtility.SetDirty(levelData);
                        AssetDatabase.SaveAssets();
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(levelData, assetPath);
                    }
                    
                    successCount++;
                }
                catch (System.Exception e)
                {
                    errorCount++;
                    errors.Add($"第{rowIndex + 1}行: {e.Message}");
                    Debug.LogError($"导入第{rowIndex + 1}行时出错: {e.Message}");
                }
            }
            
            AssetDatabase.Refresh();
            
            // 显示结果
            string message = $"导入完成！\n\n" +
                $"成功: {successCount}\n" +
                $"更新: {updateCount}\n" +
                $"创建: {createCount}\n" +
                $"错误: {errorCount}";
            
            if (errors.Count > 0)
            {
                message += "\n\n错误详情:\n" + string.Join("\n", errors.Take(10));
                if (errors.Count > 10)
                {
                    message += $"\n...还有{errors.Count - 10}个错误";
                }
            }
            
            EditorUtility.DisplayDialog("导入完成", message, "确定");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"导入失败: {e.Message}", "确定");
            Debug.LogError($"导入失败: {e.Message}\n{e.StackTrace}");
        }
    }
    
    void FillLevelDataFromCSV(LevelData levelData, Dictionary<int, string> headerMap, string[] values)
    {
        foreach (var kvp in headerMap)
        {
            int columnIndex = kvp.Key;
            string fieldName = kvp.Value;
            
            if (columnIndex >= values.Length)
                continue;
            
            string value = values[columnIndex].Trim();
            if (string.IsNullOrEmpty(value))
                continue;
            
            try
            {
                FillLevelDataField(levelData, fieldName, value);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"设置字段 {fieldName} 时出错: {e.Message}");
            }
        }
    }
    
    void FillLevelDataField(LevelData levelData, string fieldName, string value)
    {
        switch (fieldName)
        {
            case "levelNumber":
                if (int.TryParse(value, out int levelNum))
                    levelData.levelNumber = levelNum;
                break;
            case "levelName":
                levelData.levelName = value;
                break;
            case "targetScore":
                if (int.TryParse(value, out int target))
                    levelData.targetScore = target;
                break;
            case "spawnRateMultiplier":
                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float spawnRate))
                    levelData.spawnRateMultiplier = spawnRate;
                break;
            case "moveSpeedMultiplier":
                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float moveSpeed))
                    levelData.moveSpeedMultiplier = moveSpeed;
                break;
            case "heightOffset":
                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float height))
                    levelData.heightOffset = height;
                break;
            case "pipePassScore":
                if (int.TryParse(value, out int pipeScore))
                    levelData.pipePassScore = pipeScore;
                break;
            case "completionBonus":
                if (int.TryParse(value, out int bonus))
                    levelData.completionBonus = bonus;
                break;
            case "levelDescription":
                levelData.levelDescription = value;
                break;
            case "useLevelItemSettings":
                if (bool.TryParse(value, out bool useItem))
                    levelData.useLevelItemSettings = useItem;
                else if (value == "1" || value.ToLower() == "true" || value == "是" || value == "Y" || value == "y")
                    levelData.useLevelItemSettings = true;
                else if (value == "0" || value.ToLower() == "false" || value == "否" || value == "N" || value == "n")
                    levelData.useLevelItemSettings = false;
                break;
            case "itemSpawnChance":
                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float chance))
                    levelData.itemSpawnChance = chance;
                break;
            case "itemSpawnOffsetX":
                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float offsetX))
                    levelData.itemSpawnOffsetX = offsetX;
                break;
        }
    }
    
    string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string currentField = "";
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    currentField += '"';
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField);
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }
        
        result.Add(currentField);
        return result.ToArray();
    }
    
    // ========== 导出功能 ==========
    
    void ExportToCSV()
    {
        if (string.IsNullOrEmpty(exportInputFolderPath) || !Directory.Exists(exportInputFolderPath))
        {
            EditorUtility.DisplayDialog("错误", "SO文件夹不存在！", "确定");
            return;
        }
        
        if (string.IsNullOrEmpty(exportOutputFilePath))
        {
            EditorUtility.DisplayDialog("错误", "请选择CSV输出路径！", "确定");
            return;
        }
        
        try
        {
            // 查找所有LevelData SO文件
            string[] guids = AssetDatabase.FindAssets("t:LevelData", new[] { exportInputFolderPath });
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", $"在文件夹 {exportInputFolderPath} 中未找到LevelData文件！", "确定");
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
                csvContent.AppendLine("关卡编号,关卡名称,目标分数,生成率倍数,移动速度倍数,高度偏移,管道通过分数,完成奖励,关卡描述,使用关卡道具设置,道具生成概率,道具X轴偏移");
            }
            
            // 数据行
            foreach (LevelData levelData in levelDataList)
            {
                string line = $"{levelData.levelNumber}," +
                    $"\"{EscapeCSV(levelData.levelName)}\"," +
                    $"{levelData.targetScore}," +
                    $"{levelData.spawnRateMultiplier}," +
                    $"{levelData.moveSpeedMultiplier}," +
                    $"{levelData.heightOffset}," +
                    $"{levelData.pipePassScore}," +
                    $"{levelData.completionBonus}," +
                    $"\"{EscapeCSV(levelData.levelDescription)}\"," +
                    $"{(levelData.useLevelItemSettings ? "是" : "否")}," +
                    $"{levelData.itemSpawnChance}," +
                    $"{levelData.itemSpawnOffsetX}";
                
                csvContent.AppendLine(line);
            }
            
            // 使用UTF-8 with BOM编码写入文件
            byte[] bom = new byte[] { 0xEF, 0xBB, 0xBF };
            byte[] contentBytes = Encoding.UTF8.GetBytes(csvContent.ToString());
            byte[] fileBytes = new byte[bom.Length + contentBytes.Length];
            
            System.Buffer.BlockCopy(bom, 0, fileBytes, 0, bom.Length);
            System.Buffer.BlockCopy(contentBytes, 0, fileBytes, bom.Length, contentBytes.Length);
            
            File.WriteAllBytes(exportOutputFilePath, fileBytes);
            
            EditorUtility.DisplayDialog("导出成功", 
                $"成功导出 {levelDataList.Count} 个关卡数据到：\n{exportOutputFilePath}\n\n" +
                "现在可以用Excel打开该CSV文件进行编辑。", 
                "确定");
            
            // 如果在Unity项目内，刷新资源
            if (exportOutputFilePath.StartsWith(Application.dataPath))
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
    
    string EscapeCSV(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            return value.Replace("\"", "\"\"");
        }
        
        return value;
    }
}

