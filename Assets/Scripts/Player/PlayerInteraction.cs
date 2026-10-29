using System;
using SignalLost.Core;
using SignalLost.Interactables;
using UnityEngine;

namespace SignalLost.Player
{
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private Camera interactionCamera;
        [SerializeField, Min(0.5f)] private float interactionDistance = 3.25f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private PlayerInputReader input;
        private string lastPrompt;

        public PlayerInventory Inventory { get; private set; }

        public PlayerHealth Health { get; private set; }

        public ArcPistol Weapon { get; private set; }

        public IInteractable CurrentTarget { get; private set; }

        public event Action<IInteractable> TargetChanged;

        public event Action<string> PromptChanged;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            Inventory = GetComponent<PlayerInventory>();
            Health = GetComponent<PlayerHealth>();
            Weapon = GetComponentInChildren<ArcPistol>(true);

            if (interactionCamera == null)
            {
                interactionCamera = GetComponentInChildren<Camera>(true);
            }
        }

        private void Update()
        {
            GameManager manager = GameManager.Instance;

            if (manager == null || manager.State != GameState.Playing)
            {
                SetTarget(null);
                return;
            }

            IInteractable target = FindTarget();
            SetTarget(target);

            if (target != null && input != null && input.InteractPressedThisFrame && target.CanInteract(this))
            {
                target.Interact(this);
            }
        }

        private IInteractable FindTarget()
        {
            if (interactionCamera == null)
            {
                return null;
            }

            Ray ray = new Ray(interactionCamera.transform.position, interactionCamera.transform.forward);

            if (!Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    interactionDistance,
                    interactionMask,
                    QueryTriggerInteraction.Collide))
            {
                return null;
            }

            return hit.collider.GetComponentInParent<IInteractable>();
        }

        private void SetTarget(IInteractable target)
        {
            if (!ReferenceEquals(CurrentTarget, target))
            {
                CurrentTarget = target;
                TargetChanged?.Invoke(target);
            }

            string prompt = target != null ? target.InteractionPrompt : string.Empty;

            if (prompt != lastPrompt)
            {
                lastPrompt = prompt;
                PromptChanged?.Invoke(prompt);
            }
        }
    }
}
