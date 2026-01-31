using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 虚拟摇杆 - 用于移动设备上的触屏输入。
/// 
/// 使用方式：
/// 1. 在 Canvas 下创建一个 Panel，命名为 "JoystickPanel"
/// 2. 在 JoystickPanel 下创建两个 Image（或 SpriteRenderer）：
///    - 背景圆圈（Background）
///    - 摇杆圆圈（Handle）
/// 3. 将这个脚本挂载到 JoystickPanel 上
/// 4. 在 Inspector 中拖入 Background 和 Handle 的引用
/// 5. 设置 Joystick Radius（摇杆可移动范围）
/// </summary>
public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("摇杆组件")]
    [Tooltip("摇杆背景（外部圆圈）")]
    public RectTransform background;

    [Tooltip("摇杆手柄（内部圆圈）")]
    public RectTransform handle;

    [Header("摇杆设置")]
    [Tooltip("摇杆可移动范围（半径）")]
    public float joystickRadius = 50f;

    [Tooltip("摇杆是否自动回到中心")]
    public bool returnToCenter = true;

    [Tooltip("返回中心的速度")]
    public float returnSpeed = 10f;

    private Vector2 inputVector; // 当前输入值（-1 到 1）
    private bool isDragging = false;

    /// <summary>
    /// 获取当前输入向量（-1 到 1）
    /// </summary>
    public Vector2 GetInput()
    {
        return inputVector;
    }

    /// <summary>
    /// 获取输入强度（0 到 1）
    /// </summary>
    public float GetInputMagnitude()
    {
        return inputVector.magnitude;
    }

    /// <summary>
    /// 是否正在使用摇杆
    /// </summary>
    public bool IsActive()
    {
        return isDragging || inputVector.magnitude > 0.01f;
    }

    private void Update()
    {
        // 如果没有拖拽且需要自动回中
        if (!isDragging && returnToCenter && inputVector.magnitude > 0.01f)
        {
            // 平滑回到中心
            inputVector = Vector2.Lerp(inputVector, Vector2.zero, Time.deltaTime * returnSpeed);
            UpdateHandlePosition();
        }
    }

    /// <summary>
    /// 拖拽时触发
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null)
            return;

        // 获取拖拽位置（相对于背景的中心）
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, eventData.pressEventCamera, out localPoint);

        // 限制在摇杆范围内
        if (localPoint.magnitude > joystickRadius)
        {
            localPoint = localPoint.normalized * joystickRadius;
        }

        // 更新手柄位置
        handle.anchoredPosition = localPoint;

        // 计算输入向量（标准化到 -1 到 1）
        inputVector = localPoint / joystickRadius;
    }

    /// <summary>
    /// 按下时触发
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        OnDrag(eventData); // 按下时也更新位置
    }

    /// <summary>
    /// 抬起时触发
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        
        if (returnToCenter)
        {
            // 重置到中心
            inputVector = Vector2.zero;
            handle.anchoredPosition = Vector2.zero;
        }
    }

    /// <summary>
    /// 更新手柄位置（用于平滑回中）
    /// </summary>
    private void UpdateHandlePosition()
    {
        if (handle != null)
        {
            handle.anchoredPosition = inputVector * joystickRadius;
        }
    }

    /// <summary>
    /// 设置摇杆可见性（用于在PC上隐藏，在手机上显示）
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (gameObject != null)
        {
            gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// 立即将摇杆复位到中心（进入下一关等场景切换时调用，避免摇杆停留在上一关位置）
    /// </summary>
    public void ResetToCenter()
    {
        isDragging = false;
        inputVector = Vector2.zero;
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }
}
