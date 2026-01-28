using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 关卡计时器 - 管理关卡倒计时，时间到且玩家存活则触发胜利。
/// 挂在 GameScene 的某个对象上（例如空物体 "LevelTimer"）。
/// </summary>
public class LevelTimer : MonoBehaviour
{
    [Header("计时器配置")]
    [Tooltip("关卡时长（秒），从 LevelData 读取")]
    public float levelDuration = 30f;

    [Tooltip("是否自动开始计时（GameScene 加载后）")]
    public bool autoStart = true;

    [Header("事件")]
    [Tooltip("时间到且玩家存活时触发（关卡胜利）")]
    public UnityEvent OnLevelVictory;

    [Tooltip("玩家死亡时触发（关卡失败）")]
    public UnityEvent OnPlayerDeath;

    /// <summary>
    /// 当前剩余时间（秒）。
    /// </summary>
    public float RemainingTime { get; private set; }

    /// <summary>
    /// 是否正在计时。
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// 是否已胜利。
    /// </summary>
    public bool IsVictory { get; private set; }

    /// <summary>
    /// 是否已失败（玩家死亡）。
    /// </summary>
    public bool IsFailed { get; private set; }

    private Health playerHealth;
    private bool playerDeathSubscribed = false;

    private void Start()
    {
        // 从 LevelData 读取关卡时长
        if (EggRogue.LevelManager.Instance != null)
        {
            var levelData = EggRogue.LevelManager.Instance.GetCurrentLevelData();
            if (levelData != null && levelData.levelDuration > 0f)
            {
                levelDuration = levelData.levelDuration;
            }
        }

        RemainingTime = levelDuration;

        // 查找玩家 Health 组件
        FindPlayerHealth();

        if (autoStart)
        {
            StartTimer();
        }
    }

    private void Update()
    {
        if (!IsRunning || IsVictory || IsFailed)
            return;

        RemainingTime -= Time.deltaTime;
        RemainingTime = Mathf.Max(0f, RemainingTime);

        // 检查玩家是否死亡
        if (playerHealth != null && playerHealth.IsDead && !IsFailed)
        {
            OnPlayerDied();
            return;
        }

        // 时间到且玩家存活 → 胜利
        if (RemainingTime <= 0f && !IsVictory)
        {
            OnTimeUp();
        }
    }

    /// <summary>
    /// 开始计时。
    /// </summary>
    public void StartTimer()
    {
        if (IsRunning)
            return;

        IsRunning = true;
        IsVictory = false;
        IsFailed = false;
        RemainingTime = levelDuration;

        FindPlayerHealth();
        SubscribeToPlayerDeath();
    }

    /// <summary>
    /// 停止计时。
    /// </summary>
    public void StopTimer()
    {
        IsRunning = false;
    }

    /// <summary>
    /// 暂停计时（不重置剩余时间）。
    /// 适用于结算界面、选卡界面、属性面板等临时暂停。
    /// </summary>
    public void PauseTimer()
    {
        IsRunning = false;
    }

    /// <summary>
    /// 恢复计时（继续当前剩余时间）。
    /// 不会重置为整关时长。
    /// </summary>
    public void ResumeTimer()
    {
        if (IsVictory || IsFailed)
            return;

        IsRunning = true;
    }

    /// <summary>
    /// 重置计时器。
    /// </summary>
    public void ResetTimer()
    {
        IsRunning = false;
        IsVictory = false;
        IsFailed = false;
        RemainingTime = levelDuration;
        UnsubscribeFromPlayerDeath();
    }

    private void FindPlayerHealth()
    {
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                player = FindObjectOfType<CharacterController>()?.gameObject;
            }
            if (player != null)
            {
                playerHealth = player.GetComponent<Health>();
            }
        }
    }

    private void SubscribeToPlayerDeath()
    {
        if (playerHealth != null && !playerDeathSubscribed)
        {
            playerHealth.OnDeath.AddListener(OnPlayerDied);
            playerDeathSubscribed = true;
        }
    }

    private void UnsubscribeFromPlayerDeath()
    {
        if (playerHealth != null && playerDeathSubscribed)
        {
            playerHealth.OnDeath.RemoveListener(OnPlayerDied);
            playerDeathSubscribed = false;
        }
    }

    private void OnTimeUp()
    {
        if (IsFailed)
        {
            Debug.LogWarning("LevelTimer: 时间到但玩家已失败，不触发胜利");
            return;
        }

        IsVictory = true;
        IsRunning = false;
        Debug.Log($"LevelTimer: 关卡胜利！剩余时间: {RemainingTime:F1}秒，准备触发 OnLevelVictory 事件");
        OnLevelVictory?.Invoke();
        Debug.Log($"LevelTimer: OnLevelVictory 事件已触发，监听器数量: {OnLevelVictory.GetPersistentEventCount()}");
    }

    private void OnPlayerDied()
    {
        if (IsVictory)
            return;

        IsFailed = true;
        IsRunning = false;
        OnPlayerDeath?.Invoke();
        Debug.Log("LevelTimer: 玩家死亡，关卡失败");
    }

    private void OnDestroy()
    {
        UnsubscribeFromPlayerDeath();
    }
}
