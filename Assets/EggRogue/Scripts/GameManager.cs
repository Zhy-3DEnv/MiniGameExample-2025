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
    [Tooltip("主菜单场景名称，如：MainMenu")]
    public string mainMenuSceneName = "MainMenu";

    [Tooltip("战斗场景名称，如：GameScene")]
    public string gameSceneName = "GameScene";

    /// <summary>
    /// 当前是否在战斗场景中。
    /// 后续可以用来控制暂停、时间缩放等。
    /// </summary>
    public bool IsInGame { get; private set; }

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
        // 根据场景名称自动切换UI
        if (UIManager.Instance != null)
        {
            if (scene.name == mainMenuSceneName)
            {
                UIManager.Instance.ShowMainMenu();
            }
            else if (scene.name == gameSceneName)
            {
                UIManager.Instance.ShowGameHUD();
            }
        }
    }

    /// <summary>
    /// 从主菜单进入游戏场景。
    /// 建议挂在主菜单 UI 按钮的 OnClick 上。
    /// </summary>
    public void StartGame()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("GameManager: gameSceneName 为空，请在 Inspector 中配置战斗场景名称。");
            return;
        }

        IsInGame = true;
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// 从游戏返回主菜单。
    /// 可挂在暂停界面或结算界面的按钮上。
    /// </summary>
    public void ReturnToMenu()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("GameManager: mainMenuSceneName 为空，请在 Inspector 中配置主菜单场景名称。");
            return;
        }

        IsInGame = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// 重新开始当前游戏（从第 1 关开始）。
    /// 当前简单实现为重新加载 gameSceneName。
    /// 后续可以在这里加入关卡索引、存档恢复等逻辑。
    /// </summary>
    public void RestartGame()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("GameManager: gameSceneName 为空，请在 Inspector 中配置战斗场景名称。");
            return;
        }

        IsInGame = true;
        SceneManager.LoadScene(gameSceneName);
    }
}

