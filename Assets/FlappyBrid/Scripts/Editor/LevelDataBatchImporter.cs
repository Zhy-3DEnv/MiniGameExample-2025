using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Reflection;
#if UNITY_EXCEL_IMPORTER
using ExcelImporter;
using ExcelDataReader;
#endif

/// <summary>
/// LevelData批量导入工具
/// 支持从CSV/Excel文件批量创建或更新LevelData ScriptableObject
/// 支持从Excel单元格填充颜色中读取背景色
/// </summary>
public class LevelDataBatchImporter : EditorWindow
{
    private string csvFilePath = "";
    private string excelFilePath = "";
    private string outputFolderPath = "Assets/FlappyBrid/Level";
    private bool updateExisting = true; // 是否更新已存在的SO文件
    private bool createNew = true; // 是否创建新的SO文件
    
    // CSV列名映射（Excel表头）
    // 注意：背景颜色不在CSV中导入，需要在SO文件中手动设置
    private Dictionary<string, string> columnMapping = new Dictionary<string, string>
    {
        { "关卡编号", "levelNumber" },
        { "关卡名称", "levelName" },
        { "目标时间", "targetTime" },
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
    
    [MenuItem("Tools/FlappyBird/批量导入关卡数据")]
    public static void ShowWindow()
    {
        // 重定向到统一工具面板
        LevelDataTool.ShowWindow();
    }
    
    void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("关卡数据批量导入工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("支持Excel (.xlsx) 和 CSV 格式文件\nExcel文件支持从单元格填充颜色读取背景色", MessageType.Info);
        
        EditorGUILayout.Space();
        
        // CSV文件拖拽区域
        EditorGUILayout.LabelField("CSV文件:", GUILayout.Width(100));
        EditorGUILayout.BeginHorizontal();
        
        // 拖拽区域
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
                            excelFilePath = ""; // 清空Excel路径
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
                excelFilePath = ""; // 清空Excel路径
            }
        }
        EditorGUILayout.EndHorizontal();
        
        // 显示完整路径
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
        
        // 输出文件夹路径
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("输出文件夹:", GUILayout.Width(100));
        outputFolderPath = EditorGUILayout.TextField(outputFolderPath);
        if (GUILayout.Button("选择", GUILayout.Width(60)))
        {
            string path = EditorUtility.SaveFolderPanel("选择输出文件夹", outputFolderPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                // 转换为Unity相对路径
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
        EditorGUILayout.LabelField("CSV列名映射（表头）:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("CSV文件第一行应为表头，支持以下列名：\n" + 
            string.Join(", ", columnMapping.Keys), MessageType.None);
        
        EditorGUILayout.Space();
        
        // 导入按钮
        bool hasFile = (!string.IsNullOrEmpty(excelFilePath) && File.Exists(excelFilePath)) || 
                       (!string.IsNullOrEmpty(csvFilePath) && File.Exists(csvFilePath));
        GUI.enabled = hasFile;
        if (GUILayout.Button("开始导入", GUILayout.Height(30)))
        {
            if (!string.IsNullOrEmpty(excelFilePath) && File.Exists(excelFilePath))
            {
                ImportFromExcel();
            }
            else if (!string.IsNullOrEmpty(csvFilePath) && File.Exists(csvFilePath))
            {
                ImportFromCSV();
            }
        }
        GUI.enabled = true;
        
        EditorGUILayout.Space();
        
        // 帮助信息
        EditorGUILayout.LabelField("使用说明:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. 在Excel中编辑关卡数据\n" +
            "2. 将Excel导出为CSV格式：\n" +
            "   - 方法1：文件 -> 另存为 -> 选择'CSV UTF-8(逗号分隔)(*.csv)'\n" +
            "   - 方法2：使用菜单 Tools/FlappyBird/创建CSV模板文件（UTF-8 BOM）\n" +
            "3. 选择CSV文件路径\n" +
            "4. 选择输出文件夹（默认：Assets/FlappyBrid/Level）\n" +
            "5. 点击'开始导入'按钮\n\n" +
            "注意：\n" +
            "- CSV第一行必须是表头\n" +
            "- 如果Excel打开CSV时中文乱码，请使用'创建CSV模板文件'工具重新生成\n" +
            "- 关卡编号用于匹配已存在的SO文件\n" +
            "- 如果关卡编号已存在且'更新已存在的SO文件'为true，则会更新该文件\n" +
            "- 如果关卡编号不存在且'创建新的SO文件'为true，则会创建新文件",
            MessageType.Info);
        
        EditorGUILayout.Space();
        if (GUILayout.Button("重新生成CSV模板文件（解决Excel乱码）", GUILayout.Height(25)))
        {
            CreateCSVTemplate.CreateTemplate();
        }
    }
    
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
            // 先读取为字节数组，然后检测BOM并转换为字符串
            byte[] fileBytes = File.ReadAllBytes(csvFilePath);
            string fileContent;
            
            // 检测UTF-8 BOM (EF BB BF)
            if (fileBytes.Length >= 3 && fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF)
            {
                // 有BOM，跳过前3个字节
                fileContent = System.Text.Encoding.UTF8.GetString(fileBytes, 3, fileBytes.Length - 3);
            }
            else
            {
                // 没有BOM，尝试UTF-8，如果失败则使用系统默认编码
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
                    
                    // 创建或更新LevelData
                    LevelData levelData = null;
                    int levelNumber = 0;
                    
                    // 查找关卡编号
                    if (headerMap.ContainsKey(0) && headerMap[0] == "levelNumber")
                    {
                        if (int.TryParse(values[0], out levelNumber))
                        {
                            // 尝试加载已存在的SO文件
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
                                else
                                {
                                    continue; // 跳过已存在的文件
                                }
                            }
                            else
                            {
                                if (createNew)
                                {
                                    levelData = ScriptableObject.CreateInstance<LevelData>();
                                    createCount++;
                                }
                                else
                                {
                                    continue; // 跳过新文件创建
                                }
                            }
                        }
                    }
                    else
                    {
                        // 如果没有关卡编号列，使用行号
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
                switch (fieldName)
                {
                    case "levelNumber":
                        if (int.TryParse(value, out int levelNum))
                            levelData.levelNumber = levelNum;
                        break;
                    case "levelName":
                        levelData.levelName = value;
                        break;
                    case "targetTime":
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float targetTime))
                            levelData.targetTime = targetTime;
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
                    // 注意：背景颜色不在CSV中导入，需要在SO文件中手动设置
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"设置字段 {fieldName} 时出错: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// 从Excel文件导入（支持读取单元格颜色）
    /// 注意：由于Unity不支持COM互操作，此功能需要安装ExcelDataReader库
    /// 当前版本暂时不支持直接读取Excel文件，请使用CSV文件导入
    /// </summary>
    void ImportFromExcel()
    {
        EditorUtility.DisplayDialog("提示", 
            "由于Unity的限制，当前版本不支持直接读取Excel文件。\n\n" +
            "请使用以下方法：\n" +
            "1. 在Excel中编辑数据\n" +
            "2. 将Excel导出为CSV UTF-8格式\n" +
            "3. 使用CSV文件导入功能\n\n" +
            "或者：\n" +
            "1. 在Excel中编辑数据并填充颜色\n" +
            "2. 保存为Excel格式\n" +
            "3. 使用Excel的'另存为'功能导出为CSV\n" +
            "4. 在CSV中手动填写背景颜色的R、G、B、A数值（0-1之间）\n\n" +
            "我们正在开发更好的解决方案...", 
            "确定");
        
        // 暂时禁用Excel直接导入功能
        return;
        
        /* 原始COM代码（Unity不支持）
#if UNITY_EDITOR_WIN
        try
        {
            // 使用COM接口读取Excel（需要安装Excel）
            Type excelType = Type.GetTypeFromProgID("Excel.Application");
            if (excelType == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到Excel应用程序！\n请确保已安装Microsoft Excel。\n\n或者使用CSV文件导入。", "确定");
                return;
            }
            
            object excelApp = Activator.CreateInstance(excelType);
            excelType.InvokeMember("Visible", BindingFlags.SetProperty, null, excelApp, new object[] { false });
            excelType.InvokeMember("DisplayAlerts", BindingFlags.SetProperty, null, excelApp, new object[] { false });
            
            object workbooks = excelType.InvokeMember("Workbooks", BindingFlags.GetProperty, null, excelApp, null);
            object workbook = workbooks.GetType().InvokeMember("Open", BindingFlags.InvokeMethod, null, workbooks, new object[] { excelFilePath });
            object worksheets = workbook.GetType().InvokeMember("Worksheets", BindingFlags.GetProperty, null, workbook, null);
            object worksheet = worksheets.GetType().InvokeMember("Item", BindingFlags.GetProperty, null, worksheets, new object[] { 1 });
            object usedRange = worksheet.GetType().InvokeMember("UsedRange", BindingFlags.GetProperty, null, worksheet, null);
            
            // 获取数据范围
            object rows = usedRange.GetType().InvokeMember("Rows", BindingFlags.GetProperty, null, usedRange, null);
            object columns = usedRange.GetType().InvokeMember("Columns", BindingFlags.GetProperty, null, usedRange, null);
            int rowCount = (int)rows.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, rows, null);
            int colCount = (int)columns.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, columns, null);
            
            if (rowCount < 2)
            {
                EditorUtility.DisplayDialog("错误", "Excel文件至少需要2行（表头+数据）！", "确定");
                CloseExcel(excelApp, workbook);
                return;
            }
            
            // 解析表头
            Dictionary<int, string> headerMap = new Dictionary<int, string>();
            
            for (int col = 1; col <= colCount; col++)
            {
                object cell = worksheet.GetType().InvokeMember("Cells", BindingFlags.GetProperty, null, worksheet, new object[] { 1, col });
                object value = cell.GetType().InvokeMember("Value2", BindingFlags.GetProperty, null, cell, null);
                if (value != null)
                {
                    string header = value.ToString().Trim();
                    if (columnMapping.ContainsKey(header))
                    {
                        headerMap[col] = columnMapping[header];
                    }
                }
            }
            
            if (headerMap.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "未找到有效的表头！请检查Excel文件第一行。", "确定");
                CloseExcel(excelApp, workbook);
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
            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    LevelData levelData = null;
                    int levelNumber = 0;
                    
                    // 查找关卡编号
                    if (headerMap.ContainsKey(1) && headerMap[1] == "levelNumber")
                    {
                        object cell = worksheet.GetType().InvokeMember("Cells", BindingFlags.GetProperty, null, worksheet, new object[] { row, 1 });
                        object value = cell.GetType().InvokeMember("Value2", BindingFlags.GetProperty, null, cell, null);
                        if (value != null && int.TryParse(value.ToString(), out levelNumber))
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
                        levelNumber = row - 1;
                        if (createNew)
                        {
                            levelData = ScriptableObject.CreateInstance<LevelData>();
                            createCount++;
                        }
                    }
                    
                    if (levelData == null)
                        continue;
                    
                    // 填充数据（背景颜色不在CSV/Excel中导入，需要在SO文件中手动设置）
                    FillLevelDataFromExcel(levelData, headerMap, worksheet, row, -1);
                    
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
                    errors.Add($"第{row}行: {e.Message}");
                    Debug.LogError($"导入第{row}行时出错: {e.Message}");
                }
            }
            
            CloseExcel(excelApp, workbook);
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
            EditorUtility.DisplayDialog("错误", $"导入失败: {e.Message}\n\n请确保已安装Microsoft Excel。", "确定");
            Debug.LogError($"导入失败: {e.Message}\n{e.StackTrace}");
        }
#else
        EditorUtility.DisplayDialog("错误", "Excel导入功能仅在Windows系统可用！\n请使用CSV文件导入。", "确定");
#endif
        */
    }
    
    void CloseExcel(object excelApp, object workbook)
    {
#if UNITY_EDITOR_WIN
        try
        {
            if (workbook != null)
            {
                workbook.GetType().InvokeMember("Close", BindingFlags.InvokeMethod, null, workbook, new object[] { false });
            }
            if (excelApp != null)
            {
                excelApp.GetType().InvokeMember("Quit", BindingFlags.InvokeMethod, null, excelApp, null);
            }
        }
        catch { }
#endif
    }
    
    void FillLevelDataFromExcelTable(LevelData levelData, Dictionary<int, string> headerMap, System.Data.DataTable table, int row)
    {
        foreach (var kvp in headerMap)
        {
            int columnIndex = kvp.Key;
            string fieldName = kvp.Value;
            
            try
            {
                if (table.Rows[row][columnIndex] == null)
                    continue;
                
                string strValue = table.Rows[row][columnIndex].ToString().Trim();
                if (string.IsNullOrEmpty(strValue))
                    continue;
                
                // 填充数据（不支持读取颜色，颜色需要在CSV中手动填写）
                FillLevelDataField(levelData, fieldName, strValue);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"设置字段 {fieldName} 时出错: {e.Message}");
            }
        }
    }
    
    // 保留旧方法用于COM代码（已注释）
    void FillLevelDataFromExcel(LevelData levelData, Dictionary<int, string> headerMap, object worksheet, int row, int backgroundColorColumnIndex)
    {
        foreach (var kvp in headerMap)
        {
            int columnIndex = kvp.Key;
            string fieldName = kvp.Value;
            
            try
            {
                object cell = worksheet.GetType().InvokeMember("Cells", BindingFlags.GetProperty, null, worksheet, new object[] { row, columnIndex });
                object value = cell.GetType().InvokeMember("Value2", BindingFlags.GetProperty, null, cell, null);
                
                if (value == null)
                    continue;
                
                string strValue = value.ToString().Trim();
                if (string.IsNullOrEmpty(strValue))
                    continue;
                
                // 填充数据（背景颜色不在CSV/Excel中导入，需要在SO文件中手动设置）
                FillLevelDataField(levelData, fieldName, strValue);
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
            case "targetTime":
                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float targetTime))
                    levelData.targetTime = targetTime;
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
    
    /// <summary>
    /// 解析CSV行（处理引号和逗号）
    /// </summary>
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
                    // 转义的引号
                    currentField += '"';
                    i++; // 跳过下一个引号
                }
                else
                {
                    // 切换引号状态
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                // 字段分隔符
                result.Add(currentField);
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }
        
        // 添加最后一个字段
        result.Add(currentField);
        
        return result.ToArray();
    }
}

