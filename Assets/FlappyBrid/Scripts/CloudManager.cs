using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 云管理器
/// 统一管理场景中所有云的循环移动
/// </summary>
public class CloudManager : MonoBehaviour
{
    [Header("云对象设置")]
    [Tooltip("云对象列表（如果为空，会自动查找场景中所有带CloudMoveScript的对象）")]
    public List<CloudMoveScript> cloudObjects = new List<CloudMoveScript>();
    
    [Header("统一移动设置")]
    [Tooltip("是否启用统一速度控制")]
    public bool useUnifiedSpeed = false;
    
    [Tooltip("统一移动速度（当启用统一速度控制时使用）")]
    public float unifiedSpeed = 1f;
    
    [Tooltip("是否在游戏暂停时停止所有云")]
    public bool pauseOnGamePause = true;
    
    [Header("自动查找设置")]
    [Tooltip("是否自动查找场景中的云对象")]
    public bool autoFindClouds = true;
    
    [Tooltip("云对象的标签（用于自动查找）")]
    public string cloudTag = "";
    
    private void Start()
    {
        // 如果启用自动查找，查找所有云对象
        if (autoFindClouds)
        {
            FindAllClouds();
        }
        
        // 如果启用统一速度，设置所有云的速度
        if (useUnifiedSpeed)
        {
            SetAllCloudsSpeed(unifiedSpeed);
        }
    }
    
    private void Update()
    {
        // 如果启用统一速度控制，确保所有云使用统一速度
        if (useUnifiedSpeed)
        {
            foreach (var cloud in cloudObjects)
            {
                if (cloud != null)
                {
                    cloud.SetSpeed(unifiedSpeed);
                }
            }
        }
    }
    
    /// <summary>
    /// 自动查找场景中所有带CloudMoveScript的对象
    /// </summary>
    public void FindAllClouds()
    {
        cloudObjects.Clear();
        
        // 如果有标签，按标签查找
        if (!string.IsNullOrEmpty(cloudTag))
        {
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(cloudTag);
            foreach (var obj in taggedObjects)
            {
                CloudMoveScript cloud = obj.GetComponent<CloudMoveScript>();
                if (cloud != null)
                {
                    cloudObjects.Add(cloud);
                }
            }
        }
        
        // 查找所有带CloudMoveScript的对象
        CloudMoveScript[] allClouds = FindObjectsOfType<CloudMoveScript>();
        foreach (var cloud in allClouds)
        {
            if (!cloudObjects.Contains(cloud))
            {
                cloudObjects.Add(cloud);
            }
        }
        
        Debug.Log($"CloudManager: 找到 {cloudObjects.Count} 个云对象");
    }
    
    /// <summary>
    /// 设置所有云的移动速度
    /// </summary>
    public void SetAllCloudsSpeed(float speed)
    {
        foreach (var cloud in cloudObjects)
        {
            if (cloud != null)
            {
                cloud.SetSpeed(speed);
            }
        }
    }
    
    /// <summary>
    /// 停止所有云的移动
    /// </summary>
    public void StopAllClouds()
    {
        SetAllCloudsSpeed(0f);
    }
    
    /// <summary>
    /// 恢复所有云的移动
    /// </summary>
    public void ResumeAllClouds()
    {
        if (useUnifiedSpeed)
        {
            SetAllCloudsSpeed(unifiedSpeed);
        }
    }
    
    /// <summary>
    /// 重置所有云的位置
    /// </summary>
    public void ResetAllClouds()
    {
        foreach (var cloud in cloudObjects)
        {
            if (cloud != null)
            {
                cloud.ResetPosition();
            }
        }
    }
}

