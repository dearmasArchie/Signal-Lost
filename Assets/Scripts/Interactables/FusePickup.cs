using SignalLost.Core;
using SignalLost.Player;
using UnityEngine;

namespace SignalLost.Interactables
{
    public sealed class FusePickup : PickupBase
    {
        [SerializeField] private string pickupPrompt = "[E] Pick Up Override Fuse";

        public override string InteractionPrompt => pickupPrompt;

        protected override bool Collect(PlayerInteraction interactor)
        {
            GameManager manager = GameManager.Instance;

            if (manager == null || interactor.Inventory == null)
            {
                return false;
            }

            if (manager.Progress.FusesCollected >= manager.Progress.RequiredFuses)
            {
                manager.ShowNotification("All override fuses have already been recovered.");
                return false;
            }

            int before = manager.Progress.FusesCollected;
            interactor.Inventory.CollectFuse();
            return manager.Progress.FusesCollected > before;
        }
    }
}
