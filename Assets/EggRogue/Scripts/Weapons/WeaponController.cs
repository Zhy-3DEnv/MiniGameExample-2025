using UnityEngine;
using System.Collections;
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

    [Header("近战挥砍表现")]
    [Tooltip("近战挥砍弧度（度），例如 120 = 从左 -60° 划到右 +60°")]
    public float meleeSwingAngle = 120f;

    [Tooltip("近战挥砍时长（秒）")]
    public float meleeSwingDuration = 0.2f;

    /// <summary> 每槽位「已累积的可攻击时间」（秒），用于帧率无关的攻速计算。 </summary>
    private readonly float[] _fireAccumulators = new float[WeaponInventoryManager.MaxSlots];

    /// <summary> 近战槽位是否正在播放挥砍动画；同一槽位同时只允许一次挥砍，避免多个协程争用导致无法复位。 </summary>
    private readonly bool[] _meleeSwinging = new bool[WeaponInventoryManager.MaxSlots];

    private CharacterController _characterController;
    private CharacterStats _characterStats;
    private readonly Transform[] _slotFirePoints = new Transform[WeaponInventoryManager.MaxSlots];
    private readonly Quaternion[] _slotDefaultLocalRotations = new Quaternion[WeaponInventoryManager.MaxSlots];
    private readonly bool[] _slotDefaultLocalRotCaptured = new bool[WeaponInventoryManager.MaxSlots];
    private PlayerCombatController _legacyCombat;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _characterStats = GetComponent<CharacterStats>();

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
            var weapon = WeaponInventoryManager.Instance.GetWeaponAt(i);
            if (weapon == null) continue;

            float fireRate = _characterStats != null
                ? _characterStats.GetBaseFireRate(weapon)
                : weapon.fireRate;
            fireRate = Mathf.Max(0.1f, fireRate);
            float interval = 1f / fireRate;

            // 基于真实时间的累积：任意帧率下每秒攻击次数 = fireRate，与硬件无关
            _fireAccumulators[i] += Time.deltaTime;
            // 最多积压约 2 次攻击时间，避免长时间无目标后瞬间连发
            _fireAccumulators[i] = Mathf.Min(_fireAccumulators[i], interval * 2f);

            while (_fireAccumulators[i] >= interval)
            {
                bool fired = weapon.weaponType == WeaponType.Ranged
                    ? TryFireRanged(i, weapon)
                    : TryFireMelee(i, weapon);
                if (fired)
                    _fireAccumulators[i] -= interval;
                else
                    break; // 无目标时不扣时间，下帧再试，避免死循环
            }
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

    /// <summary> 尝试远程开火一次。成功开火返回 true，无目标或无法开火返回 false。 </summary>
    private bool TryFireRanged(int slotIndex, WeaponData weapon)
    {
        if (weapon.bulletPrefab == null) return false;

        Transform fp = GetSlotFirePoint(slotIndex);
        if (fp == null) return false;

        // 槽位根节点（整把枪的挂点），用于可视旋转
        Transform slotRoot = GetSlotRoot(slotIndex);
        if (slotRoot == null)
            slotRoot = fp; // 兜底

        // 简化版锁敌：在攻击范围内，全局查找最近的可攻击敌人。
        float range = GetWeaponRange(weapon);
        EnemyController target = FindGlobalClosestTarget(fp.position, range);
        if (target == null)
        {
            ResetSlotRotation(slotIndex, slotRoot);
            return false;
        }

        Vector3 toTarget = target.transform.position - fp.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return false;

        Vector3 dirToTarget = toTarget.normalized;
        Quaternion desiredRot = Quaternion.LookRotation(dirToTarget);
        slotRoot.rotation = desiredRot;
        Vector3 shootDir = slotRoot.forward;

        float baseDamage = _characterStats != null
            ? _characterStats.GetBaseAttackDamage(weapon)
            : weapon.damage;

        var bullet = Instantiate(weapon.bulletPrefab, fp.position, Quaternion.LookRotation(shootDir));
        var proj = bullet.GetComponent<Projectile>();
        if (proj != null)
            proj.Initialize(shootDir, baseDamage, weapon.bulletSpeed, weapon.bulletLifeTime);

        return true;
    }

    /// <summary> 尝试近战攻击一次。成功命中并结算伤害返回 true，无目标或该槽位正在挥砍返回 false。 </summary>
    private bool TryFireMelee(int slotIndex, WeaponData weapon)
    {
        if (_meleeSwinging[slotIndex])
            return false; // 同一槽位不重叠挥砍，否则多个协程争用 slotRoot 会导致复位失败

        float range = GetWeaponRange(weapon);
        EnemyController target = EnemyManager.Instance?.GetClosestEnemy(transform.position, range);
        if (target == null) return false;

        _meleeSwinging[slotIndex] = true;

        var health = target.GetComponentInChildren<Health>();
        if (health != null && !health.IsDead)
        {
            float baseDamage = _characterStats != null
                ? _characterStats.GetBaseAttackDamage(weapon)
                : weapon.damage;
            float dmg = EggRogue.ItemEffectManager.ProcessPlayerDamage(health, baseDamage);
            health.TakeDamage(dmg);
        }

        StartCoroutine(PlayMeleeSwing(slotIndex, weapon, target.transform.position));
        return true;
    }

    /// <summary>
    /// 近战挥砍动作：让武器挂点沿着指向目标的方向「飞过去劈砍再飞回」，同时做弧线旋转，可选播放 meleeHitPrefab。
    /// </summary>
    private IEnumerator PlayMeleeSwing(int slotIndex, WeaponData weapon, Vector3 targetPos)
    {
        try
        {
            Transform slotRoot = GetSlotRoot(slotIndex);
            if (slotRoot == null)
                slotRoot = transform;

            // 记录初始 local 状态，用于结束后复位
            Vector3 startLocalPos = slotRoot.localPosition;

            // 计算朝向目标的中心朝向
            Vector3 toTarget = targetPos - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
                toTarget = transform.forward;
            Vector3 dir = toTarget.normalized;
            Quaternion centerRot = Quaternion.LookRotation(dir);

            // 计算「飞过去」的目标 local 位置：沿角色→目标方向，在攻击范围内偏前一点
            float range = GetWeaponRange(weapon);
            float distToTarget = toTarget.magnitude;
            float flyDist = Mathf.Min(range, distToTarget) * 0.9f;
            Transform parent = slotRoot.parent;
            Vector3 localDir = parent != null ? parent.InverseTransformDirection(dir) : dir;
            Vector3 targetLocalPos = startLocalPos + localDir * flyDist;

            float halfAngle = meleeSwingAngle * 0.5f;
            Quaternion startRot = centerRot * Quaternion.Euler(0f, -halfAngle, 0f);
            Quaternion endRot = centerRot * Quaternion.Euler(0f, halfAngle, 0f);

            float duration = Mathf.Max(0.05f, meleeSwingDuration);
            float timer = 0f;
            bool hitVfxSpawned = false;

            while (timer < duration)
            {
                float t = timer / duration;

                // 位移（local）：前半程飞向目标，后半程飞回原位
                if (t <= 0.5f)
                {
                    float k = t / 0.5f;
                    slotRoot.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, k);
                }
                else
                {
                    float k = (t - 0.5f) / 0.5f;
                    slotRoot.localPosition = Vector3.Lerp(targetLocalPos, startLocalPos, k);
                }

                // 旋转：整段时间做一次左右挥砍
                slotRoot.rotation = Quaternion.Slerp(startRot, endRot, t);

                // 在挥砍中段生成一次近战特效（如果配置了）
                if (!hitVfxSpawned && weapon.meleeHitPrefab != null && t >= 0.4f)
                {
                    hitVfxSpawned = true;
                    Vector3 hitPos = slotRoot.position + slotRoot.forward * (range * 0.3f);
                    Object.Instantiate(weapon.meleeHitPrefab, hitPos, slotRoot.rotation);
                }

                timer += Time.deltaTime;
                yield return null;
            }

            // 挥砍结束后将武器复位到初始位置和初始朝向
            slotRoot.localPosition = startLocalPos;
            slotRoot.localRotation = _slotDefaultLocalRotCaptured[slotIndex] ? _slotDefaultLocalRotations[slotIndex] : Quaternion.identity;
        }
        finally
        {
            _meleeSwinging[slotIndex] = false;
        }
    }

    /// <summary>
    /// 统一计算某把武器的实际攻击范围：
    /// 角色当前攻击范围（含卡片/被动加成） + 武器自身 attackRange 偏移。
    /// </summary>
    private float GetWeaponRange(WeaponData weapon)
    {
        float baseRange = _characterStats != null ? _characterStats.CurrentAttackRange : 0f;
        float weaponOffset = weapon != null ? weapon.attackRange : 0f;
        return Mathf.Max(0f, baseRange + weaponOffset);
    }

    /// <summary>
    /// 在 Scene 视图中调试显示每把武器的实际攻击范围：从玩家位置沿各武器朝向画一条射程线。
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (WeaponInventoryManager.Instance == null) return;
        if (_characterStats == null)
            _characterStats = GetComponent<CharacterStats>();

        for (int i = 0; i < WeaponInventoryManager.MaxSlots; i++)
        {
            var weapon = WeaponInventoryManager.Instance.GetWeaponAt(i);
            if (weapon == null) continue;

            float range = GetWeaponRange(weapon);
            if (range <= 0f) continue;

            // 不同槽位用不同颜色
            float t = i / (float)(WeaponInventoryManager.MaxSlots - 1);
            Gizmos.color = Color.Lerp(Color.cyan, Color.red, t);

            // 方向优先使用 WeaponSlot_i 的 forward，没有就用角色 forward
            Transform slotRoot = weaponSlotRoot != null ? weaponSlotRoot.Find($"WeaponSlot_{i}") : null;
            Vector3 origin = transform.position;
            Vector3 dir = slotRoot != null ? slotRoot.forward : transform.forward;

            Gizmos.DrawLine(origin, origin + dir.normalized * range);
        }
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
