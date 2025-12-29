using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipeMoveScript : MonoBehaviour
{
    public float moveSpeed = 5;
    public float deadZone = -35;
    
    [Header("基础设置（会被关卡设置覆盖）")]
    [Tooltip("基础移动速度")]
    public float baseMoveSpeed = 5f;
    
    private bool isMoving = true;  // 是否正在移动
    
    /// <summary>
    /// 获取当前移动速度（供其他脚本使用）
    /// </summary>
    public float GetMoveSpeed()
    {
        return moveSpeed;
    }
    void Start()
    {
        baseMoveSpeed = moveSpeed; // 保存基础值
        ApplyLevelSettings();
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
                // 应用移动速度倍数
                moveSpeed = baseMoveSpeed * levelData.moveSpeedMultiplier;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 只有在移动状态且游戏进行中时才移动
        if (!isMoving) return;
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying())
        {
            isMoving = false;
            return;
        }
        
        transform.position = transform.position + (Vector3.left * moveSpeed) * Time.deltaTime;
        if (transform.position.x < deadZone)
        {
            Debug.Log("pipe delete！");
            Destroy(gameObject);
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
        isMoving = true;
    }
}
