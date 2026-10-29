using UnityEngine;

namespace SignalLost.Core
{
    public readonly struct DamageInfo
    {
        public DamageInfo(
            float amount,
            DamageType type,
            Vector3 point,
            Vector3 normal,
            GameObject source = null,
            float stunDuration = 0f)
        {
            Amount = Mathf.Max(0f, amount);
            Type = type;
            Point = point;
            Normal = normal;
            Source = source;
            StunDuration = Mathf.Max(0f, stunDuration);
        }

        public float Amount { get; }

        public DamageType Type { get; }

        public Vector3 Point { get; }

        public Vector3 Normal { get; }

        public GameObject Source { get; }

        public float StunDuration { get; }
    }
}
