using System;
using UnityEngine;

namespace SignalLost.Core
{
    public sealed class HealthState
    {
        public HealthState(float maxHealth)
        {
            MaxHealth = Mathf.Max(1f, maxHealth);
            CurrentHealth = MaxHealth;
        }

        public float MaxHealth { get; }

        public float CurrentHealth { get; private set; }

        public bool IsAlive => CurrentHealth > 0f;

        public float Normalized => Mathf.Clamp01(CurrentHealth / MaxHealth);

        public event Action<float, float> Changed;

        public event Action Died;

        public bool ApplyDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return false;
            }

            SetCurrent(CurrentHealth - amount);
            return true;
        }

        public float Heal(float amount)
        {
            if (!IsAlive || amount <= 0f)
            {
                return 0f;
            }

            float previous = CurrentHealth;
            SetCurrent(CurrentHealth + amount);
            return CurrentHealth - previous;
        }

        public void SetCurrent(float value)
        {
            float previous = CurrentHealth;
            CurrentHealth = Mathf.Clamp(value, 0f, MaxHealth);

            if (!Mathf.Approximately(previous, CurrentHealth))
            {
                Changed?.Invoke(CurrentHealth, MaxHealth);
            }

            if (previous > 0f && CurrentHealth <= 0f)
            {
                Died?.Invoke();
            }
        }

        public void Reset()
        {
            SetCurrent(MaxHealth);
        }
    }
}
