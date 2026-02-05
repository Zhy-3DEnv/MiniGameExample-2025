#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public static class CharacterInfoPanelAutoSetup
{
    /// <summary>
    /// 新版（自动生成模式）：
    /// 在 attributesContainer 下，根据 CharacterData 的 float 字段生成属性行。
    /// </summary>
    [MenuItem("EggRogue/Attribute Panel/一键生成属性行（基于 CharacterData）")]
    private static void GenerateAttributeRows()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("CharacterInfoPanel 自动生成",
                "请先在层级（Hierarchy）中选中一个包含 CharacterInfoPanel 组件的对象。", "确定");
            return;
        }

        CharacterInfoPanel panel = go.GetComponent<CharacterInfoPanel>();
        if (panel == null)
        {
            EditorUtility.DisplayDialog("CharacterInfoPanel 自动生成",
                "当前选中的对象上没有 CharacterInfoPanel 组件。\n\n请选中包含 CharacterInfoPanel 的 UI 根节点再执行此命令。", "确定");
            return;
        }

        if (panel.attributesContainer == null || panel.attributeRowPrefab == null)
        {
            EditorUtility.DisplayDialog("CharacterInfoPanel 自动生成",
                "CharacterInfoPanel 的 attributesContainer 或 attributeRowPrefab 未设置。\n\n" +
                "请先在 Inspector 上为 CharacterInfoPanel 设置：\n" +
                "- Attributes Container（属性行容器，例如 VerticalLayoutGroup）\n" +
                "- Attribute Row Prefab（包含 NameText / ValueText 的预制体）",
                "确定");
            return;
        }

        CharacterStats stats = Object.FindObjectOfType<CharacterStats>();
        if (stats == null || stats.characterData == null)
        {
            EditorUtility.DisplayDialog("CharacterInfoPanel 自动生成",
                "场景中未找到 CharacterStats，或 CharacterStats 的 characterData 未设置。\n\n" +
                "请确保：\n- 场景中存在角色对象并挂载 CharacterStats\n- 且 CharacterStats.characterData 已正确指定。",
                "确定");
            return;
        }

        var data = stats.characterData;
        var dataType = data.GetType();
        var fields = dataType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // 如果已有子节点，询问是否清空后重建
        if (panel.attributesContainer.childCount > 0)
        {
            bool rebuild = EditorUtility.DisplayDialog(
                "CharacterInfoPanel 自动生成",
                "检测到 attributesContainer 下已经存在子节点。\n\n是否删除现有子节点后重新生成？",
                "重新生成", "取消");

            if (!rebuild)
            {
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(panel.attributesContainer.gameObject, "Clear Attribute Rows");
            for (int i = panel.attributesContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = panel.attributesContainer.GetChild(i);
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        Undo.RegisterFullObjectHierarchyUndo(panel.attributesContainer.gameObject, "Generate Attribute Rows");

        foreach (var field in fields)
        {
            if (field.FieldType != typeof(float))
                continue;

            string fieldName = field.Name; // 例如 baseDamage

            // 生成一行 UI（优先保持 prefab 连接）
            GameObject rowObj;
            if (PrefabUtility.IsPartOfPrefabAsset(panel.attributeRowPrefab))
            {
                rowObj = (GameObject)PrefabUtility.InstantiatePrefab(panel.attributeRowPrefab, panel.attributesContainer);
            }
            else
            {
                rowObj = Object.Instantiate(panel.attributeRowPrefab, panel.attributesContainer);
                rowObj.name = panel.attributeRowPrefab.name;
            }

            rowObj.name = $"Attr_{fieldName}";

            Text nameText = rowObj.transform.Find("NameText")?.GetComponent<Text>();
            Text valueText = rowObj.transform.Find("ValueText")?.GetComponent<Text>();

            string displayName = GetDisplayNameForField(fieldName);

            if (nameText != null)
            {
                nameText.text = displayName;
            }

            if (valueText != null)
            {
                // 这里的数值只是占位，运行时 CharacterInfoPanel 会根据实际数值刷新
                float baseValue = (float)field.GetValue(data);
                valueText.text = baseValue.ToString("F1");
            }
        }

        EditorUtility.SetDirty(panel.attributesContainer.gameObject);
        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);

        EditorUtility.DisplayDialog("CharacterInfoPanel 自动生成",
            "属性行已根据 CharacterData 自动生成完成。\n\n" +
            "运行时 CharacterInfoPanel 会自动更新数值显示。", "确定");
    }

    /// <summary>
    /// 与 CharacterInfoPanel 中的映射规则保持一致：
    /// baseDamage -> 伤害 等。
    /// </summary>
    private static string GetDisplayNameForField(string fieldName)
    {
        switch (fieldName)
        {
            case "baseDamage":      return "伤害";
            case "baseFireRate":    return "攻击速度";
            case "baseMaxHealth":   return "最大生命值";
            case "baseMoveSpeed":   return "移动速度";
            case "baseBulletSpeed": return "子弹速度";
            case "baseAttackRange": return "攻击范围";
            case "basePickupRange":  return "拾取范围";
            default:
                if (fieldName.StartsWith("base"))
                    return fieldName.Substring("base".Length);
                return fieldName;
        }
    }
}

/// <summary>
/// 旧版 Text 自动生成与绑定工具：
/// - 仍然使用 CharacterInfoPanel 上的旧字段（damageText / fireRateText ...）
/// - 根据 CharacterData 中存在的基础属性，自动在当前面板下创建 Text，并写入到对应引用
/// - 适合你继续使用“旧版模式”，但不想手动创建和拖引用
/// </summary>
public static class CharacterInfoPanelLegacyTextSetup
{
    [MenuItem("EggRogue/Attribute Panel/一键生成旧版 Text 并自动绑定")]
    private static void GenerateLegacyTexts()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("CharacterInfoPanel 旧版 Text 自动生成",
                "请先在层级（Hierarchy）中选中一个包含 CharacterInfoPanel 组件的对象。", "确定");
            return;
        }

        CharacterInfoPanel panel = go.GetComponent<CharacterInfoPanel>();
        if (panel == null)
        {
            EditorUtility.DisplayDialog("CharacterInfoPanel 旧版 Text 自动生成",
                "当前选中的对象上没有 CharacterInfoPanel 组件。\n\n请选中包含 CharacterInfoPanel 的 UI 根节点再执行此命令。", "确定");
            return;
        }

        CharacterStats stats = Object.FindObjectOfType<CharacterStats>();
        if (stats == null || stats.characterData == null)
        {
            EditorUtility.DisplayDialog("CharacterInfoPanel 旧版 Text 自动生成",
                "场景中未找到 CharacterStats，或 CharacterStats 的 characterData 未设置。\n\n" +
                "请确保：\n- 场景中存在角色对象并挂载 CharacterStats\n- 且 CharacterStats.characterData 已正确指定。",
                "确定");
            return;
        }

        // 记录 Undo，方便撤销
        Undo.RecordObject(panel, "Setup CharacterInfoPanel Legacy Texts");

        // 内部辅助：确保有 AttributePanelContainers 作为 Text 的父节点（保留旧名以兼容场景）
        Transform EnsureContainer()
        {
            Transform container = panel.transform.Find("AttributePanelContainers");
            if (container == null)
            {
                GameObject goContainer = new GameObject("AttributePanelContainers", typeof(RectTransform), typeof(UnityEngine.UI.VerticalLayoutGroup));
                Undo.RegisterCreatedObjectUndo(goContainer, "Create AttributePanelContainers");

                goContainer.transform.SetParent(panel.transform, false);
                goContainer.transform.localScale = Vector3.one;

                RectTransform rect = goContainer.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.offsetMin = new Vector2(20f, 20f);
                rect.offsetMax = new Vector2(-20f, -80f); // 留出顶部/底部一点空白

                var layout = goContainer.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;
                layout.spacing = 5f;

                container = goContainer.transform;
            }

            // 顺便把 CharacterInfoPanel 上的 attributesContainer 指向它（方便你如果之后用新模式）
            if (panel.attributesContainer == null)
            {
                panel.attributesContainer = container;
            }

            return container;
        }

        Transform containerTransform = EnsureContainer();

        // 内部辅助：如果某个 Text 引用为空，则按名字在子节点中查找，没有就创建一个
        Text EnsureText(ref Text field, string goName, string defaultText)
        {
            if (field != null && field.gameObject != null)
            {
                return field;
            }

            // 先尝试在容器子节点中查找
            Transform found = containerTransform.Find(goName);
            Text textComp = found != null ? found.GetComponent<Text>() : null;

            if (textComp == null)
            {
                // 创建新的 Text 对象
                GameObject textGO = new GameObject(goName, typeof(RectTransform), typeof(Text));
                Undo.RegisterCreatedObjectUndo(textGO, "Create Attribute Text");

                textGO.transform.SetParent(containerTransform, false);
                textGO.transform.localScale = Vector3.one;

                RectTransform rect = textGO.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(300, 30);

                textComp = textGO.GetComponent<Text>();
                textComp.text = defaultText;
                textComp.alignment = TextAnchor.MiddleLeft;
                textComp.font = EggRogue.GameFont.GetDefault();
                textComp.color = Color.white;
            }

            field = textComp;
            return textComp;
        }

        // 内部辅助：如果关闭按钮为空，则创建一个简单的 CloseButton，并绑定到 panel.closeButton
        void EnsureCloseButton()
        {
            if (panel.closeButton != null && panel.closeButton.gameObject != null)
                return;

            // 优先查找是否已有 CloseButton 物体
            Transform found = panel.transform.Find("CloseButton");
            Button btn = found != null ? found.GetComponent<Button>() : null;

            if (btn == null)
            {
                // 创建新的 Button（Image + Button + Text）
                GameObject goBtn = new GameObject("CloseButton", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(Button));
                Undo.RegisterCreatedObjectUndo(goBtn, "Create Close Button");

                goBtn.transform.SetParent(panel.transform, false);
                goBtn.transform.localScale = Vector3.one;

                RectTransform rect = goBtn.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(100, 40);
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                rect.anchoredPosition = new Vector2(-20f, -20f); // 面板右上角稍微往内一点

                var img = goBtn.GetComponent<UnityEngine.UI.Image>();
                img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

                btn = goBtn.GetComponent<Button>();

                // 在 Button 下创建一个 Text 作为按钮文字
                GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
                Undo.RegisterCreatedObjectUndo(textGO, "Create Close Button Text");

                textGO.transform.SetParent(goBtn.transform, false);
                RectTransform textRect = textGO.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;

                Text textComp = textGO.GetComponent<Text>();
                textComp.text = "关闭";
                textComp.alignment = TextAnchor.MiddleCenter;
                textComp.color = Color.white;
                textComp.font = EggRogue.GameFont.GetDefault();
                textComp.resizeTextForBestFit = true;
            }

            panel.closeButton = btn;
        }

        // 这里根据当前 CharacterData 中“已存在”的基础属性，决定生成哪些 Text
        var data = stats.characterData;

        // 伤害
        if (HasField(data, "baseDamage"))
        {
            EnsureText(ref panel.damageText, "DamageText", "伤害: 0");
        }

        // 攻击速度
        if (HasField(data, "baseFireRate"))
        {
            EnsureText(ref panel.fireRateText, "FireRateText", "攻击速度: 0");
        }

        // 最大生命值
        if (HasField(data, "baseMaxHealth"))
        {
            EnsureText(ref panel.maxHealthText, "MaxHealthText", "最大生命值: 0");
        }

        // 移动速度
        if (HasField(data, "baseMoveSpeed"))
        {
            EnsureText(ref panel.moveSpeedText, "MoveSpeedText", "移动速度: 0");
        }

        // 子弹速度
        if (HasField(data, "baseBulletSpeed"))
        {
            EnsureText(ref panel.bulletSpeedText, "BulletSpeedText", "子弹速度: 0");
        }

        // 攻击范围
        if (HasField(data, "baseAttackRange"))
        {
            EnsureText(ref panel.attackRangeText, "AttackRangeText", "攻击范围: 0");
        }

        // 关闭按钮
        EnsureCloseButton();

        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);

        EditorUtility.DisplayDialog("CharacterInfoPanel 旧版 Text 自动生成",
            "已根据 CharacterData 中存在的基础属性，在当前面板下自动创建并绑定旧版 Text。\n\n" +
            "你可以在场景中调整这些 Text 的位置与样式，运行时 CharacterInfoPanel 会自动填充值。", "确定");
    }

    private static bool HasField(EggRogue.CharacterData data, string fieldName)
    {
        var type = data.GetType();
        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
        return field != null;
    }
}
#endif

