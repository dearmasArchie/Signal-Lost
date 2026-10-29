using NUnit.Framework;
using SignalLost.Core;

namespace SignalLost.Tests
{
    public sealed class MissionTimerTests
    {
        [Test]
        public void Tick_ReducesRemainingAndExpiresOnce()
        {
            MissionTimer timer = new MissionTimer(10f);
            int expiryCount = 0;
            timer.Expired += () => expiryCount++;
            timer.Start();

            timer.Tick(4f);

            Assert.That(timer.Remaining, Is.EqualTo(6f).Within(0.001f));
            Assert.That(timer.HasExpired, Is.False);

            timer.Tick(10f);
            timer.Tick(10f);

            Assert.That(timer.HasExpired, Is.True);
            Assert.That(expiryCount, Is.EqualTo(1));
        }

        [Test]
        public void Reset_RestoresDurationAndStopsTimer()
        {
            MissionTimer timer = new MissionTimer(30f);
            timer.Start();
            timer.Tick(5f);
            timer.Reset();

            Assert.That(timer.Remaining, Is.EqualTo(30f).Within(0.001f));
            Assert.That(timer.IsRunning, Is.False);

            timer.Tick(1f);

            Assert.That(timer.Remaining, Is.EqualTo(30f).Within(0.001f));
        }

        [Test]
        public void Format_UsesClockDisplay()
        {
            Assert.That(MissionTimer.Format(600f), Is.EqualTo("10:00"));
            Assert.That(MissionTimer.Format(59.2f), Is.EqualTo("01:00"));
            Assert.That(MissionTimer.Format(0f), Is.EqualTo("00:00"));
        }
    }
}
