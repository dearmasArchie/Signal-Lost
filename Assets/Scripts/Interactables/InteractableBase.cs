using SignalLost.Player;
using UnityEngine;

namespace SignalLost.Interactables
{
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        [SerializeField] private string interactionPrompt = "[E] Interact";

        public virtual string InteractionPrompt => interactionPrompt;

        public virtual bool CanInteract(PlayerInteraction interactor)
        {
            return isActiveAndEnabled;
        }

        public abstract void Interact(PlayerInteraction interactor);

        protected void SetPrompt(string prompt)
        {
            interactionPrompt = prompt;
        }

        protected static void SetRendererColor(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }
    }
}
