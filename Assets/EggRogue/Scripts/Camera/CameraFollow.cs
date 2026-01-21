using UnityEngine;

/// <summary>
/// 相机跟随脚本 - 让相机跟随角色移动。
/// 
/// 使用方式：
/// 1. 将本脚本挂载到主相机上
/// 2. 在 Inspector 中设置要跟随的目标（角色对象）
/// 3. 配置跟随模式和偏移量
/// 
/// 注意：
/// - 适合俯视角游戏（相机在角色上方）
/// - 支持平滑跟随和直接跟随两种模式
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    [Tooltip("要跟随的角色对象（如果为空，会自动查找 CharacterController）")]
    public Transform target;

    [Header("跟随模式")]
    [Tooltip("是否使用平滑跟随（推荐，更自然）")]
    public bool useSmoothFollow = true;

    [Tooltip("平滑跟随的速度（值越大跟随越快）")]
    [Range(1f, 20f)]
    public float smoothSpeed = 10f;

    [Header("相机偏移")]
    [Tooltip("相机相对于角色的X轴偏移")]
    public float offsetX = 0f;

    [Tooltip("相机相对于角色的Y轴偏移（高度）")]
    public float offsetY = 10f;

    [Tooltip("相机相对于角色的Z轴偏移")]
    public float offsetZ = 0f;

    [Header("限制相机移动范围（可选）")]
    [Tooltip("是否限制相机移动范围")]
    public bool useBounds = false;

    [Tooltip("X轴最小位置")]
    public float minX = -100f;

    [Tooltip("X轴最大位置")]
    public float maxX = 100f;

    [Tooltip("Z轴最小位置")]
    public float minZ = -100f;

    [Tooltip("Z轴最大位置")]
    public float maxZ = 100f;

    [Header("自动查找目标")]
    [Tooltip("如果没有手动设置目标，自动查找场景中的 CharacterController")]
    public bool autoFindTarget = true;

    private Vector3 targetPosition;

    private void Start()
    {
        // 如果没有设置目标，尝试自动查找
        if (target == null && autoFindTarget)
        {
            FindTarget();
        }

        // 初始化相机位置
        if (target != null)
        {
            UpdateTargetPosition();
            if (!useSmoothFollow)
            {
                transform.position = targetPosition;
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        // 更新目标位置
        UpdateTargetPosition();

        // 移动相机
        if (useSmoothFollow)
        {
            // 平滑跟随（使用 SmoothDamp 更平滑，减少抖动）
            float actualSmoothSpeed = smoothSpeed * Time.deltaTime;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, actualSmoothSpeed);
            transform.position = smoothedPosition;
        }
        else
        {
            // 直接跟随
            transform.position = targetPosition;
        }
    }

    /// <summary>
    /// 更新目标位置（考虑偏移量）
    /// </summary>
    private void UpdateTargetPosition()
    {
        if (target == null)
        {
            return;
        }

        // 计算目标位置（角色位置 + 偏移量）
        targetPosition = target.position + new Vector3(offsetX, offsetY, offsetZ);

        // 应用边界限制
        if (useBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);
        }
    }

    /// <summary>
    /// 自动查找场景中的角色目标
    /// </summary>
    private void FindTarget()
    {
        // 查找场景中的 CharacterController 组件
        CharacterController character = FindObjectOfType<CharacterController>();
        if (character != null)
        {
            target = character.transform;
            Debug.Log($"CameraFollow: 自动找到目标 {target.name}");
        }
        else
        {
            Debug.LogWarning("CameraFollow: 未找到 CharacterController，请手动设置 target");
        }
    }

    /// <summary>
    /// 设置跟随目标（外部调用）
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// 立即移动到目标位置（不使用平滑）
    /// </summary>
    public void SnapToTarget()
    {
        if (target != null)
        {
            UpdateTargetPosition();
            transform.position = targetPosition;
        }
    }

    // 在Scene视图中显示边界（调试用）
    private void OnDrawGizmosSelected()
    {
        if (useBounds)
        {
            Gizmos.color = Color.cyan;
            Vector3 center = new Vector3((minX + maxX) / 2f, transform.position.y, (minZ + maxZ) / 2f);
            Vector3 size = new Vector3(maxX - minX, 0.1f, maxZ - minZ);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
