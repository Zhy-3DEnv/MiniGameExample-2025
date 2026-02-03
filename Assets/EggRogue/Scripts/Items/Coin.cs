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

    [Header("视觉反馈")]
    [Tooltip("双倍金币时显示的飘字预制体（需带有 FlappyBird.ScorePopup 组件，可复用伤害飘字样式，建议改为金色）")]
    public GameObject doubleGoldPopupPrefab;

    private bool collected = false;

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
        bool isDouble = false;
        int totalGained = value;

        if (GoldManager.Instance != null)
        {
            GoldManager.Instance.AddGoldFromPickup(value);
            int bonus = GoldManager.Instance.LastPickupBonus;
            if (bonus > 0)
            {
                isDouble = true;
                totalGained = value + bonus;
            }
        }

        // 若本次为双倍金币，则在金币位置生成一个飘字提示
        if (isDouble && doubleGoldPopupPrefab != null)
        {
            try
            {
                GameObject popup = Instantiate(doubleGoldPopupPrefab);

                // 兼容 FlappyBird.ScorePopup 接口
                var popupScript = popup.GetComponent<FlappyBird.ScorePopup>();
                if (popupScript != null)
                {
                    // 位置：略高于金币位置
                    Vector3 worldPos = transform.position + Vector3.up * 0.8f;
                    popupScript.SetPosition(worldPos);

                    // 显示本次实际获得的金币总额（例如 2），为正数形式（+2）
                    popupScript.SetScore(totalGained);

                    // 可选：将颜色改为金色以区别于伤害数字（需要你在预制体里设置或在这里调用 SetColor）
                    // popupScript.SetColor(new Color(1f, 0.84f, 0f)); // 金黄色
                }
                else
                {
                    Destroy(popup);
                }
            }
            catch
            {
                // 预制体异常时不影响金币逻辑
            }
        }

        collected = true;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // 如果金币在未被拾取的情况下消失（例如关卡结束、场景切换等），
        // 则将其价值登记为“未拾取金币”，用于下一关的双倍拾取补偿。
        if (!collected && GoldManager.Instance != null && value > 0)
        {
            GoldManager.Instance.RegisterLostGold(value);
        }
    }
}
