using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 角色数据库 - 存储所有可选角色的配置。
    /// 用于角色选择界面显示和角色管理。
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterDatabase", menuName = "EggRogue/Character Database", order = 3)]
    public class CharacterDatabase : ScriptableObject
    {
        [Tooltip("所有可选角色列表")]
        public CharacterData[] characters;

        /// <summary>
        /// 根据角色名称获取角色数据
        /// </summary>
        public CharacterData GetCharacterByName(string name)
        {
            if (characters == null || string.IsNullOrEmpty(name))
                return null;

            foreach (var character in characters)
            {
                if (character != null && character.characterName == name)
                    return character;
            }

            return null;
        }

        /// <summary>
        /// 根据索引获取角色数据
        /// </summary>
        public CharacterData GetCharacterByIndex(int index)
        {
            if (characters == null || index < 0 || index >= characters.Length)
                return null;

            return characters[index];
        }

        /// <summary>
        /// 获取角色总数
        /// </summary>
        public int CharacterCount => characters != null ? characters.Length : 0;
    }
}
