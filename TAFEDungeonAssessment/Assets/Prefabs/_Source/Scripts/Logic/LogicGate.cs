using System.Collections.Generic;
using UnityEngine;

namespace PuzzleMechanics
{
    public enum GateType
    {
        [Tooltip("On when ALL of its inputs are on.")]
        And,
        [Tooltip("On when ANY one of its inputs is on.")]
        Or,
        [Tooltip("On when NONE of its inputs are on. Use it to flip a single input.")]
        Not
    }

    /// <summary>
    /// A runestone that combines several inputs into one signal. Because it is itself an input,
    /// it can be plugged into a door, a brazier, a timer or even another runestone.
    ///
    /// Example: two levers -> AND runestone -> door  (both levers must be pulled).
    /// Avoid loops (a runestone feeding back into itself), they will never settle.
    /// </summary>
    public class LogicGate : PuzzleActivator
    {
        [Header("Runestone")]
        [Tooltip("AND = all inputs must be on. OR = any one input is enough. NOT = on when its input is off.")]
        public GateType gateType = GateType.And;

        [Tooltip("Drag levers, plates, switches or other runestones in here. Then drag this runestone into a door's Inputs list.")]
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
