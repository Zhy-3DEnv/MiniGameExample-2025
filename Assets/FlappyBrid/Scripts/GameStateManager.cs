using UnityEngine;

/// <summary>
/// 游戏状态枚举
/// </summary>
public enum GameState
{
    Menu,           // 开始菜单
    Playing,        // 游戏中
    LevelComplete,  // 关卡完成
    GameOver        // 游戏结束
}

/// <summary>
/// 游戏状态管理器
/// 管理游戏的整体状态流转
/// </summary>
public class GameStateManager : MonoBehaviour
{
    private static GameStateManager instance;
    public static GameStateManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("GameStateManager");
                instance = go.AddComponent<GameStateManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private GameState currentState = GameState.Menu;
    
    // 状态变化事件
    public System.Action<GameState> OnStateChanged;

    public GameState CurrentState
    {
        get { return currentState; }
        private set
        {
            if (currentState != value)
            {
                currentState = value;
                OnStateChanged?.Invoke(currentState);
            }
        }
    }

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

    /// <summary>
    /// 开始游戏
    /// </summary>
    public void StartGame()
    {
        CurrentState = GameState.Playing;
    }

    /// <summary>
    /// 完成关卡
    /// </summary>
    public void CompleteLevel()
    {
        CurrentState = GameState.LevelComplete;
    }

    /// <summary>
    /// 游戏结束
    /// </summary>
    public void GameOver()
    {
        CurrentState = GameState.GameOver;
    }

    /// <summary>
    /// 返回菜单
    /// </summary>
    public void ReturnToMenu()
    {
        CurrentState = GameState.Menu;
    }

    /// <summary>
    /// 检查是否在游戏中
    /// </summary>
    public bool IsPlaying()
    {
        return CurrentState == GameState.Playing;
    }
}


