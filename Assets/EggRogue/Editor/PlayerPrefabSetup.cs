using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using EggRogue;

/// <summary>
/// 创建玩家预制体、角色模型预制体，并配置角色数据。
/// </summary>
public static class PlayerPrefabSetup
{
    private const string ModelPrefabFolder = "Assets/EggRogue/Prefabs/CharacterModels";
    private const string EggMan01FbxPath = "Assets/EggRogue/Meshs/EggMan01.fbx";
    private const string EggMan02FbxPath = "Assets/EggRogue/Meshs/EggMan02.fbx";
    private const string PlayerPrefabPath = "Assets/EggRogue/Prefabs/Player.prefab";

    private const string GameScenePath = "Assets/EggRogue/Scenes/GameScene.scene";

    /// <summary>
    /// 可通过 Unity 命令行 -executeMethod PlayerPrefabSetup.SetupCharacterModuleAll 调用
    /// </summary>
    [MenuItem("EggRogue/一键搭建角色模块（需先打开 GameScene）", false, 109)]
    public static void SetupCharacterModuleAll()
    {
        if (EditorSceneManager.GetActiveScene().name != "GameScene")
        {
            if (Application.isBatchMode || EditorUtility.DisplayDialog("切换场景", "需要打开 GameScene 才能创建 Player 预制体。是否现在打开？", "打开", "取消"))
            {
                EditorSceneManager.OpenScene(GameScenePath);
            }
            else
            {
                Debug.LogWarning("[PlayerPrefabSetup] 已取消。请手动打开 GameScene 后重新执行。");
                return;
            }
        }

        CreateModelPrefabs();
        if (!CreatePlayerPrefabFromScene()) return;
        AssignModelPrefabsToCharacters();
        if (!SwitchSceneToUsePlayerSpawner()) return;
        Debug.Log("[PlayerPrefabSetup] 角色模块搭建完成！请保存 GameScene (Ctrl+S)。");
    }

    [MenuItem("EggRogue/创建角色模型预制体 (EggMan01 + EggMan02)", false, 110)]
    public static void CreateModelPrefabs()
    {
        EnsureFolderExists(ModelPrefabFolder);

        GameObject egg1 = AssetDatabase.LoadAssetAtPath<GameObject>(EggMan01FbxPath);
        GameObject egg2 = AssetDatabase.LoadAssetAtPath<GameObject>(EggMan02FbxPath);

        if (egg1 == null)
        {
            Debug.LogError($"[PlayerPrefabSetup] 无法加载 {EggMan01FbxPath}");
            return;
        }
        if (egg2 == null)
        {
            Debug.LogError($"[PlayerPrefabSetup] 无法加载 {EggMan02FbxPath}");
            return;
        }

        GameObject inst1 = (GameObject)PrefabUtility.InstantiatePrefab(egg1);
        inst1.name = "EggMan01_Model";
        PrefabUtility.SaveAsPrefabAsset(inst1, $"{ModelPrefabFolder}/EggMan01_Model.prefab");
        Object.DestroyImmediate(inst1);

        GameObject inst2 = (GameObject)PrefabUtility.InstantiatePrefab(egg2);
        inst2.name = "EggMan02_Model";
        PrefabUtility.SaveAsPrefabAsset(inst2, $"{ModelPrefabFolder}/EggMan02_Model.prefab");
        Object.DestroyImmediate(inst2);

        AssetDatabase.Refresh();
        Debug.Log("[PlayerPrefabSetup] 已创建 EggMan01_Model.prefab 和 EggMan02_Model.prefab");
    }

    [MenuItem("EggRogue/从场景创建 Player 预制体", false, 111)]
    public static bool CreatePlayerPrefabFromScene()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            player = Object.FindObjectOfType<CharacterStats>()?.gameObject;

        if (player == null)
        {
            Debug.LogError("[PlayerPrefabSetup] 场景中未找到 Player（Tag=Player 或带 CharacterStats 的对象）。请先打开 GameScene。");
            return false;
        }

        EnsureFolderExists(Path.GetDirectoryName(PlayerPrefabPath).Replace("\\", "/"));

        GameObject copy = Object.Instantiate(player);
        copy.name = "Player";

        Transform modelRoot = copy.transform.Find("ModelRoot");
        Transform weaponSlot = copy.transform.Find("WeaponSlot");

        if (modelRoot == null)
        {
            GameObject modelRootGo = new GameObject("ModelRoot");
            modelRootGo.transform.SetParent(copy.transform);
            modelRootGo.transform.localPosition = Vector3.zero;
            modelRootGo.transform.localRotation = Quaternion.identity;
            modelRootGo.transform.localScale = Vector3.one;
            modelRoot = modelRootGo.transform;
        }

        if (weaponSlot == null)
        {
            GameObject weaponSlotGo = new GameObject("WeaponSlot");
            weaponSlotGo.transform.SetParent(copy.transform);
            weaponSlotGo.transform.localPosition = new Vector3(0.5f, 0.5f, 0.5f);
            weaponSlotGo.transform.localRotation = Quaternion.identity;
            weaponSlotGo.transform.localScale = Vector3.one;
        }

        for (int i = copy.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = copy.transform.GetChild(i);
            if (child.name == "ModelRoot" || child.name == "WeaponSlot" || child.name == "FirePoint")
                continue;
            if (child.GetComponent<MeshFilter>() != null || child.GetComponent<SkinnedMeshRenderer>() != null ||
                child.name.Contains("EggMan") || child.name.Contains("Body") || child.name.Contains("Cube"))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        PrefabUtility.SaveAsPrefabAsset(copy, PlayerPrefabPath);
        Object.DestroyImmediate(copy);

        AssetDatabase.Refresh();
        Debug.Log($"[PlayerPrefabSetup] 已创建 Player.prefab 到 {PlayerPrefabPath}");
        return true;
    }

    [MenuItem("EggRogue/为角色分配模型预制体 (图灵蛋=EggMan02, 其他=EggMan01)", false, 112)]
    public static void AssignModelPrefabsToCharacters()
    {
        GameObject eggMan01Model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelPrefabFolder}/EggMan01_Model.prefab");
        GameObject eggMan02Model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelPrefabFolder}/EggMan02_Model.prefab");

        if (eggMan01Model == null || eggMan02Model == null)
        {
            Debug.LogError("[PlayerPrefabSetup] 请先执行「创建角色模型预制体」菜单。");
            return;
        }

        CharacterDatabase db = AssetDatabase.LoadAssetAtPath<CharacterDatabase>("Assets/EggRogue/Configs/CharacterDatabase.asset");
        if (db == null || db.characters == null)
        {
            Debug.LogError("[PlayerPrefabSetup] 未找到 CharacterDatabase。");
            return;
        }

        int assigned = 0;
        foreach (CharacterData c in db.characters)
        {
            if (c == null) continue;
            c.characterModelPrefab = c.characterName.Contains("图灵蛋") ? eggMan02Model : eggMan01Model;
            EditorUtility.SetDirty(c);
            assigned++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[PlayerPrefabSetup] 已为 {assigned} 个角色分配模型预制体（图灵蛋→EggMan02，其他→EggMan01）");
    }

    [MenuItem("EggRogue/将 GameScene 切换为使用 PlayerSpawner", false, 113)]
    public static bool SwitchSceneToUsePlayerSpawner()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            player = Object.FindObjectOfType<CharacterStats>()?.gameObject;

        if (player == null)
        {
            if (Object.FindObjectOfType<PlayerSpawner>() != null)
            {
                Debug.Log("[PlayerPrefabSetup] 场景已使用 PlayerSpawner，无需重复切换。");
                return true;
            }
            Debug.LogError("[PlayerPrefabSetup] 场景中未找到 Player。");
            return false;
        }

        Vector3 pos = player.transform.position;
        Quaternion rot = player.transform.rotation;

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        GameObject defaultModel = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelPrefabFolder}/EggMan01_Model.prefab");

        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerPrefabSetup] 请先执行「从场景创建 Player 预制体」菜单。");
            return false;
        }

        Undo.DestroyObjectImmediate(player);

        GameObject spawnPoint = new GameObject("PlayerSpawnPoint");
        Undo.RegisterCreatedObjectUndo(spawnPoint, "Create PlayerSpawnPoint");
        spawnPoint.transform.position = pos;
        spawnPoint.transform.rotation = rot;

        PlayerSpawner spawner = spawnPoint.AddComponent<PlayerSpawner>();
        Undo.RecordObject(spawner, "Setup PlayerSpawner");
        spawner.playerPrefab = playerPrefab;
        spawner.defaultModelPrefab = defaultModel;
        spawner.spawnPoint = spawnPoint.transform;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[PlayerPrefabSetup] 已移除场景中的 Player，添加 PlayerSpawnPoint。请保存场景 (Ctrl+S)。");
        return true;
    }

    private static void EnsureFolderExists(string path)
    {
        path = path.Replace("\\", "/");
        if (AssetDatabase.IsValidFolder(path)) return;

        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
