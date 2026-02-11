#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 创建默认 CharacterData 资源。
/// 菜单：EggRogue → 创建默认角色数据
/// </summary>
public static class CharacterDataSetup
{
    private const string ConfigsDir = "Assets/EggRogue/Configs";
    private const string CharactersDir = ConfigsDir + "/Characters";

    [MenuItem("EggRogue/创建默认角色数据")]
    public static void CreateDefaultCharacterData()
    {
        if (!AssetDatabase.IsValidFolder("Assets/EggRogue"))
        {
            Debug.LogError("CharacterDataSetup: Assets/EggRogue 不存在。");
            return;
        }
        if (!AssetDatabase.IsValidFolder("Assets/EggRogue/Configs"))
            AssetDatabase.CreateFolder("Assets/EggRogue", "Configs");
        if (!AssetDatabase.IsValidFolder(CharactersDir))
            AssetDatabase.CreateFolder(ConfigsDir, "Characters");

        // 创建默认角色
        var defaultChar = ScriptableObject.CreateInstance<EggRogue.CharacterData>();
        defaultChar.characterName = "默认角色";
        defaultChar.description = "基础角色";
        defaultChar.baseDamage = 10f;
        defaultChar.baseFireRate = 2f;
        defaultChar.baseMaxHealth = 100f;
        defaultChar.baseMoveSpeed = 5f;
        defaultChar.baseAttackRange = 10f;
        defaultChar.basePickupRange = 0.5f;
        defaultChar.baseArmorPercent = 0f;
        defaultChar.baseDodgePercent = 0f;
        defaultChar.baseRewardBonus = 0;
        defaultChar.baseCritRatePercent = 0f;
        defaultChar.baseCritDamageMultiplier = 1.2f;
        defaultChar.baseLuck = 0f;
        defaultChar.baseKnockback = 0f;

        string path = $"{CharactersDir}/DefaultCharacter.asset";
        AssetDatabase.CreateAsset(defaultChar, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"CharacterDataSetup: 已创建默认角色数据 {path}。在角色对象上添加 CharacterStats 组件并指定 CharacterData 引用。");
    }
}
#endif
