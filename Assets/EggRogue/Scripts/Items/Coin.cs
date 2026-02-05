using UnityEngine;

/// <summary>
/// 金币可收集物 - 根据 CharacterStats.CurrentPickupRange 进行距离检测，玩家在范围内自动收集。
/// 拾取范围来自角色数据，可通过选卡、道具等成长。
/// 若带有 Collider(IsTrigger)，也支持碰撞触发拾取（后备）。
/// </summary>
public class Coin : MonoBehaviour
{
    [Tooltip("单个金币价值")]
    public int value = 1;

    [Header("视觉反馈")]
    [Tooltip("双倍金币时显示的飘字预制体（需带有 FlappyBird.ScorePopup 组件，可复用伤害飘字样式，建议改为金色）")]
    public GameObject doubleGoldPopupPrefab;

    [Tooltip("飞向 UI 的飞币图标（可空则用默认圆），拾取后金币飞向 HUD 金币显示")]
    public Sprite flyIconSprite;

    [Tooltip("飞币颜色：无自定义图标时作为圆形颜色，有图标时作为 tint")]
    public Color flyIconColor = new Color(1f, 0.84f, 0f, 1f);

    [Tooltip("飞币图标在 UI 上的大小（像素）")]
    [Min(8f)]
    public float flyIconSize = 40f;

    [Tooltip("飞行动画时长（秒）")]
    public float flyDuration = 1f;

    [Tooltip("检测间隔（秒），降低每帧开销")]
    public float checkInterval = 0.05f;

    [Header("Debug")]
    [Tooltip("开启后在 Console 输出拾取检测日志，用于排查无法拾取问题")]
    public bool debugLog = false;

    /// <summary>设为 true 时，所有 Coin 都输出 Debug 日志（可在代码中临时开启）</summary>
    public static bool DebugMode = false;

    private static float _lastNoStatsLogTime;

    private bool collected = false;
    private float _checkTimer;

    private void OnEnable()
    {
        if (debugLog || DebugMode) Debug.Log("[Coin] OnEnable 被调用");
    }

    private void Update()
    {
        if (collected) return;

        bool log = debugLog || DebugMode;
        if (EggRogue.GameplayPauseManager.Instance != null && EggRogue.GameplayPauseManager.Instance.IsPaused)
            return;

        _checkTimer -= Time.deltaTime;
        if (_checkTimer > 0f) return;
        _checkTimer = checkInterval;

        var stats = Object.FindObjectOfType<CharacterStats>();
        if (stats == null)
        {
            if (log && Time.time - _lastNoStatsLogTime > 2f)
            {
                _lastNoStatsLogTime = Time.time;
                ////Debug.LogWarning("[Coin] 未找到 CharacterStats（场景中无玩家？）");
            }
            return;
        }

        Transform player = stats.transform;
        float pickupRange = Mathf.Max(0.1f, stats.CurrentPickupRange + EggRogue.ItemEffectManager.GetPickupRangeBonus());
        float dist = Vector3.Distance(player.position, transform.position);
        float distSq = (player.position - transform.position).sqrMagnitude;
        float rangeSq = pickupRange * pickupRange;
        bool inRange = distSq <= rangeSq;

        if (log && dist < 3f)
            //Debug.Log($"[Coin] 距离={dist:F2}m, 拾取范围={pickupRange:F2}m, 范围内={inRange} | 玩家={player.position}, 金币={transform.position}");

        if (inRange)
        {
            if (log) //Debug.Log("[Coin] 进入范围，执行 Collect()");
            Collect();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (other == null) return;
        if (other.CompareTag("Player"))
            Collect();
        else
        {
            var stats = other.GetComponentInParent<CharacterStats>();
            if (stats != null)
                Collect();
        }
    }

    /// <summary>收集金币：加金、飞向 UI、销毁。</summary>
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

        collected = true;

        var target = CoinFlyTarget.Target;
        var canvas = CoinFlyTarget.OverlayCanvas;
        if (target != null && canvas != null)
        {
            Vector3 startPos = transform.position + Vector3.up * 0.3f;
            FlyingCoinEffect.Play(startPos, target, canvas, flyIconSprite, flyIconSize, flyDuration, flyIconColor);
        }

        Destroy(gameObject);

        if (isDouble && doubleGoldPopupPrefab != null)
        {
            try
            {
                GameObject popup = Instantiate(doubleGoldPopupPrefab);

                // 兼容 FlappyBird.ScorePopup 接口
                var popupScript = popup.GetComponent<FlappyBird.ScorePopup>();
                if (popupScript != null)
                {
                    Vector3 worldPos = transform.position + Vector3.up * 0.8f;
                    popupScript.SetPosition(worldPos);
                    popupScript.SetScore(totalGained);
                }
                else
                {
                    Destroy(popup);
                }
            }
            catch { }
        }
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
