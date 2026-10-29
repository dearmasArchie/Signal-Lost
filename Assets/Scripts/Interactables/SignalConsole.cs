using SignalLost.Core;
using SignalLost.Player;
using UnityEngine;

namespace SignalLost.Interactables
{
    public sealed class SignalConsole : InteractableBase
    {
        [SerializeField] private Renderer statusRenderer;
        [SerializeField] private Light beaconLight;
        [SerializeField] private Color waitingColor = new Color(1f, 0.55f, 0.15f);
        [SerializeField] private Color onlineColor = new Color(0.2f, 1f, 0.55f);

        private bool signalSent;

        private void Awake()
        {
            SetRendererColor(statusRenderer, waitingColor);

            if (beaconLight != null)
            {
                beaconLight.color = waitingColor;
                beaconLight.enabled = false;
            }
        }

        public override string InteractionPrompt
        {
            get
            {
                GameManager manager = GameManager.Instance;

                if (manager == null)
                {
                    return "Emergency Transmitter";
                }

                if (manager.Progress.SignalSent)
                {
                    return "Emergency Transmitter - Online";
                }

                if (manager.Progress.FusesInstalled < manager.Progress.RequiredFuses)
                {
                    return $"Fuse Console ({manager.Progress.FusesInstalled}/{manager.Progress.RequiredFuses})";
                }

                return "[E] Activate Emergency Transmitter";
            }
        }

        public override bool CanInteract(PlayerInteraction interactor)
        {
            GameManager manager = GameManager.Instance;
            return base.CanInteract(interactor)
                && manager != null
                && !manager.Progress.SignalSent
                && manager.Progress.FusesInstalled >= manager.Progress.RequiredFuses;
        }

        public override void Interact(PlayerInteraction interactor)
        {
            GameManager manager = GameManager.Instance;

            if (manager == null)
            {
                return;
            }

            if (manager.Progress.FusesInstalled < manager.Progress.RequiredFuses)
            {
                manager.ShowNotification("The transmitter needs all three override fuses.");
                return;
            }

            manager.NotifySignalSent();
            signalSent = manager.Progress.SignalSent;

            if (!signalSent)
            {
                return;
            }

            SetRendererColor(statusRenderer, onlineColor);

            if (beaconLight != null)
            {
                beaconLight.color = onlineColor;
                beaconLight.enabled = true;
            }
        }
    }
}
