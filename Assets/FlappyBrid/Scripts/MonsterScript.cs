using UnityEngine;

/// <summary>
/// 怪物脚本
/// 控制怪物的血量、移动、死亡和掉落金币
/// </summary>
public class MonsterScript : MonoBehaviour
{
    [Header("怪物数据")]
    [Tooltip("怪物数据配置（如果为空，使用默认值）")]
    public MonsterData monsterData;
    
    [Header("血量设置")]
    [Tooltip("当前血量")]
    public int currentHP;
    
    [Tooltip("最大血量")]
    public int maxHP = 10;
    
    [Header("掉落设置")]
    [Tooltip("死亡后掉落的金币数量")]
    public int dropCoins = 5;
    
    [Header("分数显示设置")]
    [Tooltip("分数弹跳显示预制体（如果为空，会尝试从ItemScript或logicManager获取）")]
    public GameObject scorePopupPrefab; // 分数弹跳显示预制体（可选）
    
    [Tooltip("分数显示位置偏移")]
    public Vector3 popupOffset = new Vector3(0, 1, 0); // 分数显示位置偏移
    
    [Tooltip("分数显示颜色（如果为空则使用ScorePopup预制体的默认颜色）")]
    public Color scoreColor = Color.yellow; // 分数显示颜色（默认黄色，区别于道具的白色）
    
    [Header("移动设置")]
    [Tooltip("是否跟随管道移动")]
    public bool followPipeMovement = true;
    
    [Tooltip("移动速度（仅在followPipeMovement=false时使用）")]
    public float moveSpeed = 5f;
    
    [Tooltip("销毁区域（X坐标小于此值时销毁）")]
    public float deadZone = -35f;
    
    [Header("基础设置（会被关卡设置覆盖）")]
    [Tooltip("基础移动速度")]
    public float baseMoveSpeed = 5f;
    
    private bool isMoving = true;      // 是否正在移动
    private bool isDead = false;       // 是否已死亡
    
    void Start()
    {
        // 初始化血量
        // 注意：PipeSpawner 会在 Instantiate 后立即设置 maxHP 和 currentHP
        // 如果 PipeSpawner 已经设置过，这里不会覆盖；否则从 monsterData 读取
        if (monsterData != null)
        {
            // 如果 maxHP 还是默认值（10），说明 PipeSpawner 没有设置，从 monsterData 读取
            if (maxHP == 10)
            {
                maxHP = monsterData.maxHP;
            }
            
            // 如果 currentHP 为 0 或未初始化，设置为 maxHP
            if (currentHP == 0)
            {
                currentHP = maxHP;
            }
            
            dropCoins = monsterData.dropCoins;
            followPipeMovement = monsterData.followPipeMovement;
            if (!followPipeMovement)
            {
                moveSpeed = monsterData.customMoveSpeed;
            }
        }
        else
        {
            // 如果没有 monsterData，确保 currentHP 被正确初始化
            if (currentHP == 0)
            {
                currentHP = maxHP;
            }
        }
        
        baseMoveSpeed = moveSpeed;
        
        // 应用关卡设置
        ApplyLevelSettings();
        
        Debug.Log($"MonsterScript.Start: 怪物初始化完成 - maxHP={maxHP}, currentHP={currentHP}, monsterData={monsterData?.monsterName ?? "null"}");
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
                if (followPipeMovement)
                {
                    // 跟随管道移动：尝试从 PipeMoveScript 获取当前移动速度
                    PipeMoveScript pipeMover = FindObjectOfType<PipeMoveScript>();
                    if (pipeMover != null)
                    {
                        // 获取管道当前的实际移动速度（已经应用了关卡倍数）
                        moveSpeed = pipeMover.GetMoveSpeed();
                        Debug.Log($"MonsterScript: 跟随管道移动，从PipeMoveScript获取速度: {moveSpeed:F2}");
                    }
                    else
                    {
                        // 如果没有找到管道，使用基础速度乘以关卡倍数
                        moveSpeed = baseMoveSpeed * levelData.moveSpeedMultiplier;
                        Debug.Log($"MonsterScript: 跟随管道移动，但未找到PipeMoveScript，使用计算速度: {moveSpeed:F2} (base={baseMoveSpeed:F2}, multiplier={levelData.moveSpeedMultiplier:F2})");
                    }
                }
                else
                {
                    // 不跟随管道：如果设置了自定义速度，则不应用关卡倍数
                    // 否则使用基础速度乘以关卡倍数
                    if (monsterData != null && monsterData.customMoveSpeed > 0)
                    {
                        moveSpeed = monsterData.customMoveSpeed;
                        Debug.Log($"MonsterScript: 使用自定义速度: {moveSpeed:F2}");
                    }
                    else
                    {
                        moveSpeed = baseMoveSpeed * levelData.moveSpeedMultiplier;
                        Debug.Log($"MonsterScript: 不跟随管道，使用计算速度: {moveSpeed:F2} (base={baseMoveSpeed:F2}, multiplier={levelData.moveSpeedMultiplier:F2})");
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("MonsterScript: LevelManager.Instance 为 null，无法应用关卡设置！");
        }
    }
    
    void Update()
    {
        // 如果已死亡，不移动
        if (isDead) return;
        
        // 只有在移动状态且游戏进行中时才移动
        if (!isMoving) return;
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying())
        {
            isMoving = false;
            return;
        }
        
        // 根据是否跟随管道移动来决定移动方式
        if (followPipeMovement)
        {
            // 跟随管道移动：使用管道的移动速度
            // 从 PipeMoveScript 获取当前移动速度（已经应用了关卡倍数）
            PipeMoveScript pipeMover = FindObjectOfType<PipeMoveScript>();
            if (pipeMover != null)
            {
                float currentPipeSpeed = pipeMover.GetMoveSpeed();
                transform.position = transform.position + (Vector3.left * currentPipeSpeed) * Time.deltaTime;
            }
            else
            {
                // 如果找不到管道，使用当前设置的 moveSpeed（应该已经应用了关卡倍数）
                transform.position = transform.position + (Vector3.left * moveSpeed) * Time.deltaTime;
            }
        }
        else
        {
            // 不跟随管道：使用自定义速度
            transform.position = transform.position + (Vector3.left * moveSpeed) * Time.deltaTime;
        }
        
        // 检查是否超出销毁区域
        if (transform.position.x < deadZone)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(int damage)
    {
        if (isDead) 
        {
            Debug.LogWarning($"MonsterScript: 尝试对已死亡的怪物造成伤害！怪物: {gameObject.name}");
            return;
        }
        
        int oldHP = currentHP;
        currentHP -= damage;
        
        Debug.Log($"MonsterScript: 怪物受到 {damage} 点伤害！怪物: {gameObject.name}, 血量变化: {oldHP}/{maxHP} -> {currentHP}/{maxHP}");
        
        // 检查是否死亡
        if (currentHP <= 0)
        {
            Debug.Log($"MonsterScript: 怪物血量归零，触发死亡！怪物: {gameObject.name}");
            Die();
        }
    }
    
    /// <summary>
    /// 死亡
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        isMoving = false;
        
        Debug.Log($"MonsterScript: 怪物死亡，掉落 {dropCoins} 金币");
        
        // 显示分数弹跳效果（在掉落金币之前显示，这样可以看到金币数量）
        ShowScorePopup();
        
        // 掉落金币
        DropCoins();
        
        // 销毁怪物（可以添加死亡动画或特效）
        Destroy(gameObject);
    }
    
    /// <summary>
    /// 掉落金币
    /// </summary>
    private void DropCoins()
    {
        if (dropCoins <= 0) return;
        
        // 通知 logicManager 增加金币
        logicManager logic = FindObjectOfType<logicManager>();
        if (logic != null)
        {
            logic.addCoins(dropCoins);
            Debug.Log($"MonsterScript: 掉落 {dropCoins} 金币，已通知 logicManager");
        }
        else
        {
            Debug.LogWarning("MonsterScript: 未找到 logicManager，无法掉落金币！");
        }
    }
    
    /// <summary>
    /// 停止移动
    /// </summary>
    public void StopMoving()
    {
        isMoving = false;
    }
    
    /// <summary>
    /// 恢复移动
    /// </summary>
    public void ResumeMoving()
    {
        if (!isDead)
        {
            isMoving = true;
        }
    }
    
    /// <summary>
    /// 获取当前血量
    /// </summary>
    public int GetCurrentHP()
    {
        return currentHP;
    }
    
    /// <summary>
    /// 获取最大血量
    /// </summary>
    public int GetMaxHP()
    {
        return maxHP;
    }
    
    /// <summary>
    /// 显示分数弹跳效果
    /// </summary>
    private void ShowScorePopup()
    {
        // 如果没有设置预制体，尝试从其他地方获取
        if (scorePopupPrefab == null)
        {
            // 尝试从 ItemScript 获取（如果场景中有道具）
            ItemScript itemScript = FindObjectOfType<ItemScript>();
            if (itemScript != null && itemScript.scorePopupPrefab != null)
            {
                scorePopupPrefab = itemScript.scorePopupPrefab;
            }
            else
            {
                // 尝试从 logicManager 获取（如果有的话）
                logicManager logic = FindObjectOfType<logicManager>();
                if (logic != null)
                {
                    // 可以通过反射或其他方式获取，这里先尝试直接查找
                    // 如果 logicManager 有 scorePopupPrefab 字段，可以在这里获取
                }
            }
        }
        
        if (scorePopupPrefab != null)
        {
            // 查找Canvas（优先查找世界空间的Canvas）
            Canvas canvas = FindCanvas();
            if (canvas == null)
            {
                Debug.LogWarning("MonsterScript: 未找到Canvas，分数弹跳可能无法正确显示！");
            }
            
            // 计算显示位置（怪物位置 + 偏移）
            Vector3 popupPosition = transform.position + popupOffset;
            
            // 实例化分数显示
            GameObject popup;
            if (canvas != null)
            {
                // 如果找到Canvas，将其作为父对象
                popup = Instantiate(scorePopupPrefab, canvas.transform);
            }
            else
            {
                // 如果没有Canvas，在世界空间创建
                popup = Instantiate(scorePopupPrefab, popupPosition, Quaternion.identity);
            }
            
            // 设置分数值
            ScorePopup scorePopup = popup.GetComponent<ScorePopup>();
            if (scorePopup != null)
            {
                scorePopup.SetScore(dropCoins);
                scorePopup.SetPosition(popupPosition);
                // 设置分数颜色
                scorePopup.SetColor(scoreColor);
            }
            else
            {
                // 如果没有ScorePopup组件，尝试直接设置Text组件
                UnityEngine.UI.Text text = popup.GetComponent<UnityEngine.UI.Text>();
                if (text != null)
                {
                    text.text = "+" + dropCoins.ToString();
                }
            }
        }
        else
        {
            Debug.LogWarning("MonsterScript: 未设置ScorePopup预制体，无法显示分数弹跳效果！请设置 scorePopupPrefab 或确保场景中有 ItemScript 组件。");
        }
    }
    
    /// <summary>
    /// 查找场景中的Canvas
    /// </summary>
    private Canvas FindCanvas()
    {
        // 优先查找世界空间的Canvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                return canvas;
            }
        }
        
        // 如果没有世界空间Canvas，返回第一个找到的Canvas
        if (canvases.Length > 0)
        {
            return canvases[0];
        }
        
        return null;
    }
}

