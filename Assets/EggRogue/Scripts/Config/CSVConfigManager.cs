using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// CSV 配置管理器 - 从本地 CSV 文件读取配置。
/// 
/// 功能：
/// 1. 从 StreamingAssets 或本地文件读取 CSV 配置
/// 2. 解析 CSV 并应用到游戏对象
/// 3. 提供"更新配置"按钮，重新读取文件
/// 
/// CSV 格式：
/// Key,Value,Type,Desc
/// PlayerDamage,10,float,玩家伤害
/// EnemyHealth,20,float,怪物血量
/// </summary>
public class CSVConfigManager : MonoBehaviour
{
    private static CSVConfigManager _instance;
    public static CSVConfigManager Instance => _instance;

    [Header("配置文件路径")]
    [Tooltip("CSV 文件名（放在 StreamingAssets 目录）")]
    public string csvFileName = "gameconfig.csv";

    [Header("HTTP 服务器配置（推荐）")]
    [Tooltip("是否启用 HTTP 服务器下载（优先级最高）")]
    public bool useHttpServer = false;

    [Tooltip("HTTP 服务器 URL（例如：https://your-server.com/gameconfig.csv）")]
    public string httpServerUrl = "";

    [Header("本地文件配置（备用）")]
    [Tooltip("是否允许从本地文件系统读取（用于开发调试）")]
    public bool allowLocalFileRead = true;

    [Tooltip("本地文件完整路径（开发时使用，例如：C:/Config/gameconfig.csv）")]
    public string localFilePath = "";

    private Dictionary<string, string> configDict = new Dictionary<string, string>();
    private Dictionary<string, string> configTypes = new Dictionary<string, string>();

    public event Action OnConfigUpdated;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 【已禁用】CSVConfigManager 的自动应用逻辑
        // 现在所有配置都通过 ScriptableObject（CharacterData、EnemyData、LevelData）管理
        // 如需重新启用，取消下面的注释即可
        
        /*
        // 如果启用 HTTP 服务器，使用协程异步加载
        if (useHttpServer && !string.IsNullOrEmpty(httpServerUrl))
        {
            StartCoroutine(LoadConfigFromHttp());
        }
        else
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL 环境：使用协程异步加载
            StartCoroutine(LoadConfigAsync());
            #else
            LoadConfig();
            ApplyConfig();
            #endif
        }
        */
        
        Debug.Log("CSVConfigManager: 自动应用已禁用，现在使用 ScriptableObject 配置系统");
    }

    /// <summary>
    /// 加载配置（从 CSV 文件）
    /// </summary>
    public void LoadConfig()
    {
        configDict.Clear();
        configTypes.Clear();

        string csvContent = ReadCSVFile();
        if (string.IsNullOrEmpty(csvContent))
        {
            Debug.LogWarning("CSVConfigManager: 无法读取 CSV 文件，使用默认配置");
            return;
        }

        ParseCSV(csvContent);
        Debug.Log($"CSVConfigManager: 已加载 {configDict.Count} 个配置项");
    }

    /// <summary>
    /// 读取 CSV 文件内容
    /// </summary>
    private string ReadCSVFile()
    {
        // 优先尝试本地文件（开发调试用）
        if (allowLocalFileRead && !string.IsNullOrEmpty(localFilePath) && File.Exists(localFilePath))
        {
            try
            {
                string content = File.ReadAllText(localFilePath, System.Text.Encoding.UTF8);
                Debug.Log($"CSVConfigManager: 从本地文件读取: {localFilePath}");
                return content;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"CSVConfigManager: 读取本地文件失败: {e.Message}");
            }
        }

        // 编辑器模式：尝试从 Assets 路径读取
        #if UNITY_EDITOR
        // 使用 Application.dataPath 构建完整路径
        string editorPath = Path.Combine(Application.dataPath, "EggRogue", "StreamingAssets", csvFileName);
        if (File.Exists(editorPath))
        {
            try
            {
                string content = File.ReadAllText(editorPath, System.Text.Encoding.UTF8);
                Debug.Log($"CSVConfigManager: 从编辑器路径读取: {editorPath}");
                return content;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"CSVConfigManager: 读取编辑器路径失败: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"CSVConfigManager: 编辑器路径文件不存在: {editorPath}");
        }
        #endif

        // 从 StreamingAssets 读取（运行时）
        string streamingPath = Path.Combine(Application.streamingAssetsPath, csvFileName);
        Debug.Log($"CSVConfigManager: 尝试读取 StreamingAssets: {streamingPath}");
        
        // WebGL 环境：File.ReadAllText 可能不工作，但先尝试
        // 如果失败，会在 LoadConfigAsync 中使用 UnityWebRequest
        #if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL 下 File.ReadAllText 可能失败，但先尝试
        Debug.LogWarning($"CSVConfigManager: WebGL 环境，如果 File.ReadAllText 失败，将使用 UnityWebRequest");
        #endif
        
        if (File.Exists(streamingPath))
        {
            try
            {
                string content = File.ReadAllText(streamingPath, System.Text.Encoding.UTF8);
                Debug.Log($"CSVConfigManager: 从 StreamingAssets 读取成功: {streamingPath}");
                return content;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"CSVConfigManager: File.ReadAllText 失败: {e.Message}，WebGL 环境可能需要 UnityWebRequest");
            }
        }
        else
        {
            Debug.LogWarning($"CSVConfigManager: StreamingAssets 文件不存在: {streamingPath}");
        }

        return null;
    }

    /// <summary>
    /// 解析 CSV 内容
    /// </summary>
    private void ParseCSV(string csvContent)
    {
        string[] lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 2)
        {
            Debug.LogWarning("CSVConfigManager: CSV 文件格式错误（至少需要表头和数据行）");
            return;
        }

        // 跳过表头（第一行）
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;

            string[] parts = ParseCSVLine(line);
            if (parts.Length < 2)
                continue;

            string key = parts[0].Trim();
            string value = parts[1].Trim();
            string type = parts.Length > 2 ? parts[2].Trim() : "float";

            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                configDict[key] = value;
                configTypes[key] = type;
            }
        }
    }

    /// <summary>
    /// 解析 CSV 行（处理引号和逗号）
    /// </summary>
    private string[] ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        System.Text.StringBuilder current = new System.Text.StringBuilder();

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString().Trim());

        return result.ToArray();
    }

    /// <summary>
    /// 获取配置值（字符串）
    /// </summary>
    public string GetString(string key, string defaultValue = "")
    {
        return configDict.ContainsKey(key) ? configDict[key] : defaultValue;
    }

    /// <summary>
    /// 获取配置值（浮点数）
    /// </summary>
    public float GetFloat(string key, float defaultValue = 0f)
    {
        if (configDict.ContainsKey(key))
        {
            if (float.TryParse(configDict[key], out float value))
                return value;
        }
        return defaultValue;
    }

    /// <summary>
    /// 获取配置值（整数）
    /// </summary>
    public int GetInt(string key, int defaultValue = 0)
    {
        if (configDict.ContainsKey(key))
        {
            if (int.TryParse(configDict[key], out int value))
                return value;
        }
        return defaultValue;
    }

    /// <summary>
    /// 应用配置到游戏对象
    /// </summary>
    public void ApplyConfig()
    {
        // 应用玩家配置
        PlayerCombatController playerCombat = FindObjectOfType<PlayerCombatController>();
        if (playerCombat != null)
        {
            float damage = GetFloat("PlayerDamage", 10f);
            float attackRange = GetFloat("PlayerAttackRange", 10f);
            float fireRate = GetFloat("PlayerFireRate", 2f);
            float bulletSpeed = GetFloat("BulletSpeed", 20f);
            
            Debug.Log($"CSVConfigManager: 应用玩家配置 - Damage: {damage}, AttackRange: {attackRange}, FireRate: {fireRate}, BulletSpeed: {bulletSpeed}");
            
            playerCombat.SetDamage(damage);
            playerCombat.SetAttackRange(attackRange);
            playerCombat.SetFireRate(fireRate);
            playerCombat.SetBulletSpeed(bulletSpeed);
        }
        else
        {
            Debug.LogWarning("CSVConfigManager: 未找到 PlayerCombatController，配置将在找到时应用");
        }

        // 应用已存在的怪物配置
        EnemyController[] enemies = FindObjectsOfType<EnemyController>();
        foreach (var enemy in enemies)
        {
            Health health = enemy.GetComponent<Health>();
            if (health != null)
            {
                health.SetMaxHealth(GetFloat("EnemyHealth", 20f));
            }

            EnemyController enemyController = enemy.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                enemyController.SetMoveSpeed(GetFloat("EnemyMoveSpeed", 3f));
            }
        }

        // 通知配置已更新
        OnConfigUpdated?.Invoke();
    }

    /// <summary>
    /// 从 HTTP 服务器加载配置（推荐方案）
    /// </summary>
    private IEnumerator LoadConfigFromHttp()
    {
        // 添加时间戳避免缓存（如果 URL 包含提交哈希，可以注释掉这行）
        string urlWithCacheBust = httpServerUrl;
        if (!urlWithCacheBust.Contains("?") && !urlWithCacheBust.Contains("#"))
        {
            urlWithCacheBust += $"?t={System.DateTime.Now.Ticks}";
        }
        
        Debug.Log($"CSVConfigManager: 从 HTTP 服务器加载 CSV: {urlWithCacheBust}");

        using (UnityWebRequest www = UnityWebRequest.Get(urlWithCacheBust))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string csvContent = www.downloadHandler.text;
                Debug.Log($"CSVConfigManager: 获取到 CSV 内容（前100字符）: {csvContent.Substring(0, Mathf.Min(100, csvContent.Length))}");
                
                ParseCSV(csvContent);
                
                // 输出解析后的配置值（用于调试）
                Debug.Log($"CSVConfigManager: 已解析 {configDict.Count} 个配置项");
                foreach (var kvp in configDict)
                {
                    Debug.Log($"  {kvp.Key} = {kvp.Value}");
                }
                
                ApplyConfig();
                Debug.Log($"CSVConfigManager: 从 HTTP 服务器加载成功，已加载 {configDict.Count} 个配置项");
            }
            else
            {
                Debug.LogWarning($"CSVConfigManager: HTTP 服务器加载失败: {www.error}，尝试备用方案");
                // HTTP 失败，尝试备用方案
                #if UNITY_WEBGL && !UNITY_EDITOR
                yield return StartCoroutine(LoadConfigAsync());
                #else
                LoadConfig();
                ApplyConfig();
                #endif
            }
        }
    }

    /// <summary>
    /// WebGL 环境下的异步加载配置
    /// </summary>
    #if UNITY_WEBGL && !UNITY_EDITOR
    private IEnumerator LoadConfigAsync()
    {
        // 微信小游戏中，StreamingAssets 可能指向 CDN，但 CSV 可能没有上传
        // 尝试多种路径：1. 本地构建目录 2. CDN 3. persistentDataPath
        
        string streamingPath = Path.Combine(Application.streamingAssetsPath, csvFileName);
        Debug.Log($"CSVConfigManager: WebGL 异步加载 CSV，尝试路径: {streamingPath}");
        Debug.Log($"CSVConfigManager: Application.streamingAssetsPath = {Application.streamingAssetsPath}");
        
        // 方案1：尝试从 CDN/StreamingAssets 读取
        using (UnityWebRequest www = UnityWebRequest.Get(streamingPath))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string csvContent = www.downloadHandler.text;
                ParseCSV(csvContent);
                ApplyConfig();
                Debug.Log($"CSVConfigManager: 从 StreamingAssets/CDN 加载成功，已加载 {configDict.Count} 个配置项");
                yield break;
            }
            else
            {
                Debug.LogWarning($"CSVConfigManager: 从 StreamingAssets/CDN 加载失败: {www.error}，尝试其他路径");
            }
        }
        
        // 方案2：尝试从本地构建目录读取（如果文件在本地）
        // 注意：WebGL 环境下 File.ReadAllText 可能不工作，但先尝试
        try
        {
            if (File.Exists(streamingPath))
            {
                string csvContent = File.ReadAllText(streamingPath, System.Text.Encoding.UTF8);
                ParseCSV(csvContent);
                ApplyConfig();
                Debug.Log($"CSVConfigManager: 从本地文件读取成功，已加载 {configDict.Count} 个配置项");
                yield break;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"CSVConfigManager: 本地文件读取失败: {e.Message}");
        }
        
        // 方案3：尝试从 persistentDataPath 读取（用户可能手动放置了文件）
        string persistentPath = Path.Combine(Application.persistentDataPath, csvFileName);
        Debug.Log($"CSVConfigManager: 尝试从 persistentDataPath 读取: {persistentPath}");
        
        try
        {
            if (File.Exists(persistentPath))
            {
                string csvContent = File.ReadAllText(persistentPath, System.Text.Encoding.UTF8);
                ParseCSV(csvContent);
                ApplyConfig();
                Debug.Log($"CSVConfigManager: 从 persistentDataPath 读取成功，已加载 {configDict.Count} 个配置项");
                yield break;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"CSVConfigManager: persistentDataPath 读取失败: {e.Message}");
        }
        
        // 所有方案都失败，使用默认配置
        Debug.LogWarning("CSVConfigManager: 所有读取方案都失败，使用默认配置");
        ApplyConfig(); // 使用默认值
    }
    #endif

    /// <summary>
    /// 更新配置（重新读取文件并应用）
    /// </summary>
    public void UpdateConfig()
    {
        #if UNITY_EDITOR
        // 编辑器模式下，刷新 AssetDatabase 确保文件更新被检测到
        AssetDatabase.Refresh();
        #endif

        // 如果启用 HTTP 服务器，从 HTTP 重新加载
        if (useHttpServer && !string.IsNullOrEmpty(httpServerUrl))
        {
            StartCoroutine(UpdateConfigFromHttp());
        }
        else
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL 环境：使用协程异步加载
            StartCoroutine(UpdateConfigAsync());
            #else
            LoadConfig();
            ApplyConfig();
            
            Debug.Log($"CSVConfigManager: 配置已更新，当前配置项数量: {configDict.Count}");
            
            // 输出当前配置值（用于调试）
            foreach (var kvp in configDict)
            {
                Debug.Log($"  {kvp.Key} = {kvp.Value}");
            }
            #endif
        }
    }

    /// <summary>
    /// 从 HTTP 服务器更新配置
    /// </summary>
    private IEnumerator UpdateConfigFromHttp()
    {
        yield return StartCoroutine(LoadConfigFromHttp());
        
        Debug.Log($"CSVConfigManager: 配置已更新，当前配置项数量: {configDict.Count}");
        
        // 输出当前配置值（用于调试）
        foreach (var kvp in configDict)
        {
            Debug.Log($"  {kvp.Key} = {kvp.Value}");
        }
    }
    
    #if UNITY_WEBGL && !UNITY_EDITOR
    /// <summary>
    /// WebGL 环境下的异步更新配置
    /// </summary>
    private IEnumerator UpdateConfigAsync()
    {
        yield return StartCoroutine(LoadConfigAsync());
        
        Debug.Log($"CSVConfigManager: 配置已更新，当前配置项数量: {configDict.Count}");
        
        // 输出当前配置值（用于调试）
        foreach (var kvp in configDict)
        {
            Debug.Log($"  {kvp.Key} = {kvp.Value}");
        }
    }
    #endif

    /// <summary>
    /// 获取所有配置项（用于调试）
    /// </summary>
    public Dictionary<string, string> GetAllConfigs()
    {
        return new Dictionary<string, string>(configDict);
    }
}
