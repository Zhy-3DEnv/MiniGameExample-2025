using UnityEngine;
using UnityEngine.UI;
using EggRogue;

/// <summary>
/// 失败结算界面 - 玩家死亡后显示，可查看到达关卡、当前金币等，选择返回主菜单或再试一次。
/// 使用方式：
/// 1. 在 Canvas 下创建 Panel，命名为 FailurePanel
/// 2. 添加本脚本
/// 3. 在 Inspector 中绑定 UI 元素（标题、关卡/金币文本、返回主菜单 / 再试一次按钮）
/// </summary>
public class FailurePanel : BaseUIPanel
{
    [Header("UI 引用")]
    [Tooltip("标题文本（例如：挑战失败）")]
    public Text titleText;

    [Tooltip("到达关卡文本（可选，例如：到达关卡: 3）")]
    public Text levelReachedText;

    [Tooltip("当前金币文本（可选，例如：当前金币: 120）")]
    public Text goldText;

    [Tooltip("返回主菜单按钮")]
    public Button returnToMenuButton;

    [Tooltip("再试一次按钮（从第 1 关重新开始）")]
    public Button retryButton;

    private void Start()
    {
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveAllListeners();
            returnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);
        }
        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetryClicked);
        }
    }

    /// <summary>
    /// 显示失败结算界面（外部调用，例如 LevelFlowManager.OnPlayerDeath）。
    /// </summary>
    /// <param name="levelReached">到达的关卡（死亡时所在关）</param>
    /// <param name="gold">当前总金币</param>
    public void ShowFailure(int levelReached, int gold)
    {
        if (titleText != null)
            titleText.text = "挑战失败";

        if (levelReachedText != null)
            levelReachedText.text = $"到达关卡: {levelReached}";

        if (goldText != null)
            goldText.text = $"当前金币: {gold}";

        Show();
    }

    private void OnReturnToMenuClicked()
    {
        Hide();
        if (GameManager.Instance != null)
            GameManager.Instance.ReturnToMenu();
    }

    private void OnRetryClicked()
    {
        Hide();
        if (LevelManager.Instance != null)
            LevelManager.Instance.RestartFromLevel1();
        else if (GameManager.Instance != null)
            GameManager.Instance.LoadGameScene(1);
    }

    protected override void OnShow()
    {
        base.OnShow();
        if (EggRogue.GameplayPauseManager.Instance != null)
            EggRogue.GameplayPauseManager.Instance.RequestPause("FailurePanel");
    }

    protected override void OnHide()
    {
        base.OnHide();
        if (EggRogue.GameplayPauseManager.Instance != null)
            EggRogue.GameplayPauseManager.Instance.RequestResume("FailurePanel");
    }
}
