using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace WeChatWASM
{
    /// <summary>
    /// 禁用微信小游戏插件更新提示
    /// </summary>
    [InitializeOnLoad]
    public static class DisableWXUpdateCheck
    {
        static DisableWXUpdateCheck()
        {
            // 尝试通过反射禁用更新检查
            try
            {
                // 延迟执行，确保所有类都已加载
                EditorApplication.delayCall += () =>
                {
                    DisableUpdateCheck();
                };
            }
            catch (Exception e)
            {
                Debug.LogWarning($"禁用微信小游戏更新检查时出错: {e.Message}");
            }
        }

        private static void DisableUpdateCheck()
        {
            try
            {
                // 尝试找到PluginUpdateManager类并禁用更新检查
                Type pluginUpdateManagerType = Type.GetType("WeChatWASM.PluginUpdateManager, wx-editor");
                if (pluginUpdateManagerType != null)
                {
                    // 尝试设置一个标志来禁用更新检查
                    FieldInfo disableField = pluginUpdateManagerType.GetField("m_DisableUpdateCheck", BindingFlags.NonPublic | BindingFlags.Static);
                    if (disableField != null)
                    {
                        disableField.SetValue(null, true);
                        Debug.Log("已通过反射禁用微信小游戏插件更新检查");
                    }
                }
            }
            catch (Exception e)
            {
                // 如果反射失败，至少记录一下
                Debug.Log($"无法通过反射禁用更新检查: {e.Message}");
            }
        }
    }
}

