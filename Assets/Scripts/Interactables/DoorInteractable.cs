using System.Collections;
using SignalLost.Audio;
using SignalLost.Core;
using SignalLost.Player;
using UnityEngine;

namespace SignalLost.Interactables
{
    public sealed class DoorInteractable : InteractableBase
    {
        [SerializeField] private Transform doorPanel;
        [SerializeField] private Vector3 openOffset = new Vector3(0f, 2.4f, 0f);
        [SerializeField, Min(0.05f)] private float moveDuration = 0.55f;
        [SerializeField] private bool startsLocked;
        [SerializeField] private bool startsOpen;

        private Vector3 closedLocalPosition;
        private Vector3 openLocalPosition;
        private Coroutine movement;
        private bool isOpen;
        private bool isLocked;

        public bool IsOpen => isOpen;

        public bool IsLocked => isLocked;

        private void Awake()
        {
            if (doorPanel == null)
            {
                doorPanel = transform;
            }

            closedLocalPosition = doorPanel.localPosition;
            openLocalPosition = closedLocalPosition + openOffset;
            isLocked = startsLocked;
            isOpen = startsOpen;

            if (isOpen)
            {
                doorPanel.localPosition = openLocalPosition;
            }
        }

        public override string InteractionPrompt
        {
            get
            {
                if (isLocked)
                {
                    return "Locked - restore auxiliary power or enter the access code";
                }

                return isOpen ? "[E] Close Door" : "[E] Open Door";
            }
        }

        public override bool CanInteract(PlayerInteraction interactor)
        {
            return base.CanInteract(interactor) && !isLocked;
        }

        public override void Interact(PlayerInteraction interactor)
        {
            if (isLocked)
            {
                GameManager.Instance?.ShowNotification("The door is locked.");
                Sfx.PlayAt("UI_Error", doorPanel.position, 0.5f);
                return;
            }

            Toggle();
        }

        public void SetLocked(bool locked)
        {
            isLocked = locked;
        }

        public void Unlock()
        {
            isLocked = false;
        }

        public void Open()
        {
            if (!isOpen)
            {
                Toggle();
            }
        }

        public void Close()
        {
            if (isOpen)
            {
                Toggle();
            }
        }

        public void Toggle()
        {
            if (isLocked)
            {
                return;
            }

            isOpen = !isOpen;
            Vector3 target = isOpen ? openLocalPosition : closedLocalPosition;

            if (movement != null)
            {
                StopCoroutine(movement);
            }

            Sfx.PlayAt(isOpen ? "Door_Open" : "Door_Close", doorPanel.position, 0.7f);
            movement = StartCoroutine(MovePanel(target));
        }

        private IEnumerator MovePanel(Vector3 target)
        {
            Vector3 start = doorPanel.localPosition;
            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);
                t = t * t * (3f - 2f * t);
                doorPanel.localPosition = Vector3.Lerp(start, target, t);
                yield return null;
            }

            doorPanel.localPosition = target;
            movement = null;
        }
    }
}
