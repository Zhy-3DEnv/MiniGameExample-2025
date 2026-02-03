using UnityEngine;
using EggRogue;

/// <summary>
/// 武器控制器 - 管理多武器槽，每把武器独立攻击。
/// 替代/与 PlayerCombatController 二选一，优先使用本组件。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class WeaponController : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("发射点（旧逻辑：所有远程武器共用，若为空则自动查找 FirePoint）")]
    public Transform firePoint;

    [Header("朝向")]
    [Tooltip("是否根据移动方向旋转角色")]
    public bool faceFiringDirection = true;

    [Tooltip("朝向插值速度")]
    public float faceRotationSpeed = 60f;

    [Header("多武器挂点 / 自动锁敌")]
    [Tooltip("武器挂点根（如 Player 下的 WeaponSlot）。若为空则自动查找 \"WeaponSlot\" 子节点。")]
    public Transform weaponSlotRoot;

    [Tooltip("单把武器自动朝向目标的旋转速度（度/秒）")]
    public float weaponAimSpeed = 360f;

    [Tooltip("单把武器相对角色前方最大偏转角度（度）")]
    public float maxWeaponAimAngle = 90f;

    private CharacterController _characterController;
    private readonly float[] _cooldowns = new float[WeaponInventoryManager.MaxSlots];
    private readonly Transform[] _slotFirePoints = new Transform[WeaponInventoryManager.MaxSlots];
    private readonly Quaternion[] _slotDefaultLocalRotations = new Quaternion[WeaponInventoryManager.MaxSlots];
    private readonly bool[] _slotDefaultLocalRotCaptured = new bool[WeaponInventoryManager.MaxSlots];
    private PlayerCombatController _legacyCombat;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();

        if (firePoint == null)
        {
            var found = transform.Find("FirePoint");
            if (found != null)
                firePoint = found;
            else
            {
                var fp = new GameObject("FirePoint");
                fp.transform.SetParent(transform);
                fp.transform.localPosition = new Vector3(0f, 0.5f, 0.5f);
                firePoint = fp.transform;
            }
        }

        if (weaponSlotRoot == null)
        {
            var slotRoot = transform.Find("WeaponSlot");
            if (slotRoot != null)
                weaponSlotRoot = slotRoot;
        }

        _legacyCombat = GetComponent<PlayerCombatController>();
        if (_legacyCombat != null)
            _legacyCombat.enabled = false;
    }

    private void Update()
    {
        if (WeaponInventoryManager.Instance == null) return;

        if (faceFiringDirection)
            UpdateFacing();

        for (int i = 0; i < WeaponInventoryManager.MaxSlots; i++)
        {
            if (_cooldowns[i] > 0f)
                _cooldowns[i] -= Time.deltaTime;

            var weapon = WeaponInventoryManager.Instance.GetWeaponAt(i);
            if (weapon == null) continue;
            if (_cooldowns[i] > 0f) continue;

            if (weapon.weaponType == WeaponType.Ranged)
                TryFireRanged(i, weapon);
            else
                TryFireMelee(i, weapon);
        }
    }

    private void UpdateFacing()
    {
        // 仅根据玩家输入控制角色朝向，不再自动朝向敌人
        Vector2 moveInput = _characterController.GetMoveInput();
        if (moveInput.sqrMagnitude < 0.01f) return;

        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        Quaternion targetRot = Quaternion.LookRotation(direction);

        if (faceRotationSpeed <= 0f)
            transform.rotation = targetRot;
        else
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * faceRotationSpeed);
    }

    private void TryFireRanged(int slotIndex, WeaponData weapon)
    {
        if (weapon.bulletPrefab == null) return;

        Transform fp = GetSlotFirePoint(slotIndex);
        if (fp == null) return;

        // 槽位根节点（整把枪的挂点），用于可视旋转
        Transform slotRoot = GetSlotRoot(slotIndex);
        if (slotRoot == null)
            slotRoot = fp; // 兜底

        // 简化版锁敌：在攻击范围内，全局查找最近的可攻击敌人。
        // 如果场上只有 1 个敌人，所有枪都会瞄准它并开火。
        EnemyController target = FindGlobalClosestTarget(fp.position, weapon.attackRange);
        if (target == null)
        {
            // 没有锁定目标时，槽位回正到初始朝向
            ResetSlotRotation(slotIndex, slotRoot);
            return;
        }

        Vector3 toTarget = target.transform.position - fp.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        // 直接让该武器挂点朝向目标，不限制最大旋转角度，且不做插值（瞬间对准）
        Vector3 dirToTarget = toTarget.normalized;
        Quaternion desiredRot = Quaternion.LookRotation(dirToTarget);
        // 旋转整把武器（WeaponSlot_X），视觉上枪会立刻对准敌人
        slotRoot.rotation = desiredRot;

        Vector3 shootDir = slotRoot.forward;

        var bullet = Instantiate(weapon.bulletPrefab, fp.position, Quaternion.LookRotation(shootDir));
        var proj = bullet.GetComponent<Projectile>();
        if (proj != null)
            proj.Initialize(shootDir, weapon.damage, weapon.bulletSpeed, weapon.bulletLifeTime);

        _cooldowns[slotIndex] = 1f / Mathf.Max(0.1f, weapon.fireRate);
    }

    private void TryFireMelee(int slotIndex, WeaponData weapon)
    {
        EnemyController target = EnemyManager.Instance?.GetClosestEnemy(transform.position, weapon.attackRange);
        if (target == null) return;

        var health = target.GetComponentInChildren<Health>();
        if (health != null && !health.IsDead)
            health.TakeDamage(weapon.damage);

        _cooldowns[slotIndex] = 1f / Mathf.Max(0.1f, weapon.fireRate);
    }

    private Transform GetSlotFirePoint(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= WeaponInventoryManager.MaxSlots)
            return firePoint;

        if (_slotFirePoints[slotIndex] != null)
            return _slotFirePoints[slotIndex];

        if (weaponSlotRoot == null)
            return firePoint;

        var slot = weaponSlotRoot.Find($"WeaponSlot_{slotIndex}");
        if (slot == null)
            return firePoint;

        // 优先在该槽位下「武器预制体实例」的层级里寻找名为 Muzzle 或 FirePoint 的子节点
        Transform best = null;
        var children = slot.GetComponentsInChildren<Transform>(true);
        foreach (var t in children)
        {
            if (t == slot) continue;
            var name = t.name;
            if (name == "Muzzle" || name == "FirePoint" || name == "MuzzlePoint")
            {
                best = t;
                break;
            }
        }

        if (best == null)
            best = slot;

        _slotFirePoints[slotIndex] = best;
        return best;
    }

    /// <summary>
    /// 获取某个槽位的根挂点（WeaponSlot_X），用于旋转整把武器
    /// </summary>
    private Transform GetSlotRoot(int slotIndex)
    {
        if (weaponSlotRoot == null) return null;
        if (slotIndex < 0 || slotIndex >= WeaponInventoryManager.MaxSlots) return null;

        var root = weaponSlotRoot.Find($"WeaponSlot_{slotIndex}");
        if (root != null && !_slotDefaultLocalRotCaptured[slotIndex])
        {
            _slotDefaultLocalRotations[slotIndex] = root.localRotation;
            _slotDefaultLocalRotCaptured[slotIndex] = true;
        }
        return root;
    }

    /// <summary>
    /// 在全场范围内查找最近的、可被攻击的敌人（不做扇形/方向限制）。
    /// 所有枪使用同一逻辑：只要在攻击范围内，就可以共同锁定这个敌人。
    /// </summary>
    private EnemyController FindGlobalClosestTarget(Vector3 origin, float attackRange)
    {
        if (EnemyManager.Instance == null)
            return null;

        var enemies = EnemyManager.Instance.GetAllAliveEnemies();
        if (enemies == null || enemies.Count == 0)
            return null;

        float maxDistSqr = attackRange * attackRange;
        EnemyController best = null;
        float bestDistSqr = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            if (!enemy.IsAttackable) continue; // 出场中/已死亡的不参与锁定

            Vector3 toEnemy = enemy.transform.position - origin;
            toEnemy.y = 0f;
            float distSqr = toEnemy.sqrMagnitude;
            if (distSqr <= 0.0001f) continue;
            if (distSqr > maxDistSqr) continue; // 超出该武器攻击范围

            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                best = enemy;
            }
        }

        return best;
    }

    /// <summary>
    /// 当没有锁定目标时，将槽位的朝向恢复到初始本地旋转（布阵工具设置的默认朝向）。
    /// </summary>
    private void ResetSlotRotation(int slotIndex, Transform slotRoot)
    {
        if (slotRoot == null) return;
        if (slotIndex < 0 || slotIndex >= WeaponInventoryManager.MaxSlots) return;

        if (_slotDefaultLocalRotCaptured[slotIndex])
        {
            slotRoot.localRotation = _slotDefaultLocalRotations[slotIndex];
        }
    }
}
