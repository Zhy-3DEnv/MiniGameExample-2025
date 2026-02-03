using UnityEngine;
using UnityEditor;
using EggRogue;

/// <summary>
/// 武器对齐工具 - 在场景中实例化角色携带 6 把武器的预览，方便调整武器模型位置。
/// 调整完成后保存回 PlayerSpawner 的挂点位置配置或武器预制体。
/// </summary>
public class WeaponAlignmentTool : EditorWindow
{
    private const string PreviewRootName = "WeaponAlignmentPreview";

    private GameObject _playerPrefab;
    private GameObject _characterModel;
    private WeaponDatabase _weaponDatabase;
    private WeaponData[] _selectedWeapons = new WeaponData[6];

    [MenuItem("EggRogue/武器对齐工具", false, 121)]
    public static void ShowWindow()
    {
        var win = GetWindow<WeaponAlignmentTool>("武器对齐");
        win.minSize = new Vector2(360, 450);
    }

    private void OnEnable()
    {
        _playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EggRogue/Prefabs/Player.prefab");
        _weaponDatabase = AssetDatabase.LoadAssetAtPath<WeaponDatabase>("Assets/EggRogue/Configs/WeaponDatabase.asset");
        _characterModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EggRogue/Prefabs/CharacterModels/EggMan01_Model.prefab");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("武器对齐工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        _playerPrefab = (GameObject)EditorGUILayout.ObjectField("Player 预制体", _playerPrefab, typeof(GameObject), false);
        _characterModel = (GameObject)EditorGUILayout.ObjectField("角色模型", _characterModel, typeof(GameObject), false);
        _weaponDatabase = (WeaponDatabase)EditorGUILayout.ObjectField("武器数据库", _weaponDatabase, typeof(WeaponDatabase), false);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("选择武器（最多 6 把）", EditorStyles.boldLabel);

        for (int i = 0; i < 6; i++)
        {
            _selectedWeapons[i] = (WeaponData)EditorGUILayout.ObjectField(
                $"槽位 {i}", _selectedWeapons[i], typeof(WeaponData), false);
        }

        EditorGUILayout.Space(8);

        if (GUILayout.Button("生成武器预览", GUILayout.Height(28)))
        {
            SpawnWeaponPreview();
        }

        if (IsPreviewPresent())
        {
            EditorGUILayout.HelpBox(
                "场景中已有预览对象。在 Hierarchy 中展开 WeaponAlignmentPreview → WeaponSlot → WeaponSlot_X → 选中武器模型，在 Inspector 中调整 Transform。",
                MessageType.Info);

            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("保存挂点位置到 PlayerSpawner", GUILayout.Height(28)))
            {
                SaveSlotPositionsToCode();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("仅清理（不保存）", GUILayout.Height(22)))
            {
                CleanupOnly();
            }
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "说明：6 把武器左右各 3 把，按 30° 间隔（半径 1.0f）。\n右侧：-120°, -90°(中间对齐-X), -60° | 左侧：60°, 90°(中间对齐X), 120°\n调整各 WeaponSlot_X 的位置/旋转后，保存会更新到 PlayerSpawner 中。",
            MessageType.None);
    }

    private bool IsPreviewPresent()
    {
        return GameObject.Find(PreviewRootName) != null;
    }

    private void SpawnWeaponPreview()
    {
        if (_playerPrefab == null)
        {
            Debug.LogError("[WeaponAlignmentTool] 请先设置 Player 预制体。");
            return;
        }

        CleanupOnly();

        var root = new GameObject(PreviewRootName);
        Undo.RegisterCreatedObjectUndo(root, "Create WeaponAlignmentPreview");

        var player = (GameObject)PrefabUtility.InstantiatePrefab(_playerPrefab, root.transform);
        player.name = "PlayerPreview";

        var modelRoot = player.transform.Find("ModelRoot");
        if (modelRoot == null)
        {
            var go = new GameObject("ModelRoot");
            go.transform.SetParent(player.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            modelRoot = go.transform;
        }

        for (int i = modelRoot.childCount - 1; i >= 0; i--)
            DestroyImmediate(modelRoot.GetChild(i).gameObject);

        if (_characterModel != null)
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(_characterModel, modelRoot);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
        }

        var weaponSlotRoot = player.transform.Find("WeaponSlot");
        if (weaponSlotRoot == null)
        {
            var go = new GameObject("WeaponSlot");
            go.transform.SetParent(player.transform);
            go.transform.localPosition = Vector3.zero; // 对齐到角色中心高度
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            weaponSlotRoot = go.transform;
        }
        else
        {
            // 更新已有的 WeaponSlot 位置
            weaponSlotRoot.localPosition = Vector3.zero;
        }

        for (int i = weaponSlotRoot.childCount - 1; i >= 0; i--)
            DestroyImmediate(weaponSlotRoot.GetChild(i).gameObject);

        for (int i = 0; i < 6; i++)
        {
            var weapon = _selectedWeapons[i];
            if (weapon == null) continue;

            var slot = new GameObject($"WeaponSlot_{i}");
            slot.transform.SetParent(weaponSlotRoot);
            slot.transform.localPosition = GetDefaultSlotPosition(i);
            slot.transform.localRotation = GetDefaultSlotRotation(i);
            slot.transform.localScale = Vector3.one;

            if (weapon.modelPrefab != null)
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(weapon.modelPrefab, slot.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                model.name = weapon.weaponName;
            }
            else
            {
                var placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                placeholder.transform.SetParent(slot.transform);
                placeholder.transform.localPosition = Vector3.zero;
                placeholder.transform.localScale = new Vector3(0.2f, 0.2f, 0.5f);
                placeholder.name = $"{weapon.weaponName}_Placeholder";
            }
        }

        foreach (var c in player.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (c != null) c.enabled = false;
        }
        var rb = player.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log("[WeaponAlignmentTool] 已生成武器预览。在 Hierarchy 中展开 WeaponSlot，调整各 WeaponSlot_X 的 Position。");
    }

    private Vector3 GetDefaultSlotPosition(int index)
    {
        // 左右各 3 把武器，按 30° 间隔围绕角色
        // 右侧：-60°, -30°, 0° (槽位 0, 1, 2)
        // 左侧：0°, 30°, 60° (槽位 3, 4, 5)
        float radius = 1.0f;
        float angleDeg = GetWeaponAngle(index);
        float angleRad = angleDeg * Mathf.Deg2Rad;
        
        float x = Mathf.Sin(angleRad) * radius;
        float z = Mathf.Cos(angleRad) * radius;
        
        return new Vector3(x, 0f, z);
    }

    private Quaternion GetDefaultSlotRotation(int index)
    {
        float angleDeg = GetWeaponAngle(index);
        return Quaternion.Euler(0f, angleDeg, 0f);
    }

    private float GetWeaponAngle(int index)
    {
        // 左右各 3 把，按 30° 间隔，中间那把对齐 X / -X
        // 右侧：槽位 0: -120° (右后外), 槽位 1: -90° (右中，对齐 -X), 槽位 2: -60° (右前外)
        // 左侧：槽位 3: 60° (左前外), 槽位 4: 90° (左中，对齐 X), 槽位 5: 120° (左后外)
        switch (index)
        {
            case 0: return -120f; // 右后外
            case 1: return -90f;  // 右中，对齐 -X
            case 2: return -60f;  // 右前外
            case 3: return 60f;   // 左前外
            case 4: return 90f;   // 左中，对齐 X
            case 5: return 120f; // 左后外
            default: return 0f;
        }
    }

    private void SaveSlotPositionsToCode()
    {
        var root = GameObject.Find(PreviewRootName);
        if (root == null)
        {
            Debug.LogWarning("[WeaponAlignmentTool] 未找到预览对象。");
            return;
        }

        var player = root.transform.Find("PlayerPreview");
        if (player == null) return;

        var weaponSlotRoot = player.Find("WeaponSlot");
        if (weaponSlotRoot == null) return;

        string posCode = "// 复制以下代码到 PlayerSpawner.GetWeaponSlotPosition():\n";
        posCode += "switch (index)\n{\n";

        string rotCode = "\n// 复制以下代码到 PlayerSpawner.GetWeaponSlotRotation():\n";
        rotCode += "switch (index)\n{\n";

        for (int i = 0; i < 6; i++)
        {
            var slot = weaponSlotRoot.Find($"WeaponSlot_{i}");
            if (slot != null)
            {
                var pos = slot.localPosition;
                var rot = slot.localEulerAngles;
                posCode += $"    case {i}: return new Vector3({pos.x:F2}f, {pos.y:F2}f, {pos.z:F2}f);\n";
                rotCode += $"    case {i}: return Quaternion.Euler({rot.x:F2}f, {rot.y:F2}f, {rot.z:F2}f);\n";
            }
        }
        posCode += "    default: return Vector3.zero;\n}\n";
        rotCode += "    default: return Quaternion.identity;\n}\n";

        string fullCode = posCode + rotCode;
        Debug.Log($"[WeaponAlignmentTool] 挂点代码：\n{fullCode}");
        EditorGUIUtility.systemCopyBuffer = fullCode;
        EditorUtility.DisplayDialog("保存成功", "挂点位置和旋转代码已复制到剪贴板，请粘贴到 PlayerSpawner 中。", "确定");
    }

    private void CleanupOnly()
    {
        var root = GameObject.Find(PreviewRootName);
        if (root != null)
        {
            Undo.DestroyObjectImmediate(root);
            Debug.Log("[WeaponAlignmentTool] 已清理预览对象。");
        }
    }
}
