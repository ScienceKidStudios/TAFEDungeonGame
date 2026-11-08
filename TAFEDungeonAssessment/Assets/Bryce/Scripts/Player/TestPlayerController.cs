using UnityEngine;
using UnityEngine.InputSystem;

namespace PuzzleMechanics
{
    /// <summary>
    /// A basic first-person controller for the mechanics test scene.
    /// WASD to move, mouse to look, Space to jump, Shift to sprint, Esc to free the mouse.
    ///
    /// This is only here so the test scene works by itself. You can use your own player instead:
    /// tag it "Player" and give it a PlayerInteractor (or call IInteractable yourself).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class TestPlayerController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The first-person camera (a child of the player).")]
        public Camera playerCamera;

        [Header("Movement")]
        public float walkSpeed = 4.5f;
        public float sprintSpeed = 7f;
        public float jumpHeight = 1.1f;
        public float gravity = -20f;

        [Header("Looking")]
        [Tooltip("Degrees turned per pixel of mouse movement.")]
        public float mouseSensitivity = 0.1f;
        public float maxLookAngle = 85f;

        [Header("Pushing")]
        [Tooltip("Rigidbodies up to this mass (kg) can be pushed by walking into them.")]
        public float maxPushMass = 150f;

        [Tooltip("Speed (m/s) at which light objects are pushed.")]
        public float pushSpeed = 3f;

        [Tooltip("Objects heavier than this (kg) are pushed more slowly: speed = Push Speed x (this / mass).")]
        public float easyPushMass = 40f;

        private CharacterController controller;
        private float verticalVelocity;
        private float pitch;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();
        }

        private void Start()
        {
            LockCursor(true);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null || mouse == null)
                return;

            // Esc frees the mouse, clicking captures it again.
            if (keyboard.escapeKey.wasPressedThisFrame)
                LockCursor(false);
            else if (Cursor.lockState != CursorLockMode.Locked && mouse.leftButton.wasPressedThisFrame)
                LockCursor(true);

            if (Cursor.lockState == CursorLockMode.Locked)
                Look(mouse.delta.ReadValue());

            Move(keyboard);
        }

        private void Look(Vector2 mouseDelta)
        {
            transform.Rotate(0f, mouseDelta.x * mouseSensitivity, 0f);

            pitch = Mathf.Clamp(pitch - mouseDelta.y * mouseSensitivity, -maxLookAngle, maxLookAngle);
            if (playerCamera != null)
                playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void Move(Keyboard keyboard)
        {
            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);

            float speed = keyboard.leftShiftKey.isPressed ? sprintSpeed : walkSpeed;
            Vector3 move = (transform.right * input.x + transform.forward * input.y) * speed;

            if (controller.isGrounded)
            {
                verticalVelocity = -2f; // keeps the controller snapped to the ground
                if (keyboard.spaceKey.wasPressedThisFrame)
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            verticalVelocity += gravity * Time.deltaTime;
            move.y = verticalVelocity;

            controller.Move(move * Time.deltaTime);
        }

        // Lets the player shove light-ish objects by walking into them.
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody rb = hit.collider.attachedRigidbody;
            if (rb == null || rb.isKinematic || rb.mass > maxPushMass)
                return;

            // Don't push things we are standing on.
            if (hit.moveDirection.y < -0.3f)
                return;

            // Heavier objects move more slowly, so mass still matters when pushing.
            float speed = pushSpeed * Mathf.Min(1f, easyPushMass / rb.mass);
            Vector3 pushVelocity = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z).normalized * speed;
            pushVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = pushVelocity;
        }

        private static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
