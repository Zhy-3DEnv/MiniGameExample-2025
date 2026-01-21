using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏HUD面板 - 战斗时显示的UI（血条、金币、返回按钮等）。
/// 
/// 使用方式：
/// 1. 在 Unity 中创建一个 Canvas，下面创建一个 Panel（命名为 GameHudPanel）
/// 2. 在 Panel 下创建按钮（例如"返回主菜单"按钮）
/// 3. 将这个脚本挂载到 Panel 上
/// 4. 在 Inspector 中拖入按钮引用
/// 
/// 后续可以在这里添加：
/// - 血量条、护盾条
/// - 金币/经验值显示
/// - 技能冷却图标
/// - 暂停按钮等
/// </summary>
public class GameHudPanel : BaseUIPanel
{
    [Header("按钮引用")]
    [Tooltip("返回主菜单按钮")]
    public Button returnToMenuButton;

    [Header("HUD元素（后续扩展用）")]
    [Tooltip("血量条（后续添加）")]
    public Slider healthBar;

    [Tooltip("金币文本（后续添加）")]
    public Text goldText;

    private void Start()
    {
        // 自动绑定按钮事件
        SetupButtons();
    }

    /// <summary>
    /// 设置按钮事件
    /// </summary>
    private void SetupButtons()
    {
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveAllListeners();
            returnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);
        }
        else
        {
            Debug.LogWarning("GameHudPanel: returnToMenuButton 未设置，请在 Inspector 中拖入按钮引用。");
        }
    }

    /// <summary>
    /// 返回主菜单按钮点击事件
    /// </summary>
    private void OnReturnToMenuClicked()
    {
        Debug.Log("GameHudPanel: 返回主菜单按钮被点击");

        // 调用 GameManager 返回主菜单
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMenu();
            
            // 切换UI到主菜单
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowMainMenu();
            }
        }
        else
        {
            Debug.LogError("GameHudPanel: GameManager.Instance 为空，请确保场景中有 GameManager 对象。");
        }
    }

    /// <summary>
    /// 更新血量条（后续扩展用）
    /// </summary>
    public void UpdateHealthBar(float currentHP, float maxHP)
    {
        if (healthBar != null)
        {
            healthBar.value = currentHP / maxHP;
        }
    }

    /// <summary>
    /// 更新金币显示（后续扩展用）
    /// </summary>
    public void UpdateGold(int gold)
    {
        if (goldText != null)
        {
            goldText.text = $"金币: {gold}";
        }
    }

    /// <summary>
    /// 面板显示时的额外逻辑
    /// </summary>
    protected override void OnShow()
    {
        base.OnShow();
        Debug.Log("GameHudPanel: 游戏HUD面板已显示");
        // 后续可以在这里刷新HUD数据
    }

    /// <summary>
    /// 面板隐藏时的额外逻辑
    /// </summary>
    protected override void OnHide()
    {
        base.OnHide();
        Debug.Log("GameHudPanel: 游戏HUD面板已隐藏");
    }
}
