using SignalLost.Audio;
using SignalLost.Core;
using SignalLost.Player;
using UnityEngine;

namespace SignalLost.Interactables
{
    public sealed class KeypadInteractable : InteractableBase
    {
        [SerializeField] private string accessCode = "4471";
        [SerializeField] private DoorInteractable[] controlledDoors;
        [SerializeField] private Renderer statusRenderer;
        [SerializeField] private Color lockedColor = new Color(1f, 0.25f, 0.15f);
        [SerializeField] private Color unlockedColor = new Color(0.2f, 1f, 0.45f);

        public KeypadLock Lock { get; private set; }

        private void Awake()
        {
            Lock = new KeypadLock(accessCode);
            Lock.Unlocked += HandleUnlocked;
            SetRendererColor(statusRenderer, lockedColor);
            SetPrompt("[E] Enter Armory Code");
        }

        public override void Interact(PlayerInteraction interactor)
        {
            if (Lock.IsUnlocked)
            {
                GameManager.Instance?.ShowNotification("Armory access granted.");
                return;
            }

            GameManager.Instance?.RequestKeypad(this);
        }

        public bool TrySubmitCode(string code)
        {
            if (Lock.TryUnlock(code))
            {
                GameManager.Instance?.ShowNotification("Access granted. Armory door unlocked.");
                Sfx.Play2D("UI_Confirm", 0.75f);
                return true;
            }

            GameManager.Instance?.ShowNotification("Access denied. Incorrect code.");
            Sfx.Play2D("UI_Error", 0.7f);
            return false;
        }

        private void HandleUnlocked()
        {
            SetRendererColor(statusRenderer, unlockedColor);
            SetPrompt("Armory Keypad - Unlocked");

            if (controlledDoors == null)
            {
                return;
            }

            foreach (DoorInteractable door in controlledDoors)
            {
                if (door == null)
                {
                    continue;
                }

                door.SetLocked(false);
                door.Open();
            }
        }

        private void OnDestroy()
        {
            if (Lock != null)
            {
                Lock.Unlocked -= HandleUnlocked;
            }
        }
    }
}
