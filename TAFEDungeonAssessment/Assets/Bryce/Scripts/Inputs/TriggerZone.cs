using System.Collections.Generic;
using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// An invisible area that turns ON while the player (and/or a carryable object) is inside it.
    /// Great for "walk here to open the door" moments.
    ///
    /// Setup: needs a collider set to "Is Trigger" on the SAME GameObject.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TriggerZone : PuzzleActivator
    {
        [Header("What counts")]
        [Tooltip("Objects tagged \"Player\" activate the zone.")]
        public bool detectPlayer = true;

        [Tooltip("Objects with a Carryable component (cubes) activate the zone.")]
        public bool detectCarryables = false;

        [Header("Behaviour")]
        [Tooltip("If ticked, the zone stays on forever after the first time something enters it.")]
        public bool stayActivated = false;

        private readonly HashSet<Collider> inside = new HashSet<Collider>();

        protected override void Awake()
        {
            base.Awake();
            GetComponent<Collider>().isTrigger = true;
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
            inside.RemoveWhere(c => c == null || !c.gameObject.activeInHierarchy);

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
