using UnityEngine;
using UnityEngine.InputSystem;

namespace PuzzleMechanics
{
    /// <summary>
    /// Lets the player pick up, drag and throw objects that have a Carryable component.
    /// Hold Left Mouse to carry, release to drop, Right Mouse to throw.
    ///
    /// THE MASS RULE (Rigidbody mass):
    ///   mass up to 'Max Lift Mass'  -> can be lifted, carried and thrown
    ///   mass up to 'Max Drag Mass'  -> too heavy to lift, but can be dragged along the floor
    ///   heavier than that           -> too heavy to move at all
    /// </summary>
    public class PlayerCarrier : MonoBehaviour
    {
        [Header("References")]
        public Camera playerCamera;

        [Header("Reach")]
        [Tooltip("How far away (metres) the player can grab things.")]
        public float reach = 3f;

        [Tooltip("How far in front of the camera a carried object is held.")]
        public float holdDistance = 1.8f;

        [Tooltip("If a carried object gets snagged this far from the hold point, it is dropped.")]
        public float breakDistance = 3f;

        [Header("Mass Rule")]
        [Tooltip("Objects up to this mass (kg) can be lifted and thrown.")]
        public float maxLiftMass = 60f;

        [Tooltip("Objects up to this mass (kg) can be dragged but not lifted.")]
        public float maxDragMass = 150f;

        [Header("Carrying")]
        [Tooltip("How quickly a carried object follows the hold point. Heavier objects follow more slowly.")]
        public float carryResponsiveness = 12f;
        public float maxCarrySpeed = 12f;

        [Tooltip("Force used to drag heavy objects. Bigger = heavy objects drag more easily.")]
        public float dragStrength = 900f;
        public float maxDragForce = 1500f;

        [Header("Throwing")]
        [Tooltip("Throw impulse. Heavier objects fly slower (speed = impulse / mass).")]
        public float throwImpulse = 220f;

        [Tooltip("Speed limit (m/s) for very light objects, so a small ball isn't thrown like a bullet.")]
        public float maxThrowSpeed = 16f;

        private Rigidbody heldBody;
        private Carryable heldCarryable;
        private Collider[] heldColliders;
        private Collider playerCollider;
        private bool isHolding;
        private bool isLifting;
        private bool savedUseGravity;

        private string message;
        private float messageTimer;

        /// <summary>The Rigidbody currently being carried or dragged (null if none).</summary>
        public Rigidbody HeldBody => heldBody;
        public bool IsLifting => heldBody != null && isLifting;

        /// <summary>Short status text for the HUD, e.g. "Too heavy to move (250 kg)".</summary>
        public string Message => messageTimer > 0f ? message : null;

        private void Awake()
        {
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();
            playerCollider = GetComponent<Collider>();
        }

        private void Update()
        {
            messageTimer -= Time.deltaTime;

            // The object may have been destroyed (fizzler, dispenser) while held.
            if (isHolding && heldBody == null)
                ClearHeld();

            Mouse mouse = Mouse.current;
            if (mouse == null || Cursor.lockState != CursorLockMode.Locked)
                return;

            if (mouse.leftButton.wasPressedThisFrame && heldBody == null)
                TryPickUp();
            else if (mouse.leftButton.wasReleasedThisFrame && heldBody != null)
                Drop();
            else if (mouse.rightButton.wasPressedThisFrame && heldBody != null)
                Throw();
        }

        private void FixedUpdate()
        {
            if (heldBody == null)
                return;

            Vector3 target = playerCamera.transform.position + playerCamera.transform.forward * holdDistance;
            Vector3 toTarget = target - heldBody.worldCenterOfMass;

            if (toTarget.magnitude > breakDistance)
            {
                Drop();
                return;
            }

            if (isLifting)
            {
                // Light object: steer its velocity straight towards the hold point.
                // Objects near the lift limit feel sluggish (half speed at the limit).
                float weightFactor = Mathf.Lerp(1f, 0.5f, heldBody.mass / maxLiftMass);
                heldBody.linearVelocity = Vector3.ClampMagnitude(toTarget * carryResponsiveness * weightFactor, maxCarrySpeed);
                heldBody.angularVelocity *= 0.8f;
            }
            else
            {
                // Heavy object: pull it along the floor, but never upwards.
                Vector3 force = toTarget * dragStrength - heldBody.linearVelocity * (dragStrength * 0.1f);
                force.y = Mathf.Min(force.y, 0f);
                heldBody.AddForce(Vector3.ClampMagnitude(force, maxDragForce), ForceMode.Force);
            }
        }

        /// <summary>Returns the Carryable under the crosshair (or null). Used by the HUD.</summary>
        public Carryable LookTarget()
        {
            if (heldBody != null || playerCamera == null)
                return null;

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, reach, ~0, QueryTriggerInteraction.Ignore) &&
                hit.rigidbody != null)
                return hit.rigidbody.GetComponent<Carryable>();

            return null;
        }

        private void TryPickUp()
        {
            Carryable carryable = LookTarget();
            if (carryable == null || carryable.Body.isKinematic)
                return;

            float mass = carryable.Mass;
            if (mass > maxDragMass)
            {
                ShowMessage($"Too heavy to move ({mass:0} kg)");
                return;
            }

            heldCarryable = carryable;
            heldBody = carryable.Body;
            isLifting = mass <= maxLiftMass;
            heldCarryable.IsHeld = isLifting; // dragged objects still press plates
            heldColliders = heldBody.GetComponentsInChildren<Collider>();
            isHolding = true;
            SetIgnorePlayer(true);

            if (isLifting)
            {
                savedUseGravity = heldBody.useGravity;
                heldBody.useGravity = false;
            }
            else
            {
                ShowMessage($"Too heavy to lift ({mass:0} kg) - dragging");
            }

            heldBody.WakeUp();
        }

        private void Drop()
        {
            if (heldBody != null && isLifting)
                heldBody.useGravity = savedUseGravity;
            ClearHeld();
        }

        private void Throw()
        {
            if (!isLifting)
                return; // can't throw something you can't lift

            Rigidbody body = heldBody;
            Drop();
            float speed = Mathf.Min(throwImpulse / body.mass, maxThrowSpeed);
            body.AddForce(playerCamera.transform.forward * speed, ForceMode.VelocityChange);
        }

        private void ClearHeld()
        {
            if (heldCarryable != null)
                heldCarryable.IsHeld = false;
            SetIgnorePlayer(false);

            heldBody = null;
            heldCarryable = null;
            heldColliders = null;
            isHolding = false;
        }

        // Stops the held object from bumping into (and pushing) the player.
        private void SetIgnorePlayer(bool ignore)
        {
            if (playerCollider == null || heldColliders == null)
                return;

            foreach (Collider c in heldColliders)
            {
                if (c != null)
                    Physics.IgnoreCollision(playerCollider, c, ignore);
            }
        }

        private void ShowMessage(string text)
        {
            message = text;
            messageTimer = 2f;
        }

        private void OnDisable()
        {
            Drop();
        }
    }
}
