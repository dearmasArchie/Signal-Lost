using UnityEngine;

namespace SignalLost.Core
{
    public interface IDamageable
    {
        bool IsAlive { get; }

        Transform DamageTransform { get; }

        bool TakeDamage(DamageInfo damage);
    }
}
