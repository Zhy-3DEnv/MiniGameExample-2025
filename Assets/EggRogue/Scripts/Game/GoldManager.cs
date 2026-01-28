using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 金币管理器 - 单例，存储当前金币数，收集时加金、UI 订阅刷新。
/// 
/// 使用方式：
/// 1. 场景中放空物体挂本脚本，或由 DontDestroyOnLoad 的 GameManager 等创建
/// 2. Coin 收集时调用 AddGold(1)
/// 3. GameHudPanel 订阅 OnGoldChanged，在回调里 UpdateGold(GoldManager.Instance.Gold)
/// </summary>
public class GoldManager : MonoBehaviour
{
    private static GoldManager _instance;
    public static GoldManager Instance => _instance;

    [Header("事件")]
    public UnityEvent<int> OnGoldChanged;

    private int gold;

    /// <summary>当前金币数</summary>
    public int Gold => gold;

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

    /// <summary>增加金币并触发 OnGoldChanged</summary>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        gold += amount;
        OnGoldChanged?.Invoke(gold);
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
