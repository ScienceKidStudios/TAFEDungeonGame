using UnityEngine;

namespace PuzzleMechanics
{
    /// <summary>
    /// Copies one input, but with a delay.
    ///
    /// - Activate Delay: how long the input must stay ON before this turns ON.
    /// - Deactivate Delay: how long this stays ON after the input turns OFF.
    ///
    /// Examples:
    ///   Door that opens 2 seconds after a button:      Activate Delay = 2, Deactivate Delay = 0
    ///   Plate that keeps a door open 5 seconds longer: Activate Delay = 0, Deactivate Delay = 5
    /// </summary>
    public class TimerDelay : PuzzleActivator
    {
        [Header("Timer")]
        [Tooltip("The input to follow.")]
        public PuzzleActivator input;

        [Tooltip("Seconds the input must stay ON before this turns ON.")]
        [Min(0f)] public float activateDelay = 1f;

        [Tooltip("Seconds this stays ON after the input turns OFF.")]
        [Min(0f)] public float deactivateDelay = 0f;

        private float timer;

        /// <summary>0 to 1 progress towards the next change. Handy for UI or effects.</summary>
        public float Progress { get; private set; }

        private void Update()
        {
            bool wanted = input != null && input.IsActive;

            // Already matching the input: nothing to count down.
            if (wanted == IsActive)
            {
                timer = 0f;
                Progress = 0f;
                return;
            }

            float delay = wanted ? activateDelay : deactivateDelay;
            timer += Time.deltaTime;
            Progress = delay > 0f ? Mathf.Clamp01(timer / delay) : 1f;

            if (timer >= delay)
            {
                timer = 0f;
                SetActiveState(wanted);
            }
        }

        private void OnDrawGizmos()
        {
            PuzzleLogic.DrawConnectionGizmo(input, transform.position);
        }
    }
}
