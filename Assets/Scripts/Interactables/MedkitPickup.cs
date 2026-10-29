using SignalLost.Player;
using UnityEngine;

namespace SignalLost.Interactables
{
    public sealed class MedkitPickup : PickupBase
    {
        [SerializeField, Min(1)] private int medkits = 1;
        [SerializeField] private string pickupPrompt = "[E] Pick Up Medkit";

        public override string InteractionPrompt => pickupPrompt;

        protected override bool Collect(PlayerInteraction interactor)
        {
            if (interactor.Inventory == null)
            {
                return false;
            }

            interactor.Inventory.AddMedkit(medkits);
            return true;
        }
    }
}
