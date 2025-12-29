using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 关卡管理器
/// 负责管理关卡切换、背景环境变化等
/// </summary>
public class LevelManager : MonoBehaviour
{
    private static LevelManager instance;
    public static LevelManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("LevelManager");
                instance = go.AddComponent<LevelManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [Header("关卡数据库")]
    [Tooltip("关卡数据库SO文件")]
    public LevelDatabase levelDatabase;

    [Header("背景设置")]
    [Tooltip("背景渲染器（用于改变背景颜色）")]
    public Camera backgroundCamera;

    [Tooltip("背景图片显示（可选，如果有背景图片）")]
    public Image backgroundImage;

    private int currentLevelNumber = 1;
    private LevelData currentLevelData;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // 如果没有指定背景相机，尝试查找主相机
        if (backgroundCamera == null)
        {
            backgroundCamera = Camera.main;
        }
    }

    void Start()
    {
        // 初始化第一关
        if (levelDatabase != null)
        {
            LoadLevel(1);
        }
        else
        {
            Debug.LogWarning("LevelManager: 未设置LevelDatabase！");
        }
    }

    /// <summary>
    /// 加载指定关卡
    /// </summary>
    public void LoadLevel(int levelNumber)
    {
        if (levelDatabase == null)
        {
            Debug.LogError("LevelManager: LevelDatabase未设置！");
            return;
        }

        currentLevelNumber = levelNumber;
        currentLevelData = levelDatabase.GetLevelData(levelNumber);

        if (currentLevelData == null)
        {
            Debug.LogError($"LevelManager: 无法加载关卡 {levelNumber} 的数据！");
            return;
        }

        // 应用关卡设置
        ApplyLevelSettings(currentLevelData);

        Debug.Log($"已加载关卡: {currentLevelData.levelName} (目标分数: {currentLevelData.targetScore})");
    }

    /// <summary>
    /// 应用关卡设置
    /// </summary>
    private void ApplyLevelSettings(LevelData levelData)
    {
        // 设置背景颜色
        if (backgroundCamera != null)
        {
            backgroundCamera.backgroundColor = levelData.backgroundColor;
        }

        // 设置背景图片
        if (backgroundImage != null)
        {
            if (levelData.backgroundSprite != null)
            {
                backgroundImage.sprite = levelData.backgroundSprite;
                backgroundImage.color = Color.white;
            }
            else
            {
                backgroundImage.color = Color.clear;
            }
        }

        // 设置背景音乐（通过AudioManager统一管理）
        if (levelData.backgroundMusic != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBackgroundMusic(levelData.backgroundMusic, true);
        }
        else if (levelData.backgroundMusic == null && AudioManager.Instance != null)
        {
            // 如果关卡没有背景音乐，停止当前音乐
            AudioManager.Instance.StopBackgroundMusic(true);
        }

        // 应用游戏难度设置到PipeSpawner
        ApplyDifficultySettings(levelData);
    }

    /// <summary>
    /// 应用难度设置到游戏对象
    /// </summary>
    private void ApplyDifficultySettings(LevelData levelData)
    {
        // 查找所有PipeSpawner并应用设置
        PipeSpawner[] spawners = FindObjectsOfType<PipeSpawner>();
        foreach (var spawner in spawners)
        {
            spawner.ApplyLevelSettings();
        }

        // 查找所有PipeMoveScript并应用速度倍数
        PipeMoveScript[] pipeMovers = FindObjectsOfType<PipeMoveScript>();
        foreach (var mover in pipeMovers)
        {
            mover.ApplyLevelSettings();
        }

        // 更新所有pipeMiddle的分数设置
        pipeMiddle[] pipeMiddles = FindObjectsOfType<pipeMiddle>();
        foreach (var pipeMiddle in pipeMiddles)
        {
            if (pipeMiddle != null)
            {
                pipeMiddle.UpdateScoreFromLevel();
            }
        }
    }

    /// <summary>
    /// 获取当前关卡数据
    /// </summary>
    public LevelData GetCurrentLevelData()
    {
        return currentLevelData;
    }

    /// <summary>
    /// 获取当前关卡编号
    /// </summary>
    public int GetCurrentLevelNumber()
    {
        return currentLevelNumber;
    }

    /// <summary>
    /// 获取当前关卡的目标分数
    /// </summary>
    public int GetCurrentTargetScore()
    {
        if (currentLevelData != null)
        {
            return currentLevelData.targetScore;
        }
        return 10; // 默认值
    }

    /// <summary>
    /// 进入下一关
    /// </summary>
    public void NextLevel()
    {
        if (levelDatabase == null)
        {
            Debug.LogError("LevelManager: LevelDatabase未设置！");
            return;
        }

        if (levelDatabase.HasNextLevel(currentLevelNumber))
        {
            LoadLevel(currentLevelNumber + 1);
        }
        else
        {
            Debug.Log("已到达最后一关！");
        }
    }

    /// <summary>
    /// 重置到第一关
    /// </summary>
    public void ResetToFirstLevel()
    {
        LoadLevel(1);
    }
}

