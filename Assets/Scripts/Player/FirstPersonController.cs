using SignalLost.Core;
using UnityEngine;

namespace SignalLost.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float walkSpeed = 4.5f;
        [SerializeField, Min(0.1f)] private float sprintSpeed = 7f;
        [SerializeField, Min(0.1f)] private float jumpHeight = 1.15f;
        [SerializeField] private float gravity = -20f;
        [SerializeField, Min(0.01f)] private float lookSensitivity = 0.12f;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float maxPitch = 85f;

        private CharacterController controller;
        private PlayerInputReader input;
        private float verticalVelocity;
        private float yaw;
        private float pitch;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();

            if (cameraPivot == null)
            {
                Camera childCamera = GetComponentInChildren<Camera>(true);
                cameraPivot = childCamera != null ? childCamera.transform : null;
            }

            yaw = transform.eulerAngles.y;
        }

        private void Update()
        {
            GameManager manager = GameManager.Instance;

            if (manager == null || manager.State != GameState.Playing || input == null || input.GameplayBlocked)
            {
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            HandleLook();
            HandleMovement();
        }

        private void HandleLook()
        {
            Vector2 look = input.LookInput;
            yaw += look.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - look.y * lookSensitivity, -maxPitch, maxPitch);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            Vector2 move = input.MoveInput;

            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            Vector3 planarMovement = transform.right * move.x + transform.forward * move.y;
            float speed = input.SprintHeld ? sprintSpeed : walkSpeed;

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (controller.isGrounded && input.JumpPressedThisFrame)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;
            Vector3 velocity = planarMovement * speed + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            yaw = rotation.eulerAngles.y;
            pitch = 0f;

            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.identity;
            }

            controller.enabled = true;
        }
    }
}
