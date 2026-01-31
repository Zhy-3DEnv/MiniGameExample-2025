#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 创建 LevelDatabase 与默认 LevelData 资源。
/// 菜单：EggRogue → 创建 LevelDatabase 与默认关卡
/// </summary>
public static class LevelDatabaseSetup
{
    private const string ConfigsDir = "Assets/EggRogue/Configs";
    private const string LevelsDir = ConfigsDir + "/Levels";

    [MenuItem("EggRogue/创建 LevelDatabase 与默认关卡")]
    public static void CreateLevelDatabaseAndLevels()
    {
        if (!AssetDatabase.IsValidFolder("Assets/EggRogue"))
        {
            Debug.LogError("LevelDatabaseSetup: Assets/EggRogue 不存在。");
            return;
        }
        if (!AssetDatabase.IsValidFolder("Assets/EggRogue/Configs"))
            AssetDatabase.CreateFolder("Assets/EggRogue", "Configs");
        if (!AssetDatabase.IsValidFolder(LevelsDir))
            AssetDatabase.CreateFolder(ConfigsDir, "Levels");

        EggRogue.LevelDatabase db = ScriptableObject.CreateInstance<EggRogue.LevelDatabase>();
        EggRogue.LevelData[] levels = new EggRogue.LevelData[20];
        for (int i = 0; i < 20; i++)
        {
            var ld = ScriptableObject.CreateInstance<EggRogue.LevelData>();
            ld.levelNumber = i + 1;
            ld.levelName = $"第{i + 1}关";
            ld.levelDuration = 30f + i * 5f;
            ld.victoryRewardGold = 50 + i * 10;
            // 每秒刷怪数量：从第1关的0.25个/秒（4秒一个）逐渐增加到第20关的0.44个/秒（约2.3秒一个）
            ld.spawnsPerSecond = 0.25f + i * 0.01f;
            ld.maxAliveEnemies = 10 + i * 2;
            ld.randomOffsetRadius = 2f;
            ld.enemyHealthMultiplier = 1f + i * 0.05f;
            ld.enemyMoveSpeedMultiplier = 1f + i * 0.02f;
            ld.coinDropMultiplier = 1f + i * 0.03f;

            string path = $"{LevelsDir}/Level_{i + 1:D2}.asset";
            AssetDatabase.CreateAsset(ld, path);
            levels[i] = ld;
        }

        db.levels = levels;
        AssetDatabase.CreateAsset(db, ConfigsDir + "/LevelDatabase.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("LevelDatabaseSetup: 已创建 LevelDatabase 与 20 个 LevelData。在 LevelManager 上指定 LevelDatabase 引用。");
    }
}
#endif
