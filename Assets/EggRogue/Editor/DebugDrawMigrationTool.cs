#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using EggRogue;

/// <summary>
/// 将玩家上的旧调试绘制组件替换为统一的 DebugDrawController。
/// </summary>
public static class DebugDrawMigrationTool
{
    [MenuItem("EggRogue/调试/将选中对象迁移为 DebugDrawController")]
    public static void MigrateSelectedToDebugDrawController()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("迁移", "请先选中玩家对象。", "确定");
            return;
        }

        int removed = 0;
        var arr = go.GetComponent<AttackRangeDebugRegistration>();
        if (arr != null) { Object.DestroyImmediate(arr); removed++; }
        var arv = go.GetComponent<AttackRangeRuntimeVisualizer>();
        if (arv != null) { Object.DestroyImmediate(arv); removed++; }
        var wrv = go.GetComponent<WeaponRangeRuntimeVisualizer>();
        if (wrv != null) { Object.DestroyImmediate(wrv); removed++; }
        var prv = go.GetComponent<PickupRangeRuntimeVisualizer>();
        if (prv != null) { Object.DestroyImmediate(prv); removed++; }

        if (go.GetComponent<DebugDrawController>() == null)
            go.AddComponent<DebugDrawController>();

        EditorUtility.SetDirty(go);
        Debug.Log($"[DebugDrawMigrationTool] 已迁移：移除 {removed} 个旧组件，添加 DebugDrawController。");
        EditorUtility.DisplayDialog("迁移完成", $"已移除 {removed} 个旧组件并添加 DebugDrawController。", "确定");
    }
}
#endif
