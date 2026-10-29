using System;
using UnityEngine;

namespace SignalLost.Core
{
    public sealed class MissionTimer
    {
        public MissionTimer(float duration)
        {
            Duration = Mathf.Max(1f, duration);
            Remaining = Duration;
        }

        public float Duration { get; private set; }

        public float Remaining { get; private set; }

        public bool IsRunning { get; private set; }

        public bool HasExpired => Remaining <= 0f;

        public float Normalized => Mathf.Clamp01(Remaining / Duration);

        public string Clock => Format(Remaining);

        public event Action<float> Changed;

        public event Action Expired;

        public void Start()
        {
            if (!HasExpired)
            {
                IsRunning = true;
            }
        }

        public void Pause()
        {
            IsRunning = false;
        }

        public void Reset(float? duration = null)
        {
            if (duration.HasValue)
            {
                Duration = Mathf.Max(1f, duration.Value);
            }

            Remaining = Duration;
            IsRunning = false;
            Changed?.Invoke(Remaining);
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning || deltaTime <= 0f || HasExpired)
            {
                return;
            }

            Remaining = Mathf.Max(0f, Remaining - deltaTime);
            Changed?.Invoke(Remaining);

            if (Remaining <= 0f)
            {
                IsRunning = false;
                Expired?.Invoke();
            }
        }

        public static string Format(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
            int minutes = totalSeconds / 60;
            int remainder = totalSeconds % 60;
            return $"{minutes:00}:{remainder:00}";
        }
    }
}
