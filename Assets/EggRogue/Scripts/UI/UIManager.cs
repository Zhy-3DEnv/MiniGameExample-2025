using UnityEngine;

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

    [Tooltip("游戏HUD面板（战斗界面）")]
    public GameHudPanel gameHudPanel;

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
        // 初始化时显示主菜单，隐藏游戏HUD
        ShowMainMenu();
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
    /// 隐藏所有面板（用于切换场景或状态时）
    /// </summary>
    private void HideAllPanels()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.Hide();
        }
        if (gameHudPanel != null)
        {
            gameHudPanel.Hide();
        }
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
        return "None";
    }
}
