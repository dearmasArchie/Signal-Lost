using SignalLost.Core;
using SignalLost.Player;
using UnityEngine;

namespace SignalLost.Interactables
{
    public sealed class ArcPistolPickup : PickupBase
    {
        [SerializeField] private string pickupPrompt = "[E] Pick Up Arc Pistol";

        public override string InteractionPrompt => pickupPrompt;

        protected override bool Collect(PlayerInteraction interactor)
        {
            if (interactor.Inventory == null)
            {
                return false;
            }

            if (interactor.Inventory.HasArcPistol)
            {
                GameManager.Instance?.ShowNotification("The Arc Pistol is already equipped.");
                return false;
            }

            interactor.Inventory.GiveArcPistol();
            return true;
        }
    }
}
