using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using EggRogue;

/// <summary>
/// UI管理器 - 负责统一管理所有UI面板的显示和隐藏。
/// 
/// 使用方式：
/// 1. 在场景中创建一个空物体挂载本脚本，命名为 "UIManager"
/// 2. 在 Inspector 中拖入各个 Panel 的引用（MainMenuPanel、GameHudPanel 等）
/// 3. 通过 UIManager.Instance.ShowXXX() 来显示对应面板
/// 
/// 注意：
/// - 采用单例模式，通过 DontDestroyOnLoad 常驻
/// - 显示某个面板时会自动隐藏其他同类型的面板（避免重叠）
/// </summary>
public class UIManager : MonoBehaviour
{
    private static UIManager _instance;
    public static UIManager Instance => _instance;

    [Header("UI面板引用")]
    [Tooltip("主菜单面板")]
    public MainMenuPanel mainMenuPanel;

    [Tooltip("角色选择面板")]
    public CharacterSelectionPanel characterSelectionPanel;

    [Tooltip("游戏HUD面板（战斗界面）")]
    public GameHudPanel gameHudPanel;

    [Tooltip("结算面板（关卡胜利后显示）")]
    public ResultPanel resultPanel;

    [Tooltip("失败结算面板（玩家死亡后显示）")]
    public FailurePanel failurePanel;

    [Tooltip("完整通关结算面板（通过最后一关后显示）")]
    public ClearPanel clearPanel;

    [Tooltip("卡片选择面板")]
    public CardSelectionPanel cardSelectionPanel;

    [Tooltip("武器选择面板（首次进入游戏时选择起始武器）")]
    public WeaponSelectionPanel weaponSelectionPanel;

    [Tooltip("属性面板（按ESC打开）")]
    public AttributePanel attributePanel;

    [Tooltip("设置面板（从 GameHUD 设置按钮打开，调节操控手感等）")]
    public SettingsPanel settingsPanel;

    private void Awake()
    {
        // Unity 限制：DontDestroyOnLoad 只能作用于“根 GameObject”（或挂在根物体上的组件）。
        // 为了兼容你把 UIManager 挂在 Canvas/子物体的情况，这里一律对根节点做常驻。
        GameObject rootGO = transform.root != null ? transform.root.gameObject : gameObject;

        if (_instance != null && _instance != this)
        {
            // 销毁整套重复的 UI 根节点，避免残留子物体导致面板引用错乱
            Destroy(rootGO);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(rootGO);
    }

    private void Start()
    {
        LevelManager.OnGameClear += OnGameClear;
        ShowMainMenu();
    }

    private void OnDestroy()
    {
        LevelManager.OnGameClear -= OnGameClear;
    }

    private void OnGameClear(int level, int gold)
    {
        ShowClear(level, gold);
    }

        private void Update()
        {
            // 全局检测 ESC，用于打开/关闭属性面板
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                // 仅在“战斗场景/战斗状态”时响应
                // 不能只依赖 HUD 的 IsVisible：因为场景刚切换时，HUD 可能还没来得及 Show()，会导致第一次 ESC 没反应
                bool inGameContext = false;

                if (GameManager.Instance != null)
                {
                    // 以当前场景名判断最稳（避免 UI 初始化时序问题）
                    string activeSceneName = SceneManager.GetActiveScene().name;
                    inGameContext = (activeSceneName == GameManager.Instance.GameSceneName) || GameManager.Instance.IsInGame;
                }
                else
                {
                    // 兜底：仍保留旧判断
                    inGameContext =
                        (gameHudPanel != null && gameHudPanel.IsVisible()) ||
                        (resultPanel != null && resultPanel.IsVisible()) ||
                        (failurePanel != null && failurePanel.IsVisible()) ||
                        (clearPanel != null && clearPanel.IsVisible()) ||
                        (cardSelectionPanel != null && cardSelectionPanel.IsVisible());
                }

                if (inGameContext)
                {
                    ToggleAttributePanel();
                }
            }
        }

    /// <summary>
    /// 显示主菜单面板，隐藏其他面板
    /// </summary>
    public void ShowMainMenu()
    {
        HideAllPanels();
        if (mainMenuPanel != null)
        {
            mainMenuPanel.Show();
        }
        else
        {
            Debug.LogWarning("UIManager: mainMenuPanel 未设置，请在 Inspector 中拖入引用。");
        }
    }

    /// <summary>
    /// 显示角色选择面板，隐藏其他面板
    /// </summary>
    public void ShowCharacterSelection()
    {
        HideAllPanels();
        if (characterSelectionPanel != null)
        {
            characterSelectionPanel.Show();
        }
        else
        {
            Debug.LogWarning("UIManager: characterSelectionPanel 未设置，跳过角色选择，直接进入游戏");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadGameScene(1);
            }
        }
    }

    /// <summary>
    /// 显示游戏HUD面板，隐藏其他面板
    /// </summary>
    public void ShowGameHUD()
    {
        HideAllPanels();
        if (gameHudPanel != null)
        {
            gameHudPanel.Show();
        }
        else
        {
            Debug.LogWarning("UIManager: gameHudPanel 未设置，请在 Inspector 中拖入引用。");
        }
    }

    /// <summary>
    /// 显示结算面板（关卡胜利后）。
    /// </summary>
    public void ShowResult(int collectedGold, int victoryReward)
    {
        Debug.Log($"UIManager: ShowResult({collectedGold}, {victoryReward}) 被调用");
        HideAllPanels();
        
        if (resultPanel != null)
        {
            Debug.Log("UIManager: 找到 resultPanel，调用 ShowResult");
            resultPanel.ShowResult(collectedGold, victoryReward);
        }
        else
        {
            Debug.LogWarning("UIManager: resultPanel 未设置！请在 Inspector 中绑定 ResultPanel 引用。跳过结算界面，直接进入卡片选择");
            // 如果没有结算界面，直接进入卡片选择
            ShowCardSelection();
        }
    }

    /// <summary>
    /// 显示失败结算面板（玩家死亡后）。
    /// </summary>
    /// <param name="levelReached">到达的关卡（死亡时所在关）</param>
    /// <param name="gold">当前总金币</param>
    public void ShowFailure(int levelReached, int gold)
    {
        HideAllPanels();
        if (failurePanel != null)
        {
            failurePanel.ShowFailure(levelReached, gold);
        }
        else
        {
            Debug.LogWarning("UIManager: failurePanel 未设置！跳过失败界面，直接返回主菜单。");
            if (GameManager.Instance != null)
                GameManager.Instance.ReturnToMenu();
        }
    }

    /// <summary>
    /// 显示完整通关结算面板（通过最后一关后）。
    /// </summary>
    /// <param name="levelReached">通关的关卡（最后一关）</param>
    /// <param name="gold">当前总金币</param>
    public void ShowClear(int levelReached, int gold)
    {
        HideAllPanels();
        if (clearPanel != null)
        {
            clearPanel.ShowClear(levelReached, gold);
        }
        else
        {
            Debug.LogWarning("UIManager: clearPanel 未设置！跳过完整通关界面，直接返回主菜单。");
            if (GameManager.Instance != null)
                GameManager.Instance.ReturnToMenu();
        }
    }

    /// <summary>
    /// 显示武器选择面板（首次进入游戏时）。
    /// </summary>
    public void ShowWeaponSelection()
    {
        HideAllPanels();
        if (weaponSelectionPanel != null)
        {
            weaponSelectionPanel.Show();
        }
        else
        {
            Debug.LogWarning("UIManager: weaponSelectionPanel 未设置，跳过武器选择，直接进入游戏");
            if (WeaponInventoryManager.Instance != null)
                WeaponInventoryManager.Instance.InitializeFromStarterWeapon();
            if (GameManager.Instance != null)
                GameManager.Instance.LoadGameScene(1);
        }
    }

    /// <summary>
    /// 是否存在武器选择面板
    /// </summary>
    public bool HasWeaponSelectionPanel()
    {
        return weaponSelectionPanel != null;
    }

    /// <summary>
    /// 显示卡片选择面板。
    /// </summary>
    public void ShowCardSelection()
    {
        HideAllPanels();
        if (cardSelectionPanel != null)
        {
            cardSelectionPanel.ShowCardSelection();
        }
        else
        {
            Debug.LogWarning("UIManager: cardSelectionPanel 未设置，请在 Inspector 中拖入引用。");
        }
    }

    /// <summary>
    /// 显示设置面板（覆盖层，不隐藏 GameHUD）
    /// </summary>
    public void ShowSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.Show();
        }
        else
        {
            Debug.LogWarning("UIManager: settingsPanel 未设置，请在 Inspector 中拖入引用。");
        }
    }

    /// <summary>
    /// 切换属性面板显示/隐藏（按ESC调用）
    /// 注意：属性面板是覆盖层，不会隐藏其他面板
    /// </summary>
    public void ToggleAttributePanel()
    {
        if (attributePanel != null)
        {
            // 属性面板是覆盖层，直接切换显示/隐藏，不影响其他面板
            attributePanel.TogglePanel();
        }
        else
        {
            Debug.LogWarning("UIManager: attributePanel 未设置，请在 Inspector 中拖入引用。");
        }
    }

    /// <summary>
    /// 隐藏所有面板（用于切换场景或状态时）
    /// 注意：属性面板是覆盖层，通常不在这里隐藏
    /// </summary>
    private void HideAllPanels()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.Hide();
        }
        if (characterSelectionPanel != null)
        {
            characterSelectionPanel.Hide();
        }
        if (gameHudPanel != null)
        {
            gameHudPanel.Hide();
        }
        if (resultPanel != null)
        {
            resultPanel.Hide();
        }
        if (failurePanel != null)
        {
            failurePanel.Hide();
        }
        if (clearPanel != null)
        {
            clearPanel.Hide();
        }
        if (cardSelectionPanel != null)
        {
            cardSelectionPanel.Hide();
        }
        if (weaponSelectionPanel != null)
        {
            weaponSelectionPanel.Hide();
        }
        if (settingsPanel != null)
        {
            settingsPanel.Hide();
        }
        // 属性面板是覆盖层，通常不在这里隐藏
        // 如果需要强制隐藏，可以取消下面的注释
        // if (attributePanel != null)
        // {
        //     attributePanel.Hide();
        // }
    }

    /// <summary>
    /// 获取当前显示的面板（用于调试）
    /// </summary>
    public string GetCurrentPanelName()
    {
        if (mainMenuPanel != null && mainMenuPanel.IsVisible())
        {
            return "MainMenu";
        }
        if (gameHudPanel != null && gameHudPanel.IsVisible())
        {
            return "GameHUD";
        }
        if (resultPanel != null && resultPanel.IsVisible())
        {
            return "Result";
        }
        if (failurePanel != null && failurePanel.IsVisible())
        {
            return "Failure";
        }
        if (clearPanel != null && clearPanel.IsVisible())
        {
            return "Clear";
        }
        if (characterSelectionPanel != null && characterSelectionPanel.IsVisible())
        {
            return "CharacterSelection";
        }
        if (weaponSelectionPanel != null && weaponSelectionPanel.IsVisible())
        {
            return "WeaponSelection";
        }
        if (cardSelectionPanel != null && cardSelectionPanel.IsVisible())
        {
            return "CardSelection";
        }
        if (attributePanel != null && attributePanel.IsVisible())
        {
            return "Attribute";
        }
        return "None";
    }
}
