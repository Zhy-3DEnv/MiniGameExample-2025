using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipeSpawner : MonoBehaviour
{
    public GameObject pipe;  //public 表示公共的，可访问，会在inspector显示，能调整
    public float spawnRate = 2;
    private float timer = 0; //private 表示私有的，不可访问，只能在内部逻辑中使用
    public float heightoffset = 10;
    
    [Header("基础设置（会被关卡设置覆盖）")]
    [Tooltip("基础生成速度（秒，值越小生成越快）")]
    public float baseSpawnRate = 2f;
    
    [Tooltip("基础移动速度（用于计算管道间距，确保屏幕上始终有管道）")]
    public float baseMoveSpeed = 5f;
    
    [Tooltip("基础高度偏移（管道生成位置的Y轴随机范围）")]
    public float baseHeightOffset = 10f;
    
    [Header("道具生成默认设置（当关卡未设置时使用）")]
    [Range(0f, 1f)]
    [Tooltip("道具生成概率（0-1之间，0.3表示30%概率生成道具）\n这是第一层概率：决定是否生成道具\n如果关卡设置了道具生成，则使用关卡设置")]
    public float defaultItemSpawnChance = 0.3f;
    
    [Header("怪物生成默认设置（当关卡未设置时使用）")]
    [Range(0f, 1f)]
    [Tooltip("怪物生成概率（0-1之间，0.2表示20%概率生成怪物）\n这是第一层概率：决定是否生成怪物\n如果关卡设置了怪物生成，则使用关卡设置")]
    public float defaultMonsterSpawnChance = 0.2f;
    
    [Tooltip("怪物相对管道的X轴偏移（可以放在管道附近位置）\n如果关卡设置了怪物生成，则使用关卡设置")]
    public float defaultMonsterSpawnOffsetX = 0f;
    
    [Tooltip("怪物相对管道的Y轴偏移（可以放在管道上方或下方）\n如果关卡设置了怪物生成，则使用关卡设置")]
    public float defaultMonsterSpawnOffsetY = 0f;
    
    [Tooltip("默认怪物类型列表（当关卡未设置怪物列表时使用）\n每个怪物的Spawn Weight控制相对生成概率\n实际概率 = (该怪物权重/总权重) × Monster Spawn Chance")]
    public List<MonsterData> defaultMonsterTypes = new List<MonsterData>();
    
    [Tooltip("道具相对管道的X轴偏移（可以放在管道中间位置）\n如果关卡设置了道具生成，则使用关卡设置")]
    public float defaultItemSpawnOffsetX = 0f;
    
    [Tooltip("默认道具类型列表（当关卡未设置道具列表时使用）\n每个道具的Spawn Weight控制相对生成概率\n实际概率 = (该道具权重/总权重) × Item Spawn Chance")]
    public List<ItemData> defaultItemTypes = new List<ItemData>();
    
    [Header("当前使用的道具设置（运行时自动更新，只读）")]
    [SerializeField, Tooltip("当前使用的道具生成概率")]
    private float currentItemSpawnChance = 0.3f;
    
    [SerializeField, Tooltip("当前使用的道具X轴偏移")]
    private float currentItemSpawnOffsetX = 0f;
    
    [SerializeField, Tooltip("当前使用的道具类型列表")]
    private List<ItemData> currentItemTypes = new List<ItemData>();
    
    [Header("当前使用的怪物设置（运行时自动更新，只读）")]
    [SerializeField, Tooltip("当前使用的怪物生成概率")]
    private float currentMonsterSpawnChance = 0.2f;
    
    [SerializeField, Tooltip("当前使用的怪物X轴偏移")]
    private float currentMonsterSpawnOffsetX = 0f;
    
    [SerializeField, Tooltip("当前使用的怪物Y轴偏移")]
    private float currentMonsterSpawnOffsetY = 0f;
    
    [SerializeField, Tooltip("当前使用的怪物类型列表")]
    private List<MonsterData> currentMonsterTypes = new List<MonsterData>();
    
    [Header("概率预览（只读）")]
    [SerializeField, Tooltip("显示每个道具的实际生成概率（自动计算）")]
    private string probabilityPreview = ""; // 用于在Inspector中显示概率信息
    
    void Start()
    {
        // 不在Start中立即生成管道，等待游戏开始
        baseSpawnRate = spawnRate; // 保存基础值
        baseHeightOffset = heightoffset; // 保存基础高度偏移
        
        // 获取基础移动速度（从PipeMoveScript或使用默认值）
        PipeMoveScript pipeMover = FindObjectOfType<PipeMoveScript>();
        if (pipeMover != null)
        {
            baseMoveSpeed = pipeMover.baseMoveSpeed;
        }
        
        // 初始化当前设置（使用默认值）
        currentItemSpawnChance = defaultItemSpawnChance;
        currentItemSpawnOffsetX = defaultItemSpawnOffsetX;
        currentItemTypes = new List<ItemData>(defaultItemTypes);
        
        UpdateProbabilityPreview();
        
        // 订阅关卡变化事件
        if (LevelManager.Instance != null)
        {
            // 应用当前关卡设置
            ApplyLevelSettings();
        }
    }
    
    /// <summary>
    /// 应用关卡设置
    /// </summary>
    public void ApplyLevelSettings()
    {
        if (LevelManager.Instance != null)
        {
            LevelData levelData = LevelManager.Instance.GetCurrentLevelData();
            if (levelData != null)
            {
                // 应用生成率倍数
                // spawnRate 是生成间隔时间（秒），值越小生成越快
                // spawnRateMultiplier 值越大，生成的越多（生成间隔时间应该越小）
                // 所以：spawnRate = baseSpawnRate / spawnRateMultiplier
                // 例如：baseSpawnRate=2, spawnRateMultiplier=2 → spawnRate=1（生成快）
                //      baseSpawnRate=2, spawnRateMultiplier=0.5 → spawnRate=4（生成慢）
                float calculatedSpawnRate = baseSpawnRate / levelData.spawnRateMultiplier;
                
                // 根据移动速度动态调整生成率，确保屏幕上始终有管道
                float pipeMoveSpeed = baseMoveSpeed * levelData.moveSpeedMultiplier;
                
                // 计算管道之间的间距
                float pipeSpacing = calculatedSpawnRate * pipeMoveSpeed;
                
                // 如果间距太大（超过屏幕宽度），自动调整生成率
                Camera mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    mainCamera = FindObjectOfType<Camera>();
                }
                
                float screenWidth = 20f; // 默认值
                if (mainCamera != null)
                {
                    Vector3 screenLeft = mainCamera.ScreenToWorldPoint(new Vector3(0, Screen.height / 2, mainCamera.nearClipPlane));
                    Vector3 screenRight = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height / 2, mainCamera.nearClipPlane));
                    screenWidth = screenRight.x - screenLeft.x;
                }
                
                // 如果管道间距超过屏幕宽度的80%，自动调整生成率
                // 确保屏幕上至少同时有2-3个管道
                float maxSpacing = screenWidth * 0.8f;
                if (pipeSpacing > maxSpacing && pipeMoveSpeed > 0)
                {
                    // 重新计算spawnRate，确保管道间距不超过maxSpacing
                    calculatedSpawnRate = maxSpacing / pipeMoveSpeed;
                    Debug.Log($"PipeSpawner: 自动调整生成率 - 原spawnRate={baseSpawnRate / levelData.spawnRateMultiplier:F2}, 调整后={calculatedSpawnRate:F2} (管道间距过大，已优化)");
                }
                
                spawnRate = calculatedSpawnRate;
                
                // 调试信息：验证生成率计算
                Debug.Log($"PipeSpawner: 生成率计算 - baseSpawnRate={baseSpawnRate:F2}, spawnRateMultiplier={levelData.spawnRateMultiplier:F2}, 移动速度={pipeMoveSpeed:F2}, 结果spawnRate={spawnRate:F2}, 管道间距={pipeSpacing:F2}");
                
                // 应用高度偏移
                float oldHeightOffset = heightoffset;
                heightoffset = levelData.heightOffset;
                
                // 调试信息：验证高度偏移是否正确应用
                Debug.Log($"PipeSpawner: 应用关卡设置 - 生成率倍数={levelData.spawnRateMultiplier:F2}, 移动速度倍数={levelData.moveSpeedMultiplier:F2}, 高度偏移: {oldHeightOffset:F2} -> {heightoffset:F2} (LevelData={levelData.heightOffset:F2})");
                
                // 应用道具生成设置（如果关卡启用了关卡道具设置）
                if (levelData.useLevelItemSettings)
                {
                    // 使用关卡的道具设置
                    currentItemSpawnChance = levelData.itemSpawnChance;
                    currentItemSpawnOffsetX = levelData.itemSpawnOffsetX;
                    
                    // 如果关卡有道具列表，使用关卡的道具列表；否则使用默认列表
                    if (levelData.itemTypes != null && levelData.itemTypes.Count > 0)
                    {
                        currentItemTypes = new List<ItemData>(levelData.itemTypes);
                    }
                    else
                    {
                        currentItemTypes = new List<ItemData>(defaultItemTypes);
                    }
                }
                else
                {
                    // 使用默认设置
                    currentItemSpawnChance = defaultItemSpawnChance;
                    currentItemSpawnOffsetX = defaultItemSpawnOffsetX;
                    currentItemTypes = new List<ItemData>(defaultItemTypes);
                }
                
                // 应用怪物生成设置（如果关卡启用了关卡怪物设置）
                if (levelData.useLevelMonsterSettings)
                {
                    // 使用关卡的怪物设置
                    currentMonsterSpawnChance = levelData.monsterSpawnChance;
                    currentMonsterSpawnOffsetX = levelData.monsterSpawnOffsetX;
                    currentMonsterSpawnOffsetY = levelData.monsterSpawnOffsetY;
                    
                    // 如果关卡有怪物列表，使用关卡的怪物列表；否则使用默认列表
                    if (levelData.monsterTypes != null && levelData.monsterTypes.Count > 0)
                    {
                        currentMonsterTypes = new List<MonsterData>(levelData.monsterTypes);
                    }
                    else
                    {
                        currentMonsterTypes = new List<MonsterData>(defaultMonsterTypes);
                    }
                }
                else
                {
                    // 使用默认设置
                    currentMonsterSpawnChance = defaultMonsterSpawnChance;
                    currentMonsterSpawnOffsetX = defaultMonsterSpawnOffsetX;
                    currentMonsterSpawnOffsetY = defaultMonsterSpawnOffsetY;
                    currentMonsterTypes = new List<MonsterData>(defaultMonsterTypes);
                }
                
                // 更新概率预览
                UpdateProbabilityPreview();
            }
            else
            {
                // 如果没有关卡数据，使用默认设置
                currentItemSpawnChance = defaultItemSpawnChance;
                currentItemSpawnOffsetX = defaultItemSpawnOffsetX;
                currentItemTypes = new List<ItemData>(defaultItemTypes);
                UpdateProbabilityPreview();
            }
        }
        else
        {
            // 如果没有LevelManager，使用默认设置
            currentItemSpawnChance = defaultItemSpawnChance;
            currentItemSpawnOffsetX = defaultItemSpawnOffsetX;
            currentItemTypes = new List<ItemData>(defaultItemTypes);
            
            currentMonsterSpawnChance = defaultMonsterSpawnChance;
            currentMonsterSpawnOffsetX = defaultMonsterSpawnOffsetX;
            currentMonsterSpawnOffsetY = defaultMonsterSpawnOffsetY;
            currentMonsterTypes = new List<MonsterData>(defaultMonsterTypes);
            
            UpdateProbabilityPreview();
        }
    }
    
    void OnValidate()
    {
        // 在Inspector中修改值时自动更新概率预览
        UpdateProbabilityPreview();
    }

    private bool isSpawning = true;  // 是否正在生成
    
    // Update is called once per frame
    void Update()
    {
        // 只有在生成状态且游戏中才生成管道
        if (!isSpawning) return;
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying())
        {
            isSpawning = false;
            return;
        }
        
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
    
    /// <summary>
    /// 停止生成管道
    /// </summary>
    public void StopSpawning()
    {
        isSpawning = false;
    }
    
    /// <summary>
    /// 恢复生成管道
    /// </summary>
    public void ResumeSpawning()
    {
        isSpawning = true;
        timer = 0; // 恢复生成时重置计时器
        
        // 立即生成第一个管道，避免关卡开始时屏幕没有管道
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying())
        {
            StartCoroutine(SpawnInitialPipes());
        }
    }
    
    /// <summary>
    /// 生成初始管道，确保屏幕上始终有管道
    /// </summary>
    private System.Collections.IEnumerator SpawnInitialPipes()
    {
        // 只生成第一个管道，让正常的生成逻辑接管后续生成
        // 由于生成率已经根据移动速度自动调整，不会出现断档问题
        spawnPipe();
        
        // 如果生成间隔很大（spawnRate > 2秒），可能需要预生成第二个管道
        // 但使用正常的生成间隔，避免管道堆叠
        if (spawnRate > 2f)
        {
            // 等待正常的生成间隔的一半，然后生成第二个管道
            // 这样可以确保屏幕上至少有一个管道，同时不会堆叠
            yield return new WaitForSeconds(spawnRate * 0.5f);
            spawnPipe();
        }
    }
    
    /// <summary>
    /// 重置计时器（用于新关卡开始时）
    /// </summary>
    public void ResetTimer()
    {
        timer = 0;
    }
    void spawnPipe()//instance方式生成管道，void是执行逻辑，不需要返回结果
    {
        // 计算管道生成位置的Y坐标范围
        // heightoffset 是从中心点向上和向下的偏移距离
        // 例如：如果 centerY = 0, heightoffset = 4，则范围是 [-4, 4]
        float centerY = transform.position.y;
        float lowestPoint = centerY - heightoffset;
        float highestPoint = centerY + heightoffset;
        
        // 生成随机Y坐标（在范围内）
        float pipeY = Random.Range(lowestPoint, highestPoint);
        
        // 调试信息（每次生成都输出，方便排查问题）
        Debug.Log($"PipeSpawner: 生成管道 - 中心Y={centerY:F2}, heightoffset={heightoffset:F2}, 范围=[{lowestPoint:F2}, {highestPoint:F2}], 生成Y={pipeY:F2}");
        
        // 验证生成的Y坐标是否在范围内（安全检查）
        if (pipeY < lowestPoint || pipeY > highestPoint)
        {
            Debug.LogError($"PipeSpawner: 错误！生成的pipeY={pipeY:F2}超出范围[{lowestPoint:F2}, {highestPoint:F2}]，强制限制到范围内");
            pipeY = Mathf.Clamp(pipeY, lowestPoint, highestPoint);
        }
        
        // 生成管道
        GameObject spawnedPipe = Instantiate(pipe, new Vector3(transform.position.x, pipeY, 0), transform.rotation);
        
        // 验证生成的管道位置
        if (spawnedPipe != null)
        {
            float actualPipeY = spawnedPipe.transform.position.y;
            if (Mathf.Abs(actualPipeY - pipeY) > 0.01f)
            {
                Debug.LogWarning($"PipeSpawner: 警告！期望Y={pipeY:F2}，实际Y={actualPipeY:F2}，可能存在位置设置问题");
            }
            
            // 验证是否在范围内
            if (actualPipeY < lowestPoint || actualPipeY > highestPoint)
            {
                Debug.LogError($"PipeSpawner: 错误！生成的管道Y坐标 {actualPipeY:F2} 超出范围 [{lowestPoint:F2}, {highestPoint:F2}]");
            }
        }
        
        // 根据概率生成道具（使用当前设置）
        if (currentItemTypes.Count > 0 && Random.Range(0f, 1f) < currentItemSpawnChance)
        {
            // 随机选择一个道具类型（基于权重）
            ItemData selectedItem = GetRandomItemByWeight();
            if (selectedItem != null && selectedItem.itemPrefab != null)
            {
                // 在管道中间位置生成道具（Y坐标与管道相同，X坐标稍微偏移）
                float itemX = transform.position.x + currentItemSpawnOffsetX;
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
        
        // 根据概率生成怪物（使用当前设置）
        if (currentMonsterTypes.Count > 0 && Random.Range(0f, 1f) < currentMonsterSpawnChance)
        {
            // 随机选择一个怪物类型（基于权重）
            MonsterData selectedMonster = GetRandomMonsterByWeight();
            if (selectedMonster != null && selectedMonster.monsterPrefab != null)
            {
                // 在管道附近位置生成怪物（X和Y坐标都可以偏移）
                float monsterX = transform.position.x + currentMonsterSpawnOffsetX;
                float monsterY = pipeY + currentMonsterSpawnOffsetY;
                GameObject spawnedMonster = Instantiate(selectedMonster.monsterPrefab, new Vector3(monsterX, monsterY, 0), transform.rotation);
                
                // 设置怪物的数据
                MonsterScript monsterScript = spawnedMonster.GetComponent<MonsterScript>();
                if (monsterScript != null)
                {
                    monsterScript.monsterData = selectedMonster;
                    monsterScript.maxHP = selectedMonster.maxHP;
                    monsterScript.currentHP = selectedMonster.maxHP;  // 重要：同步设置当前血量为最大血量
                    monsterScript.dropCoins = selectedMonster.dropCoins;
                    monsterScript.followPipeMovement = selectedMonster.followPipeMovement;
                    
                    // 设置基础移动速度（用于后续计算）
                    if (selectedMonster.followPipeMovement)
                    {
                        // 跟随管道：从 PipeMoveScript 获取基础速度
                        PipeMoveScript pipeMover = FindObjectOfType<PipeMoveScript>();
                        if (pipeMover != null)
                        {
                            monsterScript.baseMoveSpeed = pipeMover.baseMoveSpeed;
                        }
                    }
                    else
                    {
                        // 不跟随管道：使用自定义速度
                        monsterScript.moveSpeed = selectedMonster.customMoveSpeed;
                        monsterScript.baseMoveSpeed = selectedMonster.customMoveSpeed;
                    }
                    
                    // 应用关卡设置（确保移动速度正确）
                    // 注意：这里调用 ApplyLevelSettings() 会在 Start() 之前执行
                    // 但 Start() 中也会调用一次，所以这里先设置基础值
                    monsterScript.ApplyLevelSettings();
                    
                    Debug.Log($"PipeSpawner: 生成怪物 - maxHP={monsterScript.maxHP}, currentHP={monsterScript.currentHP}, followPipeMovement={monsterScript.followPipeMovement}, moveSpeed={monsterScript.moveSpeed:F2}, 来自MonsterData: {selectedMonster.monsterName}");
                }
            }
        }
    }
    
    /// <summary>
    /// 根据权重随机选择一个道具类型
    /// </summary>
    private ItemData GetRandomItemByWeight()
    {
        if (currentItemTypes == null || currentItemTypes.Count == 0) return null;
        
        // 计算总权重
        float totalWeight = 0f;
        foreach (ItemData item in currentItemTypes)
        {
            if (item != null && item.itemPrefab != null)
            {
                totalWeight += item.spawnWeight;
            }
        }
        
        if (totalWeight <= 0f) return null;
        
        // 随机一个0到总权重之间的值
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        // 遍历道具列表，找到对应的道具
        foreach (ItemData item in currentItemTypes)
        {
            if (item == null || item.itemPrefab == null) continue;
            
            currentWeight += item.spawnWeight;
            if (randomValue <= currentWeight)
            {
                return item;
            }
        }
        
        // 如果没找到（理论上不会发生），返回第一个有效道具
        foreach (ItemData item in currentItemTypes)
        {
            if (item != null && item.itemPrefab != null)
            {
                return item;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 根据权重随机选择一个怪物类型
    /// </summary>
    private MonsterData GetRandomMonsterByWeight()
    {
        if (currentMonsterTypes == null || currentMonsterTypes.Count == 0) return null;
        
        // 计算总权重
        float totalWeight = 0f;
        foreach (MonsterData monster in currentMonsterTypes)
        {
            if (monster != null && monster.monsterPrefab != null)
            {
                totalWeight += monster.spawnWeight;
            }
        }
        
        if (totalWeight <= 0f) return null;
        
        // 随机一个0到总权重之间的值
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        // 遍历怪物列表，找到对应的怪物
        foreach (MonsterData monster in currentMonsterTypes)
        {
            if (monster == null || monster.monsterPrefab == null) continue;
            
            currentWeight += monster.spawnWeight;
            if (randomValue <= currentWeight)
            {
                return monster;
            }
        }
        
        // 如果没找到（理论上不会发生），返回第一个有效怪物
        foreach (MonsterData monster in currentMonsterTypes)
        {
            if (monster != null && monster.monsterPrefab != null)
            {
                return monster;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 更新概率预览信息（用于Inspector显示）
    /// </summary>
    private void UpdateProbabilityPreview()
    {
        // 使用当前设置（可能是关卡设置或默认设置）
        List<ItemData> itemsToPreview = currentItemTypes;
        float chanceToPreview = currentItemSpawnChance;
        
        if (itemsToPreview == null || itemsToPreview.Count == 0)
        {
            probabilityPreview = "暂无道具配置（使用默认或关卡设置）";
            return;
        }
        
        // 计算总权重
        float totalWeight = 0f;
        int validItemCount = 0;
        foreach (ItemData item in itemsToPreview)
        {
            if (item != null && item.itemPrefab != null)
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
        sb.AppendLine($"道具生成概率: {chanceToPreview * 100:F1}%");
        sb.AppendLine($"有效道具数: {validItemCount}");
        sb.AppendLine($"总权重: {totalWeight:F2}");
        sb.AppendLine("---");
        
        foreach (ItemData item in itemsToPreview)
        {
            if (item == null || item.itemPrefab == null) continue;
            
            float relativeProbability = item.spawnWeight / totalWeight;
            float actualProbability = relativeProbability * chanceToPreview;
            string itemName = string.IsNullOrEmpty(item.itemName) ? item.itemPrefab.name : item.itemName;
            
            sb.AppendLine($"{itemName}:");
            sb.AppendLine($"  权重: {item.spawnWeight:F2} → 相对概率: {relativeProbability * 100:F1}%");
            sb.AppendLine($"  实际概率: {actualProbability * 100:F1}% (分数: +{item.scoreValue})");
        }
        
        probabilityPreview = sb.ToString();
    }
}
