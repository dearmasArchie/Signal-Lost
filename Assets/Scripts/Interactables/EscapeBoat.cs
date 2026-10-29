using SignalLost.Audio;
using SignalLost.Core;
using SignalLost.Player;
using UnityEngine;

namespace SignalLost.Interactables
{
    public sealed class EscapeBoat : InteractableBase
    {
        public override string InteractionPrompt
        {
            get
            {
                GameManager manager = GameManager.Instance;
                bool ready = manager != null && manager.Progress.SignalSent;
                return ready ? "[E] Board Extraction Boat" : "[E] Inspect Extraction Boat";
            }
        }

        public override void Interact(PlayerInteraction interactor)
        {
            Sfx.PlayAt("Boat_Thruster", transform.position, 0.7f);
            GameManager.Instance?.AttemptEscape();
        }
    }
}
