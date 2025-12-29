using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class BirdScript : MonoBehaviour
{
    public Rigidbody2D myRigidbody;
    public float flapStrength = 5;
    public logicManager logic;
    public bool birdIsAlive = true;
    private PlayerInputAction inputActions;
    
    [Header("血量系统")]
    [Tooltip("小鸟最大血量（可升级）")]
    public int maxHP = 1;
    
    [Tooltip("小鸟当前血量（运行时）")]
    [SerializeField]
    private int currentHP = 1;
    
    [Header("翅膀设置")]
    public GameObject wingUp; // 向上飞的翅膀（跳跃时显示）
    public GameObject wingDown; // 向下掉的翅膀（掉落时显示）
    public float velocityThreshold = 0.1f; // 速度阈值，用于判断向上还是向下
    
    [Header("初始位置")]
    [Tooltip("小鸟的初始位置（游戏开始时重置到这里）")]
    public Vector3 initialPosition = Vector3.zero;
    
    [Header("游戏开始设置")]
    [Tooltip("游戏开始后延迟多少秒开始掉落")]
    [Range(0.1f, 3f)]
    public float startDelay = 1f;  // 延迟时间（秒）
    
    [Header("拖尾效果设置（水平方向）")]
    [Tooltip("拖尾渲染器组件（LineRenderer，用于水平拖尾）")]
    public LineRenderer trailRenderer;
    [Tooltip("最小拖尾长度（总分数为0时，单位：世界单位）")]
    [Range(0f, 50f)]
    public float minTrailLength = 2f; // 增加默认值
    [Tooltip("最大拖尾长度（达到最大分数时，单位：世界单位）")]
    [Range(1f, 100f)]
    public float maxTrailLength = 20f; // 增加默认值和范围
    [Tooltip("达到最大拖尾长度所需的总分数")]
    [Range(10, 1000)]
    public int maxTrailScore = 500;
    [Tooltip("拖尾宽度（起始点宽度）")]
    [Range(0.01f, 10f)]
    public float trailWidth = 0.5f; // 起始点宽度，最大10
    [Tooltip("拖尾结束点宽度比例（相对于起始宽度，0=完全消失，1=与起始相同）")]
    [Range(0f, 1f)]
    public float trailEndWidthRatio = 0.1f; // 结束点宽度比例，默认10%
    [Tooltip("拖尾分段数（越多越平滑，但性能消耗越大）")]
    [Range(10, 100)]
    public int trailSegments = 30;
    [Tooltip("拖尾位置偏移（相对于小鸟位置）")]
    public Vector2 trailOffset = Vector2.zero;
    [Tooltip("拖尾平滑度（0=不平滑，1=完全平滑，值越大越平滑）")]
    [Range(0f, 1f)]
    public float trailSmoothness = 0.5f;
    [Tooltip("拖尾渲染排序顺序（数值越小越靠后，建议-2或更低，确保在背景后面）")]
    [Range(-10, 10)]
    public int trailSortingOrder = -2;
    [Tooltip("拖尾颜色（从起始到结束的渐变）")]
    public Gradient trailColorGradient;
    
    [Header("子弹发射设置")]
    [Tooltip("子弹预制体")]
    public GameObject bulletPrefab;
    
    [Tooltip("发射间隔（秒，值越小发射越快）")]
    [Range(0.1f, 5f)]
    public float fireInterval = 1f;
    
    [Tooltip("子弹发射位置偏移（相对于小鸟位置）")]
    public Vector2 fireOffset = new Vector2(0.5f, 0f);
    
    [Tooltip("子弹伤害值（可升级）")]
    public int bulletDamage = 1;
    
    [Tooltip("子弹速度")]
    public float bulletSpeed = 10f;
    
    [Tooltip("同时发射的子弹数量（可升级）")]
    [Range(1, 5)]
    public int bulletCount = 1;
    
    [Tooltip("多发子弹时的Y轴间距（仅在bulletCount>1时有效）")]
    public float bulletSpacing = 0.5f;
    
    [Header("攻击范围设置")]
    [Tooltip("自动攻击范围（圆形半径，怪物在此范围内才会自动发射）")]
    [Range(1f, 50f)]
    public float attackRange = 10f;
    
    [Tooltip("攻击范围检测图层（用于检测怪物）")]
    public LayerMask monsterLayer;
    
    [Tooltip("是否显示攻击范围（调试用）")]
    public bool showAttackRange = false;
    
    [Header("调试设置")]
    [Tooltip("启用调试可视化（在Scene视图中显示拖尾预览）")]
    public bool enableDebugVisualization = false; // 默认关闭调试模式
    [Tooltip("调试模式下的测试总分数（用于预览拖尾长度）")]
    [Range(0, 1000)]
    public int debugTotalScore = 200;
    [Tooltip("调试模式下的测试Y坐标变化（模拟小鸟跳跃轨迹）")]
    public AnimationCurve debugYCurve = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    
    // 用于记录小鸟在不同时间点的Y坐标（模拟水平拖尾）
    private struct TrailPoint
    {
        public float time;
        public float y;
    }
    private List<TrailPoint> trailHistory = new List<TrailPoint>(); // 拖尾历史记录
    private Coroutine startGameCoroutine;  // 游戏开始协程
    private Coroutine fireCoroutine;       // 发射子弹协程
    private float fireTimer = 0f;          // 发射计时器

    void Awake()
    {
        inputActions = new PlayerInputAction();
    }
    void OnEnable()
    {
        // 【新增】启用 Player Action Map
        inputActions.Player.Enable();

        // 【新增】订阅 Jump 事件
        inputActions.Player.Jump.performed += OnJump;
    }

    void OnDisable()
    {
        // 【新增】取消订阅（非常重要）
        inputActions.Player.Jump.performed -= OnJump;
        inputActions.Player.Disable();
    }

    void Start()
    {
        logic = GameObject.FindGameObjectWithTag("Logic").GetComponent<logicManager>();//通过tag来自定将逻辑管理对象填入到输入中

        // 如果没有手动指定翅膀，尝试自动获取子物体（假设前两个子物体是翅膀）
        if (wingUp == null || wingDown == null)
        {
            if (transform.childCount >= 2)
            {
                if (wingUp == null) wingUp = transform.GetChild(0).gameObject;
                if (wingDown == null) wingDown = transform.GetChild(1).gameObject;
            }
        }
        
        // 初始化：默认显示向上翅膀，隐藏向下翅膀
        if (wingUp != null) wingUp.SetActive(true);
        if (wingDown != null) wingDown.SetActive(false);
        
        // 保存初始位置
        if (initialPosition == Vector3.zero)
        {
            initialPosition = transform.position;
        }
        
        // 订阅游戏状态变化
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
        }
        
        // 初始化：在菜单状态下冻结小鸟
        InitializeBird();
        
        // 初始化拖尾效果
        InitializeTrail();
    }
    
    void OnDestroy()
    {
        // 取消订阅
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
        }
    }
    
    /// <summary>
    /// 初始化小鸟状态
    /// </summary>
    private void InitializeBird()
    {
        if (myRigidbody != null)
        {
            // 重置位置
            transform.position = initialPosition;
            
            // 重置速度和旋转
            myRigidbody.velocity = Vector2.zero;
            myRigidbody.angularVelocity = 0f;
            
            // 根据游戏状态设置物理状态
            if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying())
            {
                // 菜单状态下冻结物理
                myRigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
            }
            else
            {
                // 游戏中只冻结旋转
                myRigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
        }
        
        birdIsAlive = true;
    }
    
    /// <summary>
    /// 游戏状态变化回调
    /// </summary>
    private void OnGameStateChanged(GameState newState)
    {
        Debug.Log($"BirdScript: 游戏状态变化 -> {newState}");
        
        // 停止之前的协程
        if (startGameCoroutine != null)
        {
            Debug.Log("BirdScript: 停止之前的协程");
            StopCoroutine(startGameCoroutine);
            startGameCoroutine = null;
        }
        
        switch (newState)
        {
            case GameState.Menu:
                // 菜单状态：冻结小鸟，重置位置，禁用拖尾
                ResetBirdToInitial();
                if (myRigidbody != null)
                {
                    myRigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
                }
                // 菜单状态：清除拖尾
                ClearTrail();
                break;
                
            case GameState.Playing:
                // 游戏开始：延迟1秒后解冻小鸟，开始掉落，启用拖尾
                Debug.Log($"BirdScript: 游戏开始，准备延迟 {startDelay} 秒");
                
                // 应用升级属性（确保属性在关卡间保持）
                if (BirdUpgradeManager.Instance != null)
                {
                    BirdUpgradeManager.Instance.ApplyAllUpgrades();
                }
                
                if (trailRenderer != null)
                {
                    trailRenderer.enabled = true;
                    Debug.Log("BirdScript: 拖尾已启用");
                }
                else
                {
                    Debug.LogWarning("BirdScript: trailRenderer 为 null，尝试重新初始化");
                    InitializeTrail();
                }
                StartGameWithDelay();
                // 开始自动发射子弹
                StartFiring();
                break;
                
            case GameState.LevelComplete:
            case GameState.GameOver:
                // 游戏结束：冻结小鸟，保持拖尾显示，停止发射子弹
                if (myRigidbody != null)
                {
                    myRigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
                }
                StopFiring();
                break;
        }
    }
    
    /// <summary>
    /// 延迟开始游戏（1秒后解冻物理）
    /// </summary>
    private void StartGameWithDelay()
    {
        // 如果已经有协程在运行，先停止它
        if (startGameCoroutine != null)
        {
            StopCoroutine(startGameCoroutine);
            startGameCoroutine = null;
        }
        
        Debug.Log($"BirdScript: 开始延迟协程，延迟时间: {startDelay}秒");
        startGameCoroutine = StartCoroutine(StartGameWithDelayCoroutine());
    }
    
    /// <summary>
    /// 游戏开始协程
    /// </summary>
    private System.Collections.IEnumerator StartGameWithDelayCoroutine()
    {
        Debug.Log($"BirdScript: 协程开始执行，时间: {Time.realtimeSinceStartup}");
        
        // 重置小鸟位置和状态
        ResetBirdToInitial();
        birdIsAlive = true;
        
        // 确保小鸟被冻结
        if (myRigidbody != null)
        {
            myRigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
            myRigidbody.velocity = Vector2.zero;
            myRigidbody.angularVelocity = 0f;
            Debug.Log("BirdScript: 小鸟已冻结");
        }
        
        // 记录开始时间（用于调试）
        float startTime = Time.realtimeSinceStartup;
        Debug.Log($"BirdScript: 开始等待，当前时间: {startTime}, 等待时长: {startDelay}秒");
        
        // 使用WaitForSecondsRealtime，不受Time.timeScale影响
        yield return new WaitForSecondsRealtime(startDelay);
        
        // 记录实际延迟时间（用于调试）
        float actualDelay = Time.realtimeSinceStartup - startTime;
        Debug.Log($"BirdScript: 等待完成，实际延迟: {actualDelay}秒 (预期: {startDelay}秒)");
        
        if (actualDelay > startDelay * 1.5f)  // 如果延迟超过预期的1.5倍，记录警告
        {
            Debug.LogWarning($"BirdScript: 延迟时间异常！预期: {startDelay}秒, 实际: {actualDelay}秒");
        }
        
        // 延迟后解冻物理，开始掉落
        if (myRigidbody != null)
        {
            if (GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying())
            {
                // 确保重力启用
                if (myRigidbody.gravityScale <= 0)
                {
                    Debug.LogWarning($"BirdScript: 重力为 {myRigidbody.gravityScale}，设置为3（默认值）");
                    myRigidbody.gravityScale = 3f;  // 设置默认重力值
                }
                
                // 确保Rigidbody是激活的
                if (!myRigidbody.simulated)
                {
                    myRigidbody.simulated = true;
                    Debug.Log("BirdScript: 已启用Rigidbody2D的simulated");
                }
                
                // 解冻物理约束（必须在最后执行，确保其他设置已生效）
                myRigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
                myRigidbody.velocity = Vector2.zero;  // 初始速度为0，让重力自然作用
                
                // 强制唤醒Rigidbody（确保物理立即生效）
                myRigidbody.WakeUp();
                
                Debug.Log($"BirdScript: 小鸟已解冻，开始掉落 (重力: {myRigidbody.gravityScale}, 速度: {myRigidbody.velocity}, 位置: {transform.position})");
            }
            else
            {
                Debug.LogWarning("BirdScript: 游戏状态不是Playing，不解冻小鸟");
            }
        }
        else
        {
            Debug.LogError("BirdScript: myRigidbody为null！");
        }
        
        startGameCoroutine = null;  // 清除协程引用
    }
    
    /// <summary>
    /// 重置小鸟到初始位置
    /// </summary>
    private void ResetBirdToInitial()
    {
        if (myRigidbody != null)
        {
            transform.position = initialPosition;
            myRigidbody.velocity = Vector2.zero;
            myRigidbody.angularVelocity = 0f;
        }
        else
        {
            transform.position = initialPosition;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 只有在游戏中才检查掉落
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying())
        {
        if (transform.position.y < -12)
        {
            logic.gameOver();
            }
        }

        // 根据垂直速度切换翅膀显示
        UpdateWingDisplay();
        
        // 更新水平拖尾
        UpdateHorizontalTrail();
        
        // 确保拖尾宽度始终是最新的（如果Inspector中修改了参数）
        if (trailRenderer != null && trailRenderer.enabled)
        {
            trailRenderer.startWidth = trailWidth;
            trailRenderer.endWidth = trailWidth * trailEndWidthRatio;
        }
        
        // 更新子弹发射（如果游戏进行中）
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPlaying() && bulletPrefab != null)
        {
            UpdateFiring();
        }
    }
    
    private void UpdateWingDisplay()
    {
        if (!birdIsAlive || wingUp == null || wingDown == null) return;
        
        // 获取垂直速度
        float verticalVelocity = myRigidbody.velocity.y;
        
        // 根据速度方向切换翅膀
        if (verticalVelocity > velocityThreshold)
        {
            // 向上飞，显示向上翅膀
            wingUp.SetActive(true);
            wingDown.SetActive(false);
        }
        else if (verticalVelocity < -velocityThreshold)
        {
            // 向下掉，显示向下翅膀
            wingUp.SetActive(false);
            wingDown.SetActive(true);
        }
        // 如果速度接近0，保持当前状态（避免频繁切换）
    }
    
    /// <summary>
    /// 初始化拖尾效果（水平方向）
    /// </summary>
    private void InitializeTrail()
    {
        // 如果没有指定 LineRenderer，尝试获取或添加
        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<LineRenderer>();
            if (trailRenderer == null)
            {
                // 如果不存在，添加 LineRenderer 组件
                trailRenderer = gameObject.AddComponent<LineRenderer>();
            }
        }
        
        if (trailRenderer != null)
        {
            // 设置 LineRenderer 的基本属性
            trailRenderer.useWorldSpace = true;
            trailRenderer.startWidth = trailWidth;
            trailRenderer.endWidth = trailWidth * trailEndWidthRatio;
            trailRenderer.positionCount = 0;
            trailRenderer.sortingOrder = trailSortingOrder; // 使用可配置的排序顺序
            trailRenderer.sortingLayerName = "Default"; // 设置排序层
            
            // 设置材质（使用更通用的Shader）
            Material trailMaterial = new Material(Shader.Find("Sprites/Default"));
            if (trailMaterial.shader.name == "Hidden/InternalErrorShader")
            {
                // 如果找不到Shader，使用默认材质
                trailMaterial = new Material(Shader.Find("Unlit/Color"));
            }
            // 设置为白色，让渐变色完全控制颜色
            trailMaterial.color = Color.white; // 白色，不干扰渐变色
            trailRenderer.sharedMaterial = trailMaterial; // 使用sharedMaterial避免材质泄漏
            
            // 设置颜色渐变
            if (trailColorGradient == null || trailColorGradient.alphaKeys.Length == 0)
            {
                trailColorGradient = CreateDefaultGradient();
            }
            trailRenderer.colorGradient = trailColorGradient;
            
            // 确保拖尾可见
            trailRenderer.enabled = true;
            
            Debug.Log($"BirdScript: 拖尾初始化完成 - 宽度: {trailWidth}, 分段数: {trailSegments}");
        }
        
        // 初始化拖尾历史记录
        trailHistory.Clear();
    }
    
    /// <summary>
    /// 创建默认的拖尾颜色渐变
    /// </summary>
    private Gradient CreateDefaultGradient()
    {
        Gradient gradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[2];
        // 使用白色渐变，让用户自定义的渐变色生效
        colorKeys[0] = new GradientColorKey(Color.white, 0f); // 起始：白色
        colorKeys[1] = new GradientColorKey(Color.white, 1f); // 结束：白色
        
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1f, 0f); // 起始完全不透明
        alphaKeys[1] = new GradientAlphaKey(0.3f, 1f); // 结束半透明
        
        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }
    
    /// <summary>
    /// 更新水平拖尾（每帧调用）- 模拟小鸟在水平方向上的移动轨迹
    /// </summary>
    private void UpdateHorizontalTrail()
    {
        if (trailRenderer == null)
        {
            Debug.LogWarning("BirdScript: trailRenderer 为 null！");
            return;
        }
        
        if (!GameStateManager.Instance.IsPlaying())
        {
            trailRenderer.positionCount = 0;
            return;
        }
        
        // 确保拖尾启用
        if (!trailRenderer.enabled)
        {
            trailRenderer.enabled = true;
        }
        
        // 获取当前小鸟位置和时间
        float currentY = transform.position.y;
        float currentTime = Time.time;
        
        // 获取管道移动速度（用于计算相对移动）
        float pipeSpeed = GetPipeSpeed();
        
        // 添加当前时间点的Y坐标到历史记录
        TrailPoint newPoint = new TrailPoint
        {
            time = currentTime,
            y = currentY
        };
        trailHistory.Add(newPoint);
        
        // 获取当前拖尾长度（根据总分数计算）
        float currentTrailLength = GetCurrentTrailLength();
        
        // 计算拖尾应该显示的时间范围（从当前时间往前推）
        float trailTimeRange = currentTrailLength / pipeSpeed; // 根据长度和速度计算时间范围
        float oldestTime = currentTime - trailTimeRange;
        
        // 移除超出时间范围的旧记录
        trailHistory.RemoveAll(point => point.time < oldestTime);
        
        // 如果历史记录为空或只有一个点，直接返回
        if (trailHistory.Count < 2)
        {
            trailRenderer.positionCount = 0;
            // 调试信息：每60帧输出一次
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"BirdScript: 拖尾历史记录不足 ({trailHistory.Count} 个点)，需要至少2个点");
            }
            return;
        }
        
        // 更新拖尾显示
        UpdateHorizontalTrailLine(currentTime, pipeSpeed, currentTrailLength);
    }
    
    /// <summary>
    /// 获取管道移动速度（用于模拟相对移动）
    /// </summary>
    private float GetPipeSpeed()
    {
        // 查找管道移动脚本获取速度
        PipeMoveScript pipeMover = FindObjectOfType<PipeMoveScript>();
        if (pipeMover != null)
        {
            return pipeMover.GetMoveSpeed();
        }
        return 5f; // 默认速度
    }
    
    /// <summary>
    /// 更新水平拖尾线段
    /// </summary>
    private void UpdateHorizontalTrailLine(float currentTime, float pipeSpeed, float trailLength)
    {
        if (trailRenderer == null || trailHistory.Count < 2)
        {
            if (trailRenderer == null)
            {
                Debug.LogError("BirdScript: trailRenderer 为 null！");
            }
            return;
        }
        
        // 获取当前小鸟的位置
        Vector3 birdPosition = transform.position;
        float currentX = birdPosition.x;
        float currentY = birdPosition.y;
        
        // 计算拖尾起始X位置（向左延伸）
        float startX = currentX - trailLength;
        
        // 生成拖尾点
        trailRenderer.positionCount = trailSegments;
        
        // 确保拖尾启用
        if (!trailRenderer.enabled)
        {
            trailRenderer.enabled = true;
        }
        
        // 生成平滑的拖尾点
        // 注意：LineRenderer的第一个点（startWidth）应该对应当前位置（大），最后一个点（endWidth）对应旧位置（小）
        for (int i = 0; i < trailSegments; i++)
        {
            float t = (float)i / (trailSegments - 1); // 0到1的插值
            
            // X坐标从currentX到startX（水平方向，从当前位置到旧位置）
            // 这样第一个点（t=0）是当前位置（大），最后一个点（t=1）是旧位置（小）
            float x = Mathf.Lerp(currentX, startX, t);
            
            // 计算对应的时间点（从新到旧，与X坐标方向一致）
            float targetTime = currentTime - (trailLength / pipeSpeed) * t;
            
            // 在历史记录中查找对应时间点的Y坐标（使用平滑插值）
            float y = GetSmoothYAtTime(targetTime, t);
            
            // 应用位置偏移
            Vector3 position = new Vector3(x + trailOffset.x, y + trailOffset.y, 0);
            
            trailRenderer.SetPosition(i, position);
        }
        
        // 调试信息：每60帧输出一次拖尾状态
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"BirdScript: 拖尾已更新 - 长度={trailLength:F2}, 点数={trailSegments}, 历史记录={trailHistory.Count}, X范围=[{startX:F2}, {currentX:F2}], 偏移={trailOffset}");
        }
    }
    
    /// <summary>
    /// 获取平滑的Y坐标（使用Catmull-Rom样条插值）
    /// </summary>
    private float GetSmoothYAtTime(float targetTime, float t)
    {
        if (trailHistory.Count == 0) return transform.position.y;
        
        // 如果平滑度太低或历史记录不足，使用简单插值
        if (trailSmoothness < 0.01f || trailHistory.Count < 4)
        {
            return GetYAtTime(targetTime);
        }
        
        // 找到目标时间所在的位置
        int index = FindTimeIndex(targetTime);
        
        if (index < 1 || index >= trailHistory.Count - 1)
        {
            // 边界情况，使用简单插值
            return GetYAtTime(targetTime);
        }
        
        // 获取四个控制点（用于Catmull-Rom样条）
        // p1和p2是目标时间所在的两个点
        TrailPoint p0 = trailHistory[Mathf.Max(0, index - 2)];
        TrailPoint p1 = trailHistory[index - 1];
        TrailPoint p2 = trailHistory[index];
        TrailPoint p3 = trailHistory[Mathf.Min(trailHistory.Count - 1, index + 1)];
        
        // 计算在p1和p2之间的插值参数
        float localT = 0f;
        if (p2.time != p1.time)
        {
            localT = (targetTime - p1.time) / (p2.time - p1.time);
            localT = Mathf.Clamp01(localT);
        }
        else
        {
            // 如果时间相同，直接返回p1的Y值
            return p1.y;
        }
        
        // 使用Catmull-Rom样条插值
        float smoothY = CatmullRom(p0.y, p1.y, p2.y, p3.y, localT);
        
        // 混合平滑值和原始值
        float originalY = GetYAtTime(targetTime);
        return Mathf.Lerp(originalY, smoothY, trailSmoothness);
    }
    
    /// <summary>
    /// Catmull-Rom样条插值
    /// </summary>
    private float CatmullRom(float p0, float p1, float p2, float p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }
    
    /// <summary>
    /// 查找时间索引（二分查找优化）
    /// </summary>
    private int FindTimeIndex(float targetTime)
    {
        if (trailHistory.Count == 0) return -1;
        
        // 如果目标时间超出范围
        if (targetTime <= trailHistory[0].time)
            return 0;
        if (targetTime >= trailHistory[trailHistory.Count - 1].time)
            return trailHistory.Count - 1;
        
        // 二分查找
        int left = 0;
        int right = trailHistory.Count - 1;
        
        while (left < right)
        {
            int mid = (left + right) / 2;
            if (trailHistory[mid].time < targetTime)
            {
                left = mid + 1;
            }
            else
            {
                right = mid;
            }
        }
        
        return left;
    }
    
    /// <summary>
    /// 根据时间获取对应的Y坐标（使用插值）
    /// </summary>
    private float GetYAtTime(float targetTime)
    {
        if (trailHistory.Count == 0) return transform.position.y;
        
        // 如果目标时间超出范围，返回最接近的点
        if (targetTime <= trailHistory[0].time)
            return trailHistory[0].y;
        if (targetTime >= trailHistory[trailHistory.Count - 1].time)
            return trailHistory[trailHistory.Count - 1].y;
        
        // 使用FindTimeIndex查找索引（更高效）
        int index = FindTimeIndex(targetTime);
        if (index > 0 && index < trailHistory.Count)
        {
            // 在两个点之间插值
            int prevIndex = index - 1;
            if (prevIndex >= 0 && trailHistory[index].time != trailHistory[prevIndex].time)
            {
                float t = (targetTime - trailHistory[prevIndex].time) / 
                         (trailHistory[index].time - trailHistory[prevIndex].time);
                return Mathf.Lerp(trailHistory[prevIndex].y, trailHistory[index].y, t);
            }
            return trailHistory[index].y;
        }
        
        // 兜底：线性查找
        for (int i = 0; i < trailHistory.Count - 1; i++)
        {
            if (trailHistory[i].time <= targetTime && targetTime <= trailHistory[i + 1].time)
            {
                // 在两个点之间插值
                float t = (targetTime - trailHistory[i].time) / 
                         (trailHistory[i + 1].time - trailHistory[i].time);
                return Mathf.Lerp(trailHistory[i].y, trailHistory[i + 1].y, t);
            }
        }
        
        return transform.position.y;
    }
    
    /// <summary>
    /// 获取当前拖尾长度（根据总分数+当前关卡分数计算）
    /// </summary>
    private float GetCurrentTrailLength()
    {
        if (logic == null)
        {
            Debug.LogWarning("BirdScript: logic 为 null，使用最小拖尾长度");
            return minTrailLength;
        }
        
        // 使用总金币 + 当前关卡金币来计算拖尾长度
        int totalScore = logic.totalCoins;
        int currentLevelScore = logic.playerCoins;
        int combinedScore = totalScore + currentLevelScore;
        
        float t = Mathf.Clamp01((float)combinedScore / maxTrailScore);
        float length = Mathf.Lerp(minTrailLength, maxTrailLength, t);
        
        // 调试信息（只在第一次或长度变化时输出）
        if (Time.frameCount % 60 == 0) // 每60帧输出一次
        {
            Debug.Log($"BirdScript: 总分数={totalScore}, 当前关卡分数={currentLevelScore}, 合计={combinedScore}, 拖尾长度={length:F2}, 插值t={t:F2}, min={minTrailLength}, max={maxTrailLength}, 宽度={trailWidth}");
        }
        
        return length;
    }
    
    /// <summary>
    /// 清除拖尾
    /// </summary>
    private void ClearTrail()
    {
        if (trailRenderer != null)
        {
            trailRenderer.positionCount = 0;
        }
    }
    
    /// <summary>
    /// 根据总金币更新拖尾长度（由 logicManager 调用）
    /// </summary>
    /// <param name="totalScore">总金币</param>
    public void UpdateTrailLength(int totalScore)
    {
        // 这个方法现在由 UpdateHorizontalTrail 自动处理
        // 保留此方法以保持接口兼容性
    }
    
    private void OnJump(InputAction.CallbackContext context)
    {
        // 检查游戏状态和鸟的生命状态
        if (!birdIsAlive) return;
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying()) return;

        // 播放小鸟飞行音效
        PlayFlapSound();

        myRigidbody.velocity = Vector2.up * flapStrength;
    }
    
    /// <summary>
    /// 播放小鸟飞行音效
    /// </summary>
    private void PlayFlapSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBirdFlapSound();
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 只有在游戏中才处理碰撞
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying()) return;
        if (!birdIsAlive) return;
        
        // 检查是否是怪物
        MonsterScript monster = collision.gameObject.GetComponent<MonsterScript>();
        if (monster != null)
        {
            Debug.Log($"BirdScript: 小鸟碰撞到怪物！怪物: {monster.gameObject.name}");
            TakeDamage(1);
            return;
        }
        
        // 其他碰撞（管道等）造成伤害
        TakeDamage(1);
    }
    
    /// <summary>
    /// 触发器碰撞检测（用于检测怪物，如果怪物使用触发器）
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 只有在游戏中才处理碰撞
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying()) return;
        if (!birdIsAlive) return;
        
        // 检查是否是怪物
        MonsterScript monster = collision.GetComponent<MonsterScript>();
        if (monster != null)
        {
            Debug.Log($"BirdScript: 小鸟触发到怪物！怪物: {monster.gameObject.name}");
            TakeDamage(1);
        }
    }
    
    /// <summary>
    /// 小鸟受到伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(int damage)
    {
        if (!birdIsAlive) return;
        
        currentHP -= damage;
        Debug.Log($"BirdScript: 小鸟受到 {damage} 点伤害，当前血量: {currentHP}/{maxHP}");
        
        // 检查是否死亡
        if (currentHP <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// 小鸟死亡
    /// </summary>
    private void Die()
    {
        if (!birdIsAlive) return;
        
        birdIsAlive = false;
        Debug.Log("BirdScript: 小鸟死亡！");
        
        logic.gameOver();
    }
    
    /// <summary>
    /// 应用升级属性（由升级管理器调用）
    /// </summary>
    public void ApplyUpgrade(BirdUpgradeData.UpgradeType upgradeType, int valueIncrease, float percentIncrease)
    {
        switch (upgradeType)
        {
            case BirdUpgradeData.UpgradeType.Health:
                maxHP += valueIncrease;
                currentHP = maxHP; // 升级时恢复满血
                Debug.Log($"BirdScript: 血量升级 - maxHP={maxHP}, currentHP={currentHP}");
                break;
                
            case BirdUpgradeData.UpgradeType.BulletDamage:
                bulletDamage += valueIncrease;
                Debug.Log($"BirdScript: 子弹伤害升级 - bulletDamage={bulletDamage}");
                break;
                
            case BirdUpgradeData.UpgradeType.BulletCount:
                bulletCount += valueIncrease;
                if (bulletCount > 5) bulletCount = 5; // 限制最大数量
                Debug.Log($"BirdScript: 子弹数量升级 - bulletCount={bulletCount}");
                break;
                
            case BirdUpgradeData.UpgradeType.FireSpeed:
                if (percentIncrease > 0)
                {
                    // 减少发射间隔（提高发射速度）
                    fireInterval = Mathf.Max(0.1f, fireInterval * (1f - percentIncrease));
                }
                else
                {
                    // 使用固定值减少
                    fireInterval = Mathf.Max(0.1f, fireInterval - valueIncrease * 0.1f);
                }
                Debug.Log($"BirdScript: 发射速度升级 - fireInterval={fireInterval}");
                break;
                
            case BirdUpgradeData.UpgradeType.AttackRange:
                attackRange += valueIncrease;
                if (attackRange > 50f) attackRange = 50f; // 限制最大范围
                Debug.Log($"BirdScript: 攻击范围升级 - attackRange={attackRange}");
                break;
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
    /// 在Scene视图中绘制调试可视化
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!enableDebugVisualization) return;
        
        // 在编辑模式下更新LineRenderer
        if (!Application.isPlaying)
        {
            UpdateDebugTrailRenderer();
        }
        
        // 绘制调试信息（可选）
        DrawDebugInfo();
        
        // 绘制攻击范围（如果启用）
        if (showAttackRange)
        {
            DrawAttackRange();
        }
    }
    
    /// <summary>
    /// 绘制攻击范围（调试用）
    /// </summary>
    private void DrawAttackRange()
    {
        Vector2 birdPosition = transform.position;
        
        // 绘制圆形攻击范围
        Gizmos.color = Color.yellow;
        
        // 绘制圆形（使用多个线段模拟）
        int segments = 32;
        float angleStep = 360f / segments;
        Vector2 lastPoint = birdPosition + new Vector2(attackRange, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 point = birdPosition + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * attackRange;
            Gizmos.DrawLine(lastPoint, point);
            lastPoint = point;
        }
        
        // 绘制前方扇形区域（只显示前方180度，更清晰）
        Gizmos.color = Color.yellow;
        int forwardSegments = 20;
        float forwardAngleStep = 180f / forwardSegments;
        Vector2 forwardLastPoint = birdPosition + new Vector2(attackRange, 0);
        
        for (int i = 1; i <= forwardSegments; i++)
        {
            float angle = -90f + i * forwardAngleStep;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 point = birdPosition + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * attackRange;
            Gizmos.DrawLine(birdPosition, point);
            if (i > 1)
            {
                Gizmos.DrawLine(forwardLastPoint, point);
            }
            forwardLastPoint = point;
        }
        
        // 绘制中心线（从小鸟到前方）
        Gizmos.color = Color.red;
        Vector2 forwardPoint = birdPosition + new Vector2(attackRange, 0);
        Gizmos.DrawLine(birdPosition, forwardPoint);
    }
    
    /// <summary>
    /// 更新调试模式下的拖尾渲染器（可在编辑模式下调用）
    /// </summary>
    public void UpdateDebugTrailRenderer()
    {
        // 确保LineRenderer存在
        if (trailRenderer == null)
        {
            InitializeTrail();
        }
        
        if (trailRenderer == null) return;
        
        // 获取当前小鸟位置
        Vector3 birdPos = initialPosition;
        if (birdPos == Vector3.zero)
        {
            birdPos = transform.position;
        }
        
        // 计算拖尾长度
        float trailLength = GetDebugTrailLength();
        
        // 计算拖尾起始位置
        float startX = birdPos.x - trailLength;
        float endX = birdPos.x;
        
        // 使用调试曲线生成Y坐标变化
        int segments = trailSegments;
        
        // 设置LineRenderer属性
        trailRenderer.enabled = true;
        trailRenderer.useWorldSpace = true;
        trailRenderer.startWidth = trailWidth;
        trailRenderer.endWidth = trailWidth * trailEndWidthRatio;
        trailRenderer.positionCount = segments;
        trailRenderer.sortingOrder = trailSortingOrder; // 使用可配置的排序顺序
        trailRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trailRenderer.receiveShadows = false;
        
        // 确保材质存在并正确设置（使用sharedMaterial避免材质泄漏）
        if (trailRenderer.sharedMaterial == null || 
            (trailRenderer.sharedMaterial != null && trailRenderer.sharedMaterial.shader.name == "Hidden/InternalErrorShader"))
        {
            // 尝试使用不同的Shader
            Material trailMaterial = null;
            string[] shaderNames = { "Sprites/Default", "Unlit/Color", "Standard" };
            
            foreach (string shaderName in shaderNames)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader != null)
                {
                    trailMaterial = new Material(shader);
                    // 设置为白色，让渐变色完全控制颜色
                    trailMaterial.color = Color.white;
                    break;
                }
            }
            
            if (trailMaterial == null)
            {
                // 如果都找不到，使用默认材质
                trailMaterial = new Material(Shader.Find("Sprites/Default"));
                trailMaterial.color = Color.white; // 白色，不干扰渐变色
            }
            
            trailRenderer.sharedMaterial = trailMaterial; // 使用sharedMaterial避免材质泄漏
        }
        
        // 确保材质颜色正确（如果需要修改颜色，创建材质实例）
        if (trailRenderer.sharedMaterial != null)
        {
            // 在编辑模式下，如果需要修改颜色，使用material（会创建实例）
            // 在运行时，应该使用sharedMaterial
            if (!Application.isPlaying)
            {
                // 编辑模式下可以修改material，设置为白色让渐变色控制
                if (trailRenderer.material != null)
                {
                    trailRenderer.material.color = Color.white; // 白色，不干扰渐变色
                }
            }
        }
        
        // 设置颜色渐变
        if (trailColorGradient == null || trailColorGradient.alphaKeys.Length == 0)
        {
            trailColorGradient = CreateDefaultGradient();
        }
        trailRenderer.colorGradient = trailColorGradient;
        
        // 生成拖尾点
        // 注意：LineRenderer的第一个点（startWidth）应该对应当前位置（大），最后一个点（endWidth）对应旧位置（小）
        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);
            // X坐标从endX（当前位置）到startX（旧位置）
            // 这样第一个点（t=0）是当前位置（大），最后一个点（t=1）是旧位置（小）
            float x = Mathf.Lerp(endX, startX, t);
            
            // 使用调试曲线计算Y坐标（从当前位置到旧位置）
            float curveT = t;
            float yOffset = debugYCurve.Evaluate(curveT);
            float y = birdPos.y + yOffset;
            
            // 应用位置偏移
            Vector3 position = new Vector3(x + trailOffset.x, y + trailOffset.y, birdPos.z);
            
            trailRenderer.SetPosition(i, position);
        }
    }
    
    /// <summary>
    /// 绘制调试信息（Gizmos）
    /// </summary>
    private void DrawDebugInfo()
    {
        // 获取当前小鸟位置
        Vector3 birdPos = Application.isPlaying ? transform.position : initialPosition;
        if (birdPos == Vector3.zero && !Application.isPlaying)
        {
            birdPos = transform.position;
        }
        
        // 计算拖尾长度
        float trailLength = GetDebugTrailLength();
        
        // 计算拖尾起始位置
        float startX = birdPos.x - trailLength;
        float endX = birdPos.x;
        
        // 绘制起始和结束点标记
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(new Vector3(startX + trailOffset.x, birdPos.y + trailOffset.y, birdPos.z), 0.3f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(new Vector3(endX + trailOffset.x, birdPos.y + trailOffset.y, birdPos.z), 0.3f);
        
        // 绘制小鸟位置
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(birdPos, 0.2f);
    }
    
    /// <summary>
    /// 获取调试模式下的拖尾长度
    /// </summary>
    private float GetDebugTrailLength()
    {
        if (Application.isPlaying && logic != null)
        {
            // 运行时：使用总金币 + 当前关卡金币
            int totalScore = logic.totalCoins;
            int currentLevelScore = logic.playerCoins;
            int combinedScore = totalScore + currentLevelScore;
            float t = Mathf.Clamp01((float)combinedScore / maxTrailScore);
            return Mathf.Lerp(minTrailLength, maxTrailLength, t);
        }
        else
        {
            // 编辑模式：使用调试总分数
            float t = Mathf.Clamp01((float)debugTotalScore / maxTrailScore);
            return Mathf.Lerp(minTrailLength, maxTrailLength, t);
        }
    }
    
    /// <summary>
    /// 开始自动发射子弹
    /// </summary>
    private void StartFiring()
    {
        fireTimer = 0f;
    }
    
    /// <summary>
    /// 停止发射子弹
    /// </summary>
    private void StopFiring()
    {
        fireTimer = 0f;
    }
    
    /// <summary>
    /// 更新子弹发射逻辑
    /// </summary>
    private void UpdateFiring()
    {
        if (bulletPrefab == null) return;
        
        // 检查攻击范围内是否有怪物
        bool hasMonster = HasMonsterInRange();
        
        // 调试信息（每60帧输出一次，避免刷屏）
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"BirdScript: 攻击范围检测 - attackRange={attackRange:F2}, hasMonster={hasMonster}, fireTimer={fireTimer:F2}");
        }
        
        if (!hasMonster)
        {
            // 范围内没有怪物，重置计时器但不发射
            fireTimer = 0f;
            return;
        }
        
        fireTimer += Time.deltaTime;
        
        // 检查是否到了发射时间
        if (fireTimer >= fireInterval)
        {
            FireBullet();
            fireTimer = 0f;
        }
    }
    
    /// <summary>
    /// 检查攻击范围内是否有怪物
    /// </summary>
    private bool HasMonsterInRange()
    {
        Vector2 birdPosition = transform.position;
        
        // 直接查找所有 MonsterScript 组件（不依赖 Collider2D）
        // 这样可以避免 Collider2D 设置问题
        MonsterScript[] allMonsters = FindObjectsOfType<MonsterScript>();
        
        foreach (MonsterScript monster in allMonsters)
        {
            if (monster == null || monster.gameObject == null) continue;
            
            Vector2 monsterPos = monster.transform.position;
            
            // 检查怪物是否在小鸟前方（X坐标大于小鸟）
            if (monsterPos.x > birdPosition.x)
            {
                // 计算距离
                float distance = Vector2.Distance(birdPosition, monsterPos);
                
                // 检查距离是否在攻击范围内
                if (distance <= attackRange)
                {
                    // 调试信息（每60帧输出一次，避免刷屏）
                    if (Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"BirdScript: 检测到怪物在攻击范围内 - 距离: {distance:F2}, 攻击范围: {attackRange:F2}, 怪物位置: {monsterPos}");
                    }
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 发射子弹
    /// </summary>
    private void FireBullet()
    {
        if (bulletPrefab == null) return;
        
        // 计算发射位置
        Vector3 firePosition = transform.position + new Vector3(fireOffset.x, fireOffset.y, 0);
        
        // 根据子弹数量发射
        if (bulletCount == 1)
        {
            // 单发：直接发射
            CreateBullet(firePosition);
        }
        else
        {
            // 多发：按Y轴间距发射
            float totalSpacing = (bulletCount - 1) * bulletSpacing;
            float startY = firePosition.y - totalSpacing / 2f;
            
            for (int i = 0; i < bulletCount; i++)
            {
                Vector3 bulletPos = new Vector3(firePosition.x, startY + i * bulletSpacing, firePosition.z);
                CreateBullet(bulletPos);
            }
        }
    }
    
    /// <summary>
    /// 创建子弹实例
    /// </summary>
    private void CreateBullet(Vector3 position)
    {
        GameObject bulletObj = Instantiate(bulletPrefab, position, Quaternion.identity);
        BulletScript bullet = bulletObj.GetComponent<BulletScript>();
        
        if (bullet != null)
        {
            // 设置子弹属性
            bullet.SetDamage(bulletDamage);
            bullet.SetSpeed(bulletSpeed);
            
            // 如果子弹支持自动追踪，确保启用
            if (bullet.autoTrackMonster)
            {
                // 子弹会自动寻找目标，不需要手动设置
            }
        }
        else
        {
            Debug.LogWarning("BirdScript: 子弹预制体没有 BulletScript 组件！");
        }
    }
}
