using UnityEngine;

namespace EggRogue
{
/// <summary>
/// 卡片数据库（ScriptableObject）。持有所有可用卡片，用于随机选择。
/// </summary>
[CreateAssetMenu(fileName = "CardDatabase", menuName = "EggRogue/Card Database", order = 3)]
public class CardDatabase : ScriptableObject
{
    [Tooltip("所有可用卡片列表")]
    public CardData[] allCards = new CardData[0];

    /// <summary>
    /// 随机选择 N 张不同的卡片（用于卡片选择界面）。
    /// </summary>
    public CardData[] GetRandomCards(int count)
    {
        if (allCards == null || allCards.Length == 0)
            return new CardData[0];

        count = Mathf.Min(count, allCards.Length);
        CardData[] selected = new CardData[count];
        System.Collections.Generic.List<int> usedIndices = new System.Collections.Generic.List<int>();

        for (int i = 0; i < count; i++)
        {
            int randomIndex;
            do
            {
                randomIndex = Random.Range(0, allCards.Length);
            } while (usedIndices.Contains(randomIndex));

            usedIndices.Add(randomIndex);
            selected[i] = allCards[randomIndex];
        }

        return selected;
    }
}
}
