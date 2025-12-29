using UnityEngine;
using UnityEngine.UI;

public class ScorePopup : MonoBehaviour
{
    [Header("动画设置")]
    [Tooltip("向上移动速度（单位/秒）")]
    public float moveSpeed = 2f;        // 向上移动速度
    [Tooltip("显示时长（秒）")]
    public float lifetime = 1f;         // 显示时长（秒）
    [Tooltip("淡出开始时间（相对于lifetime的比例，0.5表示在50%时开始淡出）")]
    [Range(0f, 1f)]
    public float fadeStartRatio = 0.5f; // 淡出开始比例
    [Header("字体大小动画")]
    [Tooltip("是否启用字体大小动画")]
    public bool enableScaleAnimation = true;
    [Tooltip("初始字体大小（相对于原始大小的倍数，0.5表示50%）")]
    [Range(0.1f, 2f)]
    public float startScale = 0.5f;     // 起始缩放
    [Tooltip("最大字体大小（相对于原始大小的倍数，1.5表示150%）")]
    [Range(0.5f, 3f)]
    public float maxScale = 1.5f;       // 最大缩放
    [Tooltip("达到最大大小的时间（相对于lifetime的比例，0.2表示在20%时达到最大）")]
    [Range(0.1f, 0.5f)]
    public float maxScaleTime = 0.2f;   // 达到最大大小的时间比例
    [Tooltip("最终字体大小（相对于原始大小的倍数，1.0表示100%）")]
    [Range(0.1f, 2f)]
    public float endScale = 1.0f;       // 最终缩放
    
    private Text scoreText;              // 文本组件
    private RectTransform rectTransform; // RectTransform组件
    private float timer = 0f;           // 计时器
    private Vector3 startPosition;      // 起始位置（世界坐标）
    private Canvas parentCanvas;       // 父Canvas
    private float originalFontSize;     // 原始字体大小
    
    void Awake()
    {
        // 在Awake中初始化组件，确保在SetPosition调用前准备好
        scoreText = GetComponent<Text>();
        rectTransform = GetComponent<RectTransform>();
        
        if (scoreText == null)
        {
            Debug.LogWarning("ScorePopup: 未找到Text组件！");
        }
        
        // 获取父Canvas
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            // 如果不在Canvas下，尝试查找并移动到Canvas下
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                transform.SetParent(canvas.transform, false);
                parentCanvas = canvas;
                // 重新获取rectTransform，因为父对象改变了
                rectTransform = GetComponent<RectTransform>();
            }
        }
    }
    
    void Start()
    {
        // 初始化透明度
        if (scoreText != null)
        {
            Color color = scoreText.color;
            color.a = 1f;
            scoreText.color = color;
            
            // 记录原始字体大小
            originalFontSize = scoreText.fontSize;
            
            // 如果启用缩放动画，设置初始大小
            if (enableScaleAnimation)
            {
                scoreText.fontSize = Mathf.RoundToInt(originalFontSize * startScale);
            }
        }
        
        // 如果startPosition还没有设置（SetPosition还没被调用），使用当前位置
        if (rectTransform != null && startPosition == Vector3.zero)
        {
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
            {
                startPosition = rectTransform.anchoredPosition;
            }
            else
            {
                startPosition = rectTransform.position;
            }
        }
    }
    
    void Update()
    {
        timer += Time.deltaTime;
        
        // 向上移动（基于起始位置的偏移）
        Vector3 moveOffset = Vector3.up * (moveSpeed * timer);
        
        if (rectTransform != null && parentCanvas != null)
        {
            if (parentCanvas.renderMode == RenderMode.WorldSpace)
            {
                // World Space：直接使用世界坐标
                rectTransform.position = startPosition + moveOffset;
            }
            else
            {
                // Screen Space：使用anchoredPosition在Canvas本地空间内移动
                rectTransform.anchoredPosition = (Vector2)startPosition + (Vector2)moveOffset;
            }
        }
        else
        {
            // 没有Canvas信息时退回到Transform.position
            transform.position = startPosition + moveOffset;
        }
        
        // 字体大小动画
        if (scoreText != null && enableScaleAnimation)
        {
            float normalizedTime = timer / lifetime; // 0到1的时间进度
            float currentScale;
            
            if (normalizedTime <= maxScaleTime)
            {
                // 从startScale到maxScale（从小到大）
                float progress = normalizedTime / maxScaleTime;
                currentScale = Mathf.Lerp(startScale, maxScale, progress);
            }
            else
            {
                // 从maxScale到endScale（从大到小）
                float progress = (normalizedTime - maxScaleTime) / (1f - maxScaleTime);
                currentScale = Mathf.Lerp(maxScale, endScale, progress);
            }
            
            scoreText.fontSize = Mathf.RoundToInt(originalFontSize * currentScale);
        }
        
        // 淡出效果
        if (scoreText != null)
        {
            Color color = scoreText.color;
            float fadeStartTime = lifetime * fadeStartRatio;
            if (timer > fadeStartTime)
            {
                float fadeDuration = lifetime - fadeStartTime;
                if (fadeDuration > 0)
                {
                    float fadeProgress = (timer - fadeStartTime) / fadeDuration;
                    color.a = Mathf.Lerp(1f, 0f, fadeProgress);
                }
                else
                {
                    color.a = 0f;
                }
            }
            scoreText.color = color;
        }
        
        // 超过生命周期后销毁
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 设置显示的分数文本
    /// </summary>
    public void SetScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = "+" + score.ToString();
        }
    }
    
    /// <summary>
    /// 设置分数显示颜色
    /// </summary>
    public void SetColor(Color color)
    {
        if (scoreText != null)
        {
            // 保持当前的透明度，只改变颜色
            Color currentColor = scoreText.color;
            color.a = currentColor.a; // 保持原有透明度
            scoreText.color = color;
        }
    }
    
    /// <summary>
    /// 设置显示位置（道具的世界坐标）
    /// </summary>
    public void SetPosition(Vector3 worldPosition)
    {
        // 确保Canvas已初始化
        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    transform.SetParent(canvas.transform, false);
                    parentCanvas = canvas;
                    rectTransform = GetComponent<RectTransform>();
                }
            }
        }
        
        if (rectTransform != null && parentCanvas != null)
        {
            if (parentCanvas.renderMode == RenderMode.WorldSpace)
            {
                // 世界空间Canvas：直接使用世界坐标
                rectTransform.position = worldPosition;
                startPosition = worldPosition;
            }
            else
            {
                // Screen Space Canvas：先把世界坐标转成屏幕坐标，再转成Canvas本地坐标
                Camera cam = null;
                
                // 对于Screen Space - Overlay，相机应该是null
                // 对于Screen Space - Camera，使用Canvas指定的相机或Main Camera
                if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    cam = parentCanvas.worldCamera;
                    if (cam == null)
                    {
                        cam = Camera.main;
                    }
                }
                // Screen Space - Overlay模式，cam保持为null

                // 获取用于世界坐标转换的相机（用于WorldToScreenPoint）
                Camera worldToScreenCam = parentCanvas.worldCamera;
                if (worldToScreenCam == null)
                {
                    worldToScreenCam = Camera.main;
                }

                if (worldToScreenCam != null)
                {
                    Vector2 screenPos = worldToScreenCam.WorldToScreenPoint(worldPosition);
                    RectTransform canvasRect = parentCanvas.transform as RectTransform;
                    Vector2 localPoint;
                    
                    // 对于Overlay模式，cam参数应该是null；对于Camera模式，使用cam
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, cam, out localPoint))
                    {
                        rectTransform.anchoredPosition = localPoint;
                        startPosition = localPoint;
                    }
                    else
                    {
                        // 如果转换失败，尝试直接使用屏幕坐标（可能需要调整）
                        Debug.LogWarning($"ScorePopup: 坐标转换失败，屏幕坐标: {screenPos}");
                        rectTransform.anchoredPosition = screenPos;
                        startPosition = screenPos;
                    }
                }
                else
                {
                    // 没有相机时的兜底逻辑
                    Debug.LogWarning("ScorePopup: 未找到相机，无法转换世界坐标！");
                    rectTransform.position = worldPosition;
                    startPosition = worldPosition;
                }
            }
        }
        else
        {
            // 没有RectTransform或Canvas时的兜底逻辑
            transform.position = worldPosition;
            startPosition = worldPosition;
        }
    }
}

