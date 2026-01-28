using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 敌人数据（ScriptableObject）- 定义敌人的基础属性配置。
    /// 每种敌人类型对应一个 EnemyData 资源。
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy_01", menuName = "EggRogue/Enemy Data", order = 5)]
    public class EnemyData : ScriptableObject
    {
        [Header("敌人信息")]
        [Tooltip("敌人名称")]
        public string enemyName = "默认敌人";

        [Tooltip("敌人描述")]
        [TextArea(2, 4)]
        public string description = "敌人描述";

        [Tooltip("敌人图标（可选）")]
        public Sprite icon;

        [Header("基础属性")]
        [Tooltip("基础最大生命值")]
        public float baseMaxHealth = 20f;

        [Tooltip("基础移动速度")]
        public float baseMoveSpeed = 3f;

        [Header("掉落配置")]
        [Tooltip("最少掉落金币数")]
        public int coinDropMin = 1;

        [Tooltip("最多掉落金币数")]
        public int coinDropMax = 2;

        [Tooltip("掉落随机偏移半径（XZ），避免叠在一起）")]
        public float coinDropRadius = 0.3f;
    }
}
