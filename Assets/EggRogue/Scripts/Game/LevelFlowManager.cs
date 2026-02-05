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
        //debug.log("LevelFlowManager: Start() 被调用");
        
        if (levelTimer == null)
        {
            levelTimer = FindObjectOfType<LevelTimer>();
            //debug.log($"LevelFlowManager: 自动查找 LevelTimer，结果: {(levelTimer != null ? "找到" : "未找到")}");
        }

        if (levelTimer != null)
        {
            levelTimer.OnLevelVictory.AddListener(OnLevelVictory);
            levelTimer.OnPlayerDeath.AddListener(OnPlayerDeath);
            // //debug.log("LevelFlowManager: 已订阅 LevelTimer 事件");
        }
        else
        {
            //debug.logError("LevelFlowManager: 未找到 LevelTimer！请在 GameScene 中添加 LevelTimer 组件。");
        }

        // 记录关卡开始时的金币数（用于计算拾取金币）
        if (GoldManager.Instance != null)
        {
            goldAtLevelStart = GoldManager.Instance.Gold;
            //debug.log($"LevelFlowManager: 记录关卡开始金币数: {goldAtLevelStart}");
        }
        else
        {
            //debug.logWarning("LevelFlowManager: GoldManager.Instance 为空");
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
        //debug.log("LevelFlowManager: OnLevelVictory() 被调用 - 关卡胜利");

        // 计算拾取的金币数（当前金币 - 关卡开始时的金币）
        int collectedGold = 0;
        if (GoldManager.Instance != null)
        {
            collectedGold = GoldManager.Instance.Gold - goldAtLevelStart;
            //debug.log($"LevelFlowManager: 拾取金币 = {GoldManager.Instance.Gold} - {goldAtLevelStart} = {collectedGold}");
        }
        else
        {
            //debug.logWarning("LevelFlowManager: GoldManager.Instance 为空，无法计算拾取金币");
        }

        // 获取胜利奖励
        int victoryReward = 0;
        if (EggRogue.LevelManager.Instance != null)
        {
            var levelData = EggRogue.LevelManager.Instance.GetCurrentLevelData();
            if (levelData != null)
            {
                victoryReward = levelData.victoryRewardGold;
                //debug.log($"LevelFlowManager: 胜利奖励 = {victoryReward} (来自 LevelData)");
            }
            else
            {
                //debug.logWarning("LevelFlowManager: 未找到当前关卡的 LevelData");
            }
        }
        else
        {
            //debug.logWarning("LevelFlowManager: LevelManager.Instance 为空");
        }

        // Debug：统计当前场景中“还留在地上”的金币总价值（玩家此刻还能去捡的）
        int remainingCoinValue = 0;
        var coins = FindObjectsOfType<Coin>();
        foreach (var coin in coins)
        {
            if (coin != null)
            {
                remainingCoinValue += coin.value;
            }
        }
        //debug.log($"[Gold Debug] 本关结束时场景中剩余可拾取金币总价值 = {remainingCoinValue}");

        // 清空场景内的怪物、子弹等 GamePlay 元素，避免进入选卡/商店后点继续时看到上一关残留
        ClearGameplayElements();

        if (UIManager.Instance == null)
        {
            //debug.logError("LevelFlowManager: UIManager.Instance 为空，无法显示结算界面！");
            return;
        }

        // 最后一关：跳过 ResultPanel，先发胜利奖励再直接显示完整通关界面
        if (EggRogue.LevelManager.Instance != null && EggRogue.LevelManager.Instance.IsLastLevel())
        {
            ClearGameplayElements();
            if (GoldManager.Instance != null)
            {
                GoldManager.Instance.AddGold(victoryReward);
                //debug.log($"LevelFlowManager: 最后一关胜利，已添加 {victoryReward} 金币，直接显示完整通关");
            }
            int level = EggRogue.LevelManager.Instance.CurrentLevel;
            int gold = GoldManager.Instance != null ? GoldManager.Instance.Gold : 0;
            UIManager.Instance.ShowClear(level, gold);
            return;
        }

        // 非最后一关：显示 ResultPanel（拾取金币、胜利奖励 → 继续 → 选卡）
        //debug.log($"LevelFlowManager: 调用 UIManager.ShowResult({collectedGold}, {victoryReward})");
        UIManager.Instance.ShowResult(collectedGold, victoryReward);
    }

    private void OnPlayerDeath()
    {
        //debug.log("LevelFlowManager: 玩家死亡，关卡失败");
        int levelReached = EggRogue.LevelManager.Instance != null ? EggRogue.LevelManager.Instance.CurrentLevel : 1;
        int gold = GoldManager.Instance != null ? GoldManager.Instance.Gold : 0;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowFailure(levelReached, gold);
        }
        else
        {
            //debug.logWarning("LevelFlowManager: UIManager.Instance 为空，无法显示失败界面，退回主菜单");
            if (GameManager.Instance != null)
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

    /// <summary>
    /// 清空场景内所有 GamePlay 元素（怪物、子弹、金币等），避免选卡/商店后点继续时看到上一关残留。
    /// </summary>
    private void ClearGameplayElements()
    {
        // 敌人
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.ClearAllEnemies();

        // 子弹/投射物
        var projectiles = Object.FindObjectsOfType<Projectile>();
        foreach (var p in projectiles)
        {
            if (p != null)
                Object.Destroy(p.gameObject);
        }

        // 地面金币（可选，使场景更干净）
        var coins = Object.FindObjectsOfType<Coin>();
        foreach (var c in coins)
        {
            if (c != null)
                Object.Destroy(c.gameObject);
        }
    }
}
