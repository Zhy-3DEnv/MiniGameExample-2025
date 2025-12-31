using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class UGUIAlignWindow : EditorWindow
{
    private Dictionary<AlignType, Texture> alignTexture = new Dictionary<AlignType, Texture>();
    private const int ButtonSize = 64; // 定义按钮的尺寸（宽度和高度）
    private const int Padding = 20;    // 按钮之间的间距

    // 定义图标的相对路径（相对于 Textures 文件夹）
    private readonly Dictionary<AlignType, string> alignTextureFileNames = new Dictionary<AlignType, string>()
    {
        { AlignType.Left, "Left.png" },
        { AlignType.HorizontalCenter, "HorizontalCenter.png" },
        { AlignType.Right, "Right.png" },
        { AlignType.Top, "Top.png" },
        { AlignType.VerticalCenter, "VerticalCenter.png" },
        { AlignType.Bottom, "Bottom.png" },
        { AlignType.Horizontal, "Horizontal.png" },
        { AlignType.Vertical, "Vertical.png" }
    };

    void OnEnable()
    {
        // 获取当前脚本的路径
        string scriptPath = GetScriptPath();
        if (string.IsNullOrEmpty(scriptPath))
        {
            Debug.LogError("无法找到 UGUIAlignWindow.cs 的路径。请确保脚本位于 Assets/*/UGUIAlign/ 目录下。");
            return;
        }

        // 构建 Textures 文件夹的路径
        string texturesFolderPath = Path.Combine(Path.GetDirectoryName(scriptPath), "Textures");
        texturesFolderPath = texturesFolderPath.Replace("\\", "/"); // 统一使用正斜杠

        // 遍历每个 AlignType 并加载对应的纹理
        foreach (var kvp in alignTextureFileNames)
        {
            string texturePath = Path.Combine(texturesFolderPath, kvp.Value).Replace("\\", "/");
            Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
            if (tex != null)
            {
                alignTexture.Add(kvp.Key, tex);
            }
            else
            {
                Debug.LogWarning($"无法加载纹理: {texturePath}");
            }
        }
    }

    [MenuItem("ArtTools/UI 对齐工具")]
    public static UGUIAlignWindow GetWindowInstance()
    {
        var window = GetWindow<UGUIAlignWindow>();
        window.titleContent = new GUIContent("UGUI Align");
        window.minSize = new Vector2(400, 200); // 设置窗口的最小尺寸
        window.Focus();
        window.Repaint();
        return window;
    }

    void OnGUI()
    {
        // 设置窗口的边距
        GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUILayout.Space(Padding); // 上边距

        // 居中整个按钮阵列
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace(); // 左侧填充

        GUILayout.BeginVertical();

        // 第一行按钮
        GUILayout.BeginHorizontal();
        AddAlignButton(AlignType.Left);
        GUILayout.Space(Padding); // 按钮间距
        AddAlignButton(AlignType.HorizontalCenter);
        GUILayout.Space(Padding);
        AddAlignButton(AlignType.Right);
        GUILayout.Space(Padding);
        AddAlignButton(AlignType.Horizontal);
        GUILayout.EndHorizontal();

        GUILayout.Space(Padding); // 两行之间的间距

        // 第二行按钮
        GUILayout.BeginHorizontal();
        AddAlignButton(AlignType.Top);
        GUILayout.Space(Padding);
        AddAlignButton(AlignType.VerticalCenter);
        GUILayout.Space(Padding);
        AddAlignButton(AlignType.Bottom);
        GUILayout.Space(Padding);
        AddAlignButton(AlignType.Vertical);
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        GUILayout.FlexibleSpace(); // 右侧填充
        GUILayout.EndHorizontal();

        GUILayout.Space(Padding); // 下边距
        GUILayout.EndVertical();
    }

    /// <summary>
    /// 添加对齐按钮，并设置工具提示
    /// </summary>
    /// <param name="align">对齐类型</param>
    private void AddAlignButton(AlignType align)
    {
        if (alignTexture.ContainsKey(align) && alignTexture[align] != null)
        {
            // 为按钮添加工具提示
            string tooltip = align.ToString();
            if (GUILayout.Button(new GUIContent(alignTexture[align], tooltip), GUILayout.Width(ButtonSize), GUILayout.Height(ButtonSize)))
            {
                UGUIAlign.Align(align);
            }
        }
        else
        {
            // 如果纹理不存在，显示一个空的占位按钮
            GUILayout.Button(new GUIContent("Missing", tooltip: align.ToString()), GUILayout.Width(ButtonSize), GUILayout.Height(ButtonSize));
        }
    }

    /// <summary>
    /// 获取当前脚本的路径
    /// </summary>
    /// <returns>脚本的Asset路径，如果未找到则返回null</returns>
    private string GetScriptPath()
    {
        string className = this.GetType().Name;
        string[] guids = AssetDatabase.FindAssets(className + " t:Script");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return path;
        }
        return null;
    }
}
