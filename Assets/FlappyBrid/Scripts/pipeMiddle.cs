using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pipeMiddle : MonoBehaviour
{
    public logicManager logic;
    
    [Header("计分设置")]
    [Tooltip("通过管道后获得的分数（如果为-1，则使用关卡设置）")]
    public int pipePassScore = -1; // -1表示使用关卡设置
    
    private bool hasScored = false; // 是否已经计分（防止重复计分）
    private bool birdInside = false; // 小鸟是否在触发器内
    
    void Start()
    {
        logic = GameObject.FindGameObjectWithTag("Logic").GetComponent<logicManager>();//通过tag来自定将逻辑管理对象填入到输入中
        
        // 从关卡设置获取通过管道的分数
        UpdateScoreFromLevel();
    }
    
    /// <summary>
    /// 从关卡设置更新通过管道的分数
    /// </summary>
    public void UpdateScoreFromLevel()
    {
        if (pipePassScore < 0 && LevelManager.Instance != null)
        {
            LevelData levelData = LevelManager.Instance.GetCurrentLevelData();
            if (levelData != null)
            {
                pipePassScore = levelData.pipePassScore;
            }
            else
            {
                pipePassScore = 1; // 默认值
            }
        }
        else if (pipePassScore < 0)
        {
            pipePassScore = 1; // 默认值
        }
    }
    
    /// <summary>
    /// 小鸟进入触发器
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 3) // 鸟的Layer
        {
            birdInside = true;
            hasScored = false; // 重置计分标志（允许重新计分）
        }
    }
    
    /// <summary>
    /// 小鸟离开触发器时计分
    /// </summary>
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 3 && !hasScored && birdInside) // 鸟的Layer，且未计分，且确实进入过
        {
            // 检查小鸟是否真的通过了管道（小鸟在触发器右侧）
            // 这样可以确保小鸟是从左到右通过，而不是从右到左
            if (collision.transform.position.x > transform.position.x)
            {
                // 更新分数（可能在关卡切换后）
                UpdateScoreFromLevel();
                
                // 计分
                if (logic != null)
                {
                    logic.addScore(pipePassScore);
                    hasScored = true;
                }
            }
            
            birdInside = false;
        }
    }
}
