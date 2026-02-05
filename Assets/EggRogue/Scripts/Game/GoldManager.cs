using UnityEngine;
using UnityEngine.Events;
using EggRogue;

/// <summary>
/// 金币管理器 - 委托 PlayerRunState 存储，保留事件与双倍拾取逻辑。
/// </summary>
public class GoldManager : MonoBehaviour
{
    private static GoldManager _instance;
    public static GoldManager Instance => _instance;

    [Header("事件")]
    public UnityEvent<int> OnGoldChanged;

    /// <summary>上一次金币拾取时实际应用的额外奖励（双倍部分），仅用于调试和表现</summary>
    public int LastPickupBonus { get; private set; }

    /// <summary>调试用：当前累计的未拾取金币</summary>
    public int LostGoldBankDebug => RunState != null ? RunState.LostGoldBank : 0;

    private PlayerRunState RunState => PlayerRunState.Instance;

    /// <summary>当前金币数</summary>
    public int Gold => RunState != null ? RunState.Gold : 0;

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

    /// <summary>增加金币并触发 OnGoldChanged（基础方法，不含双倍逻辑）</summary>
    public void AddGold(int amount)
    {
        if (amount <= 0 || RunState == null) return;
        RunState.AddGoldInternal(amount);
        OnGoldChanged?.Invoke(RunState.Gold);
    }

    /// <summary>
    /// 由金币拾取时调用的加金币方法，应用双倍拾取逻辑。
    /// </summary>
    public void AddGoldFromPickup(int amount)
    {
        if (amount <= 0 || RunState == null) return;

        int baseAmount = amount;
        int bonus = 0;

        if (RunState.DoubleGoldQuota > 0)
        {
            bonus = Mathf.Min(baseAmount, RunState.DoubleGoldQuota);
            RunState.ConsumeDoubleGoldQuota(bonus);
        }

        LastPickupBonus = bonus;

        RunState.AddGoldInternal(baseAmount + bonus);
        OnGoldChanged?.Invoke(RunState.Gold);

        if (bonus > 0)
        {
            //Debug.log($"[Gold Debug] 本次拾取金币触发双倍：基础 {baseAmount}，额外 {bonus}，合计 {baseAmount + bonus}，剩余双倍额度={RunState.DoubleGoldQuota}");
        }
        else
        {
            //Debug.log($"[Gold Debug] 本次拾取金币为正常收益：+{baseAmount}，当前总金币={RunState.Gold}");
        }
    }

    /// <summary>注册未被拾取而消失的金币值</summary>
    public void RegisterLostGold(int amount)
    {
        if (amount <= 0 || RunState == null) return;
        RunState.AddLostGoldBank(amount);
        // //Debug.log($"[Gold Debug] 记录未拾取金币：+{amount}，当前未拾取累计={RunState.LostGoldBank}");
    }

    /// <summary>将上一关累计的未拾取金币转入下一关的双倍拾取额度</summary>
    public void PrepareDoubleGoldForNextLevel()
    {
        if (RunState == null) return;
        int toAdd = RunState.LostGoldBank;
        if (toAdd <= 0) return;

        RunState.AddDoubleGoldQuota(toAdd);
        RunState.SetLostGoldBank(0);
        //Debug.log($"[Gold Debug] 进入下一关：将未拾取金币 {toAdd} 转入双倍拾取额度");
    }

    /// <summary>重置跨关金币补偿状态（不含当前金币数，由 ResetRunState 统一重置金币）</summary>
    public void ResetGoldBonuses()
    {
        RunState?.ResetGoldBonusesInternal();
    }

    /// <summary>消耗金币，不足时返回 false</summary>
    public bool SpendGold(int amount)
    {
        if (amount <= 0 || RunState == null) return false;
        if (!RunState.SpendGoldInternal(amount)) return false;
        OnGoldChanged?.Invoke(RunState.Gold);
        return true;
    }

    /// <summary>设置金币（用于存档/读档等）</summary>
    public void SetGold(int value)
    {
        if (RunState == null) return;
        RunState.SetGold(value);
        OnGoldChanged?.Invoke(RunState.Gold);
    }

    /// <summary>通知 UI 刷新金币显示（ResetRunState 后调用）</summary>
    public void NotifyGoldChanged()
    {
        OnGoldChanged?.Invoke(Gold);
    }
}
