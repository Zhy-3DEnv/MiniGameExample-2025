using System.Collections;
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
/// - 本脚本采用简单的单例模式，位于 PersistentScene 中，该场景通过附加加载保持常驻。
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

    /// <summary>
    /// 加载主菜单后是否应显示选英雄界面（用于「再试一次」重置流程）。
    /// </summary>
    private static bool _pendingShowCharacterSelection;

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
        // PersistentScene 使用附加加载保持常驻，无需 DontDestroyOnLoad
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
    private IEnumerator ResetJoystickAndApplySettingsNextFrame()
    {
        yield return null;
        VirtualJoystick joystick = FindObjectOfType<VirtualJoystick>();
        if (joystick != null)
            joystick.ResetToCenter();
        SettingsPanel.ApplySavedToScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == gameSceneName)
        {
            StartCoroutine(ResetJoystickAndApplySettingsNextFrame());
        }

        if (UIManager.Instance == null)
            return;

        if (scene.name == mainMenuSceneName)
        {
            if (_pendingShowCharacterSelection)
            {
                _pendingShowCharacterSelection = false;
                UIManager.Instance.ShowCharacterSelection();
            }
            else
            {
                UIManager.Instance.ShowMainMenu();
            }
        }
        else if (scene.name == gameSceneName)
        {
            UIManager.Instance.ShowGameHUD();
        }
    }

    /// <summary>
    /// 失败后「再试一次」：重置本局状态（武器、卡片、金币等），返回选英雄界面重新开始。
    /// 会先清理场景内敌人/子弹/金币，确保重新进入时状态干净。
    /// </summary>
    public void ReturnToCharacterSelectionForRetry()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("GameManager: mainMenuSceneName 为空，请在 Inspector 中配置。");
            return;
        }

        // 0. 立即清理场景内 GamePlay 元素（敌人、子弹、金币），避免残留导致新局异常
        LevelFlowManager.ClearGameplayElements();

        // 1. 重置关卡
        if (EggRogue.LevelManager.Instance != null)
            EggRogue.LevelManager.Instance.SetLevelAndNotifyLoaded(1);

        // 2. 清空卡片
        if (EggRogue.CardManager.Instance != null)
            EggRogue.CardManager.Instance.ClearAllCards();

        // 3. 清空武器
        if (EggRogue.WeaponInventoryManager.Instance != null)
            EggRogue.WeaponInventoryManager.Instance.ClearAll();

        // 4. 重置单局状态（金币、等级、经验、物品、商店锁定等）
        if (EggRogue.PlayerRunState.Instance != null)
            EggRogue.PlayerRunState.Instance.ResetRunState();

        if (EggRogue.GoldManager.Instance != null)
            EggRogue.GoldManager.Instance.NotifyGoldChanged();

        // 5. 标记加载主菜单后显示选英雄界面
        _pendingShowCharacterSelection = true;
        IsInGame = false;
        LoadMainMenuScene();
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
        LoadGameSceneAdditive();
    }

    /// <summary>
    /// 从游戏返回主菜单。可挂在暂停/结算界面按钮上。
    /// 会先清理场景内敌人/子弹/金币，确保离开时无残留。
    /// </summary>
    public void ReturnToMenu()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogError("GameManager: mainMenuSceneName 为空，请在 Inspector 中配置。");
            return;
        }

        LevelFlowManager.ClearGameplayElements();
        IsInGame = false;
        LoadMainMenuScene();
    }

    /// <summary>
    /// 加载主菜单（附加模式）。先卸载 GameScene，再附加加载 MainMenu。
    /// </summary>
    private void LoadMainMenuScene()
    {
        StartCoroutine(SwitchToSceneAsync(gameSceneName, mainMenuSceneName));
    }

    /// <summary>
    /// 加载游戏场景（附加模式）。先卸载 MainMenu 或 GameScene（ whichever 已加载），再附加加载新 GameScene。
    /// 从主菜单进入时卸载 MainMenu；关卡切换时卸载当前 GameScene，避免出现两个 GameScene。
    /// </summary>
    private void LoadGameSceneAdditive()
    {
        StartCoroutine(LoadGameSceneCoroutine());
    }

    private IEnumerator LoadGameSceneCoroutine()
    {
        // 卸载 MainMenu（从主菜单进入）或 GameScene（关卡切换），避免重复加载
        yield return UnloadSceneIfLoadedAsync(mainMenuSceneName);
        yield return UnloadSceneIfLoadedAsync(gameSceneName);

        SceneManager.LoadScene(gameSceneName, LoadSceneMode.Additive);
        Scene newScene = SceneManager.GetSceneByName(gameSceneName);
        if (newScene.isLoaded)
            SceneManager.SetActiveScene(newScene);
    }

    /// <summary>
    /// 卸载指定场景（若已加载），返回可 yield 的 AsyncOperation 或 null。
    /// </summary>
    private AsyncOperation UnloadSceneIfLoadedAsync(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || sceneName == persistentSceneName)
            return null;
        Scene s = SceneManager.GetSceneByName(sceneName);
        return s.isLoaded ? SceneManager.UnloadSceneAsync(s) : null;
    }

    /// <summary>
    /// 切换场景：先卸载 fromScene，再附加加载 toScene，并设为活动场景。
    /// </summary>
    private IEnumerator SwitchToSceneAsync(string fromScene, string toScene)
    {
        yield return UnloadSceneIfLoadedAsync(fromScene);

        SceneManager.LoadScene(toScene, LoadSceneMode.Additive);
        Scene newScene = SceneManager.GetSceneByName(toScene);
        if (newScene.isLoaded)
            SceneManager.SetActiveScene(newScene);
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

