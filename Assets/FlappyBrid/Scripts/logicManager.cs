using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class logicManager : MonoBehaviour
{
    [Header("游戏数据")]
    [Tooltip("当前关卡金币（当前关卡获得的金币）")]
    public int playerCoins = 0;
    [Tooltip("总金币（累计所有关卡的金币）")]
    public int totalCoins = 0;
    
    [Tooltip("当前关卡已产出的金币（只统计道具和怪物，用于产出控制）")]
    private int currentLevelCoinsEarned = 0;
    
    [Header("时间系统")]
    [Tooltip("当前关卡已用时间（秒）")]
    private float currentLevelTime = 0f;
    [Tooltip("是否正在计时")]
    private bool isTiming = false;
    
    [Header("金币显示")]
    [Tooltip("当前关卡获得的金币（不包括完成奖励，用于显示）")]
    private int currentLevelCoinsForDisplay = 0;

    [Header("游戏HUD（游戏进行中显示的UI）")]
    [Tooltip("游戏HUD面板（包含关卡信息和时间信息，游戏进行中显示）")]
    public GameObject gameHUD;
    [Tooltip("显示当前关卡的文本（位于HUD顶部）")]
    public Text levelText;
    [Tooltip("显示时间的文本（格式：x/n，位于HUD顶部）")]
    public Text timeText;  // 显示时间
    [Tooltip("显示总金币的文本（位于HUD顶部）")]
    public Text totalCoinsText;  // 显示总金币
    
    [Header("小鸟属性显示面板（预制体）")]
    [Tooltip("属性显示面板预制体（可在HUD、商店等场景中实例化）")]
    public GameObject attributesPanelPrefab;
    
    [Header("已实例化的属性显示面板列表")]
    [Tooltip("所有已实例化的属性显示面板（自动管理，也可手动添加）")]
    public List<BirdAttributesDisplayPanel> attributesPanels = new List<BirdAttributesDisplayPanel>();

    [Header("UI界面")]
    [Tooltip("游戏结束界面")]
    public GameObject gameOverScene;
    [Tooltip("开始菜单界面")]
    public GameObject startMenuScene;

    [Header("关卡完成界面")]
    [Tooltip("关卡完成主面板（包含所有关卡完成相关UI）")]
    public GameObject levelCompletePanel;

    [Header("关卡完成 - 结算画面（第一阶段：显示1秒）")]
    [Tooltip("结算画面面板（显示'关卡通过！'消息）")]
    public GameObject levelCompleteSettlementPanel;
    [Tooltip("结算画面文本（显示'关卡通过！'）")]
    public Text levelCompleteSettlementText;

    [Header("关卡完成 - 结果界面（第二阶段：显示按钮和详细信息）")]
    [Tooltip("结果信息面板（显示关卡详情和按钮）")]
    public GameObject levelCompleteResultPanel;
    [Tooltip("结果信息文本（显示关卡名称、分数等详细信息）")]
    public Text levelCompleteResultText;
    
    [Header("升级/商店界面")]
    [Tooltip("升级界面面板（显示在结算画面后）")]
    public GameObject upgradePanel;
    [Tooltip("升级界面容器（用于放置升级按钮）")]
    public Transform upgradeButtonContainer;
    [Tooltip("升级按钮预制体")]
    public GameObject upgradeButtonPrefab;

    [Header("关卡完成设置")]
    [Tooltip("Next Level按钮显示延迟（秒），结算画面会一直显示")]
    [Range(0.5f, 3f)]
    public float settlementDisplayDuration = 1f;

    [Header("关卡设置")]
    [Tooltip("当前关卡（由LevelManager管理）")]
    public int currentLevel = 1;

    private GameStateManager gameStateManager;
    private LevelManager levelManager;
    private Coroutine levelCompleteCoroutine;  // 关卡完成协程

    void Start()
    {
        // 初始化通用游戏暂停管理器
        GamePauseManager.Instance.enableAutoPause = true;

        // 获取游戏状态管理器
        gameStateManager = GameStateManager.Instance;
        gameStateManager.OnStateChanged += OnGameStateChanged;

        // 获取关卡管理器
        levelManager = LevelManager.Instance;

        // 初始化UI状态
        InitializeUI();

        // 重置游戏数据
        ResetGame();
    }

    void OnDestroy()
    {
        if (gameStateManager != null)
        {
            gameStateManager.OnStateChanged -= OnGameStateChanged;
        }
    }

    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        // 显示开始菜单
        if (startMenuScene != null) startMenuScene.SetActive(true);

        // 隐藏其他界面
        if (gameOverScene != null) gameOverScene.SetActive(false);
        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
        if (levelCompleteSettlementPanel != null) levelCompleteSettlementPanel.SetActive(false);
        if (levelCompleteResultPanel != null) levelCompleteResultPanel.SetActive(false);

        // 隐藏游戏HUD（菜单状态下不显示）
        if (gameHUD != null) gameHUD.SetActive(false);
    }

    /// <summary>
    /// 重置游戏数据（只重置当前关卡金币和时间，不重置关卡）
    /// </summary>
    private void ResetGame()
    {
        playerCoins = 0;
        currentLevelCoinsEarned = 0; // 重置产出跟踪
        currentLevelTime = 0f;
        isTiming = false;
        // 注意：totalCoins 不在这里重置，只在重新开始游戏或死亡时重置
        // 注意：关卡不在这里重置，保持当前关卡
        if (levelManager != null)
        {
            currentLevel = levelManager.GetCurrentLevelNumber();
        }
        else
        {
            currentLevel = 1;
        }
        UpdateTimeDisplay();
        UpdateLevelDisplay();
    }
    
    /// <summary>
    /// 重置到第一关（用于死亡或返回菜单时）
    /// </summary>
    private void ResetToFirstLevel()
    {
        if (levelManager != null)
        {
            levelManager.ResetToFirstLevel();
            currentLevel = 1;
        }
        else
        {
            currentLevel = 1;
        }
        // 更新显示
        UpdateLevelDisplay();
    }

    /// <summary>
    /// 重置小鸟状态
    /// </summary>
    private void ResetBird()
    {
        // 查找小鸟并重置
        BirdScript bird = FindObjectOfType<BirdScript>();
        if (bird != null)
        {
            // 小鸟会在OnGameStateChanged中自动重置，这里可以添加额外逻辑
        }
    }

    /// <summary>
    /// 游戏状态变化回调
    /// </summary>
    private void OnGameStateChanged(GameState newState)
    {
        // 停止之前的关卡完成协程
        if (levelCompleteCoroutine != null)
        {
            StopCoroutine(levelCompleteCoroutine);
            levelCompleteCoroutine = null;
        }

        switch (newState)
        {
            case GameState.Menu:
                ShowStartMenu();
                HideUpgradePanel(); // 隐藏升级界面
                // 返回菜单时重置总金币和关卡
                totalCoins = 0;
                ResetToFirstLevel();
                break;
            case GameState.Playing:
                HideAllMenus();
                ResetGame();
                ResetBird();
                ResumeAllPipes();
                // 开始计时
                StartTiming();
                // 更新拖尾长度（确保拖尾显示正确）
                UpdateBirdTrail();
                // 更新小鸟属性显示（游戏开始时显示初始属性）
                UpdateBirdAttributesDisplay();
                break;
            case GameState.LevelComplete:
                StopTiming();  // 停止计时
                ShowLevelComplete();
                StopAllPipes();
                break;
            case GameState.GameOver:
                StopTiming();  // 停止计时
                ShowGameOver();
                StopAllPipes();
                HideUpgradePanel(); // 隐藏升级界面
                // 死亡后重置关卡到第1关，重置总金币
                ResetToFirstLevel();
                totalCoins = 0;
                break;
        }
    }

    /// <summary>
    /// 显示开始菜单
    /// </summary>
    private void ShowStartMenu()
    {
        if (startMenuScene != null) startMenuScene.SetActive(true);
        if (gameOverScene != null) gameOverScene.SetActive(false);
        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
        if (gameHUD != null) gameHUD.SetActive(false);  // 菜单状态下隐藏HUD
    }

    /// <summary>
    /// 隐藏所有菜单
    /// </summary>
    private void HideAllMenus()
    {
        if (startMenuScene != null) startMenuScene.SetActive(false);
        if (gameOverScene != null) gameOverScene.SetActive(false);
        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
        if (upgradePanel != null) upgradePanel.SetActive(false);  // 隐藏升级界面
        if (gameHUD != null) gameHUD.SetActive(true);  // 游戏进行中显示HUD
    }

    /// <summary>
    /// 显示关卡完成界面
    /// </summary>
    private void ShowLevelComplete()
    {
        // 在显示结算画面之前，先累加当前关卡的金币到总金币
        // 这样结算画面就能显示正确的总金币数
        AddCurrentLevelCoinsToTotal();
        
        // 隐藏其他界面
        if (startMenuScene != null) startMenuScene.SetActive(false);
        if (gameOverScene != null) gameOverScene.SetActive(false);
        if (gameHUD != null) gameHUD.SetActive(false);  // 关卡完成时隐藏HUD
        HideUpgradePanel(); // 隐藏升级界面

        // 显示关卡完成主面板
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }

        // 显示第一阶段：结算画面（"关卡通过！"）
        ShowSettlementScreen();

        // 启动协程：延迟显示第二阶段（结果界面）
        levelCompleteCoroutine = StartCoroutine(ShowResultScreenAfterDelay());
    }
    
    /// <summary>
    /// 将当前关卡金币累加到总金币（包括完成奖励）
    /// 注意：由于金币已经实时累加到totalCoins，这里只需要添加完成奖励
    /// </summary>
    private void AddCurrentLevelCoinsToTotal()
    {
        // 1. 保存当前关卡获得的金币（不包括完成奖励，用于显示）
        currentLevelCoinsForDisplay = playerCoins;
        
        // 2. 添加完成奖励到总金币（金币已经实时累加，这里只需要添加奖励）
        int bonus = 0;
        if (levelManager != null)
        {
            LevelData levelData = levelManager.GetCurrentLevelData();
            if (levelData != null && levelData.completionBonus > 0)
            {
                bonus = levelData.completionBonus;
                // 完成奖励直接加到总金币
                totalCoins += bonus;
                // 也加到当前关卡金币（用于显示）
                playerCoins += bonus;
            }
        }
        
        // 注意：playerCoins 已经实时累加到 totalCoins 了，这里不需要再次累加
        // 只需要添加完成奖励即可
    }

    /// <summary>
    /// 显示结算画面（第一阶段：显示"关卡通过！"，会一直显示）
    /// </summary>
    private void ShowSettlementScreen()
    {
        // 显示结算画面面板（一直显示，不会隐藏）
        if (levelCompleteSettlementPanel != null)
        {
            levelCompleteSettlementPanel.SetActive(true);
        }

        // 初始隐藏结果界面面板（延迟后显示）
        if (levelCompleteResultPanel != null)
        {
            levelCompleteResultPanel.SetActive(false);
        }

        // 更新结算画面文本
        if (levelCompleteSettlementText != null)
        {
            levelCompleteSettlementText.text = "关卡通过！";
        }
    }

    /// <summary>
    /// 延迟显示结果界面（第二阶段：显示详细信息和按钮，结算画面保持显示）
    /// </summary>
    private System.Collections.IEnumerator ShowResultScreenAfterDelay()
    {
        // 等待按钮显示延迟时间
        yield return new WaitForSecondsRealtime(settlementDisplayDuration);
        
        // 检查游戏状态是否仍然是LevelComplete（防止状态已改变）
        if (gameStateManager != null && gameStateManager.CurrentState != GameState.LevelComplete)
        {
            levelCompleteCoroutine = null;
            yield break;
        }
        
        // 结算画面保持显示，不隐藏
        // 只显示结果界面（包含详细信息和按钮）
        if (levelCompleteResultPanel != null)
        {
            levelCompleteResultPanel.SetActive(true);
        }
        
        // 更新结果信息文本
        UpdateResultInfoText();
        
        // 持续更新结果信息文本，确保总金币实时显示
        // 启动一个协程来持续更新（直到状态改变）
        StartCoroutine(UpdateResultInfoContinuously());
        
        // 显示升级界面（在结果界面之后）
        yield return new WaitForSecondsRealtime(0.5f); // 稍微延迟一下
        
        ShowUpgradePanel();
        
        levelCompleteCoroutine = null;  // 清除协程引用
    }
    
    /// <summary>
    /// 显示升级界面
    /// </summary>
    private void ShowUpgradePanel()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
            RefreshUpgradeButtons();
            UpdateBirdAttributesDisplay(); // 更新属性显示
        }
    }
    
    /// <summary>
    /// 隐藏升级界面
    /// </summary>
    private void HideUpgradePanel()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 刷新升级按钮（更新价格和状态）
    /// </summary>
    private void RefreshUpgradeButtons()
    {
        if (upgradeButtonContainer == null || BirdUpgradeManager.Instance == null) return;
        
        RectTransform containerRect = upgradeButtonContainer.GetComponent<RectTransform>();
        ContentSizeFitter sizeFitter = upgradeButtonContainer.GetComponent<ContentSizeFitter>();
        
        // 临时禁用 Content Size Fitter，以便设置初始高度
        bool wasSizeFitterEnabled = false;
        if (sizeFitter != null)
        {
            wasSizeFitterEnabled = sizeFitter.enabled;
            sizeFitter.enabled = false;
        }
        
        // 设置容器的最小高度（确保有足够的空间显示按钮）
        if (containerRect != null)
        {
            // 计算需要的高度：按钮数量 * (按钮高度 + 间距) + 上下边距
            int buttonCount = 5; // 固定5个升级类型
            float buttonHeight = 120f; // 按钮高度
            float spacing = 10f; // Vertical Layout Group 的间距
            float padding = 20f; // 上下边距
            float minHeight = buttonCount * (buttonHeight + spacing) + padding;
            
            // 确保最小高度至少为 200
            minHeight = Mathf.Max(minHeight, 200f);
            
            // 设置高度（使用 SetSizeWithCurrentAnchors 来强制设置）
            containerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, minHeight);
        }
        
        // 清除现有按钮
        foreach (Transform child in upgradeButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // 创建升级按钮
        if (BirdUpgradeManager.Instance == null || BirdUpgradeManager.Instance.upgradeConfig == null)
        {
            Debug.LogError("logicManager: BirdUpgradeManager 或 upgradeConfig 为空！");
            return;
        }
        
        BirdUpgradeConfig config = BirdUpgradeManager.Instance.upgradeConfig;
        
        // 创建5个升级按钮（对应5种升级类型）
        CreateUpgradeButton(BirdUpgradeData.UpgradeType.Health, config.healthUpgradeName, config.healthDescription);
        CreateUpgradeButton(BirdUpgradeData.UpgradeType.BulletDamage, config.damageUpgradeName, config.damageDescription);
        CreateUpgradeButton(BirdUpgradeData.UpgradeType.BulletCount, config.countUpgradeName, config.countDescription);
        CreateUpgradeButton(BirdUpgradeData.UpgradeType.FireSpeed, config.fireSpeedUpgradeName, config.fireSpeedDescription);
        CreateUpgradeButton(BirdUpgradeData.UpgradeType.AttackRange, config.rangeUpgradeName, config.rangeDescription);
        
        // 重新启用 Content Size Fitter，让它根据实际内容自动调整
        if (sizeFitter != null && wasSizeFitterEnabled)
        {
            // 延迟一帧后重新启用，确保布局已经更新
            StartCoroutine(ReenableContentSizeFitter(sizeFitter));
        }
    }
    
    /// <summary>
    /// 创建升级按钮（使用新配置系统）
    /// </summary>
    private void CreateUpgradeButton(BirdUpgradeData.UpgradeType upgradeType, string upgradeName, string description)
    {
        if (upgradeButtonPrefab == null) return;
        
        // 实例化按钮预制体（预制体的 RectTransform 应该在编辑器中手动设置好）
        GameObject buttonObj = Instantiate(upgradeButtonPrefab, upgradeButtonContainer);
        
        // 初始化按钮脚本（只设置数据，不修改 RectTransform）
        UpgradeButtonScript buttonScript = buttonObj.GetComponent<UpgradeButtonScript>();
        if (buttonScript != null)
        {
            buttonScript.Initialize(upgradeType, upgradeName, description, this);
        }
    }
    
    /// <summary>
    /// 延迟重新启用 Content Size Fitter
    /// </summary>
    private System.Collections.IEnumerator ReenableContentSizeFitter(ContentSizeFitter sizeFitter)
    {
        yield return null; // 等待一帧，让布局系统更新
        if (sizeFitter != null)
        {
            sizeFitter.enabled = true;
        }
    }
    
    /// <summary>
    /// 购买升级（由升级按钮调用）
    /// </summary>
    public void PurchaseUpgrade(BirdUpgradeData.UpgradeType upgradeType)
    {
        if (BirdUpgradeManager.Instance == null) return;
        
        int coins = totalCoins;
        if (BirdUpgradeManager.Instance.PurchaseUpgrade(upgradeType, ref coins))
        {
            totalCoins = coins;
            UpdateTimeDisplay(); // 更新显示
            UpdateBirdAttributesDisplay(); // 更新属性显示
            RefreshUpgradeButtons(); // 刷新按钮
        }
    }
    
    /// <summary>
    /// 更新所有属性显示面板
    /// </summary>
    public void UpdateBirdAttributesDisplay()
    {
        // 更新所有已注册的属性显示面板
        foreach (var panel in attributesPanels)
        {
            if (panel != null)
            {
                panel.UpdateAttributes();
            }
        }
    }
    
    /// <summary>
    /// 注册属性显示面板（由面板在 Awake 时自动调用）
    /// </summary>
    public void RegisterAttributesPanel(BirdAttributesDisplayPanel panel)
    {
        if (panel != null && !attributesPanels.Contains(panel))
        {
            attributesPanels.Add(panel);
        }
    }
    
    /// <summary>
    /// 注销属性显示面板（由面板在 OnDestroy 时自动调用）
    /// </summary>
    public void UnregisterAttributesPanel(BirdAttributesDisplayPanel panel)
    {
        if (panel != null)
        {
            attributesPanels.Remove(panel);
        }
    }
    
    /// <summary>
    /// 更新结果信息文本（显示关卡详情）
    /// </summary>
    private void UpdateResultInfoText()
    {
        if (levelCompleteResultText != null)
        {
            // 获取关卡数据
            LevelData levelData = null;
            if (levelManager != null)
            {
                levelData = levelManager.GetCurrentLevelData();
            }

            int bonus = levelData != null ? levelData.completionBonus : 0;

            string text = "";
            
            // 显示获得金币（当前关卡获得的金币，不包括完成奖励）
            if (currentLevelCoinsForDisplay > 0)
            {
                text += $"获得金币：+{currentLevelCoinsForDisplay} 金币";
            }
            
            // 显示通关奖励
            if (bonus > 0)
            {
                if (text.Length > 0) text += "\n";
                text += $"通关奖励：+{bonus} 金币";
            }
            
            // 显示当前拥有的总金币（持续显示）
            if (text.Length > 0) text += "\n";
            text += $"当前金币：{totalCoins}";

            levelCompleteResultText.text = text;
        }
    }
    
    /// <summary>
    /// 持续更新结果信息文本（确保总金币实时显示）
    /// </summary>
    private System.Collections.IEnumerator UpdateResultInfoContinuously()
    {
        while (gameStateManager != null && gameStateManager.CurrentState == GameState.LevelComplete)
        {
            UpdateResultInfoText();
            yield return new WaitForSecondsRealtime(0.1f);  // 每0.1秒更新一次
        }
    }

    /// <summary>
    /// 显示游戏结束界面
    /// </summary>
    private void ShowGameOver()
    {
        if (gameOverScene != null) gameOverScene.SetActive(true);
        if (startMenuScene != null) startMenuScene.SetActive(false);
        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
        if (gameHUD != null) gameHUD.SetActive(false);  // 游戏结束时隐藏HUD
    }

    [ContextMenu("Add Coins")]
    public void addCoins(int coinsToAdd)//增加金币的代码块
    {
        addCoins(coinsToAdd, false); // 默认不计入产出控制（兼容旧代码，管道通过分数等）
    }
    
    /// <summary>
    /// 增加金币（带产出控制标识）
    /// </summary>
    /// <param name="coinsToAdd">要增加的金币数量</param>
    /// <param name="countForCoinControl">是否计入产出控制（true=道具/怪物掉落，false=管道通过等）</param>
    public void addCoins(int coinsToAdd, bool countForCoinControl)
    {
        if (!gameStateManager.IsPlaying()) return;

        // 同时更新当前关卡金币和总金币（实时累加）
        playerCoins = playerCoins + coinsToAdd;
        totalCoins = totalCoins + coinsToAdd;
        
        // 如果计入产出控制，更新已产出金币
        if (countForCoinControl)
        {
            currentLevelCoinsEarned += coinsToAdd;
        }
        
        // 立即更新显示
        UpdateTimeDisplay();

        // 注意：不再通过分数检查关卡完成，改为通过时间检查
    }
    
    /// <summary>
    /// 增加金币（保留旧方法名以兼容现有代码）
    /// </summary>
    [System.Obsolete("请使用 addCoins 方法")]
    public void addScore(int scoreToAdd)
    {
        addCoins(scoreToAdd, false);
    }
    
    /// <summary>
    /// 获取当前产出控制调整系数（0-1之间）
    /// 用于调整道具和怪物的生成概率
    /// </summary>
    /// <returns>调整系数，1.0表示不限制，0.0表示完全停止生成</returns>
    public float GetCoinControlMultiplier()
    {
        if (levelManager == null) return 1f;
        
        LevelData levelData = levelManager.GetCurrentLevelData();
        if (levelData == null) return 1f;
        
        // 如果未设置上限或下限无效，不进行控制
        if (levelData.maxCoins <= 0 || levelData.maxCoins <= levelData.minCoins)
        {
            return 1f;
        }
        
        // 计算当前产出比例（相对于产出范围）
        int coinRange = levelData.maxCoins - levelData.minCoins;
        if (coinRange <= 0) return 1f;
        
        int coinsInRange = currentLevelCoinsEarned - levelData.minCoins;
        float progressRatio = (float)coinsInRange / coinRange;
        
        // 如果还未达到开始控制的阈值，不限制
        if (progressRatio < levelData.coinControlStartRatio)
        {
            return 1f;
        }
        
        // 计算控制范围内的进度（0-1）
        float controlProgress = (progressRatio - levelData.coinControlStartRatio) / (1f - levelData.coinControlStartRatio);
        controlProgress = Mathf.Clamp01(controlProgress);
        
        // 根据曲线类型计算调整系数
        float multiplier = 1f;
        switch (levelData.coinControlCurve)
        {
            case CoinControlCurveType.Linear:
                // 线性衰减：从1线性降到0
                multiplier = 1f - controlProgress;
                break;
                
            case CoinControlCurveType.Smooth:
                // 平滑衰减：使用平滑曲线（SmoothStep）
                multiplier = 1f - Mathf.SmoothStep(0f, 1f, controlProgress);
                break;
                
            case CoinControlCurveType.Fast:
                // 快速衰减：使用平方曲线
                multiplier = 1f - (controlProgress * controlProgress);
                break;
        }
        
        // 确保系数在0-1之间
        multiplier = Mathf.Clamp01(multiplier);
        
        return multiplier;
    }
    
    /// <summary>
    /// 获取当前关卡已产出的金币（用于调试）
    /// </summary>
    public int GetCurrentLevelCoinsEarned()
    {
        return currentLevelCoinsEarned;
    }

    /// <summary>
    /// 更新时间显示（格式：x/n，取整）
    /// </summary>
    private void UpdateTimeDisplay()
    {
        // 更新时间显示
        if (timeText != null)
        {
            float targetTime = GetCurrentLevelTargetTime();
            // 显示已用时间/目标时间，取整
            int currentTimeInt = Mathf.FloorToInt(currentLevelTime);
            int targetTimeInt = Mathf.FloorToInt(targetTime);
            timeText.text = $"{currentTimeInt}/{targetTimeInt}";
        }

        // 更新总金币显示
        if (totalCoinsText != null)
        {
            totalCoinsText.text = $"金币: {totalCoins}";
        }

        // 更新关卡显示
        UpdateLevelDisplay();

        // 通知小鸟更新拖尾长度（使用总金币）
        UpdateBirdTrail();
        
        // 更新小鸟属性显示
        UpdateBirdAttributesDisplay();
    }
    
    /// <summary>
    /// 开始计时
    /// </summary>
    private void StartTiming()
    {
        isTiming = true;
        currentLevelTime = 0f;
    }
    
    /// <summary>
    /// 停止计时
    /// </summary>
    private void StopTiming()
    {
        isTiming = false;
    }
    
    void Update()
    {
        // 如果正在游戏中且正在计时，更新时间
        if (isTiming && gameStateManager != null && gameStateManager.IsPlaying())
        {
            currentLevelTime += Time.deltaTime;
            UpdateTimeDisplay();
            
            // 检查是否完成关卡（达到目标时间）
            CheckLevelComplete();
        }
    }

    /// <summary>
    /// 通知小鸟更新拖尾长度（使用总金币）
    /// </summary>
    private void UpdateBirdTrail()
    {
        BirdScript bird = FindObjectOfType<BirdScript>();
        if (bird != null)
        {
            bird.UpdateTrailLength(totalCoins);
        }
    }

    /// <summary>
    /// 更新关卡显示
    /// </summary>
    private void UpdateLevelDisplay()
    {
        if (levelText != null)
        {
            // 自动生成关卡名：关卡 + 关卡编号
            int levelNumber = currentLevel;
            if (levelManager != null)
            {
                levelNumber = levelManager.GetCurrentLevelNumber();
            }
            levelText.text = $"关卡 {levelNumber}";
        }
    }

    /// <summary>
    /// 检查是否完成关卡（达到目标时间）
    /// </summary>
    private void CheckLevelComplete()
    {
        float targetTime = GetCurrentLevelTargetTime();
        if (currentLevelTime >= targetTime)
        {
            CompleteLevel();
        }
    }

    /// <summary>
    /// 获取当前关卡的目标时间（秒）
    /// </summary>
    private float GetCurrentLevelTargetTime()
    {
        if (levelManager != null)
        {
            return levelManager.GetCurrentTargetTime();
        }
        // 兜底方案：如果没有LevelManager，使用简单的计算
        return 15f + (currentLevel - 1) * 5f;
    }

    /// <summary>
    /// 完成关卡
    /// </summary>
    private void CompleteLevel()
    {
        // 播放通过关卡音效
        PlayLevelCompleteSound();

        gameStateManager.CompleteLevel();
    }

    /// <summary>
    /// 播放通过关卡音效
    /// </summary>
    private void PlayLevelCompleteSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLevelCompleteSound();
        }
    }

    /// <summary>
    /// 开始游戏（由开始按钮调用）
    /// </summary>
    public void StartGame()
    {
        gameStateManager.StartGame();
    }

    /// <summary>
    /// 进入下一关
    /// </summary>
    public void NextLevel()
    {
        // 1. 隐藏升级界面（确保进入下一关时升级界面已关闭）
        HideUpgradePanel();
        
        // 2. 清理所有现有的管道和道具（重要：防止残留物体导致立即碰撞）
        ClearAllPipesAndItems();

        // 注意：金币已经在 ShowLevelComplete() 时累加到 totalCoins 了
        // 这里只需要重置当前关卡的金币即可

        // 3. 更新关卡信息
        if (levelManager != null)
        {
            levelManager.NextLevel();
            currentLevel = levelManager.GetCurrentLevelNumber();
        }
        else
        {
            currentLevel++;
        }

        // 4. 重置当前关卡金币和时间（总金币已在关卡完成时累加）
        playerCoins = 0;
        currentLevelCoinsEarned = 0; // 重置产出跟踪
        currentLevelCoinsForDisplay = 0;  // 重置显示用的金币
        currentLevelTime = 0f;

        // 4. 重置管道生成器计时器
        ResetPipeSpawner();

        // 5. 更新时间显示（包括总金币）
        UpdateTimeDisplay();

        // 6. 开始新关卡（这会触发OnGameStateChanged，重置小鸟和恢复管道生成）
        gameStateManager.StartGame();
    }

    /// <summary>
    /// 清理所有管道、道具和怪物
    /// </summary>
    private void ClearAllPipesAndItems()
    {
        // 清理所有管道
        PipeMoveScript[] pipes = FindObjectsOfType<PipeMoveScript>();
        foreach (var pipe in pipes)
        {
            if (pipe != null && pipe.gameObject != null)
            {
                Destroy(pipe.gameObject);
            }
        }

        // 清理所有道具（通过ItemScript标签或组件查找）
        ItemScript[] items = FindObjectsOfType<ItemScript>();
        foreach (var item in items)
        {
            if (item != null && item.gameObject != null)
            {
                Destroy(item.gameObject);
            }
        }
        
        // 清理所有怪物
        MonsterScript[] monsters = FindObjectsOfType<MonsterScript>();
        foreach (var monster in monsters)
        {
            if (monster != null && monster.gameObject != null)
            {
                Destroy(monster.gameObject);
            }
        }
        
        // 清理所有子弹（可选，防止残留子弹）
        BulletScript[] bullets = FindObjectsOfType<BulletScript>();
        foreach (var bullet in bullets)
        {
            if (bullet != null && bullet.gameObject != null)
            {
                Destroy(bullet.gameObject);
            }
        }

        Debug.Log("LogicManager: 已清理所有管道、道具、怪物和子弹");
    }

    /// <summary>
    /// 重置管道生成器
    /// </summary>
    private void ResetPipeSpawner()
    {
        PipeSpawner[] spawners = FindObjectsOfType<PipeSpawner>();
        foreach (var spawner in spawners)
        {
            if (spawner != null)
            {
                // 重置计时器（通过反射或添加公共方法）
                // 由于timer是private，我们需要添加一个公共方法来重置它
                spawner.ResetTimer();
                // 应用新关卡的设置
                spawner.ApplyLevelSettings();
            }
        }
    }

    /// <summary>
    /// 重新开始游戏
    /// </summary>
    public void restartGame()
    {
        // 1. 清理所有游戏对象（管道、道具、怪物、子弹）
        ClearAllPipesAndItems();
        
        // 2. 重置游戏数据
        totalCoins = 0;
        playerCoins = 0;
        currentLevelCoinsEarned = 0;
        currentLevelCoinsForDisplay = 0;
        currentLevelTime = 0f;
        isTiming = false;
        
        // 3. 重置关卡到第一关
        ResetToFirstLevel();
        
        // 4. 重置小鸟升级（清除所有升级）
        if (BirdUpgradeManager.Instance != null)
        {
            BirdUpgradeManager.Instance.ResetAllUpgrades();
        }
        
        // 5. 重置管道生成器
        ResetPipeSpawner();
        
        // 6. 更新显示
        UpdateTimeDisplay();
        UpdateLevelDisplay();
        
        // 7. 重置小鸟状态
        ResetBird();
        
        // 8. 开始新游戏（这会触发OnGameStateChanged，显示菜单或直接开始游戏）
        gameStateManager.ReturnToMenu();
        
        // 9. 立即开始游戏（从菜单状态直接开始）
        // 如果需要先显示菜单，可以注释掉下面这行
        gameStateManager.StartGame();
    }

    /// <summary>
    /// 游戏结束
    /// </summary>
    public void gameOver()
    {
        // 播放死亡音效
        PlayDeathSound();

        gameStateManager.GameOver();
    }

    /// <summary>
    /// 播放死亡音效
    /// </summary>
    private void PlayDeathSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDeathSound();
        }
    }

    /// <summary>
    /// 返回菜单
    /// </summary>
    public void ReturnToMenu()
    {
        gameStateManager.ReturnToMenu();
    }

    /// <summary>
    /// 停止所有管道移动
    /// </summary>
    private void StopAllPipes()
    {
        // 停止所有管道移动
        PipeMoveScript[] pipeMovers = FindObjectsOfType<PipeMoveScript>();
        foreach (var mover in pipeMovers)
        {
            mover.StopMoving();
        }

        // 停止管道生成
        PipeSpawner[] spawners = FindObjectsOfType<PipeSpawner>();
        foreach (var spawner in spawners)
        {
            spawner.StopSpawning();
        }
    }

    /// <summary>
    /// 恢复所有管道移动
    /// </summary>
    private void ResumeAllPipes()
    {
        // 恢复所有管道移动
        PipeMoveScript[] pipeMovers = FindObjectsOfType<PipeMoveScript>();
        foreach (var mover in pipeMovers)
        {
            mover.ResumeMoving();
        }

        // 恢复管道生成
        PipeSpawner[] spawners = FindObjectsOfType<PipeSpawner>();
        foreach (var spawner in spawners)
        {
            spawner.ResumeSpawning();
        }
    }
}
