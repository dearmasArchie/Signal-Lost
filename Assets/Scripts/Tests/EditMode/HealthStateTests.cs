using NUnit.Framework;
using SignalLost.Core;

namespace SignalLost.Tests
{
    public sealed class HealthStateTests
    {
        [Test]
        public void ApplyDamage_ClampsAtZeroAndRaisesDeathOnce()
        {
            HealthState health = new HealthState(100f);
            int deathCount = 0;
            int changeCount = 0;
            health.Died += () => deathCount++;
            health.Changed += (_, _) => changeCount++;

            Assert.That(health.ApplyDamage(25f), Is.True);
            Assert.That(health.ApplyDamage(500f), Is.True);
            Assert.That(health.ApplyDamage(1f), Is.False);

            Assert.That(health.CurrentHealth, Is.EqualTo(0f));
            Assert.That(health.IsAlive, Is.False);
            Assert.That(deathCount, Is.EqualTo(1));
            Assert.That(changeCount, Is.EqualTo(2));
        }

        [Test]
        public void Heal_DoesNotReviveDeadHealth()
        {
            HealthState health = new HealthState(100f);
            health.SetCurrent(0f);

            float healed = health.Heal(25f);

            Assert.That(healed, Is.EqualTo(0f));
            Assert.That(health.CurrentHealth, Is.EqualTo(0f));
        }

        [Test]
        public void Heal_ClampsAtMaxAndReturnsActualAmount()
        {
            HealthState health = new HealthState(100f);
            health.ApplyDamage(80f);

            float healed = health.Heal(120f);

            Assert.That(healed, Is.EqualTo(80f).Within(0.001f));
            Assert.That(health.CurrentHealth, Is.EqualTo(100f).Within(0.001f));
        }
    }
}
