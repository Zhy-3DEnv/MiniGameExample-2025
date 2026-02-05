using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using EggRogue;

/// <summary>
/// 角色信息面板（原属性面板）- 显示玩家等级与角色当前属性数值。
/// 按 ESC 键打开/关闭。
/// </summary>
public class CharacterInfoPanel : BaseUIPanel
{
    [Header("UI 引用")]
    [Tooltip("关闭按钮（点击关闭面板）")]
    public Button closeButton;

    [Tooltip("玩家等级文本（格式：Lv.X 当前经验/升级所需，例如：Lv.3 25/45）")]
    public Text playerLevelText;

    [Header("（可选）旧版单独 Text 引用")]
    [Tooltip("伤害值文本（不推荐，建议使用自动生成模式）")]
    public Text damageText;

    [Tooltip("攻击速度文本（不推荐，建议使用自动生成模式）")]
    public Text fireRateText;

    [Tooltip("生命值文本（显示：当前/最大）")]
    public Text maxHealthText;

    [Tooltip("移动速度文本（不推荐，建议使用自动生成模式）")]
    public Text moveSpeedText;

    [Tooltip("子弹速度文本（不推荐，建议使用自动生成模式）")]
    public Text bulletSpeedText;

    [Tooltip("攻击范围文本（不推荐，建议使用自动生成模式）")]
    public Text attackRangeText;

    [Header("自动生成模式（推荐）")]
    [Tooltip("属性行容器（例如一个 VerticalLayoutGroup）")]
    public Transform attributesContainer;

    [Tooltip("属性行预制体（需要包含 NameText 和 ValueText 两个 Text 子节点）")]
    public GameObject attributeRowPrefab;

    private bool wasActiveOnAwake = false;
    private bool openingViaShow = false;

    private void Awake()
    {
        wasActiveOnAwake = gameObject.activeSelf;
    }

    public override void Show()
    {
        openingViaShow = true;
        base.Show();
    }

    private void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        if (playerLevelText == null)
            playerLevelText = transform.Find("PlayerLevelText")?.GetComponent<Text>();

        if (damageText == null)
            damageText = transform.Find("DamageText")?.GetComponent<Text>();
        if (fireRateText == null)
            fireRateText = transform.Find("FireRateText")?.GetComponent<Text>();
        if (maxHealthText == null)
            maxHealthText = transform.Find("MaxHealthText")?.GetComponent<Text>();
        if (moveSpeedText == null)
            moveSpeedText = transform.Find("MoveSpeedText")?.GetComponent<Text>();
        if (bulletSpeedText == null)
            bulletSpeedText = transform.Find("BulletSpeedText")?.GetComponent<Text>();
        if (attackRangeText == null)
            attackRangeText = transform.Find("AttackRangeText")?.GetComponent<Text>();

        if (!openingViaShow && wasActiveOnAwake && gameObject.activeSelf)
        {
            gameObject.SetActive(false);
            isVisible = false;
        }
        openingViaShow = false;
    }

    private void Update()
    {
        if (IsVisible())
        {
            UpdateAttributes();
        }
    }

    /// <summary>
    /// 切换面板显示/隐藏
    /// </summary>
    public void TogglePanel()
    {
        if (IsVisible())
            Hide();
        else
            Show();
    }

    private void OnCloseClicked()
    {
        Hide();
    }

    /// <summary>
    /// 更新属性显示
    /// </summary>
    private void UpdateAttributes()
    {
        if (attributesContainer != null && attributeRowPrefab != null)
        {
            UpdateAttributesDynamic();
        }
        else
        {
            CharacterStats stats = FindObjectOfType<CharacterStats>();
            if (stats != null)
            {
                UpdateAttributesFromStats(stats);
            }
            else
            {
                UpdateAttributesFromComponents();
            }
        }

        RefreshPlayerLevel();
    }

    /// <summary>
    /// 刷新玩家等级显示（格式：Lv.X 当前经验/升级所需，例如 Lv.3 25/45）
    /// </summary>
    private void RefreshPlayerLevel()
    {
        if (playerLevelText == null) return;
        var plm = PlayerLevelManager.Instance;
        if (plm != null)
        {
            int level = plm.CurrentLevel;
            int currentXP = plm.CurrentXP;
            int xpToNext = plm.GetXPToNextLevel();
            playerLevelText.text = $"玩家等级 Lv.{level} {currentXP}/{xpToNext}";
        }
        else
        {
            playerLevelText.text = "玩家等级 Lv.1 0/10";
        }
    }

    /// <summary>
    /// 自动生成属性行（推荐模式）
    /// </summary>
    private void UpdateAttributesDynamic()
    {
        CharacterStats stats = FindObjectOfType<CharacterStats>();
        if (stats == null)
            return;

        var data = stats.characterData;
        if (data == null)
            return;

        for (int i = attributesContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(attributesContainer.GetChild(i).gameObject);
        }

        var dataType = data.GetType();
        var fields = dataType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        foreach (var field in fields)
        {
            if (field.FieldType != typeof(float))
                continue;

            string fieldName = field.Name;
            float baseValue = (float)field.GetValue(data);

            string suffix = fieldName.StartsWith("base")
                ? fieldName.Substring("base".Length)
                : fieldName;

            PropertyInfo currentProp = stats.GetType().GetProperty("Current" + suffix,
                BindingFlags.Public | BindingFlags.Instance);

            float currentValue = baseValue;
            if (currentProp != null && currentProp.PropertyType == typeof(float))
            {
                currentValue = (float)currentProp.GetValue(stats);
            }

            string displayName = GetDisplayNameForField(fieldName);

            GameObject row = GameObject.Instantiate(attributeRowPrefab, attributesContainer);
            Text nameText = row.transform.Find("NameText")?.GetComponent<Text>();
            Text valueText = row.transform.Find("ValueText")?.GetComponent<Text>();

            if (nameText != null)
                nameText.text = displayName;

            if (valueText != null)
            {
                if (Mathf.Approximately(currentValue, baseValue))
                {
                    valueText.text = currentValue.ToString("F1");
                }
                else
                {
                    float bonus = currentValue - baseValue;
                    string sign = bonus >= 0 ? "+" : "";
                    valueText.text = $"{currentValue:F1} ({sign}{bonus:F1})";
                }
            }
        }
    }

    private string GetDisplayNameForField(string fieldName)
    {
        switch (fieldName)
        {
            case "baseDamage":       return "伤害";
            case "baseFireRate":     return "攻击速度";
            case "baseMaxHealth":    return "最大生命值";
            case "baseMoveSpeed":    return "移动速度";
            case "baseBulletSpeed":  return "子弹速度";
            case "baseAttackRange":  return "攻击范围";
            case "basePickupRange":  return "拾取范围";
            default:
                if (fieldName.StartsWith("base"))
                    return fieldName.Substring("base".Length);
                return fieldName;
        }
    }

    private void UpdateAttributesFromStats(CharacterStats stats)
    {
        Health health = stats.GetComponent<Health>();

        if (damageText != null)
            damageText.text = $"伤害: {stats.CurrentDamage:F1}";

        if (fireRateText != null)
            fireRateText.text = $"攻击速度: {stats.CurrentFireRate:F1} 发/秒";

        if (maxHealthText != null)
        {
            float current = stats.CurrentMaxHealth;
            float currentHp = health != null ? health.CurrentHealth : current;
            maxHealthText.text = $"生命值: {currentHp:F0}/{current:F0}";
        }

        if (moveSpeedText != null)
            moveSpeedText.text = $"移动速度: {stats.CurrentMoveSpeed:F1}";

        if (bulletSpeedText != null)
            bulletSpeedText.text = $"子弹速度: {stats.CurrentBulletSpeed:F1}";

        if (attackRangeText != null)
            attackRangeText.text = $"攻击范围: {stats.CurrentAttackRange:F1}";
    }

    private void UpdateAttributesFromComponents()
    {
        PlayerCombatController combat = FindObjectOfType<PlayerCombatController>();
        Health health = FindObjectOfType<Health>();
        CharacterController character = FindObjectOfType<CharacterController>();

        if (damageText != null && combat != null)
            damageText.text = $"伤害: {combat.damagePerShot:F1}";

        if (fireRateText != null && combat != null)
            fireRateText.text = $"攻击速度: {combat.fireRate:F1} 发/秒";

        if (maxHealthText != null && health != null)
            maxHealthText.text = $"生命值: {health.CurrentHealth:F0}/{health.maxHealth:F0}";

        if (moveSpeedText != null && character != null)
            moveSpeedText.text = $"移动速度: {character.moveSpeed:F1}";

        if (bulletSpeedText != null && combat != null)
            bulletSpeedText.text = $"子弹速度: {combat.bulletSpeed:F1}";

        if (attackRangeText != null && combat != null)
            attackRangeText.text = $"攻击范围: {combat.attackRange:F1}";
    }

    protected override void OnShow()
    {
        base.OnShow();

        if (transform.parent != null)
        {
            transform.SetAsLastSibling();
        }

        UpdateAttributes();

        if (GameplayPauseManager.Instance != null)
        {
            GameplayPauseManager.Instance.RequestPause("CharacterInfoPanel");
        }
    }

    protected override void OnHide()
    {
        base.OnHide();

        if (GameplayPauseManager.Instance != null)
        {
            GameplayPauseManager.Instance.RequestResume("CharacterInfoPanel");
        }
    }
}
