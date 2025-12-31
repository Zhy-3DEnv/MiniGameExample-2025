using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AssetBundleConfig", menuName = "AssetBundle/Config", order = 1)]
public class AssetBundleConfig : ScriptableObject
{
    [System.Serializable]
    public class BundleEntry
    {
        public string bundleName;
        public List<string> assetPaths = new List<string>();
    }

    public List<BundleEntry> bundles = new List<BundleEntry>();

    // 验证配置的有效性
    public bool Validate(out string errorMessage)
    {
        if (bundles == null || bundles.Count == 0)
        {
            errorMessage = "没有配置任何AssetBundle";
            return false;
        }

        for (int i = 0; i < bundles.Count; i++)
        {
            var bundle = bundles[i];
            if (string.IsNullOrEmpty(bundle.bundleName))
            {
                errorMessage = $"第 {i + 1} 个Bundle的名称为空";
                return false;
            }

            if (bundle.assetPaths == null || bundle.assetPaths.Count == 0)
            {
                errorMessage = $"Bundle '{bundle.bundleName}' 没有配置任何资源路径";
                return false;
            }
        }

        errorMessage = "";
        return true;
    }
}
