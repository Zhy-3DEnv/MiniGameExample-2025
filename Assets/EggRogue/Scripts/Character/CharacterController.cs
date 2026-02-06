using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 蛋黄人角色控制器 - 负责角色移动。
/// 
/// 使用方式：
/// 1. 将本脚本挂载到角色对象上（例如你的胶囊体）
/// 2. 在 Inspector 中配置移动速度
/// 3. 使用新的 Input System（PlayerInputAction.inputactions）来配置输入
/// 4. 支持键盘（WASD/方向键）和触屏输入（后续可扩展虚拟摇杆）
/// 
/// 注意：
/// - 当前是2D平面移动（XZ平面，假设相机是俯视视角）
/// - 如果需要2D游戏（XY平面），可以修改移动逻辑
/// - 需要在 Input Action Asset 中配置 "Move" 动作（Vector2类型）
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CharacterController : MonoBehaviour
{
    [Header("移动配置")]
    [Tooltip("移动速度（单位/秒）")]
    public float moveSpeed = 5f;

    [Tooltip("是否使用物理移动（推荐）")]
    public bool usePhysics = true;

    [Tooltip("加速/转向响应系数（越大越跟手；移动端建议 20–28）")]
    [Range(8f, 35f)]
    public float accelerationFactor = 24f;

    [Tooltip("松杆时减速系数（越大停得越快，跟手感更好）")]
    [Range(8f, 35f)]
    public float decelerationFactor = 20f;

    [Header("边界限制（可选）")]
    [Tooltip("是否限制角色移动范围")]
    public bool useBounds = false;

    [Tooltip("X轴最小位置")]
    public float minX = -10f;

    [Tooltip("X轴最大位置")]
    public float maxX = 10f;

    [Tooltip("Z轴最小位置")]
    public float minZ = -10f;

    [Tooltip("Z轴最大位置")]
    public float maxZ = 10f;

    // 输入相关
    private Vector2 moveInput; // 当前移动输入（-1到1的向量）
    private Vector2 joystickInput; // 摇杆输入（由 TouchInputHandler 设置）
    private Rigidbody rb;
    private Vector3 currentVelocity;

    private void Awake()
    {
        // 获取 Rigidbody 组件
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("CharacterController: 需要 Rigidbody 组件！");
        }

        // 如果使用物理移动，设置 Rigidbody 参数
        if (usePhysics)
        {
            // 冻结旋转，避免角色翻滚（3D Rigidbody 通过 constraints 设置）
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
            // 取消重力（3D Rigidbody 使用 useGravity）
            rb.useGravity = false;
            // 阻力不宜过大，否则会加重“推不动”的顿感；停止主要靠 decelerationFactor
            rb.drag = 4f;
            rb.angularDrag = 4f;
        }
    }

    /// <summary>
    /// 在 Update 中读取键盘输入（WASD/方向键），用于编辑器和 PC 端调试。
    /// 
    /// 输入优先级：
    /// - 移动端：只使用摇杆输入
    /// - 编辑器/PC端（包括 Simulator 模式）：键盘输入优先，如果没有键盘输入则使用摇杆
    /// </summary>
    private void Update()
    {
        // 移动端：直接使用摇杆输入
        if (Application.isMobilePlatform)
        {
            moveInput = joystickInput;
            return;
        }

        // 编辑器/PC端/Simulator模式：键盘输入优先，如果没有键盘输入则使用摇杆
        Vector2 keyboardInput = Vector2.zero;
        
        if (Keyboard.current != null)
        {
            var k = Keyboard.current;
            
            if (k.wKey.isPressed || k.upArrowKey.isPressed)    keyboardInput.y += 1f;
            if (k.sKey.isPressed || k.downArrowKey.isPressed)  keyboardInput.y -= 1f;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed)  keyboardInput.x -= 1f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) keyboardInput.x += 1f;

            if (keyboardInput.sqrMagnitude > 1f)
                keyboardInput = keyboardInput.normalized;
        }

        // 键盘输入优先：如果有键盘输入，使用键盘；否则使用摇杆
        // 这样在编辑器下可以用 WASD，在 Simulator 模式下如果没有键盘输入会自动使用摇杆
        if (keyboardInput.sqrMagnitude > 0.01f)
        {
            moveInput = keyboardInput;
        }
        else
        {
            moveInput = joystickInput;
        }

        // 物理移动：在 Update 里根据本帧输入更新速度并写入 rb.velocity，
        // 避免“输入在 Update、移动在 FixedUpdate”造成的延迟和顿感（尤其移动端）
        if (usePhysics && rb != null)
        {
            ApplyMovementFromInput();
        }
    }

    private void FixedUpdate()
    {
        // 物理模式下速度已在 Update 中设置，这里只做非物理的直接移动
        if (!usePhysics)
        {
            MoveDirectly();
        }
    }

    /// <summary>
    /// 手动设置移动输入（用于虚拟摇杆等外部输入源）
    /// 注意：在编辑器下，键盘输入会优先于摇杆输入
    /// </summary>
    public void SetMoveInput(Vector2 input)
    {
        joystickInput = input;
        // 在移动端，直接使用摇杆输入
        // 在编辑器/PC端，Update 中会根据键盘输入决定使用键盘还是摇杆
        if (Application.isMobilePlatform)
        {
            moveInput = input;
        }
    }

    /// <summary>
    /// 根据本帧输入在 Update 中更新速度并写入 rb.velocity，减少输入到移动的延迟（跟手）。
    /// 原先在 FixedUpdate 里 Lerp 会导致：1）输入与移动不同步 2）移动端帧率低时顿感明显。
    /// </summary>
    private void ApplyMovementFromInput()
    {
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 targetVelocity = moveDirection * moveSpeed;

        float factor = moveInput.sqrMagnitude > 0.01f ? accelerationFactor : decelerationFactor;
        float t = Mathf.Clamp01(Time.deltaTime * factor);
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, t);

        if (useBounds)
        {
            Vector3 nextPosition = rb.position + currentVelocity * Time.deltaTime;
            if (nextPosition.x < minX || nextPosition.x > maxX) currentVelocity.x = 0f;
            if (nextPosition.z < minZ || nextPosition.z > maxZ) currentVelocity.z = 0f;
        }

        rb.velocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
    }

    /// <summary>
    /// 直接移动（不使用物理，适合简单的移动需求）
    /// </summary>
    private void MoveDirectly()
    {
        // 计算移动方向
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        // 计算移动距离
        Vector3 movement = moveDirection * moveSpeed * Time.deltaTime;

        // 应用边界限制
        Vector3 nextPosition = transform.position + movement;
        if (useBounds)
        {
            nextPosition.x = Mathf.Clamp(nextPosition.x, minX, maxX);
            nextPosition.z = Mathf.Clamp(nextPosition.z, minZ, maxZ);
        }

        // 移动角色
        transform.position = nextPosition;
    }

    /// <summary>
    /// 设置 Rigidbody 阻力（Settings 面板调节手感用）
    /// </summary>
    public void SetRigidbodyDrag(float drag)
    {
        if (rb != null)
            rb.drag = Mathf.Clamp(drag, 2f, 25f);
    }

    /// <summary>
    /// 设置移动速度（外部调用，例如从属性系统读取）
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    /// <summary>
    /// 获取当前移动速度（用于调试或动画）
    /// </summary>
    public float GetCurrentMoveSpeed()
    {
        return currentVelocity.magnitude;
    }

    /// <summary>
    /// 是否正在移动
    /// </summary>
    public bool IsMoving()
    {
        return moveInput.magnitude > 0.01f;
    }

    /// <summary>
    /// 获取当前移动输入方向（用于朝向等）。XZ 平面对应 world 的 X、Z。
    /// </summary>
    public Vector2 GetMoveInput()
    {
        return moveInput;
    }

    /// <summary>
    /// 停止移动（例如被击晕、暂停时）
    /// </summary>
    public void StopMovement()
    {
        moveInput = Vector2.zero;
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
        }
        currentVelocity = Vector3.zero;
    }

    // 在Scene视图中显示边界（调试用）
    private void OnDrawGizmosSelected()
    {
        if (useBounds)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((minX + maxX) / 2f, transform.position.y, (minZ + maxZ) / 2f);
            Vector3 size = new Vector3(maxX - minX, 0.1f, maxZ - minZ);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
