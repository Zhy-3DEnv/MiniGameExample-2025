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
        defaultChar.baseDamage = 10;
        defaultChar.baseFireRate = 2;
        defaultChar.baseMaxHealth = 100;
        defaultChar.baseMoveSpeed = 5;
        defaultChar.baseAttackRange = 10;
        defaultChar.basePickupRange = 0.5f;
        defaultChar.baseArmorPercent = 0;
        defaultChar.baseDodgePercent = 0;
        defaultChar.baseRewardBonus = 0;
        defaultChar.baseCritRatePercent = 0;
        defaultChar.baseCritDamageMultiplier = 1.2f;
        defaultChar.baseLuck = 0;
        defaultChar.baseKnockback = 0;

        string path = $"{CharactersDir}/DefaultCharacter.asset";
        AssetDatabase.CreateAsset(defaultChar, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"CharacterDataSetup: 已创建默认角色数据 {path}。在角色对象上添加 CharacterStats 组件并指定 CharacterData 引用。");
    }
}
#endif
