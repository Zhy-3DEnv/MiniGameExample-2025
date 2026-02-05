using UnityEngine;
using UnityEngine.SceneManagement;

namespace EggRogue
{
    /// <summary>
    /// 玩家等级管理器 - 委托给 PlayerRunState，保留原 API 供 UI 等调用。
    /// 常驻 PersistentScene。
    /// </summary>
    public class PlayerLevelManager : MonoBehaviour
    {
        private static PlayerLevelManager _instance;
        public static PlayerLevelManager Instance => _instance;

        private void Awake()
        {
            var root = transform.root != null ? transform.root.gameObject : gameObject;
            if (_instance != null && _instance != this)
            {
                Destroy(root);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(root);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void Start()
        {
            if (PlayerRunState.Instance != null)
                PlayerRunState.Instance.RecordLevelForCardPicks();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != GameManager.Instance?.GameSceneName)
                return;
            // ResetRunState 由 LevelManager.OnSceneLoaded 统一调用
            if (PlayerRunState.Instance != null)
                PlayerRunState.Instance.RecordLevelForCardPicks();
        }

        /// <summary>当前玩家等级（委托 PlayerRunState）</summary>
        public int CurrentLevel => PlayerRunState.Instance != null ? PlayerRunState.Instance.Level : 1;

        /// <summary>当前经验值（委托 PlayerRunState）</summary>
        public int CurrentXP => PlayerRunState.Instance != null ? PlayerRunState.Instance.CurrentXP : 0;

        /// <summary>等级配置（委托 PlayerRunState，供 Inspector 等读取）</summary>
        public int BaseXPPerLevel
        {
            get => PlayerRunState.Instance != null ? PlayerRunState.Instance.baseXPPerLevel : 10;
            set { if (PlayerRunState.Instance != null) PlayerRunState.Instance.baseXPPerLevel = value; }
        }

        public void RecordLevelForCardPicks()
        {
            PlayerRunState.Instance?.RecordLevelForCardPicks();
        }

        public int GetCardPickCount()
        {
            return PlayerRunState.Instance != null ? PlayerRunState.Instance.GetCardPickCount() : 1;
        }

        public void AddXP(int amount)
        {
            PlayerRunState.Instance?.AddXP(amount);
        }

        public int GetXPToNextLevel()
        {
            return PlayerRunState.Instance != null ? PlayerRunState.Instance.GetXPToNextLevel() : 10;
        }

        [System.Obsolete("请使用 PlayerRunState.Instance.ResetRunState()")]
        public void ResetLevel()
        {
            PlayerRunState.Instance?.ResetRunState();
        }
    }
}
