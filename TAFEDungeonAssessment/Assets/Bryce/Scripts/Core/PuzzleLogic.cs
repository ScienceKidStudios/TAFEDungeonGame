using System.Collections.Generic;
using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>How an output combines several inputs.</summary>
    public enum InputMode
    {
        [Tooltip("Every input must be on (AND).")]
        All,
        [Tooltip("At least one input must be on (OR).")]
        Any
    }

    /// <summary>
    /// Small shared helpers used by outputs and logic gates.
    /// </summary>
    public static class PuzzleLogic
    {
        /// <summary>
        /// Returns true if the inputs are satisfied for the given mode.
        /// Empty slots in the list are ignored. With no inputs at all this returns false.
        /// </summary>
        public static bool Evaluate(List<PuzzleActivator> inputs, InputMode mode)
        {
            int count = 0;
            int activeCount = 0;

            foreach (PuzzleActivator input in inputs)
            {
                if (input == null)
                    continue;

                count++;
                if (input.IsActive)
                    activeCount++;
            }

            if (count == 0)
                return false;

            return mode == InputMode.All ? activeCount == count : activeCount > 0;
        }

        /// <summary>True if the list contains at least one assigned input.</summary>
        public static bool HasAnyInput(List<PuzzleActivator> inputs)
        {
            foreach (PuzzleActivator input in inputs)
            {
                if (input != null)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Scene view helper: draws a line from each input to 'target'.
        /// Green = input is on, red = input is off (always red outside Play mode).
        /// </summary>
        public static void DrawConnectionGizmos(List<PuzzleActivator> inputs, Vector3 target)
        {
            foreach (PuzzleActivator input in inputs)
                DrawConnectionGizmo(input, target);
        }

        public static void DrawConnectionGizmo(PuzzleActivator input, Vector3 target)
        {
            if (input == null)
                return;

            Vector3 from = input.transform.position;
            Gizmos.color = input.IsActive ? Color.green : new Color(1f, 0.3f, 0.2f);
            Gizmos.DrawLine(from, target);

            // A small sphere at the receiving end shows which way the signal flows.
            Gizmos.DrawSphere(Vector3.Lerp(from, target, 0.9f), 0.08f);
        }
    }
}
