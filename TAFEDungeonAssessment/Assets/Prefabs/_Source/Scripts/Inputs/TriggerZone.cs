using System.Collections.Generic;
using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// A rune circle on the floor that turns ON while the player (and/or a carryable object) is inside it.
    /// Great for "walk here to open the door" moments.
    ///
    /// Setup: needs a collider set to "Is Trigger" on the SAME GameObject.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TriggerZone : PuzzleActivator
    {
        [Header("What counts")]
        [Tooltip("Does the player turn it on by walking in? (The player must be tagged \"Player\".)")]
        public bool detectPlayer = true;

        [Tooltip("Do pickups (boxes, barrels, pots...) turn it on when they are inside?")]
        public bool detectCarryables = false;

        [Header("Behaviour")]
        [Tooltip("Ticked: stays on forever after the first time something steps in.")]
        public bool stayActivated = false;

        private readonly HashSet<Collider> inside = new HashSet<Collider>();
        private Collider zone;

        protected override void Awake()
        {
            base.Awake();
            zone = GetComponent<Collider>();
            zone.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Counts(other))
                inside.Add(other);
        }

        private void OnTriggerExit(Collider other)
        {
            inside.Remove(other);
        }

        private void FixedUpdate()
        {
            // Things that are destroyed, switched off or teleported away don't always send OnTriggerExit.
            inside.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy || !c.bounds.Intersects(zone.bounds));

            if (inside.Count > 0)
                SetActiveState(true);
            else if (!stayActivated)
                SetActiveState(false);
        }

        private bool Counts(Collider other)
        {
            if (detectPlayer && (other.CompareTag("Player") || other.transform.root.CompareTag("Player")))
                return true;

            if (detectCarryables && other.attachedRigidbody != null &&
                other.attachedRigidbody.GetComponent<Carryable>() != null)
                return true;

            return false;
        }

        private void OnDrawGizmos()
        {
            if (TryGetComponent(out BoxCollider box))
            {
                Gizmos.color = IsActive ? new Color(0.2f, 1f, 0.3f, 0.5f) : new Color(0.3f, 0.6f, 1f, 0.5f);
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
            }
        }
    }
}
