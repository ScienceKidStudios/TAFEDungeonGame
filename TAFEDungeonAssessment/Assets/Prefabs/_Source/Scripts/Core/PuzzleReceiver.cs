using System.Collections.Generic;
using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// Base class for every puzzle OUTPUT (door, dispenser, brazier, magic barrier...).
    ///
    /// Drag any inputs (plates, switches, levers, runestones...) into the Inputs list.
    /// Whenever one of them changes, the output checks whether its inputs are on
    /// (ALL of them, or ANY of them) and calls OnInputsChanged.
    /// </summary>
    public abstract class PuzzleReceiver : MonoBehaviour
    {
        [Header("Inputs")]
        [Tooltip("What makes this work. Drag levers, plates, switches, gongs or runestones from the Hierarchy into this list.")]
        public List<PuzzleActivator> inputs = new List<PuzzleActivator>();

        [Tooltip("All = every input must be on. Any = just one input is enough.")]
        public InputMode inputMode = InputMode.All;

        /// <summary>True while the inputs are satisfied.</summary>
        public bool InputsSatisfied { get; private set; }

        protected virtual void OnEnable()
        {
            foreach (PuzzleActivator input in inputs)
            {
                if (input != null)
                    input.StateChanged += HandleInputChanged;
            }
        }

        protected virtual void OnDisable()
        {
            foreach (PuzzleActivator input in inputs)
            {
                if (input != null)
                    input.StateChanged -= HandleInputChanged;
            }
        }

        protected virtual void Start()
        {
            // Only take control at start if something is actually connected.
            // This lets an unconnected door or dispenser be driven purely by UnityEvents.
            if (PuzzleLogic.HasAnyInput(inputs))
            {
                InputsSatisfied = PuzzleLogic.Evaluate(inputs, inputMode);
                OnInputsChanged(InputsSatisfied);
            }
        }

        /// <summary>Connects another input at runtime (from code).</summary>
        public void AddInput(PuzzleActivator input)
        {
            if (input == null || inputs.Contains(input))
                return;

            inputs.Add(input);
            if (isActiveAndEnabled)
            {
                input.StateChanged += HandleInputChanged;
                HandleInputChanged(input);
            }
        }

        private void HandleInputChanged(PuzzleActivator changed)
        {
            bool satisfied = PuzzleLogic.Evaluate(inputs, inputMode);
            if (satisfied == InputsSatisfied)
                return;

            InputsSatisfied = satisfied;
            OnInputsChanged(satisfied);
        }

        /// <summary>Called when the inputs go from unsatisfied to satisfied, or back again.</summary>
        protected abstract void OnInputsChanged(bool satisfied);

        protected virtual void OnDrawGizmos()
        {
            PuzzleLogic.DrawConnectionGizmos(inputs, transform.position);
        }
    }
}
