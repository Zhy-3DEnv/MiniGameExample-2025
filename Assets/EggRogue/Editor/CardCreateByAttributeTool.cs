#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using EggRogue;

/// <summary>
/// 基于角色属性批量创建/更新卡片（5 级），数值参考拾取范围.asset，图标暂用拾取范围卡片的图标。
/// 菜单：EggRogue → 基于属性创建卡片
/// </summary>
public class CardCreateByAttributeTool : EditorWindow
{
    private const string CardsDir = "Assets/EggRogue/Configs/Cards";
    private const string PickupCardPath = "Assets/EggRogue/Configs/Cards/拾取范围.asset";
    private const string DatabasePath = "Assets/EggRogue/Configs/CardDatabase.asset";

    private static readonly string[] AttributeNames = {
        "伤害", "攻速", "生命", "移速", "护甲", "攻击范围", "拾取范围",
        "闪避", "关卡奖励", "暴击率", "暴击伤害", "幸运", "击退"
    };

    // 默认 5 级数值（参考拾取范围：0.5, 1, 1.5, 2, 3），其他属性按比例
    private static readonly float[][] DefaultValues = new[]
    {
        new[] { 5f, 10f, 15f, 20f, 30f },       // 伤害
        new[] { 0.5f, 1f, 1.5f, 2f, 3f },      // 攻速
        new[] { 20f, 40f, 60f, 80f, 120f },    // 生命
        new[] { 0.5f, 1f, 1.5f, 2f, 3f },      // 移速
        new[] { 5f, 10f, 15f, 20f, 30f },      // 护甲（百分比）
        new[] { 1f, 2f, 3f, 4f, 6f },          // 攻击范围
        new[] { 0.5f, 1f, 1.5f, 2f, 3f },      // 拾取范围
        new[] { 5f, 10f, 15f, 20f, 30f },      // 闪避（百分比）
        new[] { 5f, 10f, 15f, 20f, 30f },      // 关卡奖励（整数，用 float 存再转 int）
        new[] { 5f, 10f, 15f, 20f, 30f },      // 暴击率（百分比）
        new[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f }, // 暴击伤害（倍率加算，如 0.2 即 1.2→1.4）
        new[] { 2f, 4f, 6f, 8f, 12f },         // 幸运
        new[] { 0.5f, 1f, 1.5f, 2f, 3f },      // 击退
    };

    private float[][] _values;
    private bool[] _selected; // 勾选则参与创建/更新
    private Vector2 _scrollPos;
    private Sprite _iconPlaceholder;

    [MenuItem("EggRogue/基于属性创建卡片")]
    public static void ShowWindow()
    {
        var win = GetWindow<CardCreateByAttributeTool>("按属性创建卡片");
        win.minSize = new Vector2(420, 380);
    }

    private void OnEnable()
    {
        if (_values == null || _values.Length != DefaultValues.Length)
        {
            _values = new float[DefaultValues.Length][];
            for (int i = 0; i < DefaultValues.Length; i++)
            {
                _values[i] = new float[5];
                for (int j = 0; j < 5; j++)
                    _values[i][j] = DefaultValues[i][j];
            }
        }
        if (_selected == null || _selected.Length != AttributeNames.Length)
        {
            _selected = new bool[AttributeNames.Length];
            for (int i = 0; i < _selected.Length; i++)
                _selected[i] = true;
        }
        var pickupCard = AssetDatabase.LoadAssetAtPath<CardData>(PickupCardPath);
        if (pickupCard != null && pickupCard.icon != null)
            _iconPlaceholder = pickupCard.icon;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("基于属性创建 5 级卡片", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "为每种属性生成一张卡片（5 个星级），共 13 种属性，数值可编辑。图标暂用拾取范围卡片的图标。勾选需要创建/更新的项后点击下方按钮。",
            MessageType.Info);

        if (_iconPlaceholder == null)
        {
            var pickupCard = AssetDatabase.LoadAssetAtPath<CardData>(PickupCardPath);
            if (pickupCard != null) _iconPlaceholder = pickupCard.icon;
            if (_iconPlaceholder == null)
                EditorGUILayout.HelpBox("未找到拾取范围.asset 或该卡片无图标，新卡片图标将为空。", MessageType.Warning);
        }

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        for (int attr = 0; attr < AttributeNames.Length; attr++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            _selected[attr] = EditorGUILayout.Toggle(_selected[attr], GUILayout.Width(18));
            EditorGUILayout.LabelField(AttributeNames[attr], EditorStyles.boldLabel, GUILayout.Width(64));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(22);
            for (int star = 0; star < 5; star++)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(60));
                EditorGUILayout.LabelField($"★{star + 1}", GUILayout.Width(24));
                _values[attr][star] = EditorGUILayout.FloatField(_values[attr][star], GUILayout.Width(56));
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        if (GUILayout.Button("一键创建/更新全部属性卡片", GUILayout.Height(32)))
        {
            CreateOrUpdateAllCards();
        }
        EditorGUILayout.Space(4);
        if (GUILayout.Button("恢复默认数值（参考拾取范围）", GUILayout.Height(22)))
        {
            for (int i = 0; i < DefaultValues.Length; i++)
                for (int j = 0; j < 5; j++)
                    _values[i][j] = DefaultValues[i][j];
        }
    }

    private void CreateOrUpdateAllCards()
    {
        if (!AssetDatabase.IsValidFolder("Assets/EggRogue/Configs/Cards"))
        {
            Debug.LogError("CardCreateByAttributeTool: 请先创建 Cards 目录（如通过 EggRogue/创建 CardDatabase 与默认卡片）。");
            return;
        }

        var pickupCard = AssetDatabase.LoadAssetAtPath<CardData>(PickupCardPath);
        Sprite icon = (pickupCard != null && pickupCard.icon != null) ? pickupCard.icon : null;

        int created = 0;
        int updated = 0;

        for (int attr = 0; attr < AttributeNames.Length; attr++)
        {
            if (!_selected[attr])
                continue;

            string name = AttributeNames[attr];
            string path = $"{CardsDir}/{name}.asset";
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);

            if (card == null)
            {
                card = ScriptableObject.CreateInstance<CardData>();
                card.cardTypeId = name;
                card.cardName = name;
                card.description = name;
                card.icon = icon;
                card.starBonuses = new CardStarBonus[5];
                for (int i = 0; i < 5; i++)
                    card.starBonuses[i] = BuildStarBonus(attr, i + 1, _values[attr][i]);
                AssetDatabase.CreateAsset(card, path);
                created++;
            }
            else
            {
                if (card.starBonuses == null || card.starBonuses.Length != 5)
                    card.starBonuses = new CardStarBonus[5];
                if (icon != null && card.icon == null)
                    card.icon = icon;
                for (int i = 0; i < 5; i++)
                    card.starBonuses[i] = BuildStarBonus(attr, i + 1, _values[attr][i]);
                EditorUtility.SetDirty(card);
                updated++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AddCardsToDatabaseIfNeeded();
        int skipped = 0;
        for (int i = 0; i < _selected.Length; i++)
            if (!_selected[i]) skipped++;
        string msg = $"新建 {created} 张，更新 {updated} 张。";
        if (skipped > 0) msg += $"\n已跳过 {skipped} 项（未勾选）。";
        Debug.Log($"CardCreateByAttributeTool: 完成。{msg}");
        EditorUtility.DisplayDialog("完成", msg, "确定");
    }

    private static CardStarBonus BuildStarBonus(int attributeIndex, int star, float value)
    {
        var b = new CardStarBonus
        {
            star = star,
            damageBonus = 0f,
            fireRateBonus = 0f,
            maxHealthBonus = 0f,
            moveSpeedBonus = 0f,
            armorPercentBonus = 0f,
            attackRangeBonus = 0f,
            pickupRangeBonus = 0f,
            dodgePercentBonus = 0f,
            rewardBonus = 0,
            critRatePercentBonus = 0f,
            critDamageMultiplierBonus = 0f,
            luckBonus = 0f,
            knockbackBonus = 0f
        };

        switch (attributeIndex)
        {
            case 0:  b.damageBonus = value; break;
            case 1:  b.fireRateBonus = value; break;
            case 2:  b.maxHealthBonus = value; break;
            case 3:  b.moveSpeedBonus = value; break;
            case 4:  b.armorPercentBonus = value; break;
            case 5:  b.attackRangeBonus = value; break;
            case 6:  b.pickupRangeBonus = value; break;
            case 7:  b.dodgePercentBonus = value; break;
            case 8:  b.rewardBonus = Mathf.RoundToInt(value); break;
            case 9:  b.critRatePercentBonus = value; break;
            case 10: b.critDamageMultiplierBonus = value; break;
            case 11: b.luckBonus = value; break;
            case 12: b.knockbackBonus = value; break;
        }
        return b;
    }

    private static void AddCardsToDatabaseIfNeeded()
    {
        var db = AssetDatabase.LoadAssetAtPath<CardDatabase>(DatabasePath);
        if (db == null) return;

        var list = new System.Collections.Generic.List<CardData>(db.allCards ?? new CardData[0]);
        bool changed = false;
        foreach (string name in AttributeNames)
        {
            string path = $"{CardsDir}/{name}.asset";
            var card = AssetDatabase.LoadAssetAtPath<CardData>(path);
            if (card != null && !list.Contains(card))
            {
                list.Add(card);
                changed = true;
            }
        }
        if (changed)
        {
            db.allCards = list.ToArray();
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log("CardCreateByAttributeTool: 已将新卡片加入 CardDatabase。");
        }
    }
}
#endif
