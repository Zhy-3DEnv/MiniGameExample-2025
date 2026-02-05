using UnityEngine;
using EggRogue;
using EggRogue.DebugDraw;

/// <summary>
/// 已弃用：请使用 DebugDrawController 统一管理。
/// </summary>
[System.Obsolete("请使用 DebugDrawController 替代")]
public class AttackRangeDebugRegistration : MonoBehaviour
{
    [Header("显示设置")]

    [Tooltip("圆心相对角色 pivot 的 Y 偏移（脚底平面）")]
    public float groundYOffset = -0.05f;

    [Tooltip("圆圈分段数，0 使用 DebugDrawManager 默认")]
    [Range(0, 128)]
    public int circleSegments = 0;

    [Tooltip("线宽，≤0 使用默认")]
    public float lineWidth = 0f;

    private const string Id = "attack_range";
    private Transform _transform;
    private CharacterStats _stats;

    private void Awake()
    {
        _transform = transform;
        _stats = GetComponent<CharacterStats>();
    }

    private void OnEnable()
    {
        var m = DebugDrawManager.Instance;
        m.RegisterCircle(
            Id,
            GetCenter,
            GetRadius,
            color: new Color(0f, 1f, 0f, 0.6f),
            toggleKey: null,
            visibleByDefault: DebugDrawSettings.AttackRangeVisible,
            segments: circleSegments,
            lineWidth: lineWidth);
        m.SetVisible(Id, DebugDrawSettings.AttackRangeVisible);
        DebugDrawSettings.OnSettingChanged += OnDebugDrawVisibleChanged;
    }

    private void OnDisable()
    {
        DebugDrawSettings.OnSettingChanged -= OnDebugDrawVisibleChanged;
        DebugDrawManager.TryUnregister(Id);
    }

    private void OnDebugDrawVisibleChanged(string key, bool visible)
    {
        if (key == nameof(DebugDrawSettings.AttackRangeVisible))
            DebugDrawManager.Instance?.SetVisible(Id, visible);
    }

    private Vector3 GetCenter() => _transform.position + Vector3.up * groundYOffset;
    private float GetRadius() => _stats != null ? Mathf.Max(0.01f, _stats.CurrentAttackRange) : 1f;
}
