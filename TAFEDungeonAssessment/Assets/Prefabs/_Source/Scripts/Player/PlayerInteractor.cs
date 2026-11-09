using UnityEngine;
using UnityEngine.InputSystem;

namespace PuzzleMechanics
{
    /// <summary>
    /// Lets the player use buttons, levers and anything else that implements IInteractable.
    /// Look at the object and press the interact key (E by default).
    ///
    /// You can add this component to YOUR OWN player too. It only needs a camera.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The player's camera. Found automatically if left empty.")]
        public Camera playerCamera;

        [Tooltip("Optional. Lets the ray ignore the object you are carrying.")]
        public PlayerCarrier carrier;

        [Header("Interaction")]
        [Tooltip("The key used to pull levers, press switches and open chests.")]
        public Key interactKey = Key.E;

        [Tooltip("How far away (metres) the player can reach levers, switches and chests.")]
        public float reach = 3f;

        private IInteractable pressedTarget;
        private readonly RaycastHit[] hits = new RaycastHit[8];

        /// <summary>What the player is currently looking at (null if nothing usable).</summary>
        public IInteractable CurrentTarget { get; private set; }

        private void Awake()
        {
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();
            if (carrier == null)
                carrier = GetComponent<PlayerCarrier>();
        }

        private void Update()
        {
            CurrentTarget = FindTarget();

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard[interactKey].wasPressedThisFrame && CurrentTarget != null)
            {
                pressedTarget = CurrentTarget;
                pressedTarget.Interact(gameObject);
            }

            // Release when the key comes up OR when the player looks away (matters for momentary buttons).
            if (pressedTarget != null &&
                (!keyboard[interactKey].isPressed || pressedTarget != CurrentTarget))
            {
                pressedTarget.EndInteract(gameObject);
                pressedTarget = null;
            }
        }

        private IInteractable FindTarget()
        {
            if (playerCamera == null)
                return null;

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            int count = Physics.RaycastNonAlloc(ray, hits, reach, ~0, QueryTriggerInteraction.Ignore);

            // Find the closest hit that isn't the player or the object being carried.
            RaycastHit? closest = null;
            for (int i = 0; i < count; i++)
            {
                Collider c = hits[i].collider;
                if (c.transform.IsChildOf(transform))
                    continue;
                if (carrier != null && carrier.HeldBody != null && c.attachedRigidbody == carrier.HeldBody)
                    continue;
                if (closest == null || hits[i].distance < closest.Value.distance)
                    closest = hits[i];
            }

            return closest?.collider.GetComponentInParent<IInteractable>();
        }

        private void OnDisable()
        {
            if (pressedTarget != null)
                pressedTarget.EndInteract(gameObject);
            pressedTarget = null;
        }
    }
}
