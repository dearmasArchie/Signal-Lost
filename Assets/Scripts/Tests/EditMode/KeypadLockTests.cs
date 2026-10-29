using NUnit.Framework;
using SignalLost.Interactables;

namespace SignalLost.Tests
{
    public sealed class KeypadLockTests
    {
        [Test]
        public void TryUnlock_RejectsWrongCodeAndAcceptsConfiguredCode()
        {
            KeypadLock keypad = new KeypadLock("4471");
            int unlockCount = 0;
            keypad.Unlocked += () => unlockCount++;

            Assert.That(keypad.TryUnlock("0000"), Is.False);
            Assert.That(keypad.IsUnlocked, Is.False);
            Assert.That(keypad.FailedAttempts, Is.EqualTo(1));

            Assert.That(keypad.TryUnlock(" 4471 "), Is.True);
            Assert.That(keypad.IsUnlocked, Is.True);
            Assert.That(unlockCount, Is.EqualTo(1));
        }

        [Test]
        public void TryUnlock_DoesNotRaiseEventTwice()
        {
            KeypadLock keypad = new KeypadLock("4471");
            int unlockCount = 0;
            keypad.Unlocked += () => unlockCount++;

            keypad.TryUnlock("4471");
            keypad.TryUnlock("4471");

            Assert.That(unlockCount, Is.EqualTo(1));
        }

        [Test]
        public void Reset_LocksKeypadAgain()
        {
            KeypadLock keypad = new KeypadLock("4471");
            keypad.TryUnlock("4471");
            keypad.Reset();

            Assert.That(keypad.IsUnlocked, Is.False);
            Assert.That(keypad.FailedAttempts, Is.EqualTo(0));
        }
    }
}
