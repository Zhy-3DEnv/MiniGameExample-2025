using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主菜单面板 - 显示开始游戏等按钮。
/// 
/// 使用方式：
/// 1. 在 Unity 中创建一个 Canvas，下面创建一个 Panel（命名为 MainMenuPanel）
/// 2. 在 Panel 下创建按钮（例如"开始游戏"按钮）
/// 3. 将这个脚本挂载到 Panel 上
/// 4. 在 Inspector 中拖入按钮引用
/// 5. 按钮的 OnClick 事件会自动绑定（或在 Inspector 中手动绑定）
/// </summary>
public class MainMenuPanel : BaseUIPanel
{
    [Header("按钮引用")]
    [Tooltip("开始游戏按钮")]
    public Button startGameButton;

    private void Start()
    {
        // 自动绑定按钮事件（如果按钮引用已设置）
        SetupButtons();
    }

    /// <summary>
    /// 设置按钮事件
    /// </summary>
    private void SetupButtons()
    {
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }
        else
        {
            Debug.LogWarning("MainMenuPanel: startGameButton 未设置，请在 Inspector 中拖入按钮引用。");
        }
    }

    /// <summary>
    /// 开始游戏按钮点击事件 - 进入角色选择界面
    /// </summary>
    private void OnStartGameClicked()
    {
        Debug.Log("MainMenuPanel: 开始游戏按钮被点击");

        // 显示角色选择界面
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowCharacterSelection();
        }
        else
        {
            Debug.LogError("MainMenuPanel: UIManager.Instance 为空，请确保场景中有 UIManager 对象。");
        }
    }

    /// <summary>
    /// 面板显示时的额外逻辑（可选）
    /// </summary>
    protected override void OnShow()
    {
        base.OnShow();
        // Debug.Log("MainMenuPanel: 主菜单面板已显示");
    }

    /// <summary>
    /// 面板隐藏时的额外逻辑（可选）
    /// </summary>
    protected override void OnHide()
    {
        base.OnHide();
        // Debug.Log("MainMenuPanel: 主菜单面板已隐藏");
    }
}
