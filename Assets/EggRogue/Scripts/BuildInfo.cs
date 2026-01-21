using UnityEngine;

/// <summary>
/// 构建信息数据，保存在 Resources 中，供运行时读取。
/// </summary>
[CreateAssetMenu(fileName = "BuildInfo", menuName = "EggRogue/Build Info", order = 0)]
public class BuildInfo : ScriptableObject
{
    [Tooltip("最后一次构建的时间戳（由编辑器自动写入）。")]
    public string buildTime;

    [Tooltip("构建的平台信息（例如 WebGL / WeChatMiniGame 等）。")]
    public string buildPlatform;

    [Tooltip("可选的构建编号或备注信息。")]
    public string buildTag;
}

