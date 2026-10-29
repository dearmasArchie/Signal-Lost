using NUnit.Framework;
using SignalLost.Core;

namespace SignalLost.Tests
{
    public sealed class MissionProgressTests
    {
        [Test]
        public void Mission_AdvancesThroughCompleteSequence()
        {
            MissionProgress progress = new MissionProgress(3);
            Assert.That(progress.Stage, Is.EqualTo(MissionStage.Briefing));

            progress.RecordBriefing();
            Assert.That(progress.Stage, Is.EqualTo(MissionStage.Armory));

            progress.RecordArcPistolCollected();
            Assert.That(progress.Stage, Is.EqualTo(MissionStage.CollectFuses));

            progress.CollectFuse();
            progress.CollectFuse();
            progress.CollectFuse();
            Assert.That(progress.Stage, Is.EqualTo(MissionStage.InstallFuses));

            progress.InstallFuse();
            progress.InstallFuse();
            progress.InstallFuse();
            Assert.That(progress.Stage, Is.EqualTo(MissionStage.ActivateTransmitter));

            Assert.That(progress.TrySendSignal(), Is.True);
            Assert.That(progress.Stage, Is.EqualTo(MissionStage.Escape));

            Assert.That(progress.RecordEscaped(), Is.True);
            Assert.That(progress.Stage, Is.EqualTo(MissionStage.Complete));
        }

        [Test]
        public void InstallFuse_RequiresCollectedFuse()
        {
            MissionProgress progress = new MissionProgress(3);

            Assert.That(progress.InstallFuse(), Is.False);
            Assert.That(progress.FusesInstalled, Is.EqualTo(0));
        }

        [Test]
        public void Signal_RequiresAllFusesInstalled()
        {
            MissionProgress progress = new MissionProgress(3);
            progress.RecordBriefing();
            progress.RecordArcPistolCollected();
            progress.CollectFuse();
            progress.CollectFuse();
            progress.CollectFuse();
            progress.InstallFuse();
            progress.InstallFuse();

            Assert.That(progress.TrySendSignal(), Is.False);
            Assert.That(progress.SignalSent, Is.False);
        }

        [Test]
        public void CollectFuse_StopsAtRequiredCount()
        {
            MissionProgress progress = new MissionProgress(3);
            progress.CollectFuse();
            progress.CollectFuse();
            progress.CollectFuse();

            Assert.That(progress.CollectFuse(), Is.False);
            Assert.That(progress.FusesCollected, Is.EqualTo(3));
        }
    }
}
