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
    /// <summary>护甲减伤加成（百分比，例如 10 = 额外 10% 伤害减免）。</summary>
    public float armorPercentBonus;
    public float attackRangeBonus;
    public float pickupRangeBonus;
    /// <summary>闪避率加成（百分比，例如 5 = 5%，上限 80%）。</summary>
    public float dodgePercentBonus;
    /// <summary>关卡奖励加成（每关额外金币）。</summary>
    public int rewardBonus;
    /// <summary>暴击率加成（百分比，例如 10 = 10%）。</summary>
    public float critRatePercentBonus;
    /// <summary>暴击伤害倍率加成（加算，例如 0.2 表示倍率 +0.2，1.2→1.4）。</summary>
    public float critDamageMultiplierBonus;
    /// <summary>幸运值加成。</summary>
    public float luckBonus;
    /// <summary>击退距离加成。</summary>
    public float knockbackBonus;
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
                b.moveSpeedBonus != 0f || b.armorPercentBonus != 0f ||
                b.attackRangeBonus != 0f || b.pickupRangeBonus != 0f ||
                b.dodgePercentBonus != 0f || b.rewardBonus != 0 || b.critRatePercentBonus != 0f ||
                b.critDamageMultiplierBonus != 0f || b.luckBonus != 0f || b.knockbackBonus != 0f)
            {
                b.star = idx + 1;
                return b;
            }
        }

        if (star == 1)
        {
            float d = damageBonus, fr = fireRateBonus, hp = maxHealthBonus, ms = moveSpeedBonus;
            float ar = attackRangeBonus, pr = pickupRangeBonus;
            return new CardStarBonus
            {
                star = 1,
                damageBonus = d,
                fireRateBonus = fr,
                maxHealthBonus = hp,
                moveSpeedBonus = ms,
                armorPercentBonus = 0f,
                attackRangeBonus = ar,
                pickupRangeBonus = pr,
                dodgePercentBonus = 0f,
                rewardBonus = 0,
                critRatePercentBonus = 0f,
                critDamageMultiplierBonus = 0f,
                luckBonus = 0f,
                knockbackBonus = 0f
            };
        }
        return default;
    }

    /// <summary>
    /// 根据星级加成数据生成描述文本（用于 UI 展示）。正向数值绿色，负向数值红色（需 Text 开启 Rich Text）。
    /// </summary>
    public string GetDescriptionForStar(int star)
    {
        var b = GetBonusForStar(star);
        var sb = new System.Text.StringBuilder();
        if (b.damageBonus != 0f) sb.AppendLine("伤害 " + FormatSignedValue(b.damageBonus));
        if (b.fireRateBonus != 0f) sb.AppendLine("攻击速度 " + FormatSignedValue(b.fireRateBonus));
        if (b.maxHealthBonus != 0f) sb.AppendLine("最大生命值 " + FormatSignedValue(b.maxHealthBonus));
        if (b.moveSpeedBonus != 0f) sb.AppendLine("移动速度 " + FormatSignedValue(b.moveSpeedBonus));
        if (b.armorPercentBonus != 0f) sb.AppendLine("护甲减伤 " + FormatSignedValue(b.armorPercentBonus, "F0", "%"));
        if (b.attackRangeBonus != 0f) sb.AppendLine("攻击范围 " + FormatSignedValue(b.attackRangeBonus));
        if (b.pickupRangeBonus != 0f) sb.AppendLine("拾取范围 " + FormatSignedValue(b.pickupRangeBonus));
        if (b.dodgePercentBonus != 0f) sb.AppendLine("闪避 " + FormatSignedValue(b.dodgePercentBonus, "F0", "%"));
        if (b.rewardBonus != 0) sb.AppendLine("关卡奖励 " + FormatSignedValue((float)b.rewardBonus, "F0", ""));
        if (b.critRatePercentBonus != 0f) sb.AppendLine("暴击率 " + FormatSignedValue(b.critRatePercentBonus, "F0", "%"));
        if (b.critDamageMultiplierBonus != 0f) sb.AppendLine("暴击伤害 " + FormatSignedValue(b.critDamageMultiplierBonus * 100f, "F0", "%"));
        if (b.luckBonus != 0f) sb.AppendLine("幸运 " + FormatSignedValue(b.luckBonus));
        if (b.knockbackBonus != 0f) sb.AppendLine("击退 " + FormatSignedValue(b.knockbackBonus));
        if (sb.Length > 0) return sb.ToString().TrimEnd();
        if (!string.IsNullOrEmpty(description)) return description;
        return "无属性加成";
    }

    // 正向/负向描述颜色（Hex = #RRGGBB，可自行替换）
    // 查询方式：浏览器搜 "hex color picker" 或 "颜色选择器"，选色后复制 # 开头的六位码
    private const string PositiveColorHex = "#00FF00"; // 正向绿
    private const string NegativeColorHex = "#CC0000"; // 负向红

    /// <summary>
    /// 格式化带符号的数值：正数绿色，负数红色（Unity Rich Text）。
    /// </summary>
    private static string FormatSignedValue(float value, string format = "", string suffix = "")
    {
        if (value == 0f) return "0" + suffix;
        string numStr = value > 0
            ? "+" + (string.IsNullOrEmpty(format) ? value.ToString() : value.ToString(format))
            : (string.IsNullOrEmpty(format) ? value.ToString() : value.ToString(format));
        string colored = value > 0
            ? "<color=" + PositiveColorHex + ">" + numStr + suffix + "</color>"
            : "<color=" + NegativeColorHex + ">" + numStr + suffix + "</color>";
        return colored;
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
