using UnityEngine;
using System.Collections.Generic;

namespace EggRogue
{
    /// <summary>
    /// 游戏玩法暂停管理器（EggRogue 专用）。
    /// 
    /// 职责：统一控制“Gameplay 是否运行”，包括：
    /// - 敌人生成（EnemySpawner）
    /// - 敌人移动（EnemyManager / EnemyController）
    /// - 角色移动（CharacterController）
    /// - 自动攻击（PlayerCombatController）
    /// - 关卡计时（LevelTimer）
    ///
    /// 任何需要暂停 Gameplay 的系统（结算界面、选卡界面、属性面板等）
    /// 只需调用：
    ///   RequestPause("ResultPanel");
    ///   RequestResume("ResultPanel");
    ///
    /// 内部通过“原因计数”来决定是否真正暂停/恢复，支持多重叠加：
    /// 比如属性面板 + 系统弹窗同时打开时，只有全部 Resume 后才会恢复 Gameplay。
    /// </summary>
    public class GameplayPauseManager : MonoBehaviour
    {
        private static GameplayPauseManager _instance;
        public static GameplayPauseManager Instance => _instance;

        [Header("调试")]
        [SerializeField]
        private bool isPaused = false;

        // 当前请求暂停的“原因集合”（去重）：同一个 reason 重复 Pause 不会叠加计数，避免卡住无法恢复
        private readonly HashSet<string> pauseReasons = new HashSet<string>();

        private void Awake()
        {
            // Unity 限制：DontDestroyOnLoad 只能作用于“根 GameObject”（或挂在根物体上的组件）。
            // 为了避免将本脚本挂在子物体时出现报错，这里统一对根节点做常驻。
            GameObject rootGO = transform.root != null ? transform.root.gameObject : gameObject;

            if (_instance != null && _instance != this)
            {
                // 如果已经有一个全局实例，销毁这整棵重复的根节点，避免留下多套管理器
                Destroy(rootGO);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(rootGO);
        }

        /// <summary>
        /// 请求暂停 Gameplay。
        /// </summary>
        /// <param name="reason">暂停原因（用于日志，可选）</param>
        public void RequestPause(string reason)
        {
            if (string.IsNullOrEmpty(reason))
                reason = "(Unknown)";

            bool added = pauseReasons.Add(reason);
            if (!added)
            {
                Debug.Log($"GameplayPauseManager: 重复暂停请求已忽略，原因: {reason}（当前暂停原因数: {pauseReasons.Count}）");
                return;
            }

            if (pauseReasons.Count == 1)
            {
                // 第一次从 0 -> 1，真正执行暂停
                ApplyPause();
                Debug.Log($"GameplayPauseManager: Gameplay 已暂停，原因: {reason}");
            }
            else
            {
                Debug.Log($"GameplayPauseManager: 已追加暂停原因（当前 {pauseReasons.Count} 个），最近原因: {reason}");
            }
        }

        /// <summary>
        /// 请求恢复 Gameplay。
        /// </summary>
        /// <param name="reason">恢复原因（用于日志，可选）</param>
        public void RequestResume(string reason)
        {
            if (string.IsNullOrEmpty(reason))
                reason = "(Unknown)";

            bool removed = pauseReasons.Remove(reason);
            if (!removed)
            {
                Debug.LogWarning($"GameplayPauseManager: 收到未登记的恢复请求，已忽略，原因: {reason}（当前暂停原因数: {pauseReasons.Count}）");
                return;
            }

            if (pauseReasons.Count == 0)
            {
                // 所有暂停理由都结束，真正恢复
                ApplyResume();
                Debug.Log($"GameplayPauseManager: Gameplay 已恢复，原因: {reason}");
            }
            else
            {
                Debug.Log($"GameplayPauseManager: 仍有 {pauseReasons.Count} 个暂停原因未完成，最近恢复原因: {reason}");
            }
        }

        /// <summary>
        /// 立即强制清空所有暂停请求并恢复 Gameplay（紧急使用）。
        /// </summary>
        public void ForceResumeAll(string reason = "ForceResumeAll")
        {
            pauseReasons.Clear();
            ApplyResume();
            Debug.Log($"GameplayPauseManager: ForceResumeAll 调用，原因: {reason}");
        }

        private void ApplyPause()
        {
            if (isPaused)
                return;

            isPaused = true;

            // 1. 暂停关卡计时
            LevelTimer timer = Object.FindObjectOfType<LevelTimer>();
            if (timer != null)
            {
                timer.PauseTimer();
            }

            // 2. 停止刷怪
            EnemySpawner spawner = Object.FindObjectOfType<EnemySpawner>();
            if (spawner != null)
            {
                spawner.StopSpawning();
            }

            // 3. 暂停所有敌人移动
            if (EnemyManager.Instance != null)
            {
                EnemyManager.Instance.PauseAllEnemies();
            }

            // 4. 禁用角色移动
            CharacterController characterController = Object.FindObjectOfType<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = false;
                // 同时将当前速度清零，避免恢复后瞬间冲刺
                characterController.StopMovement();
            }

            // 5. 禁用自动攻击
            PlayerCombatController combat = Object.FindObjectOfType<PlayerCombatController>();
            if (combat != null)
            {
                combat.enabled = false;
            }
        }

        private void ApplyResume()
        {
            if (!isPaused)
                return;

            isPaused = false;

            // 1. 恢复关卡计时
            LevelTimer timer = Object.FindObjectOfType<LevelTimer>();
            if (timer != null)
            {
                timer.ResumeTimer();
            }

            // 2. 恢复刷怪
            EnemySpawner spawner = Object.FindObjectOfType<EnemySpawner>();
            if (spawner != null)
            {
                spawner.StartSpawning();
            }

            // 3. 恢复敌人移动
            if (EnemyManager.Instance != null)
            {
                EnemyManager.Instance.ResumeAllEnemies();
            }

            // 4. 恢复角色移动
            CharacterController characterController = Object.FindObjectOfType<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = true;
            }

            // 5. 恢复自动攻击
            PlayerCombatController combat = Object.FindObjectOfType<PlayerCombatController>();
            if (combat != null)
            {
                combat.enabled = true;
            }
        }

        /// <summary>
        /// 当前 Gameplay 是否处于暂停状态（只读）。
        /// </summary>
        public bool IsPaused => isPaused;
    }
}

