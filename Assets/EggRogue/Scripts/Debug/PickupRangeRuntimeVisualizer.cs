using UnityEngine;

namespace EggRogue
{
    /// <summary>已弃用：请使用 DebugDrawController。在 Game 视图中显示拾取范围圈。</summary>
    [System.Obsolete("请使用 DebugDrawController 替代")]
    [RequireComponent(typeof(CharacterStats))]
    public class PickupRangeRuntimeVisualizer : MonoBehaviour
    {
        [Tooltip("是否在 Game 视图中显示拾取范围")]
        public bool showInGame = true;

        [Tooltip("圆圈线宽")]
        public float lineWidth = 0.05f;

        [Tooltip("圆圈分段数")]
        public int segments = 64;

        [Tooltip("圆圈在地面上的高度偏移")]
        public float heightOffset = 0.02f;

        private CharacterStats _stats;
        private LineRenderer _line;

        private void Awake()
        {
            _stats = GetComponent<CharacterStats>();

            var go = new GameObject("PickupRangeLine");
            go.transform.SetParent(transform, false);
            _line = go.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.loop = true;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.positionCount = Mathf.Max(segments, 8);
            _line.startWidth = lineWidth;
            _line.endWidth = lineWidth;

            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            if (shader != null)
                _line.material = new Material(shader);
            _line.startColor = new Color(0f, 1f, 1f, 0.7f);
            _line.endColor = new Color(0f, 1f, 1f, 0.7f);
        }

        private void Update()
        {
            bool visible = DebugDrawSettings.PickupRangeVisible && showInGame;
            if (!visible || _stats == null)
            {
                if (_line != null) _line.enabled = false;
                return;
            }

            float range = _stats.CurrentPickupRange + ItemEffectManager.GetPickupRangeBonus();
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
