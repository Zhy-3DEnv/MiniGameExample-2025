using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 在 Game 视图中实时显示角色基础攻击范围（用 LineRenderer 画一个圆），
    /// 用于运行时调试，和 CharacterStats.OnDrawGizmosSelected 的黄色圈一致。
    /// </summary>
    [RequireComponent(typeof(CharacterStats))]
    [RequireComponent(typeof(LineRenderer))]
    public class AttackRangeRuntimeVisualizer : MonoBehaviour
    {
        [Tooltip("是否在 Game 视图中显示基础攻击范围")]
        public bool showBaseRangeInGame = true;

        [Tooltip("圆圈线宽")]
        public float lineWidth = 0.06f;

        [Tooltip("圆圈分段数（越大越圆，开销略高）")]
        public int segments = 64;

        [Tooltip("圆圈在地面上的高度偏移（避免与地面完全重叠看不见）")]
        public float heightOffset = 0.05f;

        private CharacterStats _stats;
        private LineRenderer _line;

        private void Awake()
        {
            _stats = GetComponent<CharacterStats>();
            _line = GetComponent<LineRenderer>();

            _line.useWorldSpace = true;
            _line.loop = true;
            _line.startWidth = lineWidth;
            _line.endWidth = lineWidth;
            _line.positionCount = Mathf.Max(segments, 8);
        }

        private void Update()
        {
            bool visible = DebugDrawSettings.AttackRangeVisible && showBaseRangeInGame;
            if (!visible || _stats == null)
            {
                if (_line != null) _line.enabled = false;
                return;
            }

            float range = _stats.CurrentAttackRange;
            if (range <= 0f)
            {
                if (_line != null) _line.enabled = false;
                return;
            }

            if (_line == null) return;

            _line.enabled = true;
            _line.startWidth = lineWidth;
            _line.endWidth = lineWidth;

            int count = Mathf.Max(segments, 8);
            if (_line.positionCount != count)
                _line.positionCount = count;

            Vector3 center = transform.position + Vector3.up * heightOffset;
            float step = Mathf.PI * 2f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = step * i;
                float x = Mathf.Cos(angle) * range;
                float z = Mathf.Sin(angle) * range;
                _line.SetPosition(i, center + new Vector3(x, 0f, z));
            }
        }
    }
}

