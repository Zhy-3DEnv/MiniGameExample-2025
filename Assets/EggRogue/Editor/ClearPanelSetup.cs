#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 在当前场景的 Canvas 下创建 ClearPanel（完整通关结算），并绑定到 UIManager。
/// 菜单：EggRogue → 创建 ClearPanel（需先打开 PersistentScene）
/// </summary>
public static class ClearPanelSetup
{
    [MenuItem("EggRogue/创建 ClearPanel")]
    public static void CreateClearPanel()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("ClearPanelSetup: 当前场景未找到 Canvas，请先打开 PersistentScene。");
            return;
        }

        Transform root = canvas.transform;
        if (root.Find("ClearPanel") != null)
        {
            Debug.LogWarning("ClearPanelSetup: 已存在 ClearPanel，请勿重复创建。");
            return;
        }

        GameObject panel = new GameObject("ClearPanel");
        panel.transform.SetParent(root, false);
        var panelRect = panel.AddComponent<RectTransform>();
        AddFullRect(panelRect);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.05f, 0.2f, 0.15f, 0.95f);
        var clear = panel.AddComponent<ClearPanel>();

        GameObject title = CreateText(panel.transform, "TitleText", "恭喜通关", 36);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.85f);
        titleRect.anchorMax = new Vector2(0.5f, 0.85f);
        titleRect.sizeDelta = new Vector2(400, 60);
        titleRect.anchoredPosition = Vector2.zero;
        clear.titleText = title.GetComponent<Text>();

        GameObject levelT = CreateText(panel.transform, "LevelReachedText", "到达关卡: 3", 24);
        var lr = levelT.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0.5f, 0.6f);
        lr.anchorMax = new Vector2(0.5f, 0.6f);
        lr.sizeDelta = new Vector2(300, 40);
        lr.anchoredPosition = Vector2.zero;
        clear.levelReachedText = levelT.GetComponent<Text>();

        GameObject goldT = CreateText(panel.transform, "GoldText", "当前金币: 0", 24);
        var gr = goldT.GetComponent<RectTransform>();
        gr.anchorMin = new Vector2(0.5f, 0.5f);
        gr.anchorMax = new Vector2(0.5f, 0.5f);
        gr.sizeDelta = new Vector2(300, 40);
        gr.anchoredPosition = Vector2.zero;
        clear.goldText = goldT.GetComponent<Text>();

        GameObject returnBtn = CreateButton(panel.transform, "ReturnToMenuButton", "返回主菜单");
        var rb = returnBtn.GetComponent<RectTransform>();
        rb.anchorMin = new Vector2(0.5f, 0.3f);
        rb.anchorMax = new Vector2(0.5f, 0.3f);
        rb.sizeDelta = new Vector2(200, 44);
        rb.anchoredPosition = Vector2.zero;
        clear.returnToMenuButton = returnBtn.GetComponent<Button>();

        GameObject retryBtn = CreateButton(panel.transform, "RetryButton", "再玩一次");
        var rr = retryBtn.GetComponent<RectTransform>();
        rr.anchorMin = new Vector2(0.5f, 0.18f);
        rr.anchorMax = new Vector2(0.5f, 0.18f);
        rr.sizeDelta = new Vector2(200, 44);
        rr.anchoredPosition = Vector2.zero;
        clear.retryButton = retryBtn.GetComponent<Button>();

        panel.SetActive(false);

        UIManager um = Object.FindObjectOfType<UIManager>();
        if (um != null)
        {
            SerializedObject so = new SerializedObject(um);
            SerializedProperty sp = so.FindProperty("clearPanel");
            if (sp != null)
            {
                sp.objectReferenceValue = clear;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        else
        {
            Debug.LogWarning("ClearPanelSetup: 未找到 UIManager，请手动将 ClearPanel 拖到 UIManager 的 Clear Panel 字段。");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = panel;
        Debug.Log("ClearPanelSetup: 已创建 ClearPanel 并绑定 UIManager（若有）。请保存场景。");
    }

    private static GameObject CreateText(Transform parent, string name, string content, int fontSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var text = go.AddComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
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
        img.color = new Color(0.2f, 0.6f, 0.4f);
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
        return go;
    }

    private static void AddFullRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif
