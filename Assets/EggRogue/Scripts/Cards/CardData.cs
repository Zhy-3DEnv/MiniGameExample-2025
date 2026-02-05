using UnityEngine;

namespace EggRogue
{
/// <summary>
/// 卡片数据（ScriptableObject）。每张卡片提供属性加成。
/// </summary>
[CreateAssetMenu(fileName = "Card_01", menuName = "EggRogue/Card Data", order = 2)]
public class CardData : ScriptableObject
{
    [Header("卡片信息")]
    [Tooltip("卡片名称")]
    public string cardName = "力量提升";

    [Tooltip("卡片描述")]
    [TextArea(2, 4)]
    public string description = "增加伤害";

    [Tooltip("卡片图标（可选）")]
    public Sprite icon;

    [Header("属性加成")]
    [Tooltip("伤害加成（+N）")]
    public float damageBonus = 0f;

    [Tooltip("攻击速度加成（+N，发/秒）")]
    public float fireRateBonus = 0f;

    [Tooltip("最大生命值加成（+N）")]
    public float maxHealthBonus = 0f;

    [Tooltip("移动速度加成（+N）")]
    public float moveSpeedBonus = 0f;

    [Tooltip("子弹速度加成（+N）")]
    public float bulletSpeedBonus = 0f;

    [Tooltip("攻击范围加成（+N）")]
    public float attackRangeBonus = 0f;

    [Tooltip("拾取范围加成（+N，米）")]
    public float pickupRangeBonus = 0f;
}
}
