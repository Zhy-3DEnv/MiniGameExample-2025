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
            case "baseAttackRange": return "攻击范围";
            case "basePickupRange": return "拾取范围";
            case "baseArmorPercent":     return "护甲";
            case "baseDodgePercent":     return "闪避";
            case "baseRewardBonus":      return "关卡奖励加成";
            case "baseCritRatePercent":  return "暴击率";
            case "baseCritDamageMultiplier": return "暴击伤害";
            case "baseLuck":             return "幸运值";
            case "baseKnockback":        return "击退";
            default:
                if (fieldName.StartsWith("base"))
                    return fieldName.Substring("base".Length);
                return fieldName;
        }
    }

    /// <summary>
    /// 为 CharacterInfoPanel 添加道具区（PurchasedItems 形式）与道具说明弹窗
    /// </summary>
    [MenuItem("EggRogue/Attribute Panel/添加道具区与说明弹窗（PurchasedItems 形式）")]
    private static void AddItemsSectionAndDescriptionPopup()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("CharacterInfoPanel 道具区",
                "请先在层级（Hierarchy）中选中包含 CharacterInfoPanel 的对象。", "确定");
            return;
        }

        CharacterInfoPanel panel = go.GetComponent<CharacterInfoPanel>();
        if (panel == null)
        {
            EditorUtility.DisplayDialog("CharacterInfoPanel 道具区",
                "当前选中的对象上没有 CharacterInfoPanel 组件。", "确定");
            return;
        }

        Undo.RecordObject(panel, "Add Items Section");

        // 1. 道具区容器
        Transform itemsContainer = panel.transform.Find("ItemsContainer");
        if (itemsContainer == null)
        {
            var goContainer = new GameObject("ItemsContainer", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            Undo.RegisterCreatedObjectUndo(goContainer, "Create ItemsContainer");
            goContainer.transform.SetParent(panel.transform, false);

            var rt = goContainer.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0, 120);
            rt.anchoredPosition = new Vector2(0, 20);

            var grid = goContainer.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(44, 44);
            grid.spacing = new Vector2(6, 6);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.UpperLeft;

            var fitter = goContainer.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            itemsContainer = goContainer.transform;
        }
        panel.itemsContainer = itemsContainer;

        // 2. 道具槽预制体（PurchasedItems）
        if (panel.itemSlotPrefab == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EggRogue/UI/PurchasedItems.prefab");
            if (prefab != null)
                panel.itemSlotPrefab = prefab;
        }

        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);

        EditorUtility.DisplayDialog("CharacterInfoPanel 道具区",
            "已添加道具区。\n\n" +
            "请确保已设置 itemSlotPrefab（PurchasedItems.prefab）。点击道具图标时会在图标旁显示说明。", "确定");
    }

    /// <summary>
    /// 从 ItemDatabase 加载前 N 个道具到 editorPreviewItems，用于编辑状态下的布局预览
    /// </summary>
    [MenuItem("EggRogue/Attribute Panel/加载预览道具（编辑状态用）")]
    private static void LoadPreviewItems()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("加载预览道具",
                "请先选中包含 CharacterInfoPanel 的对象。", "确定");
            return;
        }

        CharacterInfoPanel panel = go.GetComponent<CharacterInfoPanel>();
        if (panel == null)
        {
            EditorUtility.DisplayDialog("加载预览道具",
                "当前选中的对象上没有 CharacterInfoPanel 组件。", "确定");
            return;
        }

        var db = AssetDatabase.LoadAssetAtPath<EggRogue.ItemDatabase>("Assets/EggRogue/Configs/ItemDatabase.asset");
        if (db == null || db.items == null || db.items.Length == 0)
        {
            EditorUtility.DisplayDialog("加载预览道具",
                "未找到 ItemDatabase 或其中没有道具数据。", "确定");
            return;
        }

        int count = Mathf.Min(8, db.items.Length);
        var preview = new EggRogue.ItemData[count];
        for (int i = 0; i < count; i++)
            preview[i] = db.items[i];

        Undo.RecordObject(panel, "Load Preview Items");
        panel.editorPreviewItems = preview;
        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);

        EditorUtility.DisplayDialog("加载预览道具",
            $"已加载 {count} 个道具到 editorPreviewItems。\n\n" +
            "编辑状态下将显示这些道具用于调整布局，运行时会显示真实已购买道具。", "确定");
    }
}
#endif

