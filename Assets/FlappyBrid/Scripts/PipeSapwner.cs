using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipeSpawner : MonoBehaviour
{
    public GameObject pipe;  //public 表示公共的，可访问，会在inspector显示，能调整
    public float spawnRate = 2;
    private float timer = 0; //private 表示私有的，不可访问，只能在内部逻辑中使用
    public float heightoffset = 10;
    [Header("道具生成设置")]
    [Range(0f, 1f)]
    [Tooltip("道具生成概率（0-1之间，0.3表示30%概率生成道具）\n这是第一层概率：决定是否生成道具")]
    public float itemSpawnChance = 0.3f; // 道具生成概率（0-1之间，0.3表示30%概率）
    [Tooltip("道具相对管道的X轴偏移（可以放在管道中间位置）")]
    public float itemSpawnOffsetX = 0f; // 道具相对管道的X轴偏移（可以放在管道中间位置）
    [Header("多类型道具设置")]
    [Tooltip("道具类型列表\n每个道具的Spawn Weight控制相对生成概率\n实际概率 = (该道具权重/总权重) × Item Spawn Chance")]
    public List<ItemData> itemTypes = new List<ItemData>(); // 道具类型列表
    
    [Header("概率预览（只读）")]
    [SerializeField, Tooltip("显示每个道具的实际生成概率（自动计算）")]
    private string probabilityPreview = ""; // 用于在Inspector中显示概率信息
    
    void Start()
    {
        spawnPipe();
        UpdateProbabilityPreview();
    }
    
    void OnValidate()
    {
        // 在Inspector中修改值时自动更新概率预览
        UpdateProbabilityPreview();
    }

    // Update is called once per frame
    void Update()
    {
        if (timer < spawnRate)//如果时间小于生成率，则时间+=加帧间隔时间，帧间隔时间累计相加
        {
            timer += Time.deltaTime;
        }
        else//否则生成管道，并重新记时
        {
            spawnPipe();
            timer = 0;
        }

    }
    void spawnPipe()//instance方式生成管道，void是执行逻辑，不需要返回结果
    {
        float lowestPoint = transform.position.y - heightoffset;
        float highestPoint = transform.position.y + heightoffset;
        float pipeY = Random.Range(lowestPoint, highestPoint);
        
        // 生成管道
        Instantiate(pipe, new Vector3(transform.position.x, pipeY, 0), transform.rotation);
        
        // 根据概率生成道具
        if (itemTypes.Count > 0 && Random.Range(0f, 1f) < itemSpawnChance)
        {
            // 随机选择一个道具类型（基于权重）
            ItemData selectedItem = GetRandomItemByWeight();
            if (selectedItem != null && selectedItem.itemPrefab != null)
            {
                // 在管道中间位置生成道具（Y坐标与管道相同，X坐标稍微偏移）
                float itemX = transform.position.x + itemSpawnOffsetX;
                GameObject spawnedItem = Instantiate(selectedItem.itemPrefab, new Vector3(itemX, pipeY, 0), transform.rotation);
                
                // 设置道具的分数值和颜色
                ItemScript itemScript = spawnedItem.GetComponent<ItemScript>();
                if (itemScript != null)
                {
                    itemScript.scoreValue = selectedItem.scoreValue;
                    itemScript.scoreColor = selectedItem.scoreColor;
                }
            }
        }
    }
    
    /// <summary>
    /// 根据权重随机选择一个道具类型
    /// </summary>
    private ItemData GetRandomItemByWeight()
    {
        if (itemTypes.Count == 0) return null;
        
        // 计算总权重
        float totalWeight = 0f;
        foreach (ItemData item in itemTypes)
        {
            if (item.itemPrefab != null)
            {
                totalWeight += item.spawnWeight;
            }
        }
        
        if (totalWeight <= 0f) return null;
        
        // 随机一个0到总权重之间的值
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        // 遍历道具列表，找到对应的道具
        foreach (ItemData item in itemTypes)
        {
            if (item.itemPrefab == null) continue;
            
            currentWeight += item.spawnWeight;
            if (randomValue <= currentWeight)
            {
                return item;
            }
        }
        
        // 如果没找到（理论上不会发生），返回第一个有效道具
        foreach (ItemData item in itemTypes)
        {
            if (item.itemPrefab != null)
            {
                return item;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 更新概率预览信息（用于Inspector显示）
    /// </summary>
    private void UpdateProbabilityPreview()
    {
        if (itemTypes.Count == 0)
        {
            probabilityPreview = "暂无道具配置";
            return;
        }
        
        // 计算总权重
        float totalWeight = 0f;
        int validItemCount = 0;
        foreach (ItemData item in itemTypes)
        {
            if (item.itemPrefab != null)
            {
                totalWeight += item.spawnWeight;
                validItemCount++;
            }
        }
        
        if (totalWeight <= 0f || validItemCount == 0)
        {
            probabilityPreview = "无有效道具";
            return;
        }
        
        // 生成概率预览文本
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"道具生成概率: {itemSpawnChance * 100:F1}%");
        sb.AppendLine($"有效道具数: {validItemCount}");
        sb.AppendLine($"总权重: {totalWeight:F2}");
        sb.AppendLine("---");
        
        foreach (ItemData item in itemTypes)
        {
            if (item.itemPrefab == null) continue;
            
            float relativeProbability = item.spawnWeight / totalWeight;
            float actualProbability = relativeProbability * itemSpawnChance;
            string itemName = string.IsNullOrEmpty(item.itemName) ? item.itemPrefab.name : item.itemName;
            
            sb.AppendLine($"{itemName}:");
            sb.AppendLine($"  权重: {item.spawnWeight:F2} → 相对概率: {relativeProbability * 100:F1}%");
            sb.AppendLine($"  实际概率: {actualProbability * 100:F1}% (分数: +{item.scoreValue})");
        }
        
        probabilityPreview = sb.ToString();
    }
}
