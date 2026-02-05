using UnityEngine;
using UnityEngine.InputSystem;

namespace EggRogue
{
    /// <summary>已弃用：请使用 DebugDrawController。在 Game 视图中显示每把武器射程线。</summary>
    [System.Obsolete("请使用 DebugDrawController 替代")]
    [RequireComponent(typeof(WeaponController))]
    [RequireComponent(typeof(CharacterStats))]
    public class WeaponRangeRuntimeVisualizer : MonoBehaviour
    {
        [Tooltip("是否在 Game 视图中显示每把武器的射程线")]
        public bool showInGame = true;

        [Tooltip("射程线宽度")]
        public float lineWidth = 0.04f;

        [Tooltip("用于绘制射程线的材质（建议使用 Unlit/Color 或 Sprites/Default）")]
        public Material lineMaterial;

        private WeaponController _weaponController;
        private CharacterStats _stats;
        private LineRenderer[] _lines;

        private void Awake()
        {
            _weaponController = GetComponent<WeaponController>();
            _stats = GetComponent<CharacterStats>();

            int maxSlots = WeaponInventoryManager.MaxSlots;
            _lines = new LineRenderer[maxSlots];

            for (int i = 0; i < maxSlots; i++)
            {
                var go = new GameObject($"WeaponRangeLine_{i}");
                go.transform.SetParent(transform, false);

                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.loop = false;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.positionCount = 2;
                lr.startWidth = lineWidth;
                lr.endWidth = lineWidth;
                lr.enabled = false;

                if (lineMaterial != null)
                    lr.material = lineMaterial;
                else
                {
                    // 尝试创建一个简单的 Unlit 颜色材质
                    var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
                    if (shader != null)
                        lr.material = new Material(shader);
                }

                _lines[i] = lr;
            }
        }

        private void Update()
        {
            bool visible = DebugDrawSettings.WeaponRangeVisible && showInGame;
            if (!visible || _weaponController == null || _stats == null || WeaponInventoryManager.Instance == null)
            {
                SetAllLinesEnabled(false);
                return;
            }

            for (int i = 0; i < WeaponInventoryManager.MaxSlots; i++)
            {
                var lr = _lines[i];
                if (lr == null) continue;

                var weapon = WeaponInventoryManager.Instance.GetWeaponAt(i);
                if (weapon == null)
                {
                    lr.enabled = false;
                    continue;
                }

                float range = GetWeaponRange(weapon);
                if (range <= 0f)
                {
                    lr.enabled = false;
                    continue;
                }

                // 颜色与 Gizmos 一致：从青色到红色渐变
                float t = i / (float)(WeaponInventoryManager.MaxSlots - 1);
                Color c = Color.Lerp(Color.cyan, Color.red, t);
                lr.startColor = c;
                lr.endColor = c;

                // 查找对应的 WeaponSlot_i 作为方向参考
                Transform weaponSlotRoot = transform.Find("WeaponSlot");
                Transform slotRoot = weaponSlotRoot != null ? weaponSlotRoot.Find($"WeaponSlot_{i}") : null;

                Vector3 origin = transform.position;
                Vector3 dir = slotRoot != null ? slotRoot.forward : transform.forward;

                lr.enabled = true;
                lr.startWidth = lineWidth;
                lr.endWidth = lineWidth;
                lr.SetPosition(0, origin);
                lr.SetPosition(1, origin + dir.normalized * range);
            }
        }

        private void SetAllLinesEnabled(bool enabled)
        {
            if (_lines == null) return;
            foreach (var lr in _lines)
            {
                if (lr != null) lr.enabled = enabled;
            }
        }

        /// <summary>
        /// 与 WeaponController 中统一的攻击范围公式保持一致：
        /// 角色当前攻击范围（含卡片/被动加成） + 武器自身 attackRange 偏移。
        /// </summary>
        private float GetWeaponRange(WeaponData weapon)
        {
            float baseRange = _stats != null ? _stats.CurrentAttackRange : 0f;
            float weaponOffset = weapon != null ? weapon.attackRange : 0f;
            return Mathf.Max(0f, baseRange + weaponOffset);
        }
    }
}

