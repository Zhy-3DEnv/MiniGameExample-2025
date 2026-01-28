#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using EggRogue;

/// <summary>
/// 编辑器工具：创建默认敌人数据（EnemyData）资源。
/// 菜单：EggRogue → 创建默认敌人数据
/// </summary>
public static class EnemyDataSetup
{
    private const string ConfigsDir = "Assets/EggRogue/Configs";
    private const string EnemiesDir = ConfigsDir + "/Enemies";

    [MenuItem("EggRogue/创建默认敌人数据")]
    public static void CreateDefaultEnemyData()
    {
        if (!AssetDatabase.IsValidFolder("Assets/EggRogue"))
        {
            Debug.LogError("EnemyDataSetup: Assets/EggRogue 不存在。");
            return;
        }
        if (!AssetDatabase.IsValidFolder("Assets/EggRogue/Configs"))
            AssetDatabase.CreateFolder("Assets/EggRogue", "Configs");
        if (!AssetDatabase.IsValidFolder(EnemiesDir))
            AssetDatabase.CreateFolder(ConfigsDir, "Enemies");

        // 创建默认敌人数据
        var defaultEnemy = ScriptableObject.CreateInstance<EnemyData>();
        defaultEnemy.enemyName = "默认敌人";
        defaultEnemy.description = "基础敌人类型";
        defaultEnemy.baseMaxHealth = 20f;
        defaultEnemy.baseMoveSpeed = 3f;
        defaultEnemy.coinDropMin = 1;
        defaultEnemy.coinDropMax = 2;
        defaultEnemy.coinDropRadius = 0.3f;

        string path = $"{EnemiesDir}/DefaultEnemy.asset";
        AssetDatabase.CreateAsset(defaultEnemy, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"EnemyDataSetup: 已创建默认敌人数据 {path}。在 EnemySpawner 上指定 EnemyData 引用。");
    }
}
#endif
