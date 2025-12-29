using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// 通用游戏暂停管理器
/// 支持所有平台：PC端（窗口失焦）、移动端（切屏）、微信小游戏（弹窗/切屏）
/// </summary>
public class GamePauseManager : MonoBehaviour
{
    private static GamePauseManager instance;
    public static GamePauseManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("GamePauseManager");
                instance = go.AddComponent<GamePauseManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [Header("暂停设置")]
    [Tooltip("是否启用自动暂停功能")]
    public bool enableAutoPause = true;
    [Tooltip("是否在暂停时显示提示（可选）")]
    public bool showPauseMessage = false;
    [Header("平台特定设置")]
    [Tooltip("PC端：窗口失焦时暂停")]
    public bool pauseOnFocusLoss = true;
    [Tooltip("移动端：应用暂停时暂停")]
    public bool pauseOnApplicationPause = true;
    [Tooltip("微信小游戏：切屏/弹窗时暂停")]
    public bool pauseOnWeChatHide = true;

    private bool isPaused = false;
    private float previousTimeScale = 1f;

    // 微信小游戏API（通过JSLib调用，仅在WebGL且非编辑器时使用）
    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void RegisterWeChatLifecycle();
    #else
    private static void RegisterWeChatLifecycle() { }
    #endif

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (enableAutoPause)
        {
            RegisterLifecycleEvents();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // PC端和编辑器：窗口失焦时暂停
        if (enableAutoPause && pauseOnFocusLoss)
        {
            if (!hasFocus)
            {
                PauseGame("窗口失焦");
            }
            else
            {
                ResumeGame("窗口获得焦点");
            }
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        // 移动平台：应用暂停时暂停（切屏、通知等）
        if (enableAutoPause && pauseOnApplicationPause)
        {
            if (pauseStatus)
            {
                PauseGame("应用暂停");
            }
            else
            {
                ResumeGame("应用恢复");
            }
        }
    }

    /// <summary>
    /// 注册生命周期事件（包括微信小游戏的特殊处理）
    /// </summary>
    private void RegisterLifecycleEvents()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL平台：尝试注册微信小游戏生命周期事件
        if (pauseOnWeChatHide)
        {
            try
            {
                RegisterWeChatLifecycle();
                Debug.Log("GamePauseManager: 已注册微信小游戏生命周期事件");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"GamePauseManager: 注册微信生命周期事件失败（可能不在微信环境中）: {e.Message}");
            }
        }
        #else
        Debug.Log("GamePauseManager: 使用Unity标准生命周期事件（OnApplicationFocus/OnApplicationPause）");
        #endif
    }

    /// <summary>
    /// 游戏隐藏时调用（由微信小游戏JSLib调用，或其他平台的生命周期事件）
    /// </summary>
    public void OnGameHide()
    {
        if (enableAutoPause && pauseOnWeChatHide && !isPaused)
        {
            PauseGame("微信小游戏隐藏/弹窗");
        }
    }

    /// <summary>
    /// 游戏显示时调用（由微信小游戏JSLib调用，或其他平台的生命周期事件）
    /// </summary>
    public void OnGameShow()
    {
        if (enableAutoPause && pauseOnWeChatHide && isPaused)
        {
            ResumeGame("微信小游戏显示");
        }
    }

    /// <summary>
    /// 暂停游戏
    /// </summary>
    /// <param name="reason">暂停原因（用于日志）</param>
    public void PauseGame(string reason = "手动暂停")
    {
        if (isPaused) return;

        isPaused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // 暂停音频
        AudioListener.pause = true;

        if (showPauseMessage)
        {
            Debug.Log($"游戏已暂停: {reason}");
        }

        // 可以在这里添加其他暂停逻辑，比如暂停动画、物理等
    }

    /// <summary>
    /// 恢复游戏
    /// </summary>
    /// <param name="reason">恢复原因（用于日志）</param>
    public void ResumeGame(string reason = "手动恢复")
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = previousTimeScale;

        // 恢复音频
        AudioListener.pause = false;

        if (showPauseMessage)
        {
            Debug.Log($"游戏已恢复: {reason}");
        }

        // 可以在这里添加其他恢复逻辑
    }

    /// <summary>
    /// 手动暂停/恢复（用于测试或游戏内暂停功能）
    /// </summary>
    [ContextMenu("切换暂停状态")]
    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame("手动恢复");
        }
        else
        {
            PauseGame("手动暂停");
        }
    }

    /// <summary>
    /// 获取当前暂停状态
    /// </summary>
    public bool IsPaused
    {
        get { return isPaused; }
    }

    void OnDestroy()
    {
        // 确保游戏恢复
        if (isPaused)
        {
            ResumeGame();
        }
    }
}

