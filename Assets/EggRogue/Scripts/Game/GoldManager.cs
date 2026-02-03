using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 金币管理器 - 单例，存储当前金币数，收集时加金、UI 订阅刷新。
/// 
/// 使用方式：
/// 1. 场景中放空物体挂本脚本，或由 DontDestroyOnLoad 的 GameManager 等创建
/// 2. Coin 收集时调用 AddGoldFromPickup(1)
/// 3. GameHudPanel 订阅 OnGoldChanged，在回调里 UpdateGold(GoldManager.Instance.Gold)
/// </summary>
public class GoldManager : MonoBehaviour
{
    private static GoldManager _instance;
    public static GoldManager Instance => _instance;

    [Header("事件")]
    public UnityEvent<int> OnGoldChanged;

    [Header("金币状态")]
    [SerializeField]
    [Tooltip("当前金币数")]
    private int gold;

    /// <summary>当前金币数</summary>
    public int Gold => gold;

    [Header("未拾取金币补偿（跨关使用）")]
    [Tooltip("上一关中未被玩家拾取的金币总额，会在进入下一关时转入双倍拾取额度。")]
    [SerializeField]
    private int lostGoldBank;

    [Tooltip("当前剩余的双倍拾取额度：本关前若干枚金币会获得双倍收益，直到消耗完此额度。")]
    [SerializeField]
    private int doubleGoldQuota;

    /// <summary>
    /// 上一次金币拾取时实际应用的额外奖励（双倍部分），仅用于调试和表现（例如飘字）。
    /// </summary>
    public int LastPickupBonus { get; private set; }
    /// <summary>
    /// 调试用：当前累计的未拾取金币（本关结束前）数值。
    /// </summary>
    public int LostGoldBankDebug => lostGoldBank;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    /// <summary>增加金币并触发 OnGoldChanged（基础方法，不含双倍逻辑）。</summary>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        gold += amount;
        OnGoldChanged?.Invoke(gold);
    }

    /// <summary>
    /// 由金币拾取时调用的加金币方法，应用双倍拾取逻辑。
    /// 规则：优先消耗 doubleGoldQuota，使前若干枚金币获得双倍收益。
    /// </summary>
    public void AddGoldFromPickup(int amount)
    {
        if (amount <= 0) return;

        int baseAmount = amount;
        int bonus = 0;

        if (doubleGoldQuota > 0)
        {
            // 本次最多可消耗的双倍额度为本次金币数量
            bonus = Mathf.Min(baseAmount, doubleGoldQuota);
            doubleGoldQuota -= bonus;
        }

        LastPickupBonus = bonus;

        AddGold(baseAmount + bonus);

        if (bonus > 0)
        {
            Debug.Log($"[Gold Debug] 本次拾取金币触发双倍：基础 {baseAmount}，额外 {bonus}，合计 {baseAmount + bonus}，剩余双倍额度={doubleGoldQuota}");
        }
        else
        {
            Debug.Log($"[Gold Debug] 本次拾取金币为正常收益：+{baseAmount}，当前总金币={gold}");
        }
    }

    /// <summary>
    /// 注册未被拾取而消失的金币值，在关卡结束或场景切换时由 Coin.OnDestroy 调用。
    /// </summary>
    public void RegisterLostGold(int amount)
    {
        if (amount <= 0) return;
        lostGoldBank += amount;
        Debug.Log($"[Gold Debug] 记录未拾取金币：+{amount}，当前未拾取累计={lostGoldBank}");
    }

    /// <summary>
    /// 在进入新关卡前调用：将上一关累计的未拾取金币转入下一关的双倍拾取额度。
    /// 例如上一关 lostGoldBank=10，则本关前若干枚金币将获得共计 10 点的双倍补偿。
    /// </summary>
    public void PrepareDoubleGoldForNextLevel()
    {
        if (lostGoldBank <= 0) return;

        doubleGoldQuota += lostGoldBank;
        Debug.Log($"[Gold Debug] 进入下一关：将未拾取金币 {lostGoldBank} 转入双倍拾取额度，新的双倍额度={doubleGoldQuota}");
        lostGoldBank = 0;
    }

    /// <summary>
    /// 重置所有跨关金币补偿状态（例如重新从第 1 关开始一局时调用）。
    /// 不会修改当前金币数 Gold。
    /// </summary>
    public void ResetGoldBonuses()
    {
        lostGoldBank = 0;
        doubleGoldQuota = 0;
    }

    /// <summary>消耗金币，不足时返回 false</summary>
    public bool SpendGold(int amount)
    {
        if (amount <= 0 || gold < amount) return false;
        gold -= amount;
        OnGoldChanged?.Invoke(gold);
        return true;
    }

    /// <summary>设置金币（用于存档/读档等）</summary>
    public void SetGold(int value)
    {
        gold = Mathf.Max(0, value);
        OnGoldChanged?.Invoke(gold);
    }
}
