using UnityEngine;
using UnityEditor;
using EggRogue;
using System.Collections.Generic;

/// <summary>
/// 角色模型对齐工具 - 将所有角色模型实例化到场景中，方便统一调整大小、朝向和位置。
/// 调整完成后点击「保存到预制体并清理」将修改写回预制体并删除场景中的预览实例。
/// </summary>
public class CharacterModelAlignmentTool : EditorWindow
{
    private const string PreviewRootName = "CharacterModelPreviewRoot";
    private const string DatabasePath = "Assets/EggRogue/Configs/CharacterDatabase.asset";

    private CharacterDatabase _database;
    private int _columns = 5;
    private float _spacing = 4f;
    private bool _usePlayerPrefab;
    private GameObject _playerPrefab;

    [MenuItem("EggRogue/角色模型对齐工具", false, 120)]
    public static void ShowWindow()
    {
        var win = GetWindow<CharacterModelAlignmentTool>("角色模型对齐");
        win.minSize = new Vector2(320, 200);
    }

    private void OnEnable()
    {
        _database = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(DatabasePath);
        _playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EggRogue/Prefabs/Player.prefab");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("角色模型对齐工具", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        _database = (CharacterDatabase)EditorGUILayout.ObjectField("角色数据库", _database, typeof(CharacterDatabase), false);
        _columns = Mathf.Max(1, EditorGUILayout.IntField("每行数量", _columns));
        _spacing = Mathf.Max(1f, EditorGUILayout.FloatField("间距", _spacing));
        _usePlayerPrefab = EditorGUILayout.Toggle("使用 Player 预制体预览", _usePlayerPrefab);
        if (_usePlayerPrefab)
        {
            _playerPrefab = (GameObject)EditorGUILayout.ObjectField("Player 预制体", _playerPrefab, typeof(GameObject), false);
        }

        EditorGUILayout.Space(8);

        if (GUILayout.Button("生成所有角色预览", GUILayout.Height(28)))
        {
            SpawnAllCharacterPreviews();
        }

        if (IsPreviewRootPresent())
        {
            EditorGUILayout.HelpBox(
                "场景中已有角色预览。在 Hierarchy 中展开各角色 → ModelRoot → 选中模型子物体，在 Inspector 中调整其 Transform（Position/Rotation/Scale）。完成后点击下方按钮保存。",
                MessageType.Info);

            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("保存到预制体并清理", GUILayout.Height(28)))
            {
                SaveToPrefabsAndCleanup();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("仅清理（不保存）", GUILayout.Height(22)))
            {
                CleanupOnly();
            }
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "说明：同一模型（如 EggMan01）被多个角色共用，保存时会以第一个实例的 Transform 为准。\n勾选「使用 Player 预制体预览」可看到与实际游戏一致的结构。",
            MessageType.None);
    }

    private bool IsPreviewRootPresent()
    {
        var root = GameObject.Find(PreviewRootName);
        return root != null;
    }

    private void SpawnAllCharacterPreviews()
    {
        if (_database == null || _database.characters == null)
        {
            Debug.LogError("[CharacterModelAlignmentTool] 未设置角色数据库或数据库为空。");
            return;
        }

        CleanupOnly();

        GameObject root = new GameObject(PreviewRootName);
        Undo.RegisterCreatedObjectUndo(root, "Create CharacterModelPreviewRoot");

        int index = 0;
        foreach (var character in _database.characters)
        {
            if (character == null) continue;

            GameObject modelPrefab = character.characterModelPrefab;
            if (modelPrefab == null)
            {
                modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EggRogue/Prefabs/CharacterModels/EggMan01_Model.prefab");
            }

            GameObject slot;
            Transform modelRoot;

            if (_usePlayerPrefab && _playerPrefab != null)
            {
                slot = (GameObject)PrefabUtility.InstantiatePrefab(_playerPrefab, root.transform);
                slot.name = $"Preview_{character.characterName}";
                modelRoot = slot.transform.Find("ModelRoot");
                if (modelRoot == null)
                {
                    var modelRootGo = new GameObject("ModelRoot");
                    modelRootGo.transform.SetParent(slot.transform);
                    modelRootGo.transform.localPosition = Vector3.zero;
                    modelRootGo.transform.localRotation = Quaternion.identity;
                    modelRootGo.transform.localScale = Vector3.one;
                    modelRoot = modelRootGo.transform;
                }

                for (int i = modelRoot.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(modelRoot.GetChild(i).gameObject);
                }

                var rb = slot.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;
            }
            else
            {
                slot = new GameObject($"Preview_{character.characterName}");
                slot.transform.SetParent(root.transform);
                var modelRootGo = new GameObject("ModelRoot");
                modelRootGo.transform.SetParent(slot.transform);
                modelRootGo.transform.localPosition = Vector3.zero;
                modelRootGo.transform.localRotation = Quaternion.identity;
                modelRootGo.transform.localScale = Vector3.one;
                modelRoot = modelRootGo.transform;
            }

            if (modelPrefab != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, modelRoot);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                model.name = modelPrefab.name;
            }

            int row = index / _columns;
            int col = index % _columns;
            slot.transform.position = new Vector3(col * _spacing, 0f, row * _spacing);

            index++;
        }

        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log($"[CharacterModelAlignmentTool] 已生成 {index} 个角色预览。在 Hierarchy 中选中并调整各模型的 Transform。");
    }

    private void SaveToPrefabsAndCleanup()
    {
        var root = GameObject.Find(PreviewRootName);
        if (root == null)
        {
            Debug.LogWarning("[CharacterModelAlignmentTool] 场景中无预览对象。");
            return;
        }

        var prefabToTransform = new Dictionary<GameObject, Transform>(new ObjectComparer());

        for (int i = 0; i < root.transform.childCount; i++)
        {
            var slot = root.transform.GetChild(i);
            var modelRoot = slot.Find("ModelRoot");
            if (modelRoot == null || modelRoot.childCount == 0) continue;

            Transform model = modelRoot.GetChild(0);
            GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(model.gameObject);
            if (prefab != null && !prefabToTransform.ContainsKey(prefab))
            {
                prefabToTransform[prefab] = model;
            }
        }

        foreach (var kv in prefabToTransform)
        {
            var prefab = kv.Key;
            var instanceTransform = kv.Value;

            string path = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(path)) continue;

            var contents = PrefabUtility.LoadPrefabContents(path);
            if (contents == null) continue;

            contents.transform.localPosition = instanceTransform.localPosition;
            contents.transform.localRotation = instanceTransform.localRotation;
            contents.transform.localScale = instanceTransform.localScale;

            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        int count = prefabToTransform.Count;
        Undo.DestroyObjectImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CharacterModelAlignmentTool] 已将 {count} 个模型预制体的 Transform 保存，并清理预览对象。");
    }

    private void CleanupOnly()
    {
        var root = GameObject.Find(PreviewRootName);
        if (root != null)
        {
            Undo.DestroyObjectImmediate(root);
            Debug.Log("[CharacterModelAlignmentTool] 已清理预览对象。");
        }
    }

    private class ObjectComparer : IEqualityComparer<Object>
    {
        public bool Equals(Object x, Object y) => x != null && y != null && x.GetInstanceID() == y.GetInstanceID();
        public int GetHashCode(Object obj) => obj != null ? obj.GetInstanceID() : 0;
    }
}
