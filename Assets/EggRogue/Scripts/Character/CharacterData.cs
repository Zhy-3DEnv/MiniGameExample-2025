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

    [Header("基础属性")]
    [Tooltip("基础伤害")]
    public float baseDamage = 10f;

    [Tooltip("基础攻击速度（发/秒）")]
    public float baseFireRate = 2f;

    [Tooltip("基础最大生命值")]
    public float baseMaxHealth = 100f;

    [Tooltip("基础移动速度")]
    public float baseMoveSpeed = 5f;

    [Tooltip("基础子弹速度")]
    public float baseBulletSpeed = 20f;

    [Tooltip("基础攻击范围")]
    public float baseAttackRange = 10f;
}
}
