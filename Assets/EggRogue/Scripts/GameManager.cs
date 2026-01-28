using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 蛋黄人肉鸽游戏的全局 GameManager。
/// 
/// 使用方式（建议）：
/// 1. 在主菜单场景（例如 MainMenu）和游戏场景（例如 GameScene）中各放置一个空物体挂载本脚本；
/// 2. 将这些场景名称填入 inspector 中的字段；
/// 3. 在 UI 按钮的 OnClick 中调用对应的公开方法（StartGame、ReturnToMenu、RestartGame）。
/// 
/// 注意：
/// - 本脚本采用简单的单例模式，在场景切换时通过 DontDestroyOnLoad 保持常驻。
/// - 仅负责场景切换与基础游戏状态，不包含具体玩法逻辑，后续可以在此基础上扩展。
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance => _instance;

    [Header("场景名称配置")]
    [Tooltip("常驻场景名称（UI、管理器），启动时最先加载")]
    public string persistentSceneName = "PersistentScene";

    [Tooltip("主菜单场景名称，如：MainMenu")]
    public string mainMenuSceneName = "MainMenu";

    [Tooltip("战斗场景名称，如：GameScene")]
    public string gameSceneName = "GameScene";

    /// <summary>
    /// 战斗场景名称（供 LevelManager 等使用）。
    /// </summary>
    public string GameSceneName => gameSceneName;

    /// <summary>
    /// 当前是否在战斗场景中。
    /// </summary>
    public bool IsInGame { get; private set; }

    private void Awake()
    {
        // Unity 限制：DontDestroyOnLoad 只能作用于“根 GameObject”（或挂在根物体上的组件）。
        // 为了避免把 GameManager 挂在子物体时触发报错，这里对根节点做常驻。
        GameObject rootGO = transform.root != null ? transform.root.gameObject : gameObject;

        if (_instance != null && _instance != this)
        {
            // 销毁整套重复的根节点，避免留下子物体影响状态
            Destroy(rootGO);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(rootGO);
    }

    private void OnEnable()
    {
        // 订阅场景加载完成事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // 取消订阅
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 场景加载完成时的回调，用于自动切换UI
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (UIManager.Instance == null)
            return;

        if (scene.name == mainMenuSceneName)
            UIManager.Instance.ShowMainMenu();
        else if (scene.name == gameSceneName)
            UIManager.Instance.ShowGameHUD();
    }

    /// <summary>
    /// 从主菜单进入游戏（第 1 关）。建议挂在主菜单「开始游戏」按钮 OnClick 上。
    /// </summary>
    public void StartGame()
    {
        LoadGameScene(1);
    }

    /// <summary>
    /// 加载指定关卡的 GameScene。由 LevelManager 或 UI 调用。
    /// </summary>
    public void LoadGameScene(int level)
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("GameManager: gameSceneName 为空，请在 Inspector 中配置。");
            return;
        }

        if (EggRogue.LevelManager.Instance != null)
            EggRogue.LevelManager.Instance.SetLevelAndNotifyLoaded(level);

        IsInGame = true;
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// 从游戏返回主菜单。可挂在暂停/结算界面按钮上。
    /// </summary>
    public void ReturnToMenu()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("GameManager: mainMenuSceneName 为空，请在 Inspector 中配置。");
            return;
        }

        IsInGame = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// 重新开始当前游戏（从第 1 关开始）。可挂在结算/暂停界面按钮上。
    /// </summary>
    public void RestartGame()
    {
        if (EggRogue.LevelManager.Instance != null)
            EggRogue.LevelManager.Instance.RestartFromLevel1();
        else
            LoadGameScene(1);
    }
}

