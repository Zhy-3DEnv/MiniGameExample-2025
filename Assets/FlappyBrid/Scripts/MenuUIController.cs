using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 菜单UI控制器
/// 处理开始菜单和关卡完成界面的按钮交互
/// </summary>
public class MenuUIController : MonoBehaviour
{
    [Header("开始菜单按钮")]
    public Button startButton;  // 开始按钮
    
    [Header("关卡完成界面按钮")]
    public Button nextLevelButton;  // 下一关按钮
    public Button returnToMenuButton;  // 返回菜单按钮
    
    [Header("游戏结束界面按钮")]
    public Button restartButton;  // 重新开始按钮
    public Button menuButton;     // 返回菜单按钮（游戏结束界面）
    
    private logicManager logicManager;
    
    void Start()
    {
        // 获取logicManager
        GameObject logicObject = GameObject.FindGameObjectWithTag("Logic");
        if (logicObject != null)
        {
            logicManager = logicObject.GetComponent<logicManager>();
        }
        
        // 绑定按钮事件
        SetupButtons();
    }
    
    /// <summary>
    /// 设置按钮事件
    /// </summary>
    private void SetupButtons()
    {
        // 开始按钮
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
        
        // 下一关按钮
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(OnNextLevelButtonClicked);
        }
        
        // 返回菜单按钮（关卡完成界面）
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveAllListeners();
            returnToMenuButton.onClick.AddListener(OnReturnToMenuButtonClicked);
        }
        
        // 重新开始按钮（游戏结束界面）
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }
        
        // 返回菜单按钮（游戏结束界面）
        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(OnReturnToMenuButtonClicked);
        }
    }
    
    /// <summary>
    /// 开始按钮点击事件
    /// </summary>
    private void OnStartButtonClicked()
    {
        // 播放按钮点击音效
        PlayButtonClickSound();
        
        if (logicManager != null)
        {
            logicManager.StartGame();
        }
    }
    
    /// <summary>
    /// 下一关按钮点击事件
    /// </summary>
    private void OnNextLevelButtonClicked()
    {
        // 播放按钮点击音效
        PlayButtonClickSound();
        
        if (logicManager != null)
        {
            logicManager.NextLevel();
        }
    }
    
    /// <summary>
    /// 返回菜单按钮点击事件
    /// </summary>
    private void OnReturnToMenuButtonClicked()
    {
        // 播放按钮点击音效
        PlayButtonClickSound();
        
        if (logicManager != null)
        {
            logicManager.ReturnToMenu();
        }
    }
    
    /// <summary>
    /// 重新开始按钮点击事件
    /// </summary>
    private void OnRestartButtonClicked()
    {
        // 播放按钮点击音效
        PlayButtonClickSound();
        
        if (logicManager != null)
        {
            logicManager.restartGame();
        }
    }
    
    /// <summary>
    /// 播放按钮点击音效
    /// </summary>
    private void PlayButtonClickSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSound();
        }
    }
}

