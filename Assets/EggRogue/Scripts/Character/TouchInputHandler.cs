using UnityEngine;

/// <summary>
/// 虚拟摇杆输入桥接：把 UI 摇杆的输入传给角色。
///
/// 简化版逻辑：
/// - 不再做平台判断，任何平台都可以用摇杆；是否显示摇杆由 UI 自己控制
/// - 编辑器 / PC：仍然可以用键盘 WASD（CharacterController.Update），拖动摇杆时会暂时覆盖键盘输入
/// - 移动端：通常只用摇杆（因为 CharacterController 在移动端不会读取键盘）
/// </summary>
public class TouchInputHandler : MonoBehaviour
{
    [Header("组件引用")]
    [Tooltip("虚拟摇杆组件")]
    public VirtualJoystick virtualJoystick;

    [Tooltip("角色控制器（可以留空，自动查找）")]
    public CharacterController characterController;

    [Header("参数")]
    [Tooltip("死区（摇杆长度小于该值时视为无输入，不覆盖键盘）")]
    public float deadZone = 0.1f;

    // 不再在 Awake 里查找，改为在 Update 里按需查找（场景切换后也能找到新场景的角色）

    private void Update()
    {
        if (virtualJoystick == null)
            return;

        // 如果角色引用为空（例如刚切换到新场景），尝试自动查找
        if (characterController == null)
        {
            characterController = FindObjectOfType<CharacterController>();
            if (characterController == null)
            {
                // 当前场景还没有载入角色，等下一帧再试
                return;
            }
        }

        // 读取摇杆输入
        Vector2 input = virtualJoystick.GetInput();

        // 死区处理：在死区内当作 0 向量
        if (input.sqrMagnitude < deadZone * deadZone)
        {
            input = Vector2.zero;
        }

        // 始终把摇杆输入传给角色：
        // - 没有键盘输入时：0 会让角色停下来
        // - 有键盘输入时：CharacterController.Update 会用键盘方向覆盖非零输入
        characterController.SetMoveInput(input);
    }
}
