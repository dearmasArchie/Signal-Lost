using System;
using UnityEngine;

namespace SignalLost.Core
{
    public enum MissionStage
    {
        Briefing,
        Armory,
        CollectFuses,
        InstallFuses,
        ActivateTransmitter,
        Escape,
        Complete
    }

    public sealed class MissionProgress
    {
        public const int DefaultRequiredFuses = 3;

        public MissionProgress(int requiredFuses = DefaultRequiredFuses)
        {
            RequiredFuses = Mathf.Max(1, requiredFuses);
        }

        public int RequiredFuses { get; }

        public bool BriefingReceived { get; private set; }

        public bool ArcPistolCollected { get; private set; }

        public int FusesCollected { get; private set; }

        public int FusesInstalled { get; private set; }

        public bool SignalSent { get; private set; }

        public bool Escaped { get; private set; }

        public int AvailableFuses => Mathf.Max(0, FusesCollected - FusesInstalled);

        public MissionStage Stage
        {
            get
            {
                if (Escaped)
                {
                    return MissionStage.Complete;
                }

                if (SignalSent)
                {
                    return MissionStage.Escape;
                }

                if (FusesInstalled >= RequiredFuses)
                {
                    return MissionStage.ActivateTransmitter;
                }

                if (!BriefingReceived)
                {
                    return MissionStage.Briefing;
                }

                if (!ArcPistolCollected)
                {
                    return MissionStage.Armory;
                }

                if (FusesCollected < RequiredFuses)
                {
                    return MissionStage.CollectFuses;
                }

                return MissionStage.InstallFuses;
            }
        }

        public string ObjectiveText
        {
            get
            {
                switch (Stage)
                {
                    case MissionStage.Briefing:
                        return "Talk to Dr. Reyes";
                    case MissionStage.Armory:
                        return "Retrieve the Arc Pistol from the armory";
                    case MissionStage.CollectFuses:
                        return $"Recover override fuses ({FusesCollected}/{RequiredFuses})";
                    case MissionStage.InstallFuses:
                        return $"Install fuses in the radio tower ({FusesInstalled}/{RequiredFuses})";
                    case MissionStage.ActivateTransmitter:
                        return "Activate the emergency transmitter";
                    case MissionStage.Escape:
                        return "Return to the extraction boat";
                    default:
                        return "Extraction complete";
                }
            }
        }

        public event Action Changed;

        public bool RecordBriefing()
        {
            if (BriefingReceived)
            {
                return false;
            }

            BriefingReceived = true;
            RaiseChanged();
            return true;
        }

        public bool RecordArcPistolCollected()
        {
            if (ArcPistolCollected)
            {
                return false;
            }

            ArcPistolCollected = true;
            RaiseChanged();
            return true;
        }

        public bool CollectFuse()
        {
            if (FusesCollected >= RequiredFuses)
            {
                return false;
            }

            FusesCollected++;
            RaiseChanged();
            return true;
        }

        public bool InstallFuse()
        {
            if (AvailableFuses <= 0 || FusesInstalled >= RequiredFuses)
            {
                return false;
            }

            FusesInstalled++;
            RaiseChanged();
            return true;
        }

        public bool TrySendSignal()
        {
            if (SignalSent || FusesInstalled < RequiredFuses)
            {
                return false;
            }

            SignalSent = true;
            RaiseChanged();
            return true;
        }

        public bool RecordEscaped()
        {
            if (!SignalSent || Escaped)
            {
                return false;
            }

            Escaped = true;
            RaiseChanged();
            return true;
        }

        public void Reset()
        {
            BriefingReceived = false;
            ArcPistolCollected = false;
            FusesCollected = 0;
            FusesInstalled = 0;
            SignalSent = false;
            Escaped = false;
            RaiseChanged();
        }

        private void RaiseChanged()
        {
            Changed?.Invoke();
        }
    }
}
