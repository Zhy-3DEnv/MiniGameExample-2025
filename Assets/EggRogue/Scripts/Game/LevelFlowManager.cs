using UnityEngine;
using EggRogue;

/// <summary>
/// 关卡流程管理器 - 管理关卡计时器、胜利判断、结算、卡片选择等流程。
/// 挂在 GameScene 的某个对象上（例如空物体 "LevelFlowManager"）。
/// </summary>
public class LevelFlowManager : MonoBehaviour
{
    [Header("组件引用")]
    [Tooltip("关卡计时器（自动查找）")]
    public LevelTimer levelTimer;

    private int goldAtLevelStart = 0;

    private void Start()
    {
        Debug.Log("LevelFlowManager: Start() 被调用");
        
        if (levelTimer == null)
        {
            levelTimer = FindObjectOfType<LevelTimer>();
            Debug.Log($"LevelFlowManager: 自动查找 LevelTimer，结果: {(levelTimer != null ? "找到" : "未找到")}");
        }

        if (levelTimer != null)
        {
            levelTimer.OnLevelVictory.AddListener(OnLevelVictory);
            levelTimer.OnPlayerDeath.AddListener(OnPlayerDeath);
            Debug.Log("LevelFlowManager: 已订阅 LevelTimer 事件");
        }
        else
        {
            Debug.LogError("LevelFlowManager: 未找到 LevelTimer！请在 GameScene 中添加 LevelTimer 组件。");
        }

        // 记录关卡开始时的金币数（用于计算拾取金币）
        if (GoldManager.Instance != null)
        {
            goldAtLevelStart = GoldManager.Instance.Gold;
            Debug.Log($"LevelFlowManager: 记录关卡开始金币数: {goldAtLevelStart}");
        }
        else
        {
            Debug.LogWarning("LevelFlowManager: GoldManager.Instance 为空");
        }
    }

    private void OnEnable()
    {
        // 重新记录金币（场景重新加载时）
        if (GoldManager.Instance != null)
        {
            goldAtLevelStart = GoldManager.Instance.Gold;
        }
    }

    private void OnLevelVictory()
    {
        Debug.Log("LevelFlowManager: OnLevelVictory() 被调用 - 关卡胜利");

        // 计算拾取的金币数（当前金币 - 关卡开始时的金币）
        int collectedGold = 0;
        if (GoldManager.Instance != null)
        {
            collectedGold = GoldManager.Instance.Gold - goldAtLevelStart;
            Debug.Log($"LevelFlowManager: 拾取金币 = {GoldManager.Instance.Gold} - {goldAtLevelStart} = {collectedGold}");
        }
        else
        {
            Debug.LogWarning("LevelFlowManager: GoldManager.Instance 为空，无法计算拾取金币");
        }

        // 获取胜利奖励
        int victoryReward = 0;
        if (EggRogue.LevelManager.Instance != null)
        {
            var levelData = EggRogue.LevelManager.Instance.GetCurrentLevelData();
            if (levelData != null)
            {
                victoryReward = levelData.victoryRewardGold;
                Debug.Log($"LevelFlowManager: 胜利奖励 = {victoryReward} (来自 LevelData)");
            }
            else
            {
                Debug.LogWarning("LevelFlowManager: 未找到当前关卡的 LevelData");
            }
        }
        else
        {
            Debug.LogWarning("LevelFlowManager: LevelManager.Instance 为空");
        }

        // 通过 UIManager 显示结算界面（ResultPanel 在 PersistentScene，通过 UIManager 访问）
        if (UIManager.Instance != null)
        {
            Debug.Log($"LevelFlowManager: 调用 UIManager.ShowResult({collectedGold}, {victoryReward})");
            UIManager.Instance.ShowResult(collectedGold, victoryReward);
        }
        else
        {
            Debug.LogError("LevelFlowManager: UIManager.Instance 为空，无法显示结算界面！");
        }
    }

    private void OnPlayerDeath()
    {
        Debug.Log("LevelFlowManager: 玩家死亡，关卡失败");
        // 可以在这里显示失败界面，或直接返回主菜单
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMenu();
        }
    }

    private void OnDestroy()
    {
        if (levelTimer != null)
        {
            levelTimer.OnLevelVictory.RemoveListener(OnLevelVictory);
            levelTimer.OnPlayerDeath.RemoveListener(OnPlayerDeath);
        }
    }
}
