using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 配置更新按钮 - 点击后重新从 CSV 文件读取配置。
/// 
/// 使用方式：
/// 1. 在 Canvas 下创建一个 Button
/// 2. 添加本脚本
/// 3. 按钮点击时会调用 CSVConfigManager.UpdateConfig()
/// </summary>
public class ConfigUpdateButton : MonoBehaviour
{
    [Header("按钮引用")]
    [Tooltip("更新配置按钮（如果为空，会自动查找当前 GameObject 的 Button）")]
    public Button updateButton;

    [Header("提示文本（可选）")]
    [Tooltip("更新成功后的提示文本")]
    public Text successText;

    [Tooltip("提示文本显示时长（秒）")]
    public float successTextDuration = 2f;

    private float successTextTimer = 0f;

    private void Start()
    {
        if (updateButton == null)
        {
            updateButton = GetComponent<Button>();
        }

        if (updateButton != null)
        {
            updateButton.onClick.AddListener(OnUpdateClicked);
        }

        if (successText != null)
        {
            successText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // 隐藏成功提示文本
        if (successText != null && successText.gameObject.activeSelf)
        {
            successTextTimer -= Time.deltaTime;
            if (successTextTimer <= 0f)
            {
                successText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 更新按钮点击
    /// </summary>
    private void OnUpdateClicked()
    {
        Debug.Log("ConfigUpdateButton: 更新配置按钮被点击");
        
        if (CSVConfigManager.Instance != null)
        {
            CSVConfigManager.Instance.UpdateConfig();
            ShowSuccessText();
            Debug.Log("ConfigUpdateButton: 配置更新完成");
        }
        else
        {
            Debug.LogWarning("ConfigUpdateButton: CSVConfigManager.Instance 为空，请确保场景中有 CSVConfigManager 对象");
        }
    }

    /// <summary>
    /// 显示成功提示
    /// </summary>
    private void ShowSuccessText()
    {
        if (successText != null)
        {
            successText.gameObject.SetActive(true);
            successTextTimer = successTextDuration;
        }
    }
}
