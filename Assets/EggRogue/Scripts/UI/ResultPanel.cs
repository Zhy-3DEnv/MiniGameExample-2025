using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 结算界面 - 显示关卡胜利后的奖励信息。
/// 使用方式：
/// 1. 在 Canvas 下创建 Panel，命名为 ResultPanel
/// 2. 添加本脚本
/// 3. 在 Inspector 中绑定 UI 元素（金币文本、奖励文本等）
/// </summary>
public class ResultPanel : BaseUIPanel
{
    [Header("UI 引用")]
    [Tooltip("拾取金币文本（例如：拾取金币: 50）")]
    public Text collectedGoldText;

    [Tooltip("胜利奖励文本（例如：胜利奖励: 50）")]
    public Text victoryRewardText;

    [Tooltip("总金币文本（例如：总计: 100）")]
    public Text totalGoldText;

    [Tooltip("提示文本（例如：按 Enter 键或点击按钮继续...）")]
    public Text promptText;

    [Tooltip("继续按钮（用于移动端点击继续）")]
    public Button continueButton;

    private int collectedGold = 0;
    private int victoryReward = 0;
    private bool waitingForInput = false;

    // 保存暂停前的状态
    private EnemySpawner enemySpawner;
    private CharacterController characterController;
    private PlayerCombatController playerCombatController;
    private bool wasSpawning = false;
    private bool wasCharacterEnabled = false;
    private bool wasCombatEnabled = false;

    private void Update()
    {
        if (waitingForInput)
        {
            // 使用新的 Input System 检测“确认键”
            Keyboard keyboard = Keyboard.current;

            // 仅在 PC/键盘环境下使用 Enter / 小键盘 Enter 继续
            if (keyboard != null &&
                (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                OnInputReceived();
                return;
            }
        }
    }

    /// <summary>
    /// 显示结算界面（外部调用，例如 LevelTimer.OnLevelVictory）。
    /// </summary>
    /// <param name="collected">拾取的金币数</param>
    /// <param name="reward">胜利奖励金币数</param>
    public void ShowResult(int collected, int reward)
    {
        Debug.Log($"ResultPanel: ShowResult({collected}, {reward}) 被调用");
        
        collectedGold = collected;
        victoryReward = reward;

        if (collectedGoldText != null)
            collectedGoldText.text = $"拾取金币: {collectedGold}";
        else
            Debug.LogWarning("ResultPanel: collectedGoldText 未设置");
            
        if (victoryRewardText != null)
            victoryRewardText.text = $"胜利奖励: {victoryReward}";
        else
            Debug.LogWarning("ResultPanel: victoryRewardText 未设置");
            
        if (totalGoldText != null)
            totalGoldText.text = $"总计: {collectedGold + victoryReward}";
        else
            Debug.LogWarning("ResultPanel: totalGoldText 未设置");

        // 添加奖励到总金币
        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.AddGold(victoryReward);
            Debug.Log($"ResultPanel: 已添加 {victoryReward} 金币到 GoldManager");
        }
        else
        {
            Debug.LogWarning("ResultPanel: GoldManager.Instance 为空");
        }

        Debug.Log("ResultPanel: 显示结算界面");
        Show();
        waitingForInput = true;

        // 确保继续按钮处于可见状态（用于移动端点击继续）
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnInputReceived);
        }
    }

    private void OnInputReceived()
    {
        if (!waitingForInput)
            return;

        waitingForInput = false;
        Hide();
        
        // 通知 UIManager 显示卡片选择界面
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowCardSelection();
        }
        else
        {
            Debug.LogWarning("ResultPanel: UIManager.Instance 为空，无法显示卡片选择界面");
        }
    }

    protected override void OnShow()
    {
        base.OnShow();
        if (promptText != null)
            promptText.text = "按 Enter 键或点击按钮继续...";
        waitingForInput = true;
        
        // 暂停所有游戏玩法系统（通过统一的 GameplayPauseManager）
        if (EggRogue.GameplayPauseManager.Instance != null)
        {
            EggRogue.GameplayPauseManager.Instance.RequestPause("ResultPanel");
        }
    }

    protected override void OnHide()
    {
        base.OnHide();
        waitingForInput = false;
        
        // 结束结算界面自身的暂停请求（进入选卡界面时会再次请求暂停）
        if (EggRogue.GameplayPauseManager.Instance != null)
        {
            EggRogue.GameplayPauseManager.Instance.RequestResume("ResultPanel");
        }
    }
}
