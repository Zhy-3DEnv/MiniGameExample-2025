using UnityEngine;

namespace EggRogue
{
/// <summary>
/// 单等级的属性加成。用于 CardData.levelBonuses 表中。
/// </summary>
[System.Serializable]
public struct CardLevelBonus
{
    [Tooltip("等级（1-5），对应 levelBonuses 索引 0-4")]
    public int level;

    public float damageBonus;
    public float fireRateBonus;
    public float maxHealthBonus;
    public float moveSpeedBonus;
    public float bulletSpeedBonus;
    public float attackRangeBonus;
    public float pickupRangeBonus;
}

/// <summary>
/// 卡片数据（ScriptableObject）。一种卡对应一张 CardData，内含等级加成表。
/// 抽卡时：先按 LevelData.cardLevelWeights 抽等级，再抽卡类型，得到 (CardData, level) 组合。
/// </summary>
[CreateAssetMenu(fileName = "Card_01", menuName = "EggRogue/Card Data", order = 2)]
public class CardData : ScriptableObject
{
    [Header("卡片信息")]
    [Tooltip("卡片类型 ID，用于同一卡牌类型的唯一标识")]
    public string cardTypeId = "力量提升";

    [Tooltip("卡片显示名称（如 力量提升，展示时会加上 LvX）")]
    public string cardName = "力量提升";

    [Tooltip("卡片描述模板（可含 {0} 占位符，传入等级加成文本）")]
    [TextArea(2, 4)]
    public string description = "增加伤害";

    [Tooltip("卡片图标（可选）")]
    public Sprite icon;

    [Header("等级加成表")]
    [Tooltip("等级 1~5 对应的属性加成，索引 0=等级1, 1=等级2, ...")]
    public CardLevelBonus[] levelBonuses = new CardLevelBonus[5];

    // 兼容旧资源反序列化（字段名需与旧 YAML 一致）
    [SerializeField, HideInInspector] private float damageBonus;
    [SerializeField, HideInInspector] private float fireRateBonus;
    [SerializeField, HideInInspector] private float maxHealthBonus;
    [SerializeField, HideInInspector] private float moveSpeedBonus;
    [SerializeField, HideInInspector] private float bulletSpeedBonus;
    [SerializeField, HideInInspector] private float attackRangeBonus;
    [SerializeField, HideInInspector] private float pickupRangeBonus;

    /// <summary>
    /// 获取指定等级的加成。level 1-5，超出范围或未配置则返回全零。
    /// 旧资源：若 levelBonuses 未配置，等级1 时返回旧平铺字段的兼容值。
    /// </summary>
    public CardLevelBonus GetBonusForLevel(int level)
    {
        int idx = Mathf.Clamp(level, 1, 5) - 1;
        if (levelBonuses != null && idx >= 0 && idx < levelBonuses.Length)
        {
            var b = levelBonuses[idx];
            if (b.damageBonus != 0f || b.fireRateBonus != 0f || b.maxHealthBonus != 0f ||
                b.moveSpeedBonus != 0f || b.bulletSpeedBonus != 0f || b.attackRangeBonus != 0f || b.pickupRangeBonus != 0f)
            {
                b.level = idx + 1;
                return b;
            }
        }

        if (level == 1)
        {
            float d = damageBonus, fr = fireRateBonus, hp = maxHealthBonus, ms = moveSpeedBonus;
            float bs = bulletSpeedBonus, ar = attackRangeBonus, pr = pickupRangeBonus;
            return new CardLevelBonus
            {
                level = 1,
                damageBonus = d,
                fireRateBonus = fr,
                maxHealthBonus = hp,
                moveSpeedBonus = ms,
                bulletSpeedBonus = bs,
                attackRangeBonus = ar,
                pickupRangeBonus = pr
            };
        }
        return default;
    }

    /// <summary>
    /// 根据加成数据生成描述文本（用于 UI 展示）。
    /// </summary>
    public string GetDescriptionForLevel(int level)
    {
        var b = GetBonusForLevel(level);
        if (!string.IsNullOrEmpty(description) && !description.Contains("{"))
            return description;

        var sb = new System.Text.StringBuilder();
        if (b.damageBonus != 0f) sb.AppendLine($"伤害 +{b.damageBonus}");
        if (b.fireRateBonus != 0f) sb.AppendLine($"攻击速度 +{b.fireRateBonus}");
        if (b.maxHealthBonus != 0f) sb.AppendLine($"最大生命值 +{b.maxHealthBonus}");
        if (b.moveSpeedBonus != 0f) sb.AppendLine($"移动速度 +{b.moveSpeedBonus}");
        if (b.bulletSpeedBonus != 0f) sb.AppendLine($"子弹速度 +{b.bulletSpeedBonus}");
        if (b.attackRangeBonus != 0f) sb.AppendLine($"攻击范围 +{b.attackRangeBonus}");
        if (b.pickupRangeBonus != 0f) sb.AppendLine($"拾取范围 +{b.pickupRangeBonus}");
        return sb.Length > 0 ? sb.ToString().TrimEnd() : "无属性加成";
    }
}

/// <summary>
/// 抽卡结果：一张卡牌及其抽到的等级。
/// </summary>
[System.Serializable]
public struct CardOffer
{
    public CardData card;
    public int level;

    public CardOffer(CardData card, int level)
    {
        this.card = card;
        this.level = Mathf.Clamp(level, 1, 5);
    }
}
}
