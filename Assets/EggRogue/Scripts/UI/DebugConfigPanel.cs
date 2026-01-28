using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 调试配置面板 - 运行时修改游戏参数。
/// 
/// 使用方式：
/// 1. 在 Canvas 下创建 Panel，命名为 DebugConfigPanel
/// 2. 添加本脚本
/// 3. 创建 InputField 和 Button，拖到对应字段
/// 4. 按 F1 或点击按钮显示/隐藏面板
/// </summary>
public class DebugConfigPanel : MonoBehaviour
{
    [Header("UI 引用")]
    public InputField playerDamageInput;
    public InputField enemyHealthInput;
    public InputField playerAttackRangeInput;
    public InputField playerFireRateInput;
    public InputField enemyMoveSpeedInput;
    public InputField coinDropMinInput;
    public InputField coinDropMaxInput;

    public Button applyButton;
    public Button resetButton;
    public Button closeButton;

    [Header("显示控制")]
    [Tooltip("按 F1 显示/隐藏面板")]
    public KeyCode toggleKey = KeyCode.F1;

    private bool isVisible = false;

    private void Start()
    {
        if (gameObject.activeSelf)
            gameObject.SetActive(false);

        if (applyButton != null)
            applyButton.onClick.AddListener(OnApplyClicked);

        if (resetButton != null)
            resetButton.onClick.AddListener(OnResetClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);

        RefreshUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePanel();
        }
    }

    /// <summary>
    /// 显示/隐藏面板
    /// </summary>
    public void TogglePanel()
    {
        isVisible = !isVisible;
        gameObject.SetActive(isVisible);

        if (isVisible)
        {
            RefreshUI();
        }
    }

    /// <summary>
    /// 刷新 UI（从 ConfigManager 读取当前值）
    /// </summary>
    private void RefreshUI()
    {
        if (ConfigManager.Instance == null || ConfigManager.Instance.CurrentConfig == null)
            return;

        var config = ConfigManager.Instance.CurrentConfig;

        if (playerDamageInput != null)
            playerDamageInput.text = config.playerDamage.ToString("F1");

        if (enemyHealthInput != null)
            enemyHealthInput.text = config.enemyMaxHealth.ToString("F1");

        if (playerAttackRangeInput != null)
            playerAttackRangeInput.text = config.playerAttackRange.ToString("F1");

        if (playerFireRateInput != null)
            playerFireRateInput.text = config.playerFireRate.ToString("F1");

        if (enemyMoveSpeedInput != null)
            enemyMoveSpeedInput.text = config.enemyMoveSpeed.ToString("F1");

        if (coinDropMinInput != null)
            coinDropMinInput.text = config.coinDropMin.ToString();

        if (coinDropMaxInput != null)
            coinDropMaxInput.text = config.coinDropMax.ToString();
    }

    /// <summary>
    /// 应用按钮点击
    /// </summary>
    private void OnApplyClicked()
    {
        if (ConfigManager.Instance == null)
        {
            Debug.LogWarning("DebugConfigPanel: ConfigManager.Instance 为空");
            return;
        }

        float? playerDamage = ParseFloat(playerDamageInput);
        float? enemyHealth = ParseFloat(enemyHealthInput);
        float? attackRange = ParseFloat(playerAttackRangeInput);
        float? fireRate = ParseFloat(playerFireRateInput);
        float? moveSpeed = ParseFloat(enemyMoveSpeedInput);
        int? coinMin = ParseInt(coinDropMinInput);
        int? coinMax = ParseInt(coinDropMaxInput);

        ConfigManager.Instance.UpdateConfig(
            playerDamage: playerDamage,
            enemyMaxHealth: enemyHealth,
            playerAttackRange: attackRange,
            playerFireRate: fireRate,
            enemyMoveSpeed: moveSpeed,
            coinDropMin: coinMin,
            coinDropMax: coinMax
        );

        Debug.Log("DebugConfigPanel: 配置已应用并保存");
    }

    /// <summary>
    /// 重置按钮点击
    /// </summary>
    private void OnResetClicked()
    {
        if (ConfigManager.Instance != null)
        {
            ConfigManager.Instance.ResetToDefault();
            RefreshUI();
            Debug.Log("DebugConfigPanel: 已重置为默认配置");
        }
    }

    /// <summary>
    /// 关闭按钮点击
    /// </summary>
    private void OnCloseClicked()
    {
        TogglePanel();
    }

    private float? ParseFloat(InputField field)
    {
        if (field == null || string.IsNullOrEmpty(field.text))
            return null;

        if (float.TryParse(field.text, out float value))
            return value;

        return null;
    }

    private int? ParseInt(InputField field)
    {
        if (field == null || string.IsNullOrEmpty(field.text))
            return null;

        if (int.TryParse(field.text, out int value))
            return value;

        return null;
    }
}
