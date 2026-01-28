using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Reflection;

/// <summary>
/// 属性面板 - 显示角色当前属性数值。
/// 按 ESC 键打开/关闭。
/// </summary>
public class AttributePanel : BaseUIPanel
{
    [Header("UI 引用")]
    [Tooltip("关闭按钮（点击关闭面板）")]
    public Button closeButton;

    [Header("（可选）旧版单独 Text 引用")]
    [Tooltip("伤害值文本（不推荐，建议使用自动生成模式）")]
    public Text damageText;

    [Tooltip("攻击速度文本（不推荐，建议使用自动生成模式）")]
    public Text fireRateText;

    [Tooltip("最大生命值文本（不推荐，建议使用自动生成模式）")]
    public Text maxHealthText;

    [Tooltip("当前生命值文本（不推荐，建议使用自动生成模式）")]
    public Text currentHealthText;

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

    private bool isOpen = false;

    // 保存暂停前的状态
    private EnemySpawner enemySpawner;
    private CharacterController characterController;
    private PlayerCombatController playerCombatController;
    private bool wasSpawning = false;
    private bool wasCharacterEnabled = false;
    private bool wasCombatEnabled = false;

    private void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        // 旧版模式：如果没有在 Inspector 中手动绑定 Text，就按约定名称在子节点里自动查找
        // 这样你只需要在面板下创建对应名字的 Text 即可，无需拖引用
        if (damageText == null)
            damageText = transform.Find("DamageText")?.GetComponent<Text>();
        if (fireRateText == null)
            fireRateText = transform.Find("FireRateText")?.GetComponent<Text>();
        if (maxHealthText == null)
            maxHealthText = transform.Find("MaxHealthText")?.GetComponent<Text>();
        if (currentHealthText == null)
            currentHealthText = transform.Find("CurrentHealthText")?.GetComponent<Text>();
        if (moveSpeedText == null)
            moveSpeedText = transform.Find("MoveSpeedText")?.GetComponent<Text>();
        if (bulletSpeedText == null)
            bulletSpeedText = transform.Find("BulletSpeedText")?.GetComponent<Text>();
        if (attackRangeText == null)
            attackRangeText = transform.Find("AttackRangeText")?.GetComponent<Text>();
    }

    private void Update()
    {
        // 只要当前真正可见，就实时刷新属性显示
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
        // 以实际可见状态为准，避免本地 isOpen 与 activeSelf/IsVisible 不一致时出现“要按两次才生效”的问题
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
        // 优先使用自动生成模式（基于 CharacterData / CharacterStats 动态构建）
        if (attributesContainer != null && attributeRowPrefab != null)
        {
            UpdateAttributesDynamic();
            return;
        }

        // 优先使用 CharacterStats（新系统）
        CharacterStats stats = FindObjectOfType<CharacterStats>();
        if (stats != null)
        {
            UpdateAttributesFromStats(stats);
            return;
        }

        // 兼容旧系统（如果没有 CharacterStats，从组件读取）
        UpdateAttributesFromComponents();
    }

    /// <summary>
    /// 自动生成属性行（推荐模式）
    /// 基于 CharacterData 的字段和 CharacterStats 的当前属性，自动创建 UI 行。
    /// </summary>
    private void UpdateAttributesDynamic()
    {
        CharacterStats stats = FindObjectOfType<CharacterStats>();
        if (stats == null)
            return;

        var data = stats.characterData;
        if (data == null)
            return;

        // 清空旧行
        for (int i = attributesContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(attributesContainer.GetChild(i).gameObject);
        }

        // 反射遍历 CharacterData 的 public float 字段（主要是 baseXxx）
        var dataType = data.GetType();
        var fields = dataType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        foreach (var field in fields)
        {
            if (field.FieldType != typeof(float))
                continue;

            string fieldName = field.Name;      // 例如 baseDamage
            float baseValue = (float)field.GetValue(data);

            // 约定：baseDamage -> CurrentDamage；baseMoveSpeed -> CurrentMoveSpeed
            string suffix = fieldName.StartsWith("base")
                ? fieldName.Substring("base".Length)   // Damage / MoveSpeed ...
                : fieldName;

            PropertyInfo currentProp = stats.GetType().GetProperty("Current" + suffix,
                BindingFlags.Public | BindingFlags.Instance);

            float currentValue = baseValue;
            if (currentProp != null && currentProp.PropertyType == typeof(float))
            {
                currentValue = (float)currentProp.GetValue(stats);
            }

            // 根据字段名映射一个更友好的中文显示名
            string displayName = GetDisplayNameForField(fieldName);

            // 实例化一行 UI
            GameObject row = GameObject.Instantiate(attributeRowPrefab, attributesContainer);
            Text nameText = row.transform.Find("NameText")?.GetComponent<Text>();
            Text valueText = row.transform.Find("ValueText")?.GetComponent<Text>();

            if (nameText != null)
                nameText.text = displayName;

            if (valueText != null)
            {
                // 显示当前值和相对基础值的加成
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

    /// <summary>
    /// 将 CharacterData 字段名映射为中文展示名。
    /// 例如 baseDamage -> 伤害，baseMoveSpeed -> 移动速度。
    /// 对于未知字段，退回显示原字段名。
    /// </summary>
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
            default:
                // 去掉前缀 base 并简化展示
                if (fieldName.StartsWith("base"))
                    return fieldName.Substring("base".Length);
                return fieldName;
        }
    }

    /// <summary>
    /// 从 CharacterStats 更新属性显示（新系统）
    /// </summary>
    private void UpdateAttributesFromStats(CharacterStats stats)
    {
        Health health = stats.GetComponent<Health>();

        // 显示伤害
        if (damageText != null)
        {
            damageText.text = $"伤害: {stats.CurrentDamage:F1}";
        }

        // 显示攻击速度
        if (fireRateText != null)
        {
            fireRateText.text = $"攻击速度: {stats.CurrentFireRate:F1} 发/秒";
        }

        // 显示最大生命值（显示基础值与加成）
        if (maxHealthText != null)
        {
            float baseMax = stats.characterData != null ? stats.characterData.baseMaxHealth : stats.CurrentMaxHealth;
            float current = stats.CurrentMaxHealth;
            float bonus = current - baseMax;

            if (Mathf.Approximately(bonus, 0f))
            {
                maxHealthText.text = $"最大生命值: {current:F1}";
            }
            else
            {
                string sign = bonus >= 0 ? "+" : "";
                maxHealthText.text = $"最大生命值: {current:F1} (基础 {baseMax:F1}, {sign}{bonus:F1})";
            }
        }

        // 当前生命值不作为“角色成长属性”，如需显示可单独放在战斗 HUD 中，这里不更新

        // 显示移动速度
        if (moveSpeedText != null)
        {
            moveSpeedText.text = $"移动速度: {stats.CurrentMoveSpeed:F1}";
        }

        // 显示子弹速度
        if (bulletSpeedText != null)
        {
            bulletSpeedText.text = $"子弹速度: {stats.CurrentBulletSpeed:F1}";
        }

        // 显示攻击范围
        if (attackRangeText != null)
        {
            attackRangeText.text = $"攻击范围: {stats.CurrentAttackRange:F1}";
        }

    }

    /// <summary>
    /// 从组件更新属性显示（兼容旧系统）
    /// </summary>
    private void UpdateAttributesFromComponents()
    {
        PlayerCombatController combat = FindObjectOfType<PlayerCombatController>();
        Health health = FindObjectOfType<Health>();
        CharacterController character = FindObjectOfType<CharacterController>();

        // 显示伤害
        if (damageText != null && combat != null)
        {
            damageText.text = $"伤害: {combat.damagePerShot:F1}";
        }

        // 显示攻击速度
        if (fireRateText != null && combat != null)
        {
            fireRateText.text = $"攻击速度: {combat.fireRate:F1} 发/秒";
        }

        // 显示最大生命值
        if (maxHealthText != null && health != null)
        {
            maxHealthText.text = $"最大生命值: {health.maxHealth:F1}";
        }

        // 当前生命值不作为“角色成长属性”，如需显示可单独放在战斗 HUD 中，这里不更新

        // 显示移动速度
        if (moveSpeedText != null && character != null)
        {
            moveSpeedText.text = $"移动速度: {character.moveSpeed:F1}";
        }

        // 显示子弹速度
        if (bulletSpeedText != null && combat != null)
        {
            bulletSpeedText.text = $"子弹速度: {combat.bulletSpeed:F1}";
        }

        // 显示攻击范围
        if (attackRangeText != null && combat != null)
        {
            attackRangeText.text = $"攻击范围: {combat.attackRange:F1}";
        }

    }

    protected override void OnShow()
    {
        base.OnShow();
        isOpen = true;

        // 确保属性面板在同一 Canvas 下的显示优先级最高
        // 将自身移动到兄弟节点的最后一个位置，这样不会被选卡/结算面板遮挡
        if (transform.parent != null)
        {
            transform.SetAsLastSibling();
        }

        UpdateAttributes();
        
        // 暂停所有游戏玩法系统（通过统一的 GameplayPauseManager）
        if (EggRogue.GameplayPauseManager.Instance != null)
        {
            EggRogue.GameplayPauseManager.Instance.RequestPause("AttributePanel");
        }
    }

    protected override void OnHide()
    {
        base.OnHide();
        isOpen = false;
        
        // 结束属性面板的暂停请求
        if (EggRogue.GameplayPauseManager.Instance != null)
        {
            EggRogue.GameplayPauseManager.Instance.RequestResume("AttributePanel");
        }
    }
}
