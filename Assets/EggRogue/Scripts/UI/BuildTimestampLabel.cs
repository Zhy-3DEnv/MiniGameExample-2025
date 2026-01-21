using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 在 UI 文本上显示当前运行包的构建时间戳。
/// 使用方式：
/// 1. 在 Canvas 下创建一个 Text（或使用已有 Text），用来显示构建时间。
/// 2. 将本脚本挂到该 Text 所在的 GameObject 上。
/// 3. 在 Inspector 中把 Text 组件拖到 targetText 引用。
/// 4. 确保项目中存在 Resources/BuildInfo.asset（编辑器构建脚本会自动创建/更新）。
/// </summary>
public class BuildTimestampLabel : MonoBehaviour
{
    [Header("显示构建时间戳的 Text")]
    public Text targetText;

    [Header("显示格式设置")]
    [Tooltip("前缀文本，例如 \"Build: \" 或 \"版本：\"")]
    public string prefix = "Build: ";

    private void Awake()
    {
        if (targetText == null)
        {
            targetText = GetComponent<Text>();
        }

        UpdateLabel();
    }

    /// <summary>
    /// 读取 BuildInfo 并刷新 UI 文本。
    /// </summary>
    public void UpdateLabel()
    {
        var buildInfo = Resources.Load<BuildInfo>("BuildInfo");
        if (buildInfo == null)
        {
            if (targetText != null)
            {
                targetText.text = prefix + "未找到 BuildInfo（请先执行一次构建）";
            }
            return;
        }

        if (targetText != null)
        {
            var platformPart = string.IsNullOrEmpty(buildInfo.buildPlatform)
                ? ""
                : $" ({buildInfo.buildPlatform})";

            var tagPart = string.IsNullOrEmpty(buildInfo.buildTag)
                ? ""
                : $" [{buildInfo.buildTag}]";

            targetText.text = $"{prefix}{buildInfo.buildTime}{platformPart}{tagPart}";
        }
    }
}

