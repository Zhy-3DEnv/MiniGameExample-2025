using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 角色选择管理器 - 记录玩家选择的角色，供 GameScene 中的 CharacterStats 使用。
    /// 常驻 PersistentScene，与 GameManager 配合。
    /// </summary>
    public class CharacterSelectionManager : MonoBehaviour
    {
        private static CharacterSelectionManager _instance;
        public static CharacterSelectionManager Instance => _instance;

        [Header("角色配置")]
        [Tooltip("角色数据库（ScriptableObject）")]
        public CharacterDatabase characterDatabase;

        [Tooltip("默认角色（未选择时使用）")]
        public CharacterData defaultCharacter;

        /// <summary>
        /// 当前选中的角色数据
        /// </summary>
        public CharacterData SelectedCharacter { get; private set; }

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

            // 初始化：使用默认角色
            if (SelectedCharacter == null)
            {
                SelectedCharacter = defaultCharacter;
            }
        }

        /// <summary>
        /// 选择角色（由角色选择界面调用）
        /// </summary>
        public void SelectCharacter(CharacterData character)
        {
            if (character == null)
            {
                Debug.LogWarning("CharacterSelectionManager: 试图选择空角色，已忽略");
                return;
            }

            SelectedCharacter = character;
            Debug.Log($"CharacterSelectionManager: 已选择角色 - {character.characterName}");
        }

        /// <summary>
        /// 选择角色（通过索引）
        /// </summary>
        public void SelectCharacterByIndex(int index)
        {
            if (characterDatabase == null)
            {
                Debug.LogWarning("CharacterSelectionManager: characterDatabase 未设置");
                return;
            }

            CharacterData character = characterDatabase.GetCharacterByIndex(index);
            if (character != null)
            {
                SelectCharacter(character);
            }
            else
            {
                Debug.LogWarning($"CharacterSelectionManager: 索引 {index} 超出范围或角色为空");
            }
        }

        /// <summary>
        /// 重置为默认角色（用于重新开始游戏等）
        /// </summary>
        public void ResetToDefault()
        {
            SelectedCharacter = defaultCharacter;
        }
    }
}
