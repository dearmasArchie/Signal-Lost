using SignalLost.Core;
using SignalLost.Player;
using UnityEngine;

namespace SignalLost.Interactables
{
    public sealed class NpcDialogueInteractable : InteractableBase
    {
        [SerializeField] private string speakerName = "Dr. Reyes";
        [SerializeField, TextArea(4, 10)]
        private string briefing = "You made it. The mainframe turned every sentry on the island against us. "
            + "Restore the central radio beacon and get to the extraction boat before lockdown seals the island. "
            + "The armory keypad code is 4471. Take the Arc Pistol.";
        [SerializeField, TextArea(3, 8)]
        private string repeatDialogue = "Three override fuses are still out there. Bunkers A and B have two. "
            + "The old maintenance cache beneath the radio tower has the third. Keep moving.";

        public override string InteractionPrompt => $"[E] Talk to {speakerName}";

        public override void Interact(PlayerInteraction interactor)
        {
            GameManager manager = GameManager.Instance;

            if (manager == null)
            {
                return;
            }

            bool alreadyBriefed = manager.Progress.BriefingReceived;
            manager.NotifyBriefingReceived();
            manager.RequestDialogue(speakerName, alreadyBriefed ? repeatDialogue : briefing);
        }
    }
}
