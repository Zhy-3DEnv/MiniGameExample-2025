using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AssetBundleConfig))]
public class AssetBundleConfigEditor : Editor
{
    private Vector2 scrollPosition;

    public override void OnInspectorGUI()
    {
        AssetBundleConfig config = (AssetBundleConfig)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("AssetBundle 配置", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("配置需要打包到AssetBundle的资源。每个Bundle可以包含多个资源路径。", MessageType.Info);
        EditorGUILayout.Space();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // 添加新Bundle按钮
        if (GUILayout.Button("添加新 Bundle", GUILayout.Height(30)))
        {
            if (config.bundles == null)
                config.bundles = new List<AssetBundleConfig.BundleEntry>();

            config.bundles.Add(new AssetBundleConfig.BundleEntry
            {
                bundleName = $"bundle_{config.bundles.Count + 1}",
                assetPaths = new List<string>()
            });
            EditorUtility.SetDirty(config);
        }

        EditorGUILayout.Space();

        // 显示所有Bundle
        if (config.bundles != null)
        {
            for (int i = 0; i < config.bundles.Count; i++)
            {
                DrawBundleEntry(config, i);
                EditorGUILayout.Space();
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // 验证按钮
        if (GUILayout.Button("验证配置", GUILayout.Height(30)))
        {
            if (config.Validate(out string errorMessage))
            {
                EditorUtility.DisplayDialog("验证成功", "所有配置都有效！", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("验证失败", errorMessage, "确定");
            }
        }

        // 保存
        if (GUI.changed)
        {
            EditorUtility.SetDirty(config);
        }
    }

    private void DrawBundleEntry(AssetBundleConfig config, int index)
    {
        var bundle = config.bundles[index];

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Bundle名称和删除按钮
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Bundle {index + 1}", GUILayout.Width(80));
        bundle.bundleName = EditorGUILayout.TextField("名称:", bundle.bundleName);

        if (GUILayout.Button("删除", GUILayout.Width(60)))
        {
            config.bundles.RemoveAt(index);
            EditorUtility.SetDirty(config);
            return;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 资源路径列表
        EditorGUILayout.LabelField($"资源路径 ({bundle.assetPaths.Count}):", EditorStyles.boldLabel);

        if (bundle.assetPaths == null)
            bundle.assetPaths = new List<string>();

        // 拖拽区域
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 30, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "拖拽资源到这里添加", EditorStyles.helpBox);
        
        Event evt = Event.current;
        if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
        {
            if (dropArea.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                    {
                        string path = AssetDatabase.GetAssetPath(draggedObject);
                        if (!string.IsNullOrEmpty(path) && !bundle.assetPaths.Contains(path))
                        {
                            bundle.assetPaths.Add(path);
                        }
                    }
                    
                    EditorUtility.SetDirty(config);
                    evt.Use();
                }
            }
        }

        for (int j = 0; j < bundle.assetPaths.Count; j++)
        {
            EditorGUILayout.BeginHorizontal();

            // 资源路径输入框
            bundle.assetPaths[j] = EditorGUILayout.TextField(bundle.assetPaths[j]);

            // 选择资源按钮
            if (GUILayout.Button("选择", GUILayout.Width(50)))
            {
                string path = EditorUtility.OpenFilePanel("选择资源", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    // 转换为相对于Assets的路径
                    if (path.StartsWith(Application.dataPath))
                    {
                        path = "Assets" + path.Substring(Application.dataPath.Length);
                        bundle.assetPaths[j] = path;
                        EditorUtility.SetDirty(config);
                    }
                }
            }

            // Ping资源按钮
            if (!string.IsNullOrEmpty(bundle.assetPaths[j]))
            {
                if (GUILayout.Button("定位", GUILayout.Width(50)))
                {
                    UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(bundle.assetPaths[j]);
                    if (obj != null)
                    {
                        EditorGUIUtility.PingObject(obj);
                    }
                }
            }

            // 删除路径按钮
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                bundle.assetPaths.RemoveAt(j);
                EditorUtility.SetDirty(config);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        // 添加资源路径按钮
        if (GUILayout.Button("+ 添加资源路径", GUILayout.Height(25)))
        {
            bundle.assetPaths.Add("");
            EditorUtility.SetDirty(config);
        }

        EditorGUILayout.EndVertical();
    }
}
