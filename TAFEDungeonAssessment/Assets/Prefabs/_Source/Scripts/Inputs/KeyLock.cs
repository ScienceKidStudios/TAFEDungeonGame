using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// A lock that turns ON when the right key is brought close to it.
    /// The locked door prefab already has one of these wired into its Inputs list.
    ///
    /// Setup: needs a collider set to "Is Trigger" on the SAME GameObject. The key counts
    /// as soon as it is inside this box, even while the player is still holding it.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class KeyLock : PuzzleActivator
    {
        [Header("Lock")]
        [Tooltip("Which key opens this lock. Must match the key's Key Name. Leave blank to accept any key.")]
        public string keyName = "Iron Key";

        [Tooltip("Ticked: the key vanishes when it is used, like it stays in the lock.")]
        public bool useUpKey = true;

        [Tooltip("Optional. An object (like a padlock) that disappears when the lock opens.")]
        public GameObject hideWhenUnlocked;

        protected override void Awake()
        {
            base.Awake();
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsActive || other.attachedRigidbody == null)
                return;

            if (!other.attachedRigidbody.TryGetComponent(out KeyItem key))
                return;

            if (!string.IsNullOrEmpty(keyName) && key.keyName != keyName)
                return;

            Unlock();

            if (useUpKey && key.TryGetComponent(out Carryable carryable))
                carryable.Dissolve();
        }

        /// <summary>Opens the lock without a key (can be called from a UnityEvent).</summary>
        public void Unlock()
        {
            if (hideWhenUnlocked != null)
                hideWhenUnlocked.SetActive(false);
            SetActiveState(true);
        }

        private void OnDrawGizmos()
        {
            if (TryGetComponent(out BoxCollider box))
            {
                Gizmos.color = IsActive ? new Color(0.2f, 1f, 0.3f, 0.5f) : new Color(1f, 0.8f, 0.2f, 0.5f);
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
            }
        }
    }
}
