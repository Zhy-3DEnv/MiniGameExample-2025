using UnityEngine;
using EggRogue;

/// <summary>
/// 玩家生成器 - 在游戏场景加载时，根据选中的角色实例化玩家预制体并注入对应模型。
/// 放在 GameScene 的出生点位置，替代场景中直接放置的玩家对象。
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [Header("预制体配置")]
    [Tooltip("玩家预制体（含逻辑组件，不含具体角色模型）")]
    public GameObject playerPrefab;

    [Tooltip("默认角色模型预制体（当 CharacterData.characterModelPrefab 为空时使用）")]
    public GameObject defaultModelPrefab;

    [Header("出生点")]
    [Tooltip("玩家生成位置（若为空则使用本物体位置）")]
    public Transform spawnPoint;

    private GameObject _spawnedPlayer;

    private void Start()
    {
        SpawnPlayer();
    }

    /// <summary>
    /// 实例化玩家，注入选中角色的模型，并初始化属性
    /// </summary>
    public void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("PlayerSpawner: playerPrefab 未设置！");
            return;
        }

        CharacterData selectedCharacter = CharacterSelectionManager.Instance != null
            ? CharacterSelectionManager.Instance.SelectedCharacter
            : null;

        if (selectedCharacter == null)
        {
            Debug.LogWarning("PlayerSpawner: 未选择角色，使用默认配置实例化玩家");
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        _spawnedPlayer = Instantiate(playerPrefab, pos, rot);
        _spawnedPlayer.name = "Player";
        _spawnedPlayer.tag = "Player";

        Transform modelRoot = _spawnedPlayer.transform.Find("ModelRoot");
        if (modelRoot == null)
        {
            GameObject modelRootGo = new GameObject("ModelRoot");
            modelRootGo.transform.SetParent(_spawnedPlayer.transform);
            modelRootGo.transform.localPosition = Vector3.zero;
            modelRootGo.transform.localRotation = Quaternion.identity;
            modelRootGo.transform.localScale = Vector3.one;
            modelRoot = modelRootGo.transform;
        }

        // 清空已有模型子物体（预制体可能带有默认占位）
        for (int i = modelRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(modelRoot.GetChild(i).gameObject);
        }

        GameObject modelPrefab = selectedCharacter != null && selectedCharacter.characterModelPrefab != null
            ? selectedCharacter.characterModelPrefab
            : defaultModelPrefab;

        if (modelPrefab != null)
        {
            GameObject model = Instantiate(modelPrefab, modelRoot);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            model.name = modelPrefab.name;
        }
        else
        {
            Debug.LogWarning("PlayerSpawner: 无可用角色模型预制体，角色将不显示模型");
        }

        // 确保有 WeaponController（多武器系统）
        if (_spawnedPlayer.GetComponent<WeaponController>() == null)
            _spawnedPlayer.AddComponent<WeaponController>();

        // 确保有 ItemEffectManager（道具效果）
        if (_spawnedPlayer.GetComponent<ItemEffectManager>() == null)
            _spawnedPlayer.AddComponent<ItemEffectManager>();

        // 首次进入游戏时，WeaponInventoryManager 已由 WeaponSelectionPanel 初始化
        // 若跳过武器选择（兼容），则在此初始化
        if (WeaponInventoryManager.Instance != null && WeaponInventoryManager.Instance.IsEmpty)
            WeaponInventoryManager.Instance.InitializeFromStarterWeapon();

        // 实例化武器模型到挂点
        InstantiateWeaponModels();

        CharacterStats stats = _spawnedPlayer.GetComponent<CharacterStats>();
        if (stats != null)
        {
            stats.characterData = selectedCharacter;
            stats.InitializeStats();
        }
    }

    /// <summary>
    /// 根据 WeaponInventoryManager 中的武器，实例化武器模型到对应挂点
    /// </summary>
    private void InstantiateWeaponModels()
    {
        if (_spawnedPlayer == null || WeaponInventoryManager.Instance == null) return;

        Transform weaponSlotRoot = _spawnedPlayer.transform.Find("WeaponSlot");
        if (weaponSlotRoot == null)
        {
            var go = new GameObject("WeaponSlot");
            go.transform.SetParent(_spawnedPlayer.transform);
            go.transform.localPosition = Vector3.zero; // 对齐到角色中心高度
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            weaponSlotRoot = go.transform;
        }
        else
        {
            // 确保位置正确
            weaponSlotRoot.localPosition = Vector3.zero;
        }

        // 清空已有武器子物体
        for (int i = weaponSlotRoot.childCount - 1; i >= 0; i--)
            Destroy(weaponSlotRoot.GetChild(i).gameObject);

        // 实例化当前装备的武器模型
        for (int i = 0; i < WeaponInventoryManager.MaxSlots; i++)
        {
            var weapon = WeaponInventoryManager.Instance.GetWeaponAt(i);
            if (weapon == null || weapon.modelPrefab == null) continue;

            var slot = new GameObject($"WeaponSlot_{i}");
            slot.transform.SetParent(weaponSlotRoot);
            slot.transform.localPosition = GetWeaponSlotPosition(i);
            slot.transform.localRotation = GetWeaponSlotRotation(i);
            slot.transform.localScale = Vector3.one;

            var model = Instantiate(weapon.modelPrefab, slot.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            model.name = weapon.weaponName;
        }
    }

    /// <summary>
    /// 获取武器槽位的挂点位置（左右各 3 把，按 30° 间隔）
    /// </summary>
    private Vector3 GetWeaponSlotPosition(int index)
    {
        // 左右各 3 把武器，按 30° 间隔围绕角色
        float radius = 1.0f;
        float angleDeg = GetWeaponAngle(index);
        float angleRad = angleDeg * Mathf.Deg2Rad;
        
        float x = Mathf.Sin(angleRad) * radius;
        float z = Mathf.Cos(angleRad) * radius;
        
        return new Vector3(x, 0f, z);
    }

    /// <summary>
    /// 获取武器槽位的挂点旋转（武器朝向对应方向）
    /// </summary>
    private Quaternion GetWeaponSlotRotation(int index)
    {
        float angleDeg = GetWeaponAngle(index);
        return Quaternion.Euler(0f, angleDeg, 0f);
    }

    /// <summary>
    /// 获取武器槽位的角度（左右各 3 把，30° 间隔，中间那把对齐 X / -X）
    /// </summary>
    private float GetWeaponAngle(int index)
    {
        // 右侧 3 把：-120°(右后外), -90°(右中对齐-X), -60°(右前外)
        // 左侧 3 把：60°(左前外), 90°(左中对齐X), 120°(左后外)
        switch (index)
        {
            case 0: return -120f; // 右后外
            case 1: return -90f;  // 右中，对齐 -X
            case 2: return -60f;  // 右前外
            case 3: return 60f;   // 左前外
            case 4: return 90f;   // 左中，对齐 X
            case 5: return 120f;  // 左后外
            default: return 0f;
        }
    }

    /// <summary>
    /// 获取当前生成的玩家实例
    /// </summary>
    public GameObject GetSpawnedPlayer()
    {
        return _spawnedPlayer;
    }
}
