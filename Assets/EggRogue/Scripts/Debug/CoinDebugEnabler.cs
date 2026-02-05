using UnityEngine;

/// <summary>
/// 临时挂到场景中任意物体上，运行时自动开启 Coin.DebugMode，Console 会输出拾取检测日志。
/// 排查完问题后移除本组件即可。
/// </summary>
public class CoinDebugEnabler : MonoBehaviour
{
    private void OnEnable()
    {
        Coin.DebugMode = true;
        Debug.Log("[CoinDebugEnabler] 已开启 Coin 拾取 Debug 日志");
    }

    private void OnDisable()
    {
        Coin.DebugMode = false;
    }
}
