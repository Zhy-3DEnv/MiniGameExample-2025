using UnityEngine;
using UnityEngine.InputSystem;
using EggRogue.DebugDraw;

/// <summary>
/// 向 DebugDrawManager 注册玩家攻击范围圆形绘制。
/// 挂载在玩家对象上（与 PlayerCombatController 同级）。按 P 键切换显示，默认开启。
/// </summary>
[RequireComponent(typeof(PlayerCombatController))]
public class AttackRangeDebugRegistration : MonoBehaviour
{
    [Header("显示设置")]
    [Tooltip("是否默认显示")]
    public bool visibleByDefault = true;

    [Tooltip("圆心相对角色 pivot 的 Y 偏移（脚底平面）")]
    public float groundYOffset = -0.05f;

    [Tooltip("圆圈分段数，0 使用 DebugDrawManager 默认")]
    [Range(0, 128)]
    public int circleSegments = 0;

    [Tooltip("线宽，≤0 使用默认")]
    public float lineWidth = 0f;

    private const string Id = "attack_range";
    private Transform _transform;
    private PlayerCombatController _combat;

    private void Awake()
    {
        _transform = transform;
        _combat = GetComponent<PlayerCombatController>();
    }

    private void OnEnable()
    {
        var m = DebugDrawManager.Instance;
        m.RegisterCircle(
            Id,
            GetCenter,
            GetRadius,
            color: new Color(0f, 1f, 0f, 0.6f),
            toggleKey: Key.P,
            visibleByDefault: visibleByDefault,
            segments: circleSegments,
            lineWidth: lineWidth);
    }

    private void OnDisable()
    {
        DebugDrawManager.TryUnregister(Id);
    }

    private Vector3 GetCenter() => _transform.position + Vector3.up * groundYOffset;
    private float GetRadius() => _combat != null ? Mathf.Max(0.01f, _combat.attackRange) : 1f;
}
