using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 道具数据库（ScriptableObject）
    /// </summary>
    public class ItemDatabase : ScriptableObject
    {
        public ItemData[] items;

        public ItemData GetById(string id)
        {
            if (items == null) return null;
            foreach (var item in items)
            {
                if (item != null && item.itemId == id)
                    return item;
            }
            return null;
        }

        public ItemData[] GetRandomItems(int count)
        {
            if (items == null || items.Length == 0 || count <= 0)
                return new ItemData[0];

            var result = new ItemData[count];
            for (int i = 0; i < count; i++)
            {
                int idx = Random.Range(0, items.Length);
                result[i] = items[idx];
            }
            return result;
        }
    }
}
