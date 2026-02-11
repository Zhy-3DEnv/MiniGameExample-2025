using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 挂在 GameScene 根节点或空物体上，作为「当前关卡」的 Gameplay 根。
    /// 子弹、敌人、金币、特效等关卡内动态生成物应挂在此节点（或其子节点）下，随 GameScene 卸载一起销毁，避免残留在 PersistentScene。
    /// </summary>
    public class GameplayRoot : MonoBehaviour
    {
        private static Transform _bulletsParent;
        private static Transform _enemiesParent;
        private static Transform _coinsParent;

        /// <summary>
        /// 子弹应挂的父节点。为 null 表示当前未在关卡内（GameScene 未加载或已卸载），生成子弹时可退化为不指定 parent。
        /// </summary>
        public static Transform BulletsParent => _bulletsParent;

        /// <summary>
        /// 敌人应挂的父节点。为 null 时生成敌人可不指定 parent 或退化为当前场景。
        /// </summary>
        public static Transform EnemiesParent => _enemiesParent;

        /// <summary>
        /// 金币等拾取物应挂的父节点。为 null 时生成金币可不指定 parent 或退化为当前场景。
        /// </summary>
        public static Transform CoinsParent => _coinsParent;

        [Tooltip("子弹挂在此 Transform 下；留空则使用本物体")]
        public Transform bulletsRoot;

        [Tooltip("敌人生成后挂在此 Transform 下；留空则使用本物体")]
        public Transform enemiesRoot;

        [Tooltip("金币等拾取物挂在此 Transform 下；留空则使用本物体")]
        public Transform coinsRoot;

        private void Awake()
        {
            _bulletsParent = bulletsRoot != null ? bulletsRoot : transform;
            _enemiesParent = enemiesRoot != null ? enemiesRoot : transform;
            _coinsParent = coinsRoot != null ? coinsRoot : transform;
        }

        private void OnDestroy()
        {
            if (_bulletsParent == (bulletsRoot != null ? bulletsRoot : transform))
                _bulletsParent = null;
            if (_enemiesParent == (enemiesRoot != null ? enemiesRoot : transform))
                _enemiesParent = null;
            if (_coinsParent == (coinsRoot != null ? coinsRoot : transform))
                _coinsParent = null;
        }
    }
}
