using UnityEngine;

/// <summary>
/// 云循环移动脚本
/// 让云从左向右移动，移出屏幕后循环回到起始位置
/// </summary>
public class CloudMoveScript : MonoBehaviour
{
    [Header("移动设置")]
    [Tooltip("移动速度（单位/秒，正数向左移动，负数向右移动）")]
    public float moveSpeed = 1f;
    
    [Tooltip("循环宽度（云移出屏幕左侧多少距离后重置到右侧）")]
    public float loopWidth = 20f;
    
    [Tooltip("是否在游戏暂停时停止移动")]
    public bool pauseOnGamePause = true;
    
    [Tooltip("是否使用屏幕边界计算（推荐开启）")]
    public bool useScreenBounds = true;
    
    [Header("随机设置（可选）")]
    [Tooltip("是否使用随机速度（在minSpeed和maxSpeed之间）")]
    public bool useRandomSpeed = false;
    
    [Tooltip("最小速度")]
    public float minSpeed = 0.5f;
    
    [Tooltip("最大速度")]
    public float maxSpeed = 2f;
    
    private Vector3 startPosition;
    private float actualSpeed;
    private Camera mainCamera;
    
    void Start()
    {
        // 保存初始位置
        startPosition = transform.position;
        
        // 获取主摄像机（用于计算屏幕边界）
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        // 如果使用随机速度，设置随机速度
        if (useRandomSpeed)
        {
            actualSpeed = Random.Range(minSpeed, maxSpeed);
        }
        else
        {
            actualSpeed = moveSpeed;
        }
    }
    
    void Update()
    {
        // 如果游戏暂停且设置了暂停时停止移动，则不移动
        if (pauseOnGamePause && GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying())
        {
            return;
        }
        
        // 移动云
        transform.position += Vector3.left * actualSpeed * Time.deltaTime;
        
        // 检查是否需要循环
        if (useScreenBounds && mainCamera != null)
        {
            // 使用摄像机计算屏幕边界
            Vector3 screenLeft = mainCamera.ScreenToWorldPoint(new Vector3(0, Screen.height / 2, mainCamera.nearClipPlane));
            Vector3 screenRight = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height / 2, mainCamera.nearClipPlane));
            
            float leftBound = screenLeft.x - loopWidth;
            float rightBound = screenRight.x + loopWidth;
            
            // 如果云移出屏幕左侧，重置到右侧
            if (transform.position.x < leftBound)
            {
                // 计算云需要移动的距离，保持Y和Z坐标不变
                float resetX = rightBound;
                transform.position = new Vector3(resetX, transform.position.y, transform.position.z);
            }
        }
        else
        {
            // 如果没有摄像机或禁用屏幕边界，使用简单的距离判断
            if (transform.position.x < startPosition.x - loopWidth)
            {
                float resetX = startPosition.x + loopWidth;
                transform.position = new Vector3(resetX, transform.position.y, transform.position.z);
            }
        }
    }
    
    /// <summary>
    /// 重置云的位置到初始位置
    /// </summary>
    public void ResetPosition()
    {
        transform.position = startPosition;
    }
    
    /// <summary>
    /// 设置移动速度
    /// </summary>
    public void SetSpeed(float speed)
    {
        actualSpeed = speed;
    }
}

