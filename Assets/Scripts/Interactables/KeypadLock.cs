using System;

namespace SignalLost.Interactables
{
    public sealed class KeypadLock
    {
        public KeypadLock(string code)
        {
            Code = string.IsNullOrWhiteSpace(code) ? "4471" : code.Trim();
        }

        public string Code { get; }

        public bool IsUnlocked { get; private set; }

        public int FailedAttempts { get; private set; }

        public event Action Unlocked;

        public bool TryUnlock(string input)
        {
            if (IsUnlocked)
            {
                return true;
            }

            if (string.Equals(Code, input?.Trim(), StringComparison.Ordinal))
            {
                IsUnlocked = true;
                Unlocked?.Invoke();
                return true;
            }

            FailedAttempts++;
            return false;
        }

        public void Reset()
        {
            IsUnlocked = false;
            FailedAttempts = 0;
        }
    }
}
