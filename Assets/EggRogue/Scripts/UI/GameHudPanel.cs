using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using EggRogue;

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

    [Tooltip("设置按钮（打开设置面板调节操控手感等）")]
    public Button settingsButton;

    [Header("HUD元素")]
    [Tooltip("血量条")]
    public Slider healthBar;

    [Tooltip("血量文本（显示当前HP/最大HP，例如：100/120）")]
    public Text healthText;

    [Tooltip("金币文本")]
    public Text goldText;

    [Tooltip("倒计时文本（显示关卡剩余时间）")]
    public Text timerText;

    [Tooltip("关卡文本（显示当前关卡，例如：第1关）")]
    public Text levelText;

    [Header("玩家等级条（与 HealthBar 类似的 Slider + Text）")]
    [Tooltip("玩家等级进度条（0~1 表示当前经验/升级所需）")]
    public Slider characterLevelBar;

    [Tooltip("当前等级文本（例如：Lv.3）")]
    public Text characterLevelText;

    [Tooltip("经验值文本（例如：25/45，表示当前经验/升级所需）")]
    public Text characterLevelXPText;

    [Header("头像")]
    [Tooltip("头像图标（AvatarButton 上的 Image，用于显示当前选中角色）")]
    public Image avatarIcon;

    private void Start()
    {
        SetupButtons();
        if (goldText != null)
            CoinFlyTarget.Register(goldText.rectTransform);
    }

    private void OnDestroy()
    {
        if (goldText != null)
            CoinFlyTarget.Unregister(goldText.rectTransform);
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

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        }
    }

    private void OnSettingsButtonClicked()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSettings();
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

    private void OnEnable()
    {
        if (GoldManager.Instance != null)
            GoldManager.Instance.OnGoldChanged.AddListener(OnGoldChanged);
    }

    private void OnDisable()
    {
        if (GoldManager.Instance != null)
            GoldManager.Instance.OnGoldChanged.RemoveListener(OnGoldChanged);
    }

    private void OnGoldChanged(int gold)
    {
        UpdateGold(gold);
    }

    private void Update()
    {
        if (IsVisible())
        {
            // 更新倒计时
            if (timerText != null)
            {
                LevelTimer timer = FindObjectOfType<LevelTimer>();
                if (timer != null && timer.IsRunning)
                {
                    float remaining = timer.RemainingTime;
                    int minutes = Mathf.FloorToInt(remaining / 60f);
                    int seconds = Mathf.FloorToInt(remaining % 60f);
                    timerText.text = $"{minutes:D2}:{seconds:D2}";
                }
                else if (timer != null && timer.IsVictory)
                {
                    timerText.text = "胜利！";
                }
                else
                {
                    timerText.text = "";
                }
            }

            // 更新关卡显示
            if (levelText != null && EggRogue.LevelManager.Instance != null)
            {
                int currentLevel = EggRogue.LevelManager.Instance.CurrentLevel;
                levelText.text = $"第{currentLevel}关";
            }

            RefreshPlayerLevelDisplay();

            // 更新血量条和血量文本（通过 CharacterStats 找到玩家的 Health，避免找到敌人的）
            CharacterStats playerStats = FindObjectOfType<CharacterStats>();
            if (playerStats != null)
            {
                Health playerHealth = playerStats.GetComponent<Health>();
                if (playerHealth != null)
                {
                    if (healthBar != null)
                    {
                        UpdateHealthBar(playerHealth.CurrentHealth, playerHealth.maxHealth);
                    }

                    // 更新血量文本（显示：当前HP/最大HP）
                    if (healthText != null)
                    {
                        healthText.text = $"{playerHealth.CurrentHealth:F0}/{playerHealth.maxHealth:F0}";
                    }
                }
            }
        }
    }

    /// <summary>
    /// 面板显示时的额外逻辑
    /// </summary>
    protected override void OnShow()
    {
        base.OnShow();
        if (GoldManager.Instance != null)
            UpdateGold(GoldManager.Instance.Gold);
        else
            UpdateGold(0);
        RefreshPlayerLevelDisplay();
        RefreshAvatarIcon();
    }

    /// <summary>
    /// 刷新玩家等级显示（CharacterLevelBar：Slider 进度 + 等级文本 + 经验文本）
    /// </summary>
    private void RefreshPlayerLevelDisplay()
    {
        var plm = EggRogue.PlayerLevelManager.Instance;
        int level = plm != null ? plm.CurrentLevel : 1;
        int currentXP = plm != null ? plm.CurrentXP : 0;
        int xpToNext = plm != null ? plm.GetXPToNextLevel() : 10;

        if (characterLevelBar != null)
        {
            characterLevelBar.value = xpToNext > 0 ? (float)currentXP / xpToNext : 0f;
        }
        if (characterLevelText != null)
        {
            characterLevelText.text = $"Lv.{level}";
        }
        if (characterLevelXPText != null)
        {
            characterLevelXPText.text = $"{currentXP}/{xpToNext}";
        }
    }

    /// <summary>
    /// 同步当前选中角色的图标到头像按钮
    /// </summary>
    public void RefreshAvatarIcon()
    {
        if (avatarIcon == null) return;
        if (CharacterSelectionManager.Instance == null || CharacterSelectionManager.Instance.SelectedCharacter == null)
        {
            avatarIcon.enabled = false;
            return;
        }
        CharacterData selected = CharacterSelectionManager.Instance.SelectedCharacter;
        avatarIcon.sprite = selected.icon;
        avatarIcon.enabled = selected.icon != null;
    }

    /// <summary>
    /// 面板隐藏时的额外逻辑
    /// </summary>
    protected override void OnHide()
    {
        base.OnHide();
    }
}
