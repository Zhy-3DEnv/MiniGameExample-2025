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

    [Tooltip("总关卡数上限（用于通关判断）。0=仅用 LevelDatabase 实际关卡数；>0 时与 LevelDatabase 取较小值，避免缺关。")]
    public int maxLevelCount = 0;

    [Header("单局继承")]
    [Tooltip("进入下一关是否回满血。勾选=回满；不勾选=继承上一关的当前血量。")]
    public bool fullHealOnNextLevel = true;

    /// <summary>
    /// 当前关卡编号（1-based）。
    /// </summary>
    public int CurrentLevel { get; private set; } = 1;

    /// <summary>
    /// 单局内跨关继承：上一关结束时的当前/最大血量（仅当 fullHealOnNextLevel=false 时使用）。
    /// </summary>
    private bool hasSavedRunStateHealth;
    private float savedCurrentHealth;
    private float savedMaxHealth;

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
    /// GameScene 加载完成后应用当前关卡数据，并在正确的时机处理跨关金币补偿。
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (GameManager.Instance == null)
            return;
        if (scene.name != GameManager.Instance.GameSceneName)
            return;

        // 到这里时，上一关的 GameScene 已经卸载完毕，所有 Coin.OnDestroy 都已被调用，
        // GoldManager.lostGoldBank 已经包含了上一关“未拾取金币”的最终数值。
        if (GoldManager.Instance != null)
        {
            if (CurrentLevel <= 1)
            {
                // 新开一局，从第 1 关开始：清空历史的未拾取/双倍状态
                GoldManager.Instance.ResetGoldBonuses();
            }
            else
            {
                // 进入第 N(>1) 关：把上一关累计的未拾取金币转成本关的双倍拾取额度
                GoldManager.Instance.PrepareDoubleGoldForNextLevel();
            }
        }

        ApplyLevelToScene();
    }

    /// <summary>
    /// 清除单局血量继承状态（进入第 1 关时调用）。
    /// </summary>
    public void ClearRunStateHealth()
    {
        hasSavedRunStateHealth = false;
    }

    /// <summary>
    /// 保存当前玩家血量，用于进入下一关后恢复。
    /// </summary>
    public void SaveRunStateHealth(float current, float max)
    {
        hasSavedRunStateHealth = true;
        savedCurrentHealth = current;
        savedMaxHealth = Mathf.Max(0.001f, max);
    }

    /// <summary>
    /// 尝试取出并消费已保存的血量；若有则返回 true 并清除保存，否则返回 false。
    /// </summary>
    public bool TryGetRunStateHealth(out float current, out float max)
    {
        if (!hasSavedRunStateHealth)
        {
            current = 0f;
            max = 0f;
            return false;
        }
        current = savedCurrentHealth;
        max = savedMaxHealth;
        hasSavedRunStateHealth = false;
        return true;
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

        // 进入第 1 关时清除单局血量继承，确保新局 / 重开时满血
        if (CurrentLevel == 1)
            ClearRunStateHealth();

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
    /// 有效关卡总数（用于通关判断）：以 LevelDatabase 实际数量为准，maxLevelCount>0 时取较小值。
    /// </summary>
    public int GetMaxLevel()
    {
        int dbCount = (levelDatabase != null && levelDatabase.levels != null) ? levelDatabase.levels.Length : 0;
        if (dbCount <= 0)
            return maxLevelCount > 0 ? maxLevelCount : 20;
        if (maxLevelCount > 0)
            return Mathf.Min(maxLevelCount, dbCount);
        return dbCount;
    }

    /// <summary>
    /// 当前是否为最后一关（通关后应进完整通关结算，而非下一关）。
    /// </summary>
    public bool IsLastLevel()
    {
        return CurrentLevel >= GetMaxLevel();
    }

    /// <summary>
    /// 设置当前关卡并加载 GameScene。由 GameManager 调用。
    /// </summary>
    public void SetLevelAndNotifyLoaded(int level)
    {
        CurrentLevel = Mathf.Max(1, level);
    }

    /// <summary>
    /// 通关时触发（参数：通关关卡、当前金币）。UIManager 等订阅后显示完整通关结算。
    /// </summary>
    public static event System.Action<int, int> OnGameClear;

    /// <summary>
    /// 进入下一关。若已通关则触发 OnGameClear，否则加载下一关。
    /// </summary>
    public void NextLevel()
    {
        int max = GetMaxLevel();
        if (CurrentLevel >= max)
        {
            Debug.Log("LevelManager: 通关，触发 OnGameClear");
            int gold = GoldManager.Instance != null ? GoldManager.Instance.Gold : 0;
            var evt = OnGameClear;
            bool hasSubs = evt != null && evt.GetInvocationList().Length > 0;
            evt?.Invoke(CurrentLevel, gold);
            if (!hasSubs && GameManager.Instance != null)
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
