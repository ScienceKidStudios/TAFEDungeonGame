using System.Collections.Generic;
using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// A floor plate that turns ON while enough weight is resting on it.
    ///
    /// Weight = the total Rigidbody mass of everything inside the trigger box above the plate.
    /// Players with a CharacterController (no Rigidbody) count as 'Player Mass' if tagged "Player".
    ///
    /// Setup: this script needs a BoxCollider set to "Is Trigger" on the SAME GameObject.
    /// The box should sit just above the plate's surface.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class PressurePlate : PuzzleActivator
    {
        [Header("Weight")]
        [Tooltip("Total mass (kg) needed to press the plate.")]
        [Min(0f)] public float requiredMass = 20f;

        [Tooltip("Should players standing on the plate count towards the weight?")]
        public bool countPlayers = true;

        [Tooltip("Mass (kg) used for a player tagged \"Player\" that has no Rigidbody (e.g. a CharacterController).")]
        [Min(0f)] public float playerMass = 70f;

        [Tooltip("Ignore objects the player is currently holding (like Portal).")]
        public bool ignoreHeldObjects = true;

        [Header("Visuals")]
        [Tooltip("The part of the plate that moves down when pressed (optional).")]
        public Transform pad;

        [Tooltip("How far (metres) the pad sinks when pressed.")]
        public float pressDepth = 0.05f;

        [Tooltip("How fast the pad moves (metres per second).")]
        public float padSpeed = 0.5f;

        /// <summary>The weight currently on the plate, in kg. Handy for debugging.</summary>
        [Header("Debug (read only)")]
        [SerializeField] private float currentMass;
        public float CurrentMass => currentMass;

        // Rigidbodies on the plate, with how many of their colliders are inside the trigger.
        private readonly Dictionary<Rigidbody, int> bodies = new Dictionary<Rigidbody, int>();
        // Players without a Rigidbody (CharacterController players).
        private readonly HashSet<Collider> players = new HashSet<Collider>();
        private readonly List<Rigidbody> toRemove = new List<Rigidbody>();

        private Vector3 padUpPosition;

        protected override void Awake()
        {
            base.Awake();
            GetComponent<BoxCollider>().isTrigger = true;
            if (pad != null)
                padUpPosition = pad.localPosition;
        }

        private void OnTriggerEnter(Collider other)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                bodies.TryGetValue(rb, out int count);
                bodies[rb] = count + 1;
            }
            else if (IsPlayer(other))
            {
                players.Add(other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null && bodies.TryGetValue(rb, out int count))
            {
                if (count <= 1)
                    bodies.Remove(rb);
                else
                    bodies[rb] = count - 1;
            }
            players.Remove(other);
        }

        private void FixedUpdate()
        {
            currentMass = 0f;

            // Objects that are destroyed or disabled don't always send OnTriggerExit, so clean them up.
            toRemove.Clear();
            foreach (Rigidbody rb in bodies.Keys)
            {
                if (rb == null || !rb.gameObject.activeInHierarchy)
                {
                    toRemove.Add(rb);
                    continue;
                }

                if (ignoreHeldObjects && rb.TryGetComponent(out Carryable carryable) && carryable.IsHeld)
                    continue;

                currentMass += rb.mass;
            }
            foreach (Rigidbody rb in toRemove)
                bodies.Remove(rb);

            players.RemoveWhere(p => p == null || !p.gameObject.activeInHierarchy);
            if (countPlayers)
                currentMass += players.Count * playerMass;

            SetActiveState(currentMass >= requiredMass);
        }

        private void Update()
        {
            if (pad == null)
                return;

            Vector3 target = IsActive ? padUpPosition + Vector3.down * pressDepth : padUpPosition;
            pad.localPosition = Vector3.MoveTowards(pad.localPosition, target, padSpeed * Time.deltaTime);
        }

        private static bool IsPlayer(Collider other)
        {
            return other.CompareTag("Player") || other.transform.root.CompareTag("Player");
        }

        private void OnDrawGizmos()
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null)
                return;

            Gizmos.color = IsActive ? new Color(0.2f, 1f, 0.3f, 0.6f) : new Color(1f, 0.8f, 0.2f, 0.6f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
