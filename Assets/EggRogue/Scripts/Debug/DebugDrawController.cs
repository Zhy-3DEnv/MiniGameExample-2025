using UnityEngine;
using UnityEngine.InputSystem;

namespace EggRogue
{
    /// <summary>
    /// 统一的调试绘制控制器 - 挂到玩家上即可，整合攻击范围、武器射程、拾取范围的绘制。
    /// 各类型开关由设置面板单独控制，PlayerPrefs 持久化。
    /// 编辑模式下选中玩家时也会在 Scene 视图绘制（需 characterData 已设置）。
    /// </summary>
    [RequireComponent(typeof(CharacterStats))]
    public class DebugDrawController : MonoBehaviour
    {
        [Header("攻击范围圈")]
        public float attackRangeLineWidth = 0.06f;
        public int attackRangeSegments = 64;
        public float attackRangeHeightOffset = 0.05f;

        [Header("武器射程线")]
        public float weaponRangeLineWidth = 0.04f;

        [Header("拾取范围圈")]
        public float pickupRangeLineWidth = 0.05f;
        public int pickupRangeSegments = 64;
        public float pickupRangeHeightOffset = 0.02f;

        [Header("编辑模式")]
        [Tooltip("编辑模式下选中玩家时，在 Scene 视图显示调试圈")]
        public bool drawGizmosInEditMode = true;

        private CharacterStats _stats;
        private WeaponController _weaponController;

        private LineRenderer _attackRangeLine;
        private LineRenderer[] _weaponRangeLines;
        private LineRenderer _pickupRangeLine;

        private void Awake()
        {
            _stats = GetComponent<CharacterStats>();
            _weaponController = GetComponent<WeaponController>();

            _attackRangeLine = CreateCircleLine("AttackRangeLine", 0f, 1f, 0f);
            _pickupRangeLine = CreateCircleLine("PickupRangeLine", 0f, 1f, 1f);

            int maxSlots = WeaponInventoryManager.MaxSlots;
            _weaponRangeLines = new LineRenderer[maxSlots];
            for (int i = 0; i < maxSlots; i++)
            {
                var go = new GameObject($"WeaponRangeLine_{i}");
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                SetupLineRenderer(lr, false);
                lr.positionCount = 2;
                float t = i / (float)Mathf.Max(1, maxSlots - 1);
                Color c = Color.Lerp(Color.cyan, Color.red, t);
                lr.startColor = lr.endColor = c;
                _weaponRangeLines[i] = lr;
            }
        }

        private LineRenderer CreateCircleLine(string name, float r, float g, float b)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            SetupLineRenderer(lr, true);
            lr.startColor = lr.endColor = new Color(r, g, b, 0.7f);
            return lr;
        }

        private void SetupLineRenderer(LineRenderer lr, bool loop)
        {
            lr.useWorldSpace = true;
            lr.loop = loop;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            if (shader != null) lr.material = new Material(shader);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[Key.P].wasPressedThisFrame)
                ToggleAll();

            DrawAttackRange();
            DrawWeaponRange();
            DrawPickupRange();
        }

        /// <summary>P 键：切换全部调试显示</summary>
        private void ToggleAll()
        {
            bool any = DebugDrawSettings.AnyVisible;
            DebugDrawSettings.AttackRangeVisible = !any;
            DebugDrawSettings.WeaponRangeVisible = !any;
            DebugDrawSettings.PickupRangeVisible = !any;
        }

        private void DrawAttackRange()
        {
            bool visible = DebugDrawSettings.AttackRangeVisible && _stats != null;
            float range = visible ? _stats.CurrentAttackRange : 0f;
            if (!visible || range <= 0f)
            {
                if (_attackRangeLine != null) _attackRangeLine.enabled = false;
                return;
            }
            int count = Mathf.Max(attackRangeSegments, 8);
            _attackRangeLine.enabled = true;
            _attackRangeLine.loop = true;
            _attackRangeLine.positionCount = count;
            _attackRangeLine.startWidth = _attackRangeLine.endWidth = attackRangeLineWidth;
            Vector3 center = transform.position + Vector3.up * attackRangeHeightOffset;
            float step = Mathf.PI * 2f / count;
            for (int i = 0; i < count; i++)
            {
                float angle = step * i;
                _attackRangeLine.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * range, 0f, Mathf.Sin(angle) * range));
            }
        }

        private void DrawWeaponRange()
        {
            bool visible = DebugDrawSettings.WeaponRangeVisible && _weaponController != null && _stats != null && WeaponInventoryManager.Instance != null;
            if (!visible || _weaponRangeLines == null)
            {
                SetWeaponLinesEnabled(false);
                return;
            }
            Transform weaponSlotRoot = transform.Find("WeaponSlot");
            for (int i = 0; i < _weaponRangeLines.Length; i++)
            {
                var lr = _weaponRangeLines[i];
                if (lr == null) continue;
                var weapon = WeaponInventoryManager.Instance.GetWeaponAt(i);
                if (weapon == null) { lr.enabled = false; continue; }
                float range = _stats.CurrentAttackRange + weapon.attackRange;
                if (range <= 0f) { lr.enabled = false; continue; }
                Transform slotRoot = weaponSlotRoot != null ? weaponSlotRoot.Find($"WeaponSlot_{i}") : null;
                Vector3 dir = slotRoot != null ? slotRoot.forward : transform.forward;
                Vector3 origin = transform.position;
                lr.enabled = true;
                lr.startWidth = lr.endWidth = weaponRangeLineWidth;
                lr.SetPosition(0, origin);
                lr.SetPosition(1, origin + dir.normalized * range);
            }
        }

        private void SetWeaponLinesEnabled(bool enabled)
        {
            if (_weaponRangeLines == null) return;
            foreach (var lr in _weaponRangeLines)
                if (lr != null) lr.enabled = enabled;
        }

        private void DrawPickupRange()
        {
            bool visible = DebugDrawSettings.PickupRangeVisible && _stats != null;
            float range = visible ? Mathf.Max(0.1f, _stats.CurrentPickupRange + ItemEffectManager.GetPickupRangeBonus()) : 0f;
            if (!visible || range <= 0f)
            {
                if (_pickupRangeLine != null) _pickupRangeLine.enabled = false;
                return;
            }
            int count = Mathf.Max(pickupRangeSegments, 8);
            _pickupRangeLine.enabled = true;
            _pickupRangeLine.loop = true;
            _pickupRangeLine.positionCount = count;
            _pickupRangeLine.startWidth = _pickupRangeLine.endWidth = pickupRangeLineWidth;
            Vector3 center = transform.position + Vector3.up * pickupRangeHeightOffset;
            float step = Mathf.PI * 2f / count;
            for (int i = 0; i < count; i++)
            {
                float angle = step * i;
                _pickupRangeLine.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * range, 0f, Mathf.Sin(angle) * range));
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!drawGizmosInEditMode || Application.isPlaying) return;
            var stats = GetComponent<CharacterStats>();
            if (stats == null || stats.characterData == null) return;

            var data = stats.characterData;
            Vector3 center = transform.position;
            int segments = 32;

            // 攻击范围（黄色）
            float attackRange = data.baseAttackRange;
            if (attackRange > 0f)
            {
                Gizmos.color = Color.yellow;
                DrawGizmoCircle(center, attackRange, segments);
            }

            // 拾取范围（青色）
            float pickupRange = data.basePickupRange;
            if (pickupRange > 0f)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.8f);
                DrawGizmoCircle(center, pickupRange, segments);
            }

            // 武器射程（6 条彩色线，编辑模式用基础攻击范围近似）
            if (attackRange > 0f)
            {
                for (int i = 0; i < 6; i++)
                {
                    float angleDeg = GetWeaponSlotAngle(i);
                    float angleRad = angleDeg * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad));
                    Vector3 end = center + dir * attackRange;
                    Gizmos.color = Color.Lerp(Color.cyan, Color.red, i / 5f);
                    Gizmos.DrawLine(center, end);
                }
            }
        }

        private static void DrawGizmoCircle(Vector3 center, float radius, int segments)
        {
            Vector3 prev = center + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }

        private static float GetWeaponSlotAngle(int index)
        {
            switch (index)
            {
                case 0: return -120f;
                case 1: return -90f;
                case 2: return -60f;
                case 3: return 60f;
                case 4: return 90f;
                case 5: return 120f;
                default: return 0f;
            }
        }
#endif
    }
}
