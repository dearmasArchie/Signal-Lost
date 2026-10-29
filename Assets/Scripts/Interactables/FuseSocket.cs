using SignalLost.Core;
using SignalLost.Player;
using UnityEngine;

namespace SignalLost.Interactables
{
    public sealed class FuseSocket : InteractableBase
    {
        [SerializeField] private int socketNumber = 1;
        [SerializeField] private Transform fuseVisual;
        [SerializeField] private Renderer statusRenderer;
        [SerializeField] private Color emptyColor = new Color(1f, 0.25f, 0.15f);
        [SerializeField] private Color installedColor = new Color(0.2f, 1f, 0.45f);

        public bool IsInstalled { get; private set; }

        private void Awake()
        {
            ApplyVisuals(false);
        }

        public override string InteractionPrompt
        {
            get
            {
                if (IsInstalled)
                {
                    return $"Fuse Socket {socketNumber} - Online";
                }

                GameManager manager = GameManager.Instance;
                bool hasFuse = manager != null && manager.Progress.AvailableFuses > 0;
                return hasFuse ? "[E] Insert Override Fuse" : "Requires an Override Fuse";
            }
        }

        public override bool CanInteract(PlayerInteraction interactor)
        {
            return base.CanInteract(interactor)
                && !IsInstalled
                && GameManager.Instance != null
                && GameManager.Instance.Progress.AvailableFuses > 0;
        }

        public override void Interact(PlayerInteraction interactor)
        {
            if (IsInstalled)
            {
                return;
            }

            GameManager manager = GameManager.Instance;

            if (manager == null)
            {
                return;
            }

            int before = manager.Progress.FusesInstalled;
            manager.NotifyFuseInstalled();

            if (manager.Progress.FusesInstalled <= before)
            {
                manager.ShowNotification("No override fuse is available.");
                return;
            }

            IsInstalled = true;
            ApplyVisuals(true);
        }

        private void ApplyVisuals(bool installed)
        {
            if (fuseVisual != null)
            {
                fuseVisual.gameObject.SetActive(installed);
            }

            SetRendererColor(statusRenderer, installed ? installedColor : emptyColor);
        }
    }
}
