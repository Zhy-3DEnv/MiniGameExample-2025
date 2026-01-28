using UnityEngine;
using System.IO;
using System;

/// <summary>
/// 配置管理器 - 单例，负责加载、保存和应用游戏配置。
/// 
/// 功能：
/// 1. 从 ScriptableObject 加载默认配置
/// 2. 从本地 JSON 文件加载保存的配置（如果存在）
/// 3. 运行时修改配置并保存到本地文件
/// 4. 应用配置到游戏对象（玩家、怪物等）
/// 
/// 使用方式：
/// 1. 在场景中创建空物体，挂载本脚本
/// 2. 在 Inspector 中拖入 GameConfig ScriptableObject
/// 3. 调用 LoadConfig() 加载配置，ApplyConfig() 应用配置
/// </summary>
public class ConfigManager : MonoBehaviour
{
    private static ConfigManager _instance;
    public static ConfigManager Instance => _instance;

    [Header("配置资源")]
    [Tooltip("默认配置（ScriptableObject）")]
    public GameConfig defaultConfig;

    [Header("配置文件路径")]
    [Tooltip("本地配置文件路径（相对于 Application.persistentDataPath）")]
    public string configFileName = "gameconfig.json";

    private GameConfig currentConfig;
    private string configFilePath;

    public GameConfig CurrentConfig => currentConfig;

    public event Action<GameConfig> OnConfigChanged;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        configFilePath = Path.Combine(Application.persistentDataPath, configFileName);
    }

    private void Start()
    {
        LoadConfig();
        ApplyConfig();
    }

    /// <summary>
    /// 加载配置（优先从本地文件，不存在则用默认配置）
    /// </summary>
    public void LoadConfig()
    {
        if (defaultConfig == null)
        {
            Debug.LogError("ConfigManager: defaultConfig 未设置！");
            return;
        }

        // 创建配置副本（避免修改原始 ScriptableObject）
        currentConfig = Instantiate(defaultConfig);

        // 尝试从本地文件加载
        if (File.Exists(configFilePath))
        {
            try
            {
                string json = File.ReadAllText(configFilePath);
                JsonUtility.FromJsonOverwrite(json, currentConfig);
                Debug.Log($"ConfigManager: 从本地文件加载配置: {configFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ConfigManager: 加载本地配置失败，使用默认配置。错误: {e.Message}");
            }
        }
        else
        {
            Debug.Log("ConfigManager: 本地配置文件不存在，使用默认配置");
        }
    }

    /// <summary>
    /// 保存配置到本地文件
    /// </summary>
    public void SaveConfig()
    {
        if (currentConfig == null)
        {
            Debug.LogWarning("ConfigManager: 当前配置为空，无法保存");
            return;
        }

        try
        {
            string json = JsonUtility.ToJson(currentConfig, true);
            File.WriteAllText(configFilePath, json);
            Debug.Log($"ConfigManager: 配置已保存到: {configFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"ConfigManager: 保存配置失败: {e.Message}");
        }
    }

    /// <summary>
    /// 应用配置到游戏对象（玩家、怪物等）
    /// </summary>
    public void ApplyConfig()
    {
        if (currentConfig == null)
        {
            Debug.LogWarning("ConfigManager: 当前配置为空，无法应用");
            return;
        }

        // 应用玩家配置
        PlayerCombatController playerCombat = FindObjectOfType<PlayerCombatController>();
        if (playerCombat != null)
        {
            playerCombat.SetDamage(currentConfig.playerDamage);
            playerCombat.SetAttackRange(currentConfig.playerAttackRange);
            playerCombat.SetFireRate(currentConfig.playerFireRate);
        }

        // 应用已存在的怪物配置（新生成的怪物会在 EnemySpawner 中应用）
        EnemyController[] enemies = FindObjectsOfType<EnemyController>();
        foreach (var enemy in enemies)
        {
            Health health = enemy.GetComponent<Health>();
            if (health != null)
            {
                health.SetMaxHealth(currentConfig.enemyMaxHealth);
            }
        }

        // 通知配置已更改
        OnConfigChanged?.Invoke(currentConfig);
    }

    /// <summary>
    /// 更新配置值（运行时修改）
    /// </summary>
    public void UpdateConfig(float? playerDamage = null, float? enemyMaxHealth = null, 
        float? playerAttackRange = null, float? playerFireRate = null,
        float? enemyMoveSpeed = null, int? coinDropMin = null, int? coinDropMax = null)
    {
        if (currentConfig == null)
        {
            Debug.LogWarning("ConfigManager: 当前配置为空，无法更新");
            return;
        }

        bool changed = false;

        if (playerDamage.HasValue)
        {
            currentConfig.playerDamage = playerDamage.Value;
            changed = true;
        }

        if (enemyMaxHealth.HasValue)
        {
            currentConfig.enemyMaxHealth = enemyMaxHealth.Value;
            changed = true;
        }

        if (playerAttackRange.HasValue)
        {
            currentConfig.playerAttackRange = playerAttackRange.Value;
            changed = true;
        }

        if (playerFireRate.HasValue)
        {
            currentConfig.playerFireRate = playerFireRate.Value;
            changed = true;
        }

        if (enemyMoveSpeed.HasValue)
        {
            currentConfig.enemyMoveSpeed = enemyMoveSpeed.Value;
            changed = true;
        }

        if (coinDropMin.HasValue)
        {
            currentConfig.coinDropMin = coinDropMin.Value;
            changed = true;
        }

        if (coinDropMax.HasValue)
        {
            currentConfig.coinDropMax = coinDropMax.Value;
            changed = true;
        }

        if (changed)
        {
            ApplyConfig();
            SaveConfig();
        }
    }

    /// <summary>
    /// 重置为默认配置
    /// </summary>
    public void ResetToDefault()
    {
        if (defaultConfig == null)
        {
            Debug.LogWarning("ConfigManager: defaultConfig 未设置，无法重置");
            return;
        }

        currentConfig = Instantiate(defaultConfig);
        ApplyConfig();
        SaveConfig();
    }
}
