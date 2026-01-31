using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EggRogue.DebugDraw
{
    /// <summary>
    /// 调试绘制管理器 - 统一管理各类调试可视化（攻击范围、拾取范围等）。
    /// 支持按快捷键切换显示，可扩展后续其它图形。
    /// 场景中可挂载空物体，或运行时自动创建单例。
    /// </summary>
    public class DebugDrawManager : MonoBehaviour
    {
        private static DebugDrawManager _instance;

        public static DebugDrawManager Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = FindObjectOfType<DebugDrawManager>();
                if (_instance != null) return _instance;
                var go = new GameObject("DebugDrawManager");
                _instance = go.AddComponent<DebugDrawManager>();
                return _instance;
            }
        }

        [Header("全局设置")]
        [Tooltip("默认圆圈分段数")]
        [Range(32, 128)]
        public int defaultCircleSegments = 64;

        [Tooltip("默认线宽")]
        public float defaultLineWidth = 0.08f;

        private readonly List<CircleEntry> _circles = new List<CircleEntry>();
        private Transform _transform;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _transform = transform;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                for (int i = 0; i < _circles.Count; i++)
                {
                    CircleEntry c = _circles[i];
                    if (!c.ToggleKey.HasValue) continue;
                    if (keyboard[c.ToggleKey.Value].wasPressedThisFrame)
                    {
                        c.Visible = !c.Visible;
                        if (c.LineRenderer != null)
                            c.LineRenderer.enabled = c.Visible;
                    }
                }
            }

            for (int i = 0; i < _circles.Count; i++)
            {
                CircleEntry c = _circles[i];
                if (!c.Visible || c.LineRenderer == null) continue;
                if (!c.LineRenderer.enabled) continue;
                RefreshCircle(c);
            }
        }

        /// <summary>
        /// 注册一个圆形调试绘制。按可选快捷键切换显示。
        /// </summary>
        /// <param name="id">唯一标识，用于 Unregister。</param>
        /// <param name="getCenter">每帧获取圆心（世界坐标）。</param>
        /// <param name="getRadius">每帧获取半径。</param>
        /// <param name="color">颜色，未指定则绿色半透明。</param>
        /// <param name="toggleKey">快捷键，未指定则不可按键切换。</param>
        /// <param name="visibleByDefault">是否默认显示。</param>
        /// <param name="segments">分段数，0 使用默认值。</param>
        /// <param name="lineWidth">线宽，≤0 使用默认值。</param>
        public void RegisterCircle(
            string id,
            Func<Vector3> getCenter,
            Func<float> getRadius,
            Color? color = null,
            Key? toggleKey = null,
            bool visibleByDefault = true,
            int segments = 0,
            float lineWidth = 0f)
        {
            Unregister(id);

            int seg = segments > 0 ? segments : defaultCircleSegments;
            float w = lineWidth > 0f ? lineWidth : defaultLineWidth;
            Color col = color ?? new Color(0f, 1f, 0f, 0.6f);

            var child = new GameObject($"DebugDraw_Circle_{id}");
            child.transform.SetParent(_transform);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;

            var lr = child.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.positionCount = 0;
            lr.startWidth = w;
            lr.endWidth = w * 0.6f;
            lr.material = GetDefaultLineMaterial();
            lr.startColor = col;
            lr.endColor = new Color(col.r, col.g, col.b, col.a * 0.4f);
            lr.enabled = visibleByDefault;

            _circles.Add(new CircleEntry
            {
                Id = id,
                GetCenter = getCenter,
                GetRadius = getRadius,
                Color = col,
                ToggleKey = toggleKey,
                Visible = visibleByDefault,
                LineRenderer = lr,
                Segments = seg,
                LineWidth = w
            });
        }

        /// <summary>
        /// 取消注册。注册时使用的 id。
        /// </summary>
        public void Unregister(string id)
        {
            for (int i = _circles.Count - 1; i >= 0; i--)
            {
                if (_circles[i].Id != id) continue;
                if (_circles[i].LineRenderer != null && _circles[i].LineRenderer.gameObject != null)
                    Destroy(_circles[i].LineRenderer.gameObject);
                _circles.RemoveAt(i);
                return;
            }
        }

        /// <summary>
        /// 若单例存在则取消注册，避免在 OnDisable/OnDestroy 中访问 Instance 时在场景卸载时新建对象导致 Unity 报错。
        /// </summary>
        public static void TryUnregister(string id)
        {
            if (_instance != null)
                _instance.Unregister(id);
        }

        /// <summary>
        /// 设置指定 id 的显示状态（不依赖快捷键）。
        /// </summary>
        public void SetVisible(string id, bool visible)
        {
            for (int i = 0; i < _circles.Count; i++)
            {
                if (_circles[i].Id != id) continue;
                _circles[i].Visible = visible;
                if (_circles[i].LineRenderer != null)
                    _circles[i].LineRenderer.enabled = visible;
                return;
            }
        }

        private void RefreshCircle(CircleEntry c)
        {
            if (c.GetCenter == null || c.GetRadius == null || c.LineRenderer == null) return;

            Vector3 center = c.GetCenter();
            float r = Mathf.Max(0.01f, c.GetRadius());

            c.LineRenderer.positionCount = c.Segments + 1;
            c.LineRenderer.startWidth = c.LineWidth;
            c.LineRenderer.endWidth = c.LineWidth * 0.6f;

            for (int i = 0; i <= c.Segments; i++)
            {
                float t = (float)i / c.Segments * 2f * Mathf.PI;
                Vector3 p = center + new Vector3(Mathf.Cos(t) * r, 0f, Mathf.Sin(t) * r);
                c.LineRenderer.SetPosition(i, p);
            }
        }

        private static Material _defaultLineMaterial;

        private static Material GetDefaultLineMaterial()
        {
            if (_defaultLineMaterial != null) return _defaultLineMaterial;
            Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            if (s == null) s = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (s == null) s = Shader.Find("Standard");
            _defaultLineMaterial = new Material(s != null ? s : Shader.Find("Standard"));
            _defaultLineMaterial.color = new Color(0f, 1f, 0f, 0.5f);
            return _defaultLineMaterial;
        }

        private class CircleEntry
        {
            public string Id;
            public Func<Vector3> GetCenter;
            public Func<float> GetRadius;
            public Color Color;
            public Key? ToggleKey;
            public bool Visible;
            public LineRenderer LineRenderer;
            public int Segments;
            public float LineWidth;
        }
    }
}
