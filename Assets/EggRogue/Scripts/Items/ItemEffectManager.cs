using UnityEngine;
using System.Collections.Generic;

namespace EggRogue
{
    /// <summary>
    /// 道具效果管理器 - 挂载在玩家上，根据 ItemInventoryManager 应用道具效果。
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(CharacterStats))]
    public class ItemEffectManager : MonoBehaviour
    {
        private Health _playerHealth;
        private CharacterStats _playerStats;
        private CharacterController _characterController;
        private float _lightningTimer;
        private float _iceTimer;
        private float _regenTimer;
        private readonly Dictionary<EnemyController, float> _icedEnemies = new Dictionary<EnemyController, float>();

        private void Awake()
        {
            _playerHealth = GetComponent<Health>();
            _playerStats = GetComponent<CharacterStats>();
            _characterController = GetComponent<CharacterController>();
            SetupHealthCallbacks();
        }

        private void SetupHealthCallbacks()
        {
            if (_playerHealth == null) return;

            _playerHealth.DamageModifier = (dmg, source) =>
            {
                var item = GetItem(ItemEffectType.ShieldGenerator);
                if (item != null)
                {
                    int n = GetItemCount(ItemEffectType.ShieldGenerator);
                    float totalReduce = Mathf.Min(90f, item.effectPercent * n) / 100f;
                    return dmg * (1f - totalReduce);
                }
                return dmg;
            };

            _playerHealth.OnDamageFrom = (dmg, source) =>
            {
                var item = GetItem(ItemEffectType.Thorns);
                if (item == null || source == null) return;
                int n = GetItemCount(ItemEffectType.Thorns);
                var enemyHealth = source.GetComponent<Health>();
                if (enemyHealth != null && !enemyHealth.IsDead)
                {
                    float reflect = dmg * (item.effectPercent * n / 100f);
                    enemyHealth.TakeDamage(reflect);
                }
            };
        }

        private void OnDestroy()
        {
            if (_playerHealth != null)
            {
                _playerHealth.DamageModifier = null;
                _playerHealth.OnDamageFrom = null;
            }
        }

        private ItemData GetItem(ItemEffectType type)
        {
            return ItemInventoryManager.Instance?.GetFirstItemOfType(type);
        }

        private bool HasItem(ItemEffectType type)
        {
            return ItemInventoryManager.Instance != null && ItemInventoryManager.Instance.HasItem(type);
        }

        private int GetItemCount(ItemEffectType type)
        {
            return ItemInventoryManager.Instance != null ? ItemInventoryManager.Instance.GetItemCount(type) : 0;
        }

        private void Update()
        {
            if (EggRogue.GameplayPauseManager.Instance != null && EggRogue.GameplayPauseManager.Instance.IsPaused)
                return;

            TickLightningTower();
            TickIcePack();
            TickHealthRegen();
            TickSpeedBoots();
        }

        private void TickLightningTower()
        {
            var item = GetItem(ItemEffectType.LightningTower);
            if (item == null) return;

            _lightningTimer += Time.deltaTime;
            if (_lightningTimer < item.effectDuration) return;
            _lightningTimer = 0f;

            if (EnemyManager.Instance == null) return;
            var target = EnemyManager.Instance.GetRandomEnemyInRange(transform.position, item.effectRange);
            if (target == null) return;

            int n = GetItemCount(ItemEffectType.LightningTower);
            float dmg = (_playerStats != null ? _playerStats.CurrentMaxHealth * item.effectValue : 50f) * n;
            var h = target.GetComponent<Health>();
            if (h != null && !h.IsDead)
                h.TakeDamage(dmg);
        }

        private void TickIcePack()
        {
            var item = GetItem(ItemEffectType.IcePack);
            if (item == null)
            {
                ClearIceAura();
                return;
            }

            _iceTimer += Time.deltaTime;
            if (_iceTimer < 0.3f) return;
            _iceTimer = 0f;

            float range = item.effectRange;
            int n = GetItemCount(ItemEffectType.IcePack);
            float totalSlow = Mathf.Min(75f, item.effectPercent * n) / 100f;
            float slowMult = 1f - totalSlow;
            var list = EnemyManager.Instance?.GetAllAliveEnemies();
            if (list == null) return;

            var toRemove = new List<EnemyController>();
            foreach (var kv in _icedEnemies)
            {
                if (kv.Key == null) { toRemove.Add(kv.Key); continue; }
                float dist = Vector3.Distance(transform.position, kv.Key.transform.position);
                if (dist > range + 1f)
                {
                    kv.Key.SetMoveSpeed(kv.Value);
                    toRemove.Add(kv.Key);
                }
            }
            foreach (var e in toRemove) _icedEnemies.Remove(e);

            foreach (var enemy in list)
            {
                if (enemy == null) continue;
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist > range) continue;

                if (!_icedEnemies.ContainsKey(enemy))
                    _icedEnemies[enemy] = enemy.GetCurrentMoveSpeed();
                enemy.SetMoveSpeed(_icedEnemies[enemy] * slowMult);
            }
        }

        private void ClearIceAura()
        {
            foreach (var kv in _icedEnemies)
            {
                if (kv.Key != null)
                    kv.Key.SetMoveSpeed(kv.Value);
            }
            _icedEnemies.Clear();
        }

        private void TickHealthRegen()
        {
            var item = GetItem(ItemEffectType.HealthRegen);
            if (item == null) return;

            _regenTimer += Time.deltaTime;
            if (_regenTimer < 1f) return;
            _regenTimer = 0f;

            if (_playerHealth != null && !_playerHealth.IsDead)
            {
                int n = GetItemCount(ItemEffectType.HealthRegen);
                float heal = (_playerStats != null ? _playerStats.CurrentMaxHealth * (item.effectPercent / 100f) : 2f) * n;
                _playerHealth.Heal(heal);
            }
        }

        private void TickSpeedBoots()
        {
            var item = GetItem(ItemEffectType.SpeedBoots);
            if (_characterController == null) return;

            float baseSpeed = _playerStats != null ? _playerStats.CurrentMoveSpeed : 5f;
            if (item != null)
            {
                int n = GetItemCount(ItemEffectType.SpeedBoots);
                baseSpeed *= 1f + item.effectPercent * n / 100f;
            }
            _characterController.SetMoveSpeed(baseSpeed);
        }

        /// <summary>
        /// 玩家对敌人造成伤害时调用，处理暴击、燃烧、毒、吸血
        /// </summary>
        public static float ProcessPlayerDamage(Health enemyHealth, float baseDamage)
        {
            if (ItemInventoryManager.Instance == null) return baseDamage;

            float dmg = baseDamage;

            var critItem = ItemInventoryManager.Instance.GetFirstItemOfType(ItemEffectType.CritChip);
            int critCount = ItemInventoryManager.Instance.GetItemCount(ItemEffectType.CritChip);
            float critChance = Mathf.Min(100f, critItem != null ? critItem.effectPercent * critCount : 0f) / 100f;
            if (critChance > 0f && Random.value < critChance)
                dmg *= 2f;

            if (enemyHealth != null)
            {
                var enemy = enemyHealth.GetComponent<EnemyController>();
                if (enemy != null)
                {
                    var burnItem = ItemInventoryManager.Instance.GetFirstItemOfType(ItemEffectType.Burning);
                    int burnCount = ItemInventoryManager.Instance.GetItemCount(ItemEffectType.Burning);
                    if (burnItem != null && burnCount > 0)
                        EnemyBuff.ApplyBurn(enemy.gameObject, burnItem.effectValue * burnCount, burnItem.effectDuration);

                    var poisonItem = ItemInventoryManager.Instance.GetFirstItemOfType(ItemEffectType.Poison);
                    int poisonCount = ItemInventoryManager.Instance.GetItemCount(ItemEffectType.Poison);
                    if (poisonItem != null && poisonCount > 0)
                        EnemyBuff.ApplyPoison(enemy.gameObject, poisonItem.effectValue * poisonCount, poisonItem.effectDuration);
                }
            }

            var lifeItem = ItemInventoryManager.Instance.GetFirstItemOfType(ItemEffectType.Lifesteal);
            int lifeCount = ItemInventoryManager.Instance.GetItemCount(ItemEffectType.Lifesteal);
            if (lifeItem != null && lifeCount > 0 && dmg > 0f)
            {
                float heal = dmg * (lifeItem.effectPercent * lifeCount / 100f);
                var player = GameObject.FindGameObjectWithTag("Player") ?? Object.FindObjectOfType<CharacterController>()?.gameObject;
                if (player != null)
                {
                    var ph = player.GetComponent<Health>();
                    if (ph != null && !ph.IsDead)
                        ph.Heal(heal);
                }
            }

            return dmg;
        }

        /// <summary>
        /// 获取道具提供的拾取范围加成（磁铁等，effectValue 为米）。
        /// </summary>
        public static float GetPickupRangeBonus()
        {
            if (ItemInventoryManager.Instance == null) return 0f;
            int n = ItemInventoryManager.Instance.GetItemCount(ItemEffectType.Magnet);
            if (n <= 0) return 0f;
            var item = ItemInventoryManager.Instance.GetFirstItemOfType(ItemEffectType.Magnet);
            return item != null ? item.effectValue * n : 0f;
        }
    }
}
