using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// An energy field (like Portal's "Material Emancipation Grid").
    /// Players can walk through it, but any Carryable object that touches it is destroyed.
    /// Pair it with an ObjectDispenser so destroyed cubes are replaced.
    ///
    /// By default the field switches OFF when its inputs are satisfied (e.g. a lever).
    /// With no inputs connected, it is always on.
    ///
    /// Setup: needs a collider set to "Is Trigger" on the SAME GameObject.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Fizzler : PuzzleReceiver
    {
        [Header("Fizzler")]
        [Tooltip("Ticked: inputs turn the field OFF. Unticked: inputs turn the field ON.")]
        public bool inputsTurnFieldOff = true;

        [Tooltip("The visible part of the field. It is hidden while the field is off.")]
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
