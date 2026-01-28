using UnityEngine;
using UnityEngine.SceneManagement;

namespace EggRogue
{
/// <summary>
/// 关卡管理器。使用 LevelDatabase（ScriptableObject）配置，在 GameScene 加载后应用关卡数据。
/// 常驻 PersistentScene，与 GameManager 配合。
/// </summary>
public class LevelManager : MonoBehaviour
{
    private static LevelManager _instance;
    public static LevelManager Instance => _instance;

    [Header("关卡配置")]
    [Tooltip("关卡数据库（ScriptableObject）- 拖入 Assets/EggRogue/Configs/LevelDatabase.asset")]
    public LevelDatabase levelDatabase;

    [Tooltip("总关卡数（用于通关判断，0=使用 LevelDatabase.levels 长度）")]
    public int maxLevelCount = 20;

    /// <summary>
    /// 当前关卡编号（1-based）。
    /// </summary>
    public int CurrentLevel { get; private set; } = 1;

    private void Awake()
    {
        // 确保 LevelManager 作为根节点常驻，避免在子物体上调用 DontDestroyOnLoad 导致的错误日志
        GameObject rootGO = transform.root != null ? transform.root.gameObject : gameObject;

        if (_instance != null && _instance != this)
        {
            // 已经有一个全局实例时，销毁整棵重复的根节点
            Destroy(rootGO);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(rootGO);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// GameScene 加载完成后应用当前关卡数据。
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (GameManager.Instance == null)
            return;
        if (scene.name != GameManager.Instance.GameSceneName)
            return;

        ApplyLevelToScene();
    }

    /// <summary>
    /// 将当前关卡配置应用到场景（EnemySpawner 等）。
    /// </summary>
    public void ApplyLevelToScene()
    {
        LevelData data = GetCurrentLevelData();
        if (data == null)
        {
            Debug.LogWarning("LevelManager: 未找到当前关卡配置，使用默认");
            return;
        }

        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            // 将"每秒刷几个怪"转换为"生成间隔（秒）"
            // 例如：spawnsPerSecond = 0.5 → spawnInterval = 2.0秒
            //      spawnsPerSecond = 2.0 → spawnInterval = 0.5秒
            float spawnInterval = data.spawnsPerSecond > 0f ? (1f / data.spawnsPerSecond) : 999f;
            spawner.spawnInterval = spawnInterval;
            spawner.maxAliveEnemies = data.maxAliveEnemies;
            spawner.randomOffsetRadius = Mathf.Max(0f, data.randomOffsetRadius);
            spawner.ApplyLevelData(data);
            spawner.ResetForLevel();
            Debug.Log($"LevelManager: 已应用关卡 {CurrentLevel} ({data.levelName}) -> EnemySpawner (刷怪频率: {data.spawnsPerSecond}/秒, 间隔: {spawnInterval:F2}秒)");
        }
    }

    /// <summary>
    /// 获取当前关卡的 LevelData。
    /// </summary>
    public LevelData GetCurrentLevelData()
    {
        if (levelDatabase == null)
            return null;
        return levelDatabase.GetLevel(CurrentLevel);
    }

    /// <summary>
    /// 设置当前关卡并加载 GameScene。由 GameManager 调用。
    /// </summary>
    public void SetLevelAndNotifyLoaded(int level)
    {
        CurrentLevel = Mathf.Max(1, level);
    }

    /// <summary>
    /// 进入下一关。若已通关则返回主菜单。
    /// </summary>
    public void NextLevel()
    {
        int max = maxLevelCount > 0 ? maxLevelCount : (levelDatabase != null ? levelDatabase.TotalLevelCount : 20);
        if (CurrentLevel >= max)
        {
            Debug.Log("LevelManager: 通关，返回主菜单");
            if (GameManager.Instance != null)
                GameManager.Instance.ReturnToMenu();
            return;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.LoadGameScene(CurrentLevel + 1);
    }

    /// <summary>
    /// 重新从第 1 关开始。
    /// </summary>
    public void RestartFromLevel1()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LoadGameScene(1);
    }
}
}
