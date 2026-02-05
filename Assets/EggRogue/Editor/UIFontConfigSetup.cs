#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using EggRogue;

public static class UIFontConfigSetup
{
    private const string FontPath = "Assets/FlappyBrid/Fonts/simhei.ttf";
    private const string ConfigPath = "Assets/Resources/UIFontConfig.asset";

    [MenuItem("EggRogue/创建 UI 字体配置（simhei）")]
    public static void CreateUIFontConfig()
    {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
            {
                Debug.LogError($"[UIFontConfigSetup] 未找到字体: {FontPath}");
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                if (!AssetDatabase.IsValidFolder("Assets"))
                    AssetDatabase.CreateFolder("Assets", "Resources");
                else
                    AssetDatabase.CreateFolder("Assets", "Resources");
            }

            var config = AssetDatabase.LoadAssetAtPath<UIFontConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<UIFontConfig>();
                config.defaultFont = font;
                AssetDatabase.CreateAsset(config, ConfigPath);
                Debug.Log($"[UIFontConfigSetup] 已创建 {ConfigPath} 并绑定 simhei.ttf");
            }
            else
            {
                config.defaultFont = font;
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                Debug.Log("[UIFontConfigSetup] 已更新 UIFontConfig 字体为 simhei.ttf");
            }
    }
}
#endif
