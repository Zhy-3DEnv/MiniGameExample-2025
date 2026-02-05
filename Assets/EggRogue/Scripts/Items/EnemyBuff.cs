using UnityEngine;

namespace EggRogue
{
    /// <summary>
    /// 敌人身上的 Buff（燃烧、中毒、减速等）
    /// </summary>
    public class EnemyBuff : MonoBehaviour
    {
        public enum BuffType { Burn, Poison, Slow }

        public BuffType Type { get; private set; }
        public float DamagePerSecond { get; private set; }
        public float RemainingDuration { get; private set; }
        public float SlowMultiplier { get; private set; }

        private Health _health;
        private EnemyController _enemy;
        private float _baseMoveSpeed;
        private bool _slowApplied;

        public static EnemyBuff ApplyBurn(GameObject target, float dps, float duration)
        {
            return Apply(target, BuffType.Burn, dps, duration, 1f);
        }

        public static EnemyBuff ApplyPoison(GameObject target, float dps, float duration)
        {
            return Apply(target, BuffType.Poison, dps, duration, 1f);
        }

        public static EnemyBuff ApplySlow(GameObject target, float multiplier, float duration)
        {
            return Apply(target, BuffType.Slow, 0f, duration, multiplier);
        }

        private static EnemyBuff Apply(GameObject target, BuffType type, float dps, float duration, float slowMult)
        {
            if (target == null || duration <= 0f) return null;

            var buff = target.GetComponent<EnemyBuff>();
            if (buff == null)
                buff = target.AddComponent<EnemyBuff>();

            buff._health = target.GetComponent<Health>();
            buff._enemy = target.GetComponent<EnemyController>();
            buff.Type = type;
            buff.DamagePerSecond = dps;
            buff.RemainingDuration = duration;
            buff.SlowMultiplier = Mathf.Clamp01(slowMult);

            if (buff._enemy != null && type == BuffType.Slow)
            {
                buff._baseMoveSpeed = buff._enemy.GetCurrentMoveSpeed();
                buff._enemy.SetMoveSpeed(buff._baseMoveSpeed * buff.SlowMultiplier);
                buff._slowApplied = true;
            }

            return buff;
        }

        private void Update()
        {
            if (RemainingDuration <= 0f)
            {
                RemoveSlow();
                Destroy(this);
                return;
            }

            RemainingDuration -= Time.deltaTime;

            if (Type == BuffType.Burn || Type == BuffType.Poison)
            {
                if (_health != null && !_health.IsDead && DamagePerSecond > 0f)
                    _health.TakeDamage(DamagePerSecond * Time.deltaTime);
            }

            if (RemainingDuration <= 0f)
            {
                RemoveSlow();
                Destroy(this);
            }
        }

        private void RemoveSlow()
        {
            if (_slowApplied && _enemy != null)
            {
                _enemy.SetMoveSpeed(_baseMoveSpeed);
                _slowApplied = false;
            }
        }

        private void OnDestroy()
        {
            RemoveSlow();
        }

        public void RefreshDuration(float extraDuration)
        {
            RemainingDuration += extraDuration;
        }
    }
}
