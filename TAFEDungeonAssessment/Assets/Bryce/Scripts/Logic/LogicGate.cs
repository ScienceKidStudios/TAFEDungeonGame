using System.Collections.Generic;
using UnityEngine;

namespace PuzzleMechanics
{
    public enum GateType
    {
        [Tooltip("On when ALL inputs are on.")]
        And,
        [Tooltip("On when ANY input is on.")]
        Or,
        [Tooltip("On when NO inputs are on (flips a single input).")]
        Not
    }

    /// <summary>
    /// Combines several inputs into one signal. Because a gate is itself a PuzzleActivator,
    /// its output can be plugged into a door, a light, a timer or even another gate.
    ///
    /// Example: two levers -> AND gate -> door  (both levers must be pulled).
    /// Avoid loops (a gate feeding back into itself), they will never settle.
    /// </summary>
    public class LogicGate : PuzzleActivator
    {
        [Header("Gate")]
        [Tooltip("And = all inputs on. Or = any input on. Not = on when no inputs are on.")]
        public GateType gateType = GateType.And;

        [Tooltip("The inputs this gate reads.")]
        public List<PuzzleActivator> inputs = new List<PuzzleActivator>();

        private void OnEnable()
        {
            foreach (PuzzleActivator input in inputs)
            {
                if (input != null)
                    input.StateChanged += HandleInputChanged;
            }
        }

        private void OnDisable()
        {
            foreach (PuzzleActivator input in inputs)
            {
                if (input != null)
                    input.StateChanged -= HandleInputChanged;
            }
        }

        protected override void Start()
        {
            base.Start();
            Evaluate();
        }

        private void HandleInputChanged(PuzzleActivator changed) => Evaluate();

        private void Evaluate()
        {
            bool result;
            switch (gateType)
            {
                case GateType.And: result = PuzzleLogic.Evaluate(inputs, InputMode.All); break;
                case GateType.Or: result = PuzzleLogic.Evaluate(inputs, InputMode.Any); break;
                default: result = !PuzzleLogic.Evaluate(inputs, InputMode.Any); break;
            }
            SetActiveState(result);
        }

        private void OnDrawGizmos()
        {
            PuzzleLogic.DrawConnectionGizmos(inputs, transform.position);
        }
    }
}
