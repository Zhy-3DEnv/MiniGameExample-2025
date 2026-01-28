using UnityEngine;

/// <summary>
/// 金币可收集物 - 玩家进入 0.2m 内自动收集，加金币并销毁。
/// 
/// 使用方式：
/// 1. 创建金币 Prefab：小球/胶囊 + 本脚本 + Collider(IsTrigger)
/// 2. Collider 半径建议 0.2（收集范围）
/// 3. 玩家需有 Tag "Player" 和 Collider
/// </summary>
[RequireComponent(typeof(Collider))]
public class Coin : MonoBehaviour
{
    [Tooltip("单个金币价值")]
    public int value = 1;

    private void Start()
    {
        var c = GetComponent<Collider>();
        if (c != null && !c.isTrigger)
            c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Collect();
    }

    /// <summary>收集金币：加金、销毁。</summary>
    public void Collect()
    {
        if (GoldManager.Instance != null)
            GoldManager.Instance.AddGold(value);
        Destroy(gameObject);
    }
}
