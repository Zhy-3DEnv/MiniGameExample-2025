using UnityEngine;
using System.Collections.Generic;

namespace EggRogue
{
/// <summary>
/// 卡片数据库（ScriptableObject）。持有所有可用卡片类型，用于随机选择。
/// 方案2：一种卡一张 CardData，抽卡时先按 cardLevelWeights 抽等级，再抽卡类型。
/// </summary>
[CreateAssetMenu(fileName = "CardDatabase", menuName = "EggRogue/Card Database", order = 3)]
public class CardDatabase : ScriptableObject
{
    [Tooltip("所有可用卡片类型（每种卡一张 CardData）")]
    public CardData[] allCards = new CardData[0];

    /// <summary>
    /// 随机选择 N 张不同的卡片。
    /// 先按 LevelData.cardLevelWeights 抽等级，再抽卡类型，返回 (CardData, level) 组合。
    /// 不传 levelData 时等级均匀随机。
    /// </summary>
    public CardOffer[] GetRandomCards(int count, LevelData levelDataForWeights = null)
    {
        if (allCards == null || allCards.Length == 0)
            return new CardOffer[0];

        var pool = GetWeightedOfferPool(levelDataForWeights);
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
    /// 构建可抽卡池：(CardOffer, weight)。每个 (卡类型, 等级) 组合对应一条，权重来自 cardLevelWeights。
    /// </summary>
    private List<(CardOffer offer, float weight)> GetWeightedOfferPool(LevelData levelData)
    {
        var pool = new List<(CardOffer, float)>();
        float[] weights = (levelData != null && levelData.cardLevelWeights != null && levelData.cardLevelWeights.Length >= 5)
            ? levelData.cardLevelWeights
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
