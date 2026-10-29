using System;
using SignalLost.Core;
using UnityEngine;

namespace SignalLost.Player
{
    [RequireComponent(typeof(Health))]
    public sealed class PlayerHealth : MonoBehaviour
    {
        private Health health;

        public Health Health => health;

        public float CurrentHealth => health != null ? health.CurrentHealth : 0f;

        public float MaxHealth => health != null ? health.MaxHealth : 0f;

        public bool IsAlive => health != null && health.IsAlive;

        public bool CanHeal => IsAlive && CurrentHealth < MaxHealth;

        public event Action<float, float> HealthChanged;

        public event Action Died;

        private void Awake()
        {
            health = GetComponent<Health>();

            if (health == null)
            {
                return;
            }

            health.HealthChanged += HandleHealthChanged;
            health.Died += HandleDied;
        }

        public float Heal(float amount)
        {
            return health != null ? health.Heal(amount) : 0f;
        }

        private void HandleHealthChanged(float current, float maximum)
        {
            HealthChanged?.Invoke(current, maximum);
        }

        private void HandleDied()
        {
            Died?.Invoke();
            GameManager.Instance?.NotifyPlayerDied();
        }

        private void OnDestroy()
        {
            if (health == null)
            {
                return;
            }

            health.HealthChanged -= HandleHealthChanged;
            health.Died -= HandleDied;
        }
    }
}
