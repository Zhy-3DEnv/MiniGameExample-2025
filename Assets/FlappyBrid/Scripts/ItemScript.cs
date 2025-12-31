using UnityEngine;

public class ItemScript : MonoBehaviour
{
    public logicManager logic;
    public int scoreValue = 2; // 道具分数值，默认为2分
    [Header("分数显示设置")]
    public GameObject scorePopupPrefab; // 分数弹跳显示预制体（可选）
    public Vector3 popupOffset = new Vector3(0, 1, 0); // 分数显示位置偏移
    [Header("分数颜色设置")]
    [Tooltip("分数显示颜色（如果为空则使用ScorePopup预制体的默认颜色）")]
    public Color scoreColor = Color.white; // 分数显示颜色
    
    void Start()
    {
        // 通过tag获取逻辑管理器
        GameObject logicObject = GameObject.FindGameObjectWithTag("Logic");
        if (logicObject != null)
        {
            logic = logicObject.GetComponent<logicManager>();
        }
        else
        {
            Debug.LogWarning("未找到Logic标签的游戏对象！");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 检查是否是鸟（Layer 3）
        if (collision.gameObject.layer == 3)
        {
            // 播放道具收集音效
            PlayItemCollectSound();
            
            // 显示分数弹跳效果
            ShowScorePopup();
            
            // 加分（计入产出控制）
            if (logic != null)
            {
                logic.addCoins(scoreValue, true); // true 表示计入产出控制
            }
            
            // 销毁道具
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 播放道具收集音效
    /// </summary>
    private void PlayItemCollectSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayItemCollectSound();
        }
    }
    
    /// <summary>
    /// 显示分数弹跳效果
    /// </summary>
    private void ShowScorePopup()
    {
        if (scorePopupPrefab != null)
        {
            // 查找Canvas（优先查找世界空间的Canvas）
            Canvas canvas = FindCanvas();
            if (canvas == null)
            {
                Debug.LogWarning("ItemScript: 未找到Canvas，分数弹跳可能无法正确显示！");
            }
            
            // 计算显示位置（道具位置 + 偏移）
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
                scorePopup.SetScore(scoreValue);
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
                    text.text = "+" + scoreValue.ToString();
                }
            }
        }
        else
        {
            // 如果没有预制体，可以在这里创建一个简单的文本显示
            // 但建议使用预制体方式，更灵活
            Debug.LogWarning("ItemScript: 未设置ScorePopup预制体，无法显示分数弹跳效果！");
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

