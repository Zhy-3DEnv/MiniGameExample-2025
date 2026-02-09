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

    [Header("参数")]
    [Tooltip("死区（摇杆长度小于该值时视为无输入，不覆盖键盘）")]
    public float deadZone = 0.1f;

    private CharacterController _characterController;

    private void Update()
    {
        if (virtualJoystick == null)
            return;

        if (_characterController == null)
        {
            _characterController = FindObjectOfType<CharacterController>();
            if (_characterController == null)
                return;
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
        _characterController.SetMoveInput(input);
    }
}
