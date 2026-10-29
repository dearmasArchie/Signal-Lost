using SignalLost.Player;

namespace SignalLost.Interactables
{
    public interface IInteractable
    {
        string InteractionPrompt { get; }

        bool CanInteract(PlayerInteraction interactor);

        void Interact(PlayerInteraction interactor);
    }
}
