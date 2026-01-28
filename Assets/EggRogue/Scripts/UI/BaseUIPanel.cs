using UnityEngine;

/// <summary>
/// UI面板基类 - 所有UI面板都应该继承这个类。
/// 
/// 提供基础的显示/隐藏功能，以及统一的接口。
/// 子类只需要：
/// 1. 继承 BaseUIPanel
/// 2. 在 OnShow/OnHide 中实现具体的显示/隐藏逻辑（如果需要）
/// 3. 默认实现就是简单的 SetActive(true/false)
/// </summary>
public abstract class BaseUIPanel : MonoBehaviour
{
    protected bool isVisible = false;

    /// <summary>
    /// 显示面板
    /// </summary>
    public virtual void Show()
    {
        if (gameObject != null)
        {
            // 防止 isVisible 与 activeSelf 不同步导致“显示不了/关不掉”
            if (isVisible && gameObject.activeSelf)
                return;

            gameObject.SetActive(true);
            isVisible = true;
            OnShow();
        }
    }

    /// <summary>
    /// 隐藏面板
    /// </summary>
    public virtual void Hide()
    {
        if (gameObject != null)
        {
            // 防止 isVisible 与 activeSelf 不同步导致“显示不了/关不掉”
            if (!isVisible && !gameObject.activeSelf)
                return;

            gameObject.SetActive(false);
            isVisible = false;
            OnHide();
        }
    }

    /// <summary>
    /// 检查面板是否可见
    /// </summary>
    public bool IsVisible()
    {
        return isVisible && gameObject != null && gameObject.activeSelf;
    }

    /// <summary>
    /// 面板显示时的回调（子类可以重写以实现自定义逻辑）
    /// </summary>
    protected virtual void OnShow()
    {
        // 子类可以在这里添加显示时的逻辑，比如播放动画、刷新数据等
    }

    /// <summary>
    /// 面板隐藏时的回调（子类可以重写以实现自定义逻辑）
    /// </summary>
    protected virtual void OnHide()
    {
        // 子类可以在这里添加隐藏时的逻辑，比如保存数据、停止动画等
    }
}
