using UnityEngine;

/// <summary>
/// 金币飞向目标 - 提供 UI 上金币显示位置的 RectTransform，供 Coin 拾取飞行动画使用。
/// 由 GameHudPanel 等在 Start 时注册。
/// </summary>
public static class CoinFlyTarget
{
    public static RectTransform Target { get; private set; }
    public static Canvas OverlayCanvas { get; private set; }

    public static void Register(RectTransform target)
    {
        Target = target;
        if (target != null)
            OverlayCanvas = target.GetComponentInParent<Canvas>();
    }

    public static void Unregister(RectTransform target)
    {
        if (Target == target)
            Target = null;
    }
}
