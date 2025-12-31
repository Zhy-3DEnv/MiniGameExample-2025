using System.IO;
using UnityEditor;
using UnityEngine;

public class DevelopUtil : EditorWindow
{

    [MenuItem("Utilities/Clean PersistentDataPath")]
    static void ClearPersistentData()
    {
        var folderPath = Application.persistentDataPath;
        foreach (var directory in (new DirectoryInfo(folderPath).GetDirectories()))
        {

            directory.Delete(true);
        }

        foreach (var file in (new DirectoryInfo(folderPath).GetFiles()))
        {
            file.Attributes = FileAttributes.Normal;
            file.Delete();
        }

        AssetDatabase.Refresh();
    }

    [MenuItem("Utilities/Clean StreamingAssetsPath")]
    static void ClearStreamingAssetData()
    {
        var folderPath = Application.streamingAssetsPath;
        foreach (var directory in (new DirectoryInfo(folderPath).GetDirectories()))
        {
            directory.Delete(true);
        }

        foreach (var file in (new DirectoryInfo(folderPath).GetFiles()))
        {
            file.Attributes = FileAttributes.Normal;
            file.Delete();
        }

        AssetDatabase.Refresh();
    }

    [MenuItem("Utilities/Clean PlayerPrefData")]
    static void ClearPlayerPrefsData()
    {
        PlayerPrefs.DeleteAll();
    }

    [MenuItem("Utilities/Update CustomCloudAssetPath")]
    static void CopyABToCustomCloudAssetPath()
    {
        string abDirectoryPath= Path.GetFullPath("AssetBundles");
        string abCloudAssetsPath= Path.GetFullPath("CustomCloudAssets");

        Directory.CreateDirectory(abCloudAssetsPath);

        foreach (var abDirectory in (Directory.GetDirectories(abDirectoryPath)))
        {
            foreach (var abFile in (Directory.GetFiles(abDirectory)))
            {
                string destPath = abFile.Replace(abDirectory, abCloudAssetsPath);
                string destDir = Path.GetDirectoryName(destPath);
                Directory.CreateDirectory(destDir);

                File.Copy(abFile, destPath, true);
            }
        }
    }

    // 获取或创建AssetBundle配置
    static AssetBundleConfig GetOrCreateConfig()
    {
        // 首先尝试查找现有配置
        string[] guids = AssetDatabase.FindAssets("t:AssetBundleConfig");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<AssetBundleConfig>(path);
        }

        // 如果没有找到，创建一个新的配置
        AssetBundleConfig config = ScriptableObject.CreateInstance<AssetBundleConfig>();
        string configPath = "Assets/Editor/AssetBundleConfig.asset";
        Directory.CreateDirectory(Path.GetDirectoryName(configPath));
        AssetDatabase.CreateAsset(config, configPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("创建配置文件", 
            $"已创建配置文件: {configPath}\n\n请在Project窗口中选中该文件，然后在Inspector中配置AssetBundle。", 
            "确定");
        
        Selection.activeObject = config;
        EditorUtility.FocusProjectWindow();

        return config;
    }

    [MenuItem("Utilities/AssetBundle/打开配置")]
    static void OpenAssetBundleConfig()
    {
        AssetBundleConfig config = GetOrCreateConfig();
        Selection.activeObject = config;
        EditorUtility.FocusProjectWindow();
    }

    [MenuItem("Utilities/Build WXAssetBundles")]
    static void BuildWXAssetBundles()
    {
        // 获取配置
        AssetBundleConfig config = GetOrCreateConfig();
        if (config == null)
        {
            Debug.LogError("无法加载AssetBundle配置！");
            return;
        }

        // 验证配置
        if (!config.Validate(out string errorMessage))
        {
            EditorUtility.DisplayDialog("配置错误", 
                $"AssetBundle配置无效:\n{errorMessage}\n\n请先配置AssetBundle后再构建。", 
                "确定");
            Selection.activeObject = config;
            EditorUtility.FocusProjectWindow();
            return;
        }

        string abDirectoryPath = Path.GetFullPath("AssetBundles");
        if (!Directory.Exists(abDirectoryPath))
            Directory.CreateDirectory(abDirectoryPath);
        var buildTarget = EditorUserBuildSettings.activeBuildTarget;
        var buildPath = Path.Combine(abDirectoryPath, buildTarget.ToString());
        if (!Directory.Exists(buildPath))
            Directory.CreateDirectory(buildPath);

        // 从配置创建AssetBundleBuild数组
        System.Collections.Generic.List<AssetBundleBuild> validBundles = new System.Collections.Generic.List<AssetBundleBuild>();
        
        foreach (var bundleEntry in config.bundles)
        {
            if (string.IsNullOrEmpty(bundleEntry.bundleName))
                continue;

            // 过滤掉不存在的资源路径
            System.Collections.Generic.List<string> validAssetPaths = new System.Collections.Generic.List<string>();
            foreach (string assetPath in bundleEntry.assetPaths)
            {
                if (string.IsNullOrEmpty(assetPath))
                    continue;

                // 检查资源是否存在
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
                {
                    validAssetPaths.Add(assetPath);
                }
                else
                {
                    Debug.LogWarning($"资源路径不存在，将被跳过: {assetPath}");
                }
            }

            // 如果该AssetBundle有有效资源，则添加到构建列表
            if (validAssetPaths.Count > 0)
            {
                AssetBundleBuild build = new AssetBundleBuild();
                build.assetBundleName = bundleEntry.bundleName;
                build.assetNames = validAssetPaths.ToArray();
                validBundles.Add(build);
                Debug.Log($"AssetBundle '{build.assetBundleName}' 包含 {validAssetPaths.Count} 个有效资源。");
            }
            else
            {
                Debug.LogWarning($"AssetBundle '{bundleEntry.bundleName}' 没有有效资源，将被跳过。");
            }
        }

        if (validBundles.Count == 0)
        {
            EditorUtility.DisplayDialog("构建失败", 
                "没有有效的AssetBundle可以构建！\n\n请检查配置中的资源路径是否正确。", 
                "确定");
            return;
        }

        Debug.Log($"准备构建 {validBundles.Count} 个AssetBundle...");

        // 构建AssetBundle
        var buildManifest = BuildPipeline.BuildAssetBundles(buildPath, validBundles.ToArray(),
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle,
            buildTarget);
        
        if (buildManifest != null)
        {
            Debug.Log($"Build AssetBundle successfully with Bundle Count: {buildManifest.GetAllAssetBundles().Length}");
        }
        else
        {
            Debug.LogError("AssetBundle构建失败！");
        }

        //copy all assetbundle files to CustomCloudAssets folder if needed
        if (AutoStreaming.autoStreaming) 
        {
            string customCoudAssetsDir = Path.GetFullPath("CustomCloudAssets");
            if (!Directory.Exists(customCoudAssetsDir))
                Directory.CreateDirectory(customCoudAssetsDir);

            DirectoryInfo dirInfo = new DirectoryInfo(buildPath);
            foreach (var file in dirInfo.EnumerateFiles())
            {
                file.CopyTo(Path.Combine(customCoudAssetsDir, file.Name), true);
            }
        }
    }
    
    [MenuItem("Utilities/Disable ReadWrite for Models")]
    static void DisableReadWriteForAllModels()
    {
        string[] guids = AssetDatabase.FindAssets("t:Model");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter modelImporter = AssetImporter.GetAtPath(path) as ModelImporter;

            if (modelImporter != null && modelImporter.isReadable)
            {
                modelImporter.isReadable = false;
                AssetDatabase.ImportAsset(path);
            }
        }

        Debug.Log("Read/Write disabled for eligible models in the project.");
    }
}
