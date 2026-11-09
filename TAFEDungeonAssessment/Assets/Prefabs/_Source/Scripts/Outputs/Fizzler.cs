using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// A magic barrier. Players can walk through it, but any pickup that touches it vanishes.
    /// Pair it with a dispenser so vanished boxes are replaced.
    ///
    /// By default the barrier switches OFF when its inputs are on (e.g. a lever).
    /// With no inputs connected, it is always on.
    ///
    /// Setup: needs a collider set to "Is Trigger" on the SAME GameObject.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Fizzler : PuzzleReceiver
    {
        [Header("Magic Barrier")]
        [Tooltip("Ticked: the inputs switch the barrier OFF. Unticked: the inputs switch it ON.")]
        public bool inputsTurnFieldOff = true;

        [Tooltip("The glowing part of the barrier. It is hidden while the barrier is off.")]
        public GameObject fieldVisual;

        public bool FieldActive { get; private set; } = true;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        protected override void OnInputsChanged(bool satisfied)
        {
            SetFieldActive(inputsTurnFieldOff ? !satisfied : satisfied);
        }

        /// <summary>Turns the field on or off directly (can be called from a UnityEvent).</summary>
        public void SetFieldActive(bool active)
        {
            FieldActive = active;
            if (fieldVisual != null)
                fieldVisual.SetActive(active);
        }

        // OnTriggerStay (not Enter) so objects already inside are removed when the field turns back on.
        private void OnTriggerStay(Collider other)
        {
            if (!FieldActive || other.attachedRigidbody == null)
                return;

            if (other.attachedRigidbody.TryGetComponent(out Carryable carryable))
                carryable.Dissolve();
        }
    }
}
