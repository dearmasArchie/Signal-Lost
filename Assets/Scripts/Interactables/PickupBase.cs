using SignalLost.Audio;
using SignalLost.Player;
using UnityEngine;

namespace SignalLost.Interactables
{
    public abstract class PickupBase : InteractableBase
    {
        [SerializeField, Min(0f)] private float spinSpeed = 45f;
        [SerializeField, Min(0f)] private float bobHeight = 0.12f;
        [SerializeField, Min(0f)] private float bobSpeed = 2f;

        private Vector3 startLocalPosition;
        private bool collected;

        public bool IsCollected => collected;

        protected virtual void Start()
        {
            startLocalPosition = transform.localPosition;
        }

        protected virtual void Update()
        {
            if (!collected)
            {
                transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
                float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                transform.localPosition = startLocalPosition + Vector3.up * bob;
            }
        }

        public override bool CanInteract(PlayerInteraction interactor)
        {
            return base.CanInteract(interactor) && !collected;
        }

        public override void Interact(PlayerInteraction interactor)
        {
            if (collected || !Collect(interactor))
            {
                return;
            }

            collected = true;
            Sfx.PlayAt("UI_Pickup", transform.position, 0.7f);
            Destroy(gameObject);
        }

        protected abstract bool Collect(PlayerInteraction interactor);
    }
}
