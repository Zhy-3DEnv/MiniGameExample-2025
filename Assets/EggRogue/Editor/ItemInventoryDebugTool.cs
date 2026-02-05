#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using EggRogue;

/// <summary>
/// 道具栏调试工具：在编辑器中一键填充一些已购买道具，方便预览 UI（如已购买物品栏）。
/// </summary>
public static class ItemInventoryDebugTool
{
    private const string ItemDatabasePath = "Assets/EggRogue/Configs/ItemDatabase.asset";

    [MenuItem("EggRogue/道具调试/填充测试道具到物品栏")]
    public static void FillTestItems()
    {
        var inv = ItemInventoryManager.Instance ?? Object.FindObjectOfType<ItemInventoryManager>();
        if (inv == null)
        {
            Debug.LogError("[ItemInventoryDebugTool] 场景中未找到 ItemInventoryManager，请先在 PersistentScene 中添加。");
            return;
        }

        var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(ItemDatabasePath);
        if (db == null || db.items == null || db.items.Length == 0)
        {
            Debug.LogError($"[ItemInventoryDebugTool] 未找到道具数据库或数据库为空：{ItemDatabasePath}");
            return;
        }

        // 清空当前道具
        inv.ClearAll();

        // 简单填充：把数据库里的所有道具各加 1 个
        int added = 0;
        foreach (var item in db.items)
        {
            if (item == null) continue;
            if (inv.TryAddItem(item))
                added++;
        }

        Debug.Log($"[ItemInventoryDebugTool] 已向物品栏填充 {added} 个测试道具（来自 ItemDatabase）。");

        // 尝试立即刷新场景中的 ItemInventory 显示（例如 ShopPanel 下的 ItemInventoryDisplay）
        RefreshItemInventoryDisplaysInOpenScenes();
    }

    [MenuItem("EggRogue/道具调试/清空物品栏")]
    public static void ClearItems()
    {
        var inv = ItemInventoryManager.Instance ?? Object.FindObjectOfType<ItemInventoryManager>();
        if (inv == null)
        {
            Debug.LogError("[ItemInventoryDebugTool] 场景中未找到 ItemInventoryManager。");
            return;
        }

        inv.ClearAll();
        Debug.Log("[ItemInventoryDebugTool] 已清空物品栏。");

        // 清空后也刷新一次 UI
        RefreshItemInventoryDisplaysInOpenScenes();
    }

    /// <summary>
    /// 在当前打开的场景中查找所有物品栏显示组件并触发刷新，
    /// 这样在编辑器里运行菜单命令后，Hierarchy 下的 ItemInventoryDisplay 能立刻看到子节点变化。
    /// </summary>
    private static void RefreshItemInventoryDisplaysInOpenScenes()
    {
        var displays = Object.FindObjectsOfType<ShopItemInventoryDisplay>(true);
        foreach (var display in displays)
        {
            if (display == null) continue;
            display.Refresh();
        }
    }
}
#endif

