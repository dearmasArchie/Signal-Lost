using SignalLost.Core;
using SignalLost.Player;
using UnityEngine;

namespace SignalLost.Interactables
{
    public sealed class AmmoPickup : PickupBase
    {
        [SerializeField, Min(1)] private int batteryCells = 30;
        [SerializeField] private string pickupPrompt = "[E] Pick Up Battery Cells";

        public override string InteractionPrompt => pickupPrompt;

        protected override bool Collect(PlayerInteraction interactor)
        {
            if (interactor.Weapon == null)
            {
                GameManager.Instance?.ShowNotification("You need the Arc Pistol to carry battery cells.");
                return false;
            }

            bool added = interactor.Weapon.AddAmmo(batteryCells);

            if (!added)
            {
                GameManager.Instance?.ShowNotification("Battery reserve is full.");
            }

            return added;
        }
    }
}
