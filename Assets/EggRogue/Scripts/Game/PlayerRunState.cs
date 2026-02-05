using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace EggRogue
{
    /// <summary>
    /// 单局运行时状态 - 集中管理金币、等级、血量等跨关继承数据。
    /// 常驻 PersistentScene，新局时通过 ResetRunState 统一重置。
    /// </summary>
    public class PlayerRunState : MonoBehaviour
    {
        private static PlayerRunState _instance;
        public static PlayerRunState Instance => _instance;

        [Header("等级配置")]
        [Tooltip("升到下一级所需经验（每级递增 baseXP * level）")]
        public int baseXPPerLevel = 10;

        [Tooltip("是否在进入新关卡时记录当前等级（用于计算选卡次数）")]
        public bool recordLevelOnLevelStart = true;

        // 等级状态
        private int _level = 1;
        private int _currentXP;
        private int _levelAtLevelStart = 1;

        // 血量继承
        private bool _hasSavedRunStateHealth;
        private float _savedCurrentHealth;
        private float _savedMaxHealth;

        // 金币状态（GoldManager 读写，ResetRunState 时重置）
        private int _gold;
        private int _lostGoldBank;
        private int _doubleGoldQuota;

        // 上轮商店锁定的商品（槽位索引 + 物品，跨关保留）
        private readonly List<(int slotIndex, ShopItemData item)> _persistedShopItems = new List<(int, ShopItemData)>();

        public int Level => _level;
        public int CurrentXP => _currentXP;
        public int LevelAtLevelStart => _levelAtLevelStart;

        private void Awake()
        {
            GameObject rootGO = transform.root != null ? transform.root.gameObject : gameObject;
            if (_instance != null && _instance != this)
            {
                Destroy(rootGO);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(rootGO);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
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
            RecordLevelForCardPicks();
        }

        /// <summary>
        /// 统一重置本局状态（进入第 1 关时调用）
        /// </summary>
        public void ResetRunState()
        {
            _level = GetInitialLevelFromCharacterData();
            _currentXP = 0;
            _levelAtLevelStart = _level;
            ClearRunStateHealth();
            _gold = 0;
            _lostGoldBank = 0;
            _doubleGoldQuota = 0;

            ItemInventoryManager.Instance?.ClearAll();
            _persistedShopItems.Clear();
        }

        /// <summary>保存上轮商店锁定的商品（槽位+物品），供下关商店恢复</summary>
        public void SetPersistedShopItems(List<(int slotIndex, ShopItemData item)> items)
        {
            _persistedShopItems.Clear();
            if (items != null)
                _persistedShopItems.AddRange(items);
        }

        /// <summary>取出并清空保存的锁定商品（槽位+物品）</summary>
        public List<(int slotIndex, ShopItemData item)> GetAndClearPersistedShopItems()
        {
            var copy = new List<(int, ShopItemData)>(_persistedShopItems);
            _persistedShopItems.Clear();
            return copy;
        }

        #region 金币（供 GoldManager 读写）

        public int Gold => _gold;

        public int LostGoldBank => _lostGoldBank;
        public int DoubleGoldQuota => _doubleGoldQuota;

        public void SetGold(int value)
        {
            _gold = Mathf.Max(0, value);
        }

        public void AddGoldInternal(int amount)
        {
            if (amount <= 0) return;
            _gold += amount;
        }

        public bool SpendGoldInternal(int amount)
        {
            if (amount <= 0 || _gold < amount) return false;
            _gold -= amount;
            return true;
        }

        public void SetLostGoldBank(int value) => _lostGoldBank = Mathf.Max(0, value);
        public void AddLostGoldBank(int amount)
        {
            if (amount <= 0) return;
            _lostGoldBank += amount;
        }

        public void SetDoubleGoldQuota(int value) => _doubleGoldQuota = Mathf.Max(0, value);
        public void AddDoubleGoldQuota(int amount)
        {
            if (amount <= 0) return;
            _doubleGoldQuota += amount;
        }

        public void ConsumeDoubleGoldQuota(int amount)
        {
            _doubleGoldQuota = Mathf.Max(0, _doubleGoldQuota - amount);
        }

        public void ResetGoldBonusesInternal()
        {
            _lostGoldBank = 0;
            _doubleGoldQuota = 0;
        }

        #endregion

        private int GetInitialLevelFromCharacterData()
        {
            if (CharacterSelectionManager.Instance != null &&
                CharacterSelectionManager.Instance.SelectedCharacter != null)
            {
                return Mathf.Max(1, CharacterSelectionManager.Instance.SelectedCharacter.baseLevel);
            }

            CharacterStats stats = FindObjectOfType<CharacterStats>();
            if (stats != null && stats.characterData != null)
            {
                return Mathf.Max(1, stats.characterData.baseLevel);
            }

            return 1;
        }

        /// <summary>
        /// 进入关卡时记录当前等级，用于计算选卡次数
        /// </summary>
        public void RecordLevelForCardPicks()
        {
            if (!recordLevelOnLevelStart) return;
            _levelAtLevelStart = _level;
        }

        /// <summary>
        /// 获得选卡次数 = 本关结束等级 - 本关开始等级，至少 1 次
        /// </summary>
        public int GetCardPickCount()
        {
            int gained = _level - _levelAtLevelStart;
            return Mathf.Max(1, gained);
        }

        /// <summary>
        /// 升到下一级所需经验值
        /// </summary>
        public int GetXPToNextLevel()
        {
            return baseXPPerLevel * _level;
        }

        /// <summary>
        /// 增加经验值，自动处理升级
        /// </summary>
        public void AddXP(int amount)
        {
            if (amount <= 0) return;
            _currentXP += amount;

            while (GetXPToNextLevel() > 0 && _currentXP >= GetXPToNextLevel())
            {
                _currentXP -= GetXPToNextLevel();
                _level++;
            }
        }

        /// <summary>
        /// 保存当前玩家血量，用于进入下一关后恢复
        /// </summary>
        public void SaveRunStateHealth(float current, float max)
        {
            _hasSavedRunStateHealth = true;
            _savedCurrentHealth = current;
            _savedMaxHealth = Mathf.Max(0.001f, max);
        }

        /// <summary>
        /// 尝试取出并消费已保存的血量；若有则返回 true 并清除保存
        /// </summary>
        public bool TryGetRunStateHealth(out float current, out float max)
        {
            if (!_hasSavedRunStateHealth)
            {
                current = 0f;
                max = 0f;
                return false;
            }
            current = _savedCurrentHealth;
            max = _savedMaxHealth;
            _hasSavedRunStateHealth = false;
            return true;
        }

        /// <summary>
        /// 清除单局血量继承状态
        /// </summary>
        public void ClearRunStateHealth()
        {
            _hasSavedRunStateHealth = false;
        }
    }
}
