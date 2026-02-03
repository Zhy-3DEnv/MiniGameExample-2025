using UnityEngine;
using UnityEditor;
using System.IO;
using EggRogue;

/// <summary>
/// 创建默认武器和 WeaponDatabase。
/// </summary>
public static class WeaponSetup
{
    private const string WeaponFolderPath = "Assets/EggRogue/Configs/Weapons";
    private const string DatabasePath = "Assets/EggRogue/Configs/WeaponDatabase.asset";

    [MenuItem("EggRogue/一键搭建武器系统（武器+管理器+UI）", false, 114)]
    public static void SetupWeaponSystemAll()
    {
        CreateDefaultWeapons();
        AddWeaponManagersToScene();
        AddWeaponSelectionPanel();
        Debug.Log("[WeaponSetup] 武器系统搭建完成。请确保 PersistentScene 为当前打开场景后再执行。");
    }

    [MenuItem("EggRogue/创建默认武器和 WeaponDatabase", false, 115)]
    public static void CreateDefaultWeapons()
    {
        EnsureFolderExists(WeaponFolderPath);

        var gun = CreateOrLoadWeapon("Weapon_Gun_Lv1", w =>
        {
            w.weaponId = "gun_lv1";
            w.weaponName = "手枪";
            w.weaponType = WeaponType.Ranged;
            w.level = 1;
            w.basePrice = 50;
            w.damage = 10f;
            w.fireRate = 2f;
            w.attackRange = 10f;
            w.bulletSpeed = 20f;
            w.bulletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EggRogue/Prefabs/Bullet.prefab");
        });

        var knife = CreateOrLoadWeapon("Weapon_Knife_Lv1", w =>
        {
            w.weaponId = "knife_lv1";
            w.weaponName = "小刀";
            w.weaponType = WeaponType.Melee;
            w.level = 1;
            w.basePrice = 40;
            w.damage = 25f;
            w.fireRate = 1.5f;
            w.attackRange = 2f;
        });

        var database = AssetDatabase.LoadAssetAtPath<WeaponDatabase>(DatabasePath);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<WeaponDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }
        database.weapons = new[] { gun, knife };
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[WeaponSetup] 已创建手枪、小刀及 WeaponDatabase。");
    }

    [MenuItem("EggRogue/添加武器管理器到当前场景", false, 116)]
    public static void AddWeaponManagersToScene()
    {
        var managers = GameObject.Find("Managers");
        var root = managers != null ? managers.transform : null;
        if (root == null)
        {
            var persistentRoot = GameObject.Find("PersistentRoot");
            if (persistentRoot != null)
            {
                var m = persistentRoot.transform.Find("Managers");
                root = m != null ? m : persistentRoot.transform;
            }
        }
        Transform parent = root;
        if (parent == null)
        {
            var go = new GameObject("Managers");
            parent = go.transform;
            var pr = GameObject.Find("PersistentRoot");
            if (pr != null) go.transform.SetParent(pr.transform);
            Debug.Log("[WeaponSetup] 未找到 Managers，已创建新节点。");
        }

        if (GameObject.Find("WeaponSelectionManager") == null)
        {
            var go = new GameObject("WeaponSelectionManager");
            go.transform.SetParent(parent);
            var c = go.AddComponent<WeaponSelectionManager>();
            c.weaponDatabase = AssetDatabase.LoadAssetAtPath<WeaponDatabase>(DatabasePath);
            c.defaultStarterWeapon = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/EggRogue/Configs/Weapons/Weapon_Gun_Lv1.asset");
            Undo.RegisterCreatedObjectUndo(go, "Add WeaponSelectionManager");
        }
        if (GameObject.Find("WeaponInventoryManager") == null)
        {
            var go = new GameObject("WeaponInventoryManager");
            go.transform.SetParent(parent);
            go.AddComponent<WeaponInventoryManager>();
            Undo.RegisterCreatedObjectUndo(go, "Add WeaponInventoryManager");
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("[WeaponSetup] 已添加 WeaponSelectionManager、WeaponInventoryManager。");
    }

    [MenuItem("EggRogue/添加武器选择面板到 Canvas", false, 117)]
    public static void AddWeaponSelectionPanel()
    {
        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[WeaponSetup] 场景中无 Canvas。");
            return;
        }

        if (canvas.transform.Find("WeaponSelectionPanel") != null)
        {
            Debug.Log("[WeaponSetup] WeaponSelectionPanel 已存在。");
            return;
        }

        var panel = new GameObject("WeaponSelectionPanel");
        panel.transform.SetParent(canvas.transform, false);
        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = panel.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0, 0, 0, 0.8f);

        var container = new GameObject("WeaponButtonContainer");
        container.transform.SetParent(panel.transform, false);
        var containerRt = container.AddComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0.5f, 0.5f);
        containerRt.anchorMax = new Vector2(0.5f, 0.5f);
        containerRt.sizeDelta = new Vector2(400, 100);
        containerRt.anchoredPosition = Vector2.zero;
        var hlg = container.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        hlg.spacing = 20;
        hlg.childAlignment = TextAnchor.MiddleCenter;

        var confirmBtn = new GameObject("ConfirmButton");
        confirmBtn.transform.SetParent(panel.transform, false);
        var btnRt = confirmBtn.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0.2f);
        btnRt.anchorMax = new Vector2(0.5f, 0.2f);
        btnRt.sizeDelta = new Vector2(160, 40);
        btnRt.anchoredPosition = Vector2.zero;
        confirmBtn.AddComponent<UnityEngine.UI.Image>().color = new Color(0.2f, 0.6f, 0.2f);
        var btn = confirmBtn.AddComponent<UnityEngine.UI.Button>();
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(confirmBtn.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var text = textGo.AddComponent<UnityEngine.UI.Text>();
        text.text = "确认";
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        var wsp = panel.AddComponent<WeaponSelectionPanel>();
        wsp.weaponButtonContainer = container.transform;
        wsp.confirmButton = btn;
        wsp.weaponDatabase = AssetDatabase.LoadAssetAtPath<WeaponDatabase>(DatabasePath);

        var um = Object.FindObjectOfType<UIManager>();
        if (um != null)
        {
            um.weaponSelectionPanel = wsp;
        }

        panel.SetActive(false);
        Undo.RegisterCreatedObjectUndo(panel, "Add WeaponSelectionPanel");
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("[WeaponSetup] 已添加 WeaponSelectionPanel 并绑定到 UIManager。");
    }

    private static WeaponData CreateOrLoadWeapon(string assetName, System.Action<WeaponData> configure)
    {
        string path = $"{WeaponFolderPath}/{assetName}.asset";
        var w = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
        if (w == null)
        {
            w = ScriptableObject.CreateInstance<WeaponData>();
            AssetDatabase.CreateAsset(w, path);
        }
        configure(w);
        EditorUtility.SetDirty(w);
        return w;
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
