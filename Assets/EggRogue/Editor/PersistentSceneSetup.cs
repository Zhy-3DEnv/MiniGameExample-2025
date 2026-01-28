#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using EggRogue;

/// <summary>
/// 创建 PersistentScene 并搭建 PersistentRoot、Managers、UIRoot 结构。
/// 菜单：EggRogue → 创建 PersistentScene
/// </summary>
public static class PersistentSceneSetup
{
    private const string SceneDir = "Assets/EggRogue/Scenes";
    private const string ScenePath = SceneDir + "/PersistentScene.unity";

    [MenuItem("EggRogue/创建 PersistentScene")]
    public static void CreatePersistentScene()
    {
        if (!AssetDatabase.IsValidFolder("Assets/EggRogue"))
        {
            Debug.LogError("PersistentSceneSetup: Assets/EggRogue 不存在。");
            return;
        }
        if (!AssetDatabase.IsValidFolder("Assets/EggRogue/Scenes"))
        {
            AssetDatabase.CreateFolder("Assets/EggRogue", "Scenes");
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject root = new GameObject("PersistentRoot");
        var bootstrap = root.AddComponent<PersistentBootstrap>();
        bootstrap.persistentSceneName = "PersistentScene";
        bootstrap.mainMenuSceneName = "MainMenu";

        GameObject managers = new GameObject("Managers");
        managers.transform.SetParent(root.transform, false);

        CreateManagerChild(managers.transform, "GameManager", typeof(GameManager));
        GameObject umGo = CreateManagerChild(managers.transform, "UIManager", typeof(UIManager));
        CreateManagerChild(managers.transform, "GoldManager", typeof(GoldManager));
        CreateManagerChild(managers.transform, "CSVConfigManager", typeof(CSVConfigManager));
        CreateManagerChild(managers.transform, "LevelManager", typeof(LevelManager));
        CreateManagerChild(managers.transform, "CardManager", typeof(CardManager));

        GameObject uiRoot = new GameObject("UIRoot");
        uiRoot.transform.SetParent(root.transform, false);

        GameObject canvas = new GameObject("Canvas");
        canvas.transform.SetParent(uiRoot.transform, false);
        Canvas c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvas.AddComponent<GraphicRaycaster>();

        RectTransform rt = canvas.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        GameObject es = new GameObject("EventSystem");
        es.transform.SetParent(uiRoot.transform, false);
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        GameObject mainPanel = new GameObject("MainMenuPanel");
        mainPanel.transform.SetParent(canvas.transform, false);
        AddFullRect(mainPanel.AddComponent<RectTransform>());
        var mainMenu = mainPanel.AddComponent<MainMenuPanel>();
        GameObject startBtn = CreateButton(mainPanel.transform, "StartGameButton", "开始游戏");
        mainMenu.startGameButton = startBtn.GetComponent<Button>();

        GameObject hudPanel = new GameObject("GameHudPanel");
        hudPanel.transform.SetParent(canvas.transform, false);
        AddFullRect(hudPanel.AddComponent<RectTransform>());
        var hud = hudPanel.AddComponent<GameHudPanel>();
        GameObject returnBtn = CreateButton(hudPanel.transform, "ReturnToMenuButton", "返回主菜单");
        hud.returnToMenuButton = returnBtn.GetComponent<Button>();

        var um = umGo.GetComponent<UIManager>();
        if (um != null)
        {
            um.mainMenuPanel = mainMenu;
            um.gameHudPanel = hud;
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddPersistentSceneToBuild();
        Debug.Log($"PersistentSceneSetup: 已创建并保存 {ScenePath}，已加入 Build 首场景。请按《场景迁移说明》迁移 MainMenu/GameScene。");
    }

    private static GameObject CreateManagerChild(Transform parent, string name, System.Type type)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent(type);
        return go;
    }

    private static GameObject CreateButton(Transform parent, string name, string label)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(160, 40);
        rect.anchoredPosition = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.5f, 0.9f);
        go.AddComponent<Button>();

        GameObject child = new GameObject("Text");
        child.transform.SetParent(go.transform, false);
        var textRect = child.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var text = child.AddComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.fontSize = 18;
        if (UnityEngine.Font.GetOSInstalledFontNames().Length > 0)
            text.font = UnityEngine.Font.CreateDynamicFontFromOSFont(UnityEngine.Font.GetOSInstalledFontNames()[0], 18);

        return go;
    }

    private static void AddFullRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void AddPersistentSceneToBuild()
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].path == ScenePath)
            {
                list.RemoveAt(i);
                break;
            }
        }
        list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
#endif
