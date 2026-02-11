using UnityEngine;

namespace EggRogue
{
/// <summary>
/// 角色数据（ScriptableObject）- 定义角色的基础属性配置。
/// 每个角色职业对应一个 CharacterData 资源。
/// </summary>
[CreateAssetMenu(fileName = "Character_01", menuName = "EggRogue/Character Data", order = 4)]
public class CharacterData : ScriptableObject
{
    [Header("角色信息")]
    [Tooltip("角色名称")]
    public string characterName = "默认角色";

    [Tooltip("角色描述")]
    [TextArea(2, 4)]
    public string description = "角色描述";

    [Tooltip("角色图标（可选）")]
    public Sprite icon;

    [Header("视觉表现")]
    [Tooltip("角色 3D 模型预制体（可选）。若为空则使用默认模型 EggMan01")]
    public GameObject characterModelPrefab;

    [Header("基础属性")]
    [Tooltip("基础等级（角色初始等级）")]
    public int baseLevel = 1;

    [Tooltip("基础伤害")]
    public float baseDamage = 10f;

    [Tooltip("基础攻击速度（发/秒）")]
    public float baseFireRate = 2f;

    [Tooltip("基础最大生命值")]
    public float baseMaxHealth = 100f;

    [Tooltip("基础移动速度")]
    public float baseMoveSpeed = 5f;

    [Tooltip("基础攻击范围")]
    public float baseAttackRange = 10f;

    [Tooltip("基础拾取范围（金币等可收集物，米）")]
    public float basePickupRange = 0.5f;

    [Header("扩展属性（护甲/闪避/奖励/暴击/幸运/击退）")]
    [Tooltip("基础护甲减伤（百分比，例如 10 = 10% 伤害减免，与道具/卡片叠加，上限 80%）")]
    [Range(0f, 80f)]
    public float baseArmorPercent = 0f;

    [Tooltip("基础闪避率（百分比，有 x% 几率完全闪避单次受击，上限 80%）")]
    [Range(0f, 80f)]
    public float baseDodgePercent = 0f;

    [Tooltip("基础关卡通关奖励加成（每关胜利奖励的额外金币，可与卡片/道具叠加）")]
    public int baseRewardBonus = 0;

    [Tooltip("基础暴击率（百分比，有 x% 几率造成暴击）")]
    [Range(0f, 100f)]
    public float baseCritRatePercent = 0f;

    [Tooltip("基础暴击伤害倍率（暴击时伤害乘以此值，默认 1.2 即 120%，上限 2.5）")]
    [Range(1f, 2.5f)]
    public float baseCritDamageMultiplier = 1.2f;

    [Tooltip("幸运值（越高越容易在选卡/商店刷出高等级物品，可被卡片/道具加成）")]
    public float baseLuck = 0f;

    [Tooltip("击退（攻击命中时使敌人后退的距离，0 表示无击退）")]
    public float baseKnockback = 0f;

    [Header("特殊能力")]
    [Tooltip("角色的被动能力列表，按顺序应用。例如：攻击力倍率、生命转攻击等")]
    public CharacterPassive[] passiveAbilities;
}
}
