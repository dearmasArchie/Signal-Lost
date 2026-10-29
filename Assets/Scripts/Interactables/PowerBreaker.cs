using System;
using SignalLost.Audio;
using SignalLost.Core;
using SignalLost.Player;
using UnityEngine;

namespace SignalLost.Interactables
{
    public sealed class PowerBreaker : InteractableBase
    {
        [SerializeField] private DoorInteractable[] poweredDoors;
        [SerializeField] private Renderer statusRenderer;
        [SerializeField] private Color offlineColor = new Color(1f, 0.25f, 0.15f);
        [SerializeField] private Color onlineColor = new Color(0.2f, 1f, 0.45f);

        public bool IsOn { get; private set; }

        public event Action Powered;

        private void Awake()
        {
            SetRendererColor(statusRenderer, IsOn ? onlineColor : offlineColor);
        }

        public override string InteractionPrompt => IsOn ? "Power Breaker - Online" : "[E] Flip Power Switch";

        public override bool CanInteract(PlayerInteraction interactor)
        {
            return base.CanInteract(interactor) && !IsOn;
        }

        public override void Interact(PlayerInteraction interactor)
        {
            if (IsOn)
            {
                return;
            }

            IsOn = true;
            SetRendererColor(statusRenderer, onlineColor);
            Sfx.PlayAt("UI_Switch", transform.position, 0.75f);

            if (poweredDoors != null)
            {
                foreach (DoorInteractable door in poweredDoors)
                {
                    door?.SetLocked(false);
                }
            }

            GameManager.Instance?.ShowNotification("Auxiliary power restored. Bunker doors unlocked.");
            Powered?.Invoke();
        }
    }
}
