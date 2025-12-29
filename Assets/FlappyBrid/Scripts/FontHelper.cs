using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 字体辅助类，用于确保中文字体正确显示
/// 在WebGL/小游戏平台上，需要确保字体包含中文字符集
/// </summary>
public class FontHelper : MonoBehaviour
{
    [Header("字体设置")]
    [Tooltip("支持中文的字体（如果为空，则尝试使用系统默认中文字体）")]
    public Font chineseFont;
    
    [Tooltip("是否自动为所有Text组件设置字体")]
    public bool autoSetFonts = true;
    
    [Tooltip("是否在Start时自动设置字体")]
    public bool setOnStart = true;
    
    void Start()
    {
        if (setOnStart)
        {
            SetAllTextFonts();
        }
    }
    
    /// <summary>
    /// 为所有Text组件设置中文字体
    /// </summary>
    public void SetAllTextFonts()
    {
        if (chineseFont == null)
        {
            // 尝试获取系统默认中文字体
            chineseFont = GetSystemChineseFont();
        }
        
        if (chineseFont == null)
        {
            Debug.LogWarning("FontHelper: 未找到中文字体！请手动指定一个支持中文的字体。");
            return;
        }
        
        // 查找所有Text组件
        Text[] allTexts = FindObjectsOfType<Text>(true); // true表示包括未激活的对象
        
        int count = 0;
        foreach (Text text in allTexts)
        {
            if (text != null && text.font != chineseFont)
            {
                text.font = chineseFont;
                count++;
            }
        }
        
        Debug.Log($"FontHelper: 已为 {count} 个Text组件设置中文字体: {chineseFont.name}");
    }
    
    /// <summary>
    /// 获取系统默认中文字体
    /// </summary>
    private Font GetSystemChineseFont()
    {
        // 尝试常见的系统字体名称
        string[] fontNames = {
            "Microsoft YaHei",      // 微软雅黑
            "SimHei",              // 黑体
            "SimSun",              // 宋体
            "KaiTi",               // 楷体
            "FangSong",            // 仿宋
            "Arial Unicode MS",    // Arial Unicode（如果安装了）
            "Noto Sans CJK SC",    // Noto字体
            "Source Han Sans SC"   // 思源黑体
        };
        
        foreach (string fontName in fontNames)
        {
            Font font = Font.CreateDynamicFontFromOSFont(fontName, 16);
            if (font != null)
            {
                Debug.Log($"FontHelper: 找到系统字体: {fontName}");
                return font;
            }
        }
        
        // 如果都找不到，尝试使用Resources中的字体
        Font resourceFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (resourceFont != null)
        {
            Debug.LogWarning("FontHelper: 使用默认Arial字体，可能不支持中文。建议手动指定中文字体。");
            return resourceFont;
        }
        
        return null;
    }
    
    /// <summary>
    /// 为指定Text组件设置字体
    /// </summary>
    public void SetTextFont(Text textComponent)
    {
        if (textComponent == null) return;
        
        if (chineseFont == null)
        {
            chineseFont = GetSystemChineseFont();
        }
        
        if (chineseFont != null)
        {
            textComponent.font = chineseFont;
        }
    }
    
    /// <summary>
    /// 在编辑器中测试字体设置
    /// </summary>
    [ContextMenu("设置所有Text字体")]
    public void EditorSetAllTextFonts()
    {
        SetAllTextFonts();
    }
}


