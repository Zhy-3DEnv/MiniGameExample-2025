#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 在当前场景的 Canvas 下创建 FailurePanel，并绑定到 UIManager。
/// 菜单：EggRogue → 创建 FailurePanel（需先打开 PersistentScene）
/// </summary>
public static class FailurePanelSetup
{
    [MenuItem("EggRogue/创建 FailurePanel")]
    public static void CreateFailurePanel()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("FailurePanelSetup: 当前场景未找到 Canvas，请先打开 PersistentScene。");
            return;
        }

        Transform root = canvas.transform;
        if (root.Find("FailurePanel") != null)
        {
            Debug.LogWarning("FailurePanelSetup: 已存在 FailurePanel，请勿重复创建。");
            return;
        }

        GameObject panel = new GameObject("FailurePanel");
        panel.transform.SetParent(root, false);
        var panelRect = panel.AddComponent<RectTransform>();
        AddFullRect(panelRect);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        var failure = panel.AddComponent<FailurePanel>();

        GameObject title = CreateText(panel.transform, "TitleText", "挑战失败", 36);
        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.85f);
        titleRect.anchorMax = new Vector2(0.5f, 0.85f);
        titleRect.sizeDelta = new Vector2(400, 60);
        titleRect.anchoredPosition = Vector2.zero;
        failure.titleText = title.GetComponent<Text>();

        GameObject levelT = CreateText(panel.transform, "LevelReachedText", "到达关卡: 1", 24);
        var lr = levelT.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0.5f, 0.6f);
        lr.anchorMax = new Vector2(0.5f, 0.6f);
        lr.sizeDelta = new Vector2(300, 40);
        lr.anchoredPosition = Vector2.zero;
        failure.levelReachedText = levelT.GetComponent<Text>();

        GameObject goldT = CreateText(panel.transform, "GoldText", "当前金币: 0", 24);
        var gr = goldT.GetComponent<RectTransform>();
        gr.anchorMin = new Vector2(0.5f, 0.5f);
        gr.anchorMax = new Vector2(0.5f, 0.5f);
        gr.sizeDelta = new Vector2(300, 40);
        gr.anchoredPosition = Vector2.zero;
        failure.goldText = goldT.GetComponent<Text>();

        GameObject returnBtn = CreateButton(panel.transform, "ReturnToMenuButton", "返回主菜单");
        var rb = returnBtn.GetComponent<RectTransform>();
        rb.anchorMin = new Vector2(0.5f, 0.3f);
        rb.anchorMax = new Vector2(0.5f, 0.3f);
        rb.sizeDelta = new Vector2(200, 44);
        rb.anchoredPosition = Vector2.zero;
        failure.returnToMenuButton = returnBtn.GetComponent<Button>();

        GameObject retryBtn = CreateButton(panel.transform, "RetryButton", "再试一次");
        var rr = retryBtn.GetComponent<RectTransform>();
        rr.anchorMin = new Vector2(0.5f, 0.18f);
        rr.anchorMax = new Vector2(0.5f, 0.18f);
        rr.sizeDelta = new Vector2(200, 44);
        rr.anchoredPosition = Vector2.zero;
        failure.retryButton = retryBtn.GetComponent<Button>();

        panel.SetActive(false);

        UIManager um = Object.FindObjectOfType<UIManager>();
        if (um != null)
        {
            SerializedObject so = new SerializedObject(um);
            SerializedProperty sp = so.FindProperty("failurePanel");
            if (sp != null)
            {
                sp.objectReferenceValue = failure;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        else
        {
            Debug.LogWarning("FailurePanelSetup: 未找到 UIManager，请手动将 FailurePanel 拖到 UIManager 的 Failure Panel 字段。");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = panel;
        Debug.Log("FailurePanelSetup: 已创建 FailurePanel 并绑定 UIManager（若有）。请保存场景。");
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
        text.font = EggRogue.GameFont.GetDefault();
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
        text.font = EggRogue.GameFont.GetDefault();
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
