using UnityEngine;
using UnityEngine.Serialization;

namespace EggRogue
{
/// <summary>
/// 单星级的属性加成。用于 CardData.starBonuses 表中。
/// </summary>
[System.Serializable]
public struct CardStarBonus
{
    [Tooltip("星级（1-5），对应 starBonuses 索引 0-4")]
    [FormerlySerializedAs("level")]
    public int star;

    public float damageBonus;
    public float fireRateBonus;
    public float maxHealthBonus;
    public float moveSpeedBonus;
    public float bulletSpeedBonus;
    public float attackRangeBonus;
    public float pickupRangeBonus;
}

/// <summary>
/// 卡片数据（ScriptableObject）。一种卡对应一张 CardData，内含星级加成表。
/// 抽卡时：先按 LevelData.cardStarWeights 抽星级，再抽卡类型，得到 (CardData, star) 组合。
/// </summary>
[CreateAssetMenu(fileName = "Card_01", menuName = "EggRogue/Card Data", order = 2)]
public class CardData : ScriptableObject
{
    [Header("卡片信息")]
    [Tooltip("卡片类型 ID，用于同一卡牌类型的唯一标识")]
    public string cardTypeId = "力量提升";

    [Tooltip("卡片显示名称（如 力量提升，展示时会加上 ★X）")]
    public string cardName = "力量提升";

    [Tooltip("卡片描述模板（可含 {0} 占位符，传入等级加成文本）")]
    [TextArea(2, 4)]
    public string description = "增加伤害";

    [Tooltip("卡片图标（可选）")]
    public Sprite icon;

    [Header("星级加成表")]
    [Tooltip("星级 1~5 对应的属性加成，索引 0=1星, 1=2星, ...")]
    [FormerlySerializedAs("levelBonuses")]
    public CardStarBonus[] starBonuses = new CardStarBonus[5];

    // 兼容旧资源反序列化（字段名需与旧 YAML 一致）
    [SerializeField, HideInInspector] private float damageBonus;
    [SerializeField, HideInInspector] private float fireRateBonus;
    [SerializeField, HideInInspector] private float maxHealthBonus;
    [SerializeField, HideInInspector] private float moveSpeedBonus;
    [SerializeField, HideInInspector] private float bulletSpeedBonus;
    [SerializeField, HideInInspector] private float attackRangeBonus;
    [SerializeField, HideInInspector] private float pickupRangeBonus;

    /// <summary>
    /// 获取指定星级的加成。star 1-5，超出范围或未配置则返回全零。
    /// 旧资源：若 starBonuses 未配置，1星 时返回旧平铺字段的兼容值。
    /// </summary>
    public CardStarBonus GetBonusForStar(int star)
    {
        int idx = Mathf.Clamp(star, 1, 5) - 1;
        if (starBonuses != null && idx >= 0 && idx < starBonuses.Length)
        {
            var b = starBonuses[idx];
            if (b.damageBonus != 0f || b.fireRateBonus != 0f || b.maxHealthBonus != 0f ||
                b.moveSpeedBonus != 0f || b.bulletSpeedBonus != 0f || b.attackRangeBonus != 0f || b.pickupRangeBonus != 0f)
            {
                b.star = idx + 1;
                return b;
            }
        }

        if (star == 1)
        {
            float d = damageBonus, fr = fireRateBonus, hp = maxHealthBonus, ms = moveSpeedBonus;
            float bs = bulletSpeedBonus, ar = attackRangeBonus, pr = pickupRangeBonus;
            return new CardStarBonus
            {
                star = 1,
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
    /// 根据星级加成数据生成描述文本（用于 UI 展示）。优先使用 starBonuses 的实际数值。
    /// </summary>
    public string GetDescriptionForStar(int star)
    {
        var b = GetBonusForStar(star);
        var sb = new System.Text.StringBuilder();
        if (b.damageBonus != 0f) sb.AppendLine($"伤害 +{b.damageBonus}");
        if (b.fireRateBonus != 0f) sb.AppendLine($"攻击速度 +{b.fireRateBonus}");
        if (b.maxHealthBonus != 0f) sb.AppendLine($"最大生命值 +{b.maxHealthBonus}");
        if (b.moveSpeedBonus != 0f) sb.AppendLine($"移动速度 +{b.moveSpeedBonus}");
        if (b.bulletSpeedBonus != 0f) sb.AppendLine($"子弹速度 +{b.bulletSpeedBonus}");
        if (b.attackRangeBonus != 0f) sb.AppendLine($"攻击范围 +{b.attackRangeBonus}");
        if (b.pickupRangeBonus != 0f) sb.AppendLine($"拾取范围 +{b.pickupRangeBonus}");
        if (sb.Length > 0) return sb.ToString().TrimEnd();
        if (!string.IsNullOrEmpty(description)) return description;
        return "无属性加成";
    }
}

/// <summary>
/// 抽卡结果：一张卡牌及其抽到的星级。
/// </summary>
[System.Serializable]
public struct CardOffer
{
    public CardData card;
    [FormerlySerializedAs("level")]
    public int star;

    public CardOffer(CardData card, int star)
    {
        this.card = card;
        this.star = Mathf.Clamp(star, 1, 5);
    }
}
}
