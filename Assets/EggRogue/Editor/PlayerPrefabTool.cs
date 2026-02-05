#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using EggRogue;

/// <summary>
/// 玩家预制体工具：
/// 从当前场景中的 PlayerSpawner.playerPrefab 实例化一个玩家对象，
/// 自动挂上必要组件，然后保存为一个可编辑的预制体，方便在编辑器下调参（如近战挥砍幅度等）。
/// </summary>
public static class PlayerPrefabTool
{
    private const string DefaultOutputFolder = "Assets/EggRogue/Prefabs";
    private const string DefaultPrefabName = "PlayerRuntimeTemplate.prefab";

    [MenuItem("EggRogue/玩家工具/从 PlayerSpawner 生成玩家预制体")]
    public static void CreatePlayerPrefabFromSpawner()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[PlayerPrefabTool] 请在非运行状态下使用该工具。");
            return;
        }

        var spawner = Object.FindObjectOfType<PlayerSpawner>();
        if (spawner == null)
        {
            EditorUtility.DisplayDialog(
                "未找到 PlayerSpawner",
                "请在包含 GameScene / 出生点的场景中执行此菜单（场景中需有 PlayerSpawner 组件）。",
                "确定");
            return;
        }

        if (spawner.playerPrefab == null)
        {
            EditorUtility.DisplayDialog(
                "playerPrefab 未设置",
                "PlayerSpawner.playerPrefab 为空，请先在 Inspector 中指定一个玩家基础预制体。",
                "确定");
            return;
        }

        // 在场景中临时实例化一个玩家对象
        var temp = PrefabUtility.InstantiatePrefab(spawner.playerPrefab) as GameObject;
        if (temp == null)
        {
            Debug.LogError("[PlayerPrefabTool] 实例化 playerPrefab 失败。");
            return;
        }

        temp.name = "PlayerRuntimeTemplate_Preview";
        Undo.RegisterCreatedObjectUndo(temp, "Create Player Runtime Template");

        // 确保必要组件存在（与 PlayerSpawner.SpawnPlayer 中保持一致）
        if (temp.GetComponent<CharacterController>() == null)
            temp.AddComponent<CharacterController>();
        if (temp.GetComponent<CharacterStats>() == null)
            temp.AddComponent<CharacterStats>();
        if (temp.GetComponent<WeaponController>() == null)
            temp.AddComponent<WeaponController>();
        if (temp.GetComponent<ItemEffectManager>() == null)
            temp.AddComponent<ItemEffectManager>();
        if (temp.GetComponent<DebugDrawController>() == null)
            temp.AddComponent<DebugDrawController>();

        // 确保有 WeaponSlot 根节点，方便预览武器挂点
        var weaponSlotRoot = temp.transform.Find("WeaponSlot");
        if (weaponSlotRoot == null)
        {
            var go = new GameObject("WeaponSlot");
            go.transform.SetParent(temp.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
        }

        // 确保有 ModelRoot 方便挂角色模型
        var modelRoot = temp.transform.Find("ModelRoot");
        if (modelRoot == null)
        {
            var go = new GameObject("ModelRoot");
            go.transform.SetParent(temp.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
        }

        // 创建输出目录
        if (!AssetDatabase.IsValidFolder(DefaultOutputFolder))
        {
            var parts = DefaultOutputFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        string outputPath = DefaultOutputFolder + "/" + DefaultPrefabName;
        var prefab = PrefabUtility.SaveAsPrefabAsset(temp, outputPath);
        if (prefab != null)
        {
            Debug.Log($"[PlayerPrefabTool] 已生成玩家预制体：{outputPath}，后续可在该预制体上调整近战挥砍等参数。");

            // 可选：自动将 PlayerSpawner.playerPrefab 指向新生成的模板
            Undo.RecordObject(spawner, "Set Player Prefab");
            spawner.playerPrefab = prefab;
            EditorUtility.SetDirty(spawner);
        }
        else
        {
            Debug.LogError("[PlayerPrefabTool] 保存预制体失败，请检查路径是否有效。");
        }

        // 清理临时对象
        Object.DestroyImmediate(temp);
    }
}
#endif

