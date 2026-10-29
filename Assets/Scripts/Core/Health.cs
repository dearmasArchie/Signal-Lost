using System;
using UnityEngine;

namespace SignalLost.Core
{
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private bool invulnerable;

        private HealthState state;

        public float CurrentHealth => state != null ? state.CurrentHealth : maxHealth;

        public float MaxHealth => state != null ? state.MaxHealth : maxHealth;

        public bool IsAlive => state == null || state.IsAlive;

        public Transform DamageTransform => transform;

        public HealthState State => state;

        public event Action<DamageInfo, float> Damaged;

        public event Action<float, float> HealthChanged;

        public event Action Died;

        private void Awake()
        {
            ReplaceState(new HealthState(maxHealth));
        }

        public bool TakeDamage(DamageInfo damage)
        {
            if (invulnerable || state == null || !state.ApplyDamage(damage.Amount))
            {
                return false;
            }

            Damaged?.Invoke(damage, damage.Amount);
            return true;
        }

        public float Heal(float amount)
        {
            return state != null ? state.Heal(amount) : 0f;
        }

        public void ResetHealth()
        {
            state?.Reset();
        }

        public void Configure(float newMaxHealth, bool refill = true)
        {
            newMaxHealth = Mathf.Max(1f, newMaxHealth);
            maxHealth = newMaxHealth;

            if (state == null)
            {
                return;
            }

            float previousCurrent = Mathf.Clamp(state.CurrentHealth, 0f, newMaxHealth);
            ReplaceState(new HealthState(newMaxHealth));

            if (!refill)
            {
                state.SetCurrent(previousCurrent);
            }

            HealthChanged?.Invoke(state.CurrentHealth, state.MaxHealth);
        }

        public void SetInvulnerable(bool value)
        {
            invulnerable = value;
        }

        private void ReplaceState(HealthState newState)
        {
            if (state != null)
            {
                state.Changed -= HandleStateChanged;
                state.Died -= HandleStateDied;
            }

            state = newState;
            state.Changed += HandleStateChanged;
            state.Died += HandleStateDied;
        }

        private void HandleStateChanged(float current, float maximum)
        {
            HealthChanged?.Invoke(current, maximum);
        }

        private void HandleStateDied()
        {
            Died?.Invoke();
        }

        private void OnDestroy()
        {
            if (state == null)
            {
                return;
            }

            state.Changed -= HandleStateChanged;
            state.Died -= HandleStateDied;
        }
    }
}
