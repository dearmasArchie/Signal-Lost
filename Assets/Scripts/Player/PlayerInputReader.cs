using UnityEngine;
using UnityEngine.InputSystem;

namespace SignalLost.Player
{
    [DefaultExecutionOrder(-50)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        public const string ControlsResourcePath = "Input/SignalLostControls";

        [SerializeField] private InputActionAsset inputActions;

        private InputActionMap gameplayMap;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction sprintAction;
        private InputAction interactAction;
        private InputAction fireAction;
        private InputAction aimAction;
        private InputAction reloadAction;
        private InputAction healAction;
        private InputAction pauseAction;
        private bool gameplayBlocked;

        public InputActionAsset InputActions => inputActions;

        public bool IsReady => gameplayMap != null;

        public Vector2 MoveInput => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        public Vector2 LookInput => lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;

        public bool JumpPressedThisFrame => jumpAction != null && jumpAction.WasPressedThisFrame();

        public bool SprintHeld => sprintAction != null && sprintAction.IsPressed();

        public bool InteractPressedThisFrame => interactAction != null && interactAction.WasPressedThisFrame();

        public bool FireHeld => fireAction != null && fireAction.IsPressed();

        public bool AimHeld => aimAction != null && aimAction.IsPressed();

        public bool ReloadPressedThisFrame => reloadAction != null && reloadAction.WasPressedThisFrame();

        public bool HealPressedThisFrame => healAction != null && healAction.WasPressedThisFrame();

        public bool PausePressedThisFrame => pauseAction != null && pauseAction.WasPressedThisFrame();

        public bool GameplayBlocked => gameplayBlocked;

        private void Awake()
        {
            if (inputActions == null)
            {
                inputActions = Resources.Load<InputActionAsset>(ControlsResourcePath);
            }

            if (inputActions == null)
            {
                Debug.LogError($"[SignalLostInput] Missing input actions at Resources/{ControlsResourcePath}.");
                return;
            }

            gameplayMap = inputActions.FindActionMap("Gameplay", false);

            if (gameplayMap == null)
            {
                Debug.LogError("[SignalLostInput] Gameplay action map is missing.");
                return;
            }

            moveAction = FindAction("Move");
            lookAction = FindAction("Look");
            jumpAction = FindAction("Jump");
            sprintAction = FindAction("Sprint");
            interactAction = FindAction("Interact");
            fireAction = FindAction("Fire");
            aimAction = FindAction("Aim");
            reloadAction = FindAction("Reload");
            healAction = FindAction("Heal");
            pauseAction = FindAction("Pause");
        }

        private void OnEnable()
        {
            if (!gameplayBlocked)
            {
                gameplayMap?.Enable();
            }
        }

        private void OnDisable()
        {
            gameplayMap?.Disable();
        }

        public void SetGameplayBlocked(bool blocked)
        {
            gameplayBlocked = blocked;

            if (blocked)
            {
                gameplayMap?.Disable();
            }
            else if (isActiveAndEnabled)
            {
                gameplayMap?.Enable();
            }
        }

        private InputAction FindAction(string actionName)
        {
            InputAction action = gameplayMap.FindAction(actionName, false);

            if (action == null)
            {
                Debug.LogError($"[SignalLostInput] Missing gameplay action: {actionName}");
            }

            return action;
        }
    }
}
