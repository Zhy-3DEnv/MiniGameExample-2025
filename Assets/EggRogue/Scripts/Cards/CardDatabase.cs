using UnityEngine;
using System.Collections.Generic;

namespace EggRogue
{
/// <summary>
/// 卡片数据库（ScriptableObject）。持有所有可用卡片类型，用于随机选择。
/// 方案2：一种卡一张 CardData，抽卡时先按 cardStarWeights 抽星级，再抽卡类型。
/// </summary>
[CreateAssetMenu(fileName = "CardDatabase", menuName = "EggRogue/Card Database", order = 3)]
public class CardDatabase : ScriptableObject
{
    [Tooltip("所有可用卡片类型（每种卡一张 CardData）")]
    public CardData[] allCards = new CardData[0];

    /// <summary>
    /// 随机选择 N 张不同的卡片。
    /// 先按 LevelData.cardStarWeights 抽星级，再抽卡类型，返回 (CardData, star) 组合。
    /// luck &gt; 0 时高星级权重提升，更容易刷出高星卡。
    /// </summary>
    public CardOffer[] GetRandomCards(int count, LevelData levelDataForWeights = null, float luck = 0f)
    {
        if (allCards == null || allCards.Length == 0)
            return new CardOffer[0];

        var pool = GetWeightedOfferPool(levelDataForWeights, luck);
        if (pool.Count == 0)
            return new CardOffer[0];

        count = Mathf.Min(count, pool.Count);
        var selected = new List<CardOffer>();
        var usedIndices = new HashSet<int>();

        for (int i = 0; i < count; i++)
        {
            int idx = PickWeightedIndex(pool, usedIndices);
            if (idx < 0) break;
            usedIndices.Add(idx);
            selected.Add(pool[idx].offer);
        }

        return selected.ToArray();
    }

    /// <summary>
    /// 构建可抽卡池：(CardOffer, weight)。每个 (卡类型, 星级) 组合对应一条，权重来自 cardStarWeights；luck 提升高星级权重。
    /// </summary>
    private List<(CardOffer offer, float weight)> GetWeightedOfferPool(LevelData levelData, float luck = 0f)
    {
        var pool = new List<(CardOffer, float)>();
        float[] weights = (levelData != null && levelData.cardStarWeights != null && levelData.cardStarWeights.Length >= 5)
            ? levelData.cardStarWeights
            : null;

        for (int c = 0; c < allCards.Length; c++)
        {
            CardData card = allCards[c];
            if (card == null) continue;

            for (int lv = 1; lv <= 5; lv++)
            {
                float w = 1f;
                if (weights != null)
                {
                    w = lv <= weights.Length ? weights[lv - 1] : 0f;
                    if (w <= 0f) continue;
                }
                if (luck > 0.0001f)
                    w *= (1f + luck * 0.1f * lv);
                pool.Add((new CardOffer(card, lv), w));
            }
        }

        if (pool.Count == 0 && allCards.Length > 0)
        {
            for (int c = 0; c < allCards.Length; c++)
            {
                if (allCards[c] != null)
                    pool.Add((new CardOffer(allCards[c], 1), 1f));
            }
        }
        return pool;
    }

    private int PickWeightedIndex(List<(CardOffer offer, float weight)> pool, HashSet<int> exclude)
    {
        float total = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            if (exclude.Contains(i)) continue;
            total += pool[i].weight;
        }
        if (total <= 0f) return -1;

        float r = Random.Range(0f, total);
        for (int i = 0; i < pool.Count; i++)
        {
            if (exclude.Contains(i)) continue;
            r -= pool[i].weight;
            if (r <= 0f) return i;
        }
        return -1;
    }
}
}
